using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Point lights of a dark 2.5D dungeon (View25DProfile.Dungeon; first: Rootburrow, 21/11/2026, ChatGPT "Enchanted Root Catacomb" section A2,
    /// docs/reference/rootburrow-dark-25d/rootburrow-dark-dungeon-art-direction-chatgpt.md). Instead of the sun, everything is lit by
    /// ambient + the nearest lights:
    ///  - lights come from the map's props by NAME (rbRuneOrb = amber lantern, rbRuneObelisk = cyan crystal, rbPurpleCrystal / rbGlowShrooms = glow
    ///    mushrooms, rbPinkCrystal = amber, rbRuneSeal = violet rune) plus a few placed by hand per map (<see cref="Extra"/>: the boss hall);
    ///  - every frame the 16 lights nearest the camera focus go to the shader globals _DL25DPos (x, y, radius, intensity) / _DL25DCol / _DL25DCount and
    ///    _DL25DAmb (ambient rgb x level, a = 1 when on): Sprite25DLit multiplies ground and props by ambient + sum of the lights (smooth falloff);
    ///  - heroes / monsters / pets get the same sum at their feet on the CPU (<see cref="UnitLight"/>) through View25DLighting's _Color tint;
    ///  - each light lays a soft additive pool on the floor (LightBlob, 0.8 x its radius, centre alpha ~0.15), flickering with it;
    ///  - the local hero carries a soft warm light (<see cref="HeroGlow"/>, slot 0) so the own character reads in the dark cave.
    /// Off (globals zeroed) on every other map.
    /// </summary>
    public static class DungeonLights
    {
        public const int MaxLights = 16;
        struct Source { public Vector2 Pos; public float Radius, Intensity, Flicker, Period, Phase; public Color Color; public MeshRenderer Pool; }
        struct Kind { public Color Color; public float RadiusTiles, Intensity, Flicker, Period, LiftTiles; public Kind(int hex, float r, float i, float f, float p, float lift = 0.6f) { Color = Hex(hex); RadiusTiles = r; Intensity = i; Flicker = f; Period = p; LiftTiles = lift; } }

        // ChatGPT V2 "Dark Violet Cave" (user 21/11: purple like the RO3 Irmo clip at 12:25; radius in m = tiles, intensity on its 0-1 scale, flicker share, period s)
        static readonly Kind RuneOrb = new Kind(0x65DBFF, 4.0f, 0.95f, 0.05f, 2.5f, 1.2f), PurpleCrystal = new Kind(0xA277F0, 3.2f, 0.75f, 0.04f, 3.0f, 0.8f),
            PinkCrystal = new Kind(0xED91D6, 2.8f, 0.65f, 0.05f, 2.8f), Obelisk = new Kind(0x56D6F5, 3.0f, 0.70f, 0.03f, 4.0f, 0.9f),
            Shrooms = new Kind(0xA9E9DF, 1.6f, 0.32f, 0.07f, 3.2f, 0.2f), Nest = new Kind(0xA277F0, 2.0f, 0.40f, 0.05f, 3.4f, 0.3f),
            Seal = new Kind(0x72E7FF, 3.0f, 0.55f, 0.08f, 2.0f, 0.2f), BossSeal = new Kind(0x72E7FF, 4.5f, 1.05f, 0.08f, 2.0f), BossOrb = new Kind(0x65DBFF, 3.2f, 0.7f, 0.05f, 2.5f);
        /// <summary>Soft light carried with the local hero (user 21/11: "giống ở trong hang", the cave stays dark but the own hero must read):
        /// always one of the 16 lights, no floor pool, no flicker.</summary>
        static readonly Kind HeroGlow = new Kind(0xD8E2FF, 3.4f, 0.40f, 0f, 3f);

        static bool TryKind(string type, out Kind k)
        {
            switch (type)
            {
                case "rbRuneOrb": k = RuneOrb; return true;
                case "rbRuneObelisk": k = Obelisk; return true;
                case "rbPurpleCrystal": k = PurpleCrystal; return true;
                case "rbPinkCrystal": k = PinkCrystal; return true;
                case "rbGlowShrooms": k = Shrooms; return true;
                case "rbCrystalNest": k = Nest; return true;
                case "rbRuneSeal": k = Seal; return true;
                default: k = default(Kind); return false;
            }
        }

        /// <summary>Hand-placed lights per map: (tile x, tile y (y down, centre of the tile), kind). Rootburrow: the boss hall (Ambercap Warden at 19,4).</summary>
        static IEnumerable<KeyValuePair<Vector2, Kind>> Extra(string mapId)
        {
            if (mapId != RootburrowMap.MapId) yield break;
            yield return new KeyValuePair<Vector2, Kind>(new Vector2(19.5f, 5.0f), BossSeal);
            yield return new KeyValuePair<Vector2, Kind>(new Vector2(15.0f, 5.5f), BossOrb);
            yield return new KeyValuePair<Vector2, Kind>(new Vector2(23.5f, 5.5f), BossOrb);
        }

        static readonly List<Source> Sources = new List<Source>();
        struct FlashLight { public Vector2 Pos; public Color Color; public float Radius, Peak, Ms, Age; }
        static readonly List<FlashLight> Flashes = new List<FlashLight>(4);

        /// <summary>A short burst of light (a boss skill hitting: the strike lights up the dark cave, user 21/11 "vẫn phải có độ hoành tráng"):
        /// full at once, eased out over `ms`. Takes a slot right after the hero's light while it lasts (at most 4 at a time).</summary>
        public static void Flash(Vector2 pos, Color color, float radiusUnits, float intensity, float ms)
        {
            if (!On) return;
            if (Flashes.Count >= 4) Flashes.RemoveAt(0);
            Flashes.Add(new FlashLight { Pos = pos, Color = color, Radius = radiusUnits, Peak = intensity, Ms = Mathf.Max(50f, ms) });
        }
        static readonly Vector4[] Pos = new Vector4[MaxLights], Col = new Vector4[MaxLights];
        static readonly int PosId = Shader.PropertyToID("_DL25DPos"), ColId = Shader.PropertyToID("_DL25DCol"), CountId = Shader.PropertyToID("_DL25DCount"),
            AmbId = Shader.PropertyToID("_DL25DAmb");
        static readonly List<int> Order = new List<int>(64);
        static readonly List<float> Dist = new List<float>(64);
        static readonly int[] Picked = new int[MaxLights];
        static int pickedCount;
        static Material poolMat;
        static MaterialPropertyBlock block;
        static bool globalsSet;

        /// <summary>The map on screen is a dark dungeon with the RO3 pass.</summary>
        public static bool On { get { return View25D.Ro3Look && View25D.Profile.Dungeon; } }

        /// <summary>Collects the lights of the map being built (after MapScenery) and lays their floor pools.</summary>
        public static void Build(Transform parent, IGameMap map)
        {
            Sources.Clear();
            if (!On) return;
            float t = GameConfig.TileUnits;
            foreach (MapProp p in map.Props)
            {
                Kind k; PropDef d;
                if (!TryKind(p.Type, out k) || !VillagePropDefs.TryGet(p.Type, out d)) continue;
                float x = (p.Tx + d.FootW * 0.5f) * t, y = GameConfig.MapH - (p.Ty + 1) * t + k.LiftTiles * t; // a little north of the foot: the glow sits on the lit body
                Add(parent, new Vector2(x, y), k, p.Tx * 7 + p.Ty * 13);
            }
            int n = 0;
            foreach (KeyValuePair<Vector2, Kind> e in Extra(map.Id))
                Add(parent, new Vector2(e.Key.x * t, GameConfig.MapH - e.Key.y * t), e.Value, 101 + n++);
        }

        static void Add(Transform parent, Vector2 pos, Kind k, int seed)
        {
            var s = new Source { Pos = pos, Radius = k.RadiusTiles * GameConfig.TileUnits, Intensity = k.Intensity, Flicker = k.Flicker, Color = k.Color,
                Period = k.Period * (0.85f + (seed * 0.137f) % 0.3f), Phase = (seed * 0.61f) % 6.28f };
            s.Pool = Pool(parent, pos, s.Radius * 0.8f, k.Color, k.Intensity);
            Sources.Add(s);
        }

        /// <summary>Soft additive light pool on the floor (ChatGPT: 0.75-0.9 x the light radius, centre alpha 0.12-0.22).</summary>
        static MeshRenderer Pool(Transform parent, Vector2 pos, float radius, Color c, float intensity)
        {
            if (!LightSkillAssets.Ready) return null;
            if (poolMat == null)
            {
                poolMat = new Material(LightSkillAssets.Glow) { name = "Dungeon_LightPool", hideFlags = HideFlags.DontSave };
                poolMat.SetFloat("_Falloff", 4.5f);
                poolMat.SetFloat("_CoreWhite", 0f);
            }
            MeshRenderer r = LightSkillAssets.Quad(parent, "LightPool", poolMat, false, GameConfig.GroundOrder + 6); // over the ground and flat decor, under shadows (+8)
            r.transform.position = new Vector3(pos.x, pos.y, 0f);
            r.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            if (block == null) block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", new Color(c.r, c.g, c.b, 1f) * (0.32f * intensity));
            block.SetColor("_BodyColor", new Color(c.r, c.g, c.b, 0.10f * intensity));
            r.SetPropertyBlock(block);
            return r;
        }

        static float Level(Source s, float time)
        {
            return s.Intensity * (1f + s.Flicker * Mathf.Sin(time * 6.2832f / s.Period + s.Phase));
        }

        /// <summary>Per frame (View25DLighting.TickGlobals): the 16 lights nearest the camera focus to the shader, pools flicker.</summary>
        public static void Tick(Camera cam)
        {
            if (!On || cam == null) { Clear(); return; }
            View25DProfile p = View25D.Profile;
            Vector3 c = cam.transform.position, f = cam.transform.forward;
            Vector2 focus = f.z > 0.01f ? (Vector2)(c + f * (-c.z / f.z)) : (Vector2)c;
            Order.Clear(); Dist.Clear();
            for (int i = 0; i < Sources.Count; i++) { Order.Add(i); Dist.Add(((Sources[i].Pos - focus).magnitude - Sources[i].Radius)); }
            Order.Sort((a, b) => Dist[a].CompareTo(Dist[b]));
            float time = Time.time;
            HeroController me = LocalPlayer.Hero;
            int first = me != null && !me.IsDead ? 1 : 0; // slot 0: the hero's own soft light
            if (first == 1)
            {
                Pos[0] = new Vector4(me.Position.x, me.Position.y + 4f, HeroGlow.RadiusTiles * GameConfig.TileUnits, HeroGlow.Intensity);
                Col[0] = new Vector4(HeroGlow.Color.r, HeroGlow.Color.g, HeroGlow.Color.b, 1f);
            }
            for (int i = Flashes.Count - 1; i >= 0; i--)
            {
                FlashLight fl = Flashes[i];
                fl.Age += Time.deltaTime * 1000f;
                if (fl.Age >= fl.Ms) { Flashes.RemoveAt(i); continue; }
                Flashes[i] = fl;
            }
            for (int i = 0; i < Flashes.Count && first < MaxLights - 4; i++, first++)
            {
                FlashLight fl = Flashes[i];
                float k = 1f - fl.Age / fl.Ms;
                Pos[first] = new Vector4(fl.Pos.x, fl.Pos.y, fl.Radius, fl.Peak * k * k);
                Col[first] = new Vector4(fl.Color.r, fl.Color.g, fl.Color.b, 1f);
            }
            pickedCount = Mathf.Min(MaxLights, Order.Count + first);
            for (int i = first; i < MaxLights; i++)
            {
                if (i < pickedCount)
                {
                    Source s = Sources[Order[i - first]];
                    Picked[i] = Order[i - first];
                    Pos[i] = new Vector4(s.Pos.x, s.Pos.y, s.Radius, Level(s, time));
                    Col[i] = new Vector4(s.Color.r, s.Color.g, s.Color.b, 1f);
                }
                else { Pos[i] = new Vector4(0f, 0f, 1f, 0f); Col[i] = Vector4.zero; }
            }
            Shader.SetGlobalVectorArray(PosId, Pos);
            Shader.SetGlobalVectorArray(ColId, Col);
            Shader.SetGlobalFloat(CountId, pickedCount);
            Color a = p.AmbientColor * p.AmbientLevel / Mathf.Max(0.01f, Luma(p.AmbientColor)); // the level is the ambient's brightness (luma), the colour its hue
            Shader.SetGlobalVector(AmbId, new Vector4(a.r, a.g, a.b, 1f));
            globalsSet = true;
            // pools breathe with their light (only the visible-ish ones matter; all are cheap)
            if (block == null) block = new MaterialPropertyBlock();
            for (int i = 0; i < Sources.Count; i++)
            {
                Source s = Sources[i];
                if (s.Pool == null) continue;
                float k = Level(s, time) / Mathf.Max(0.01f, s.Intensity);
                s.Pool.GetPropertyBlock(block);
                block.SetFloat("_Fade", Mathf.Clamp01(0.85f * k));
                s.Pool.SetPropertyBlock(block);
            }
        }

        /// <summary>Light at a ground point for a unit (hero / monster / pet): unit ambient (boss: boss ambient) + the lights, as a multiplier.</summary>
        public static Color UnitLight(Vector2 feet, bool boss)
        {
            View25DProfile p = View25D.Profile;
            float level = boss ? p.BossAmbient : p.UnitAmbient;
            Color sum = p.AmbientColor * level / Mathf.Max(0.01f, Luma(p.AmbientColor));
            sum = Color.Lerp(new Color(level, level, level), sum, 0.55f); // units keep their own colours better than the floor (RO Soft Fantasy art must stay readable)
            for (int i = 0; i < pickedCount; i++)
            {
                Vector4 l = Pos[i];
                float d = (new Vector2(l.x, l.y) - feet).magnitude / Mathf.Max(1f, l.z);
                float fall = Falloff(d) * l.w;
                sum += new Color(Col[i].x, Col[i].y, Col[i].z) * fall;
            }
            float m = Mathf.Max(sum.r, Mathf.Max(sum.g, sum.b));
            if (m > 1.1f) sum *= 1.1f / m; // never brighter than painted + 10 %
            sum.a = 1f;
            return sum;
        }

        /// <summary>Same curve as Sprite25DLit: smooth to 0 at the radius, brighter core.</summary>
        static float Falloff(float d)
        {
            float x = Mathf.Clamp01(1f - d);
            return x * x * (0.6f + 0.4f * x);
        }

        public static void Clear()
        {
            if (!globalsSet) return;
            Shader.SetGlobalVector(AmbId, Vector4.zero);
            Shader.SetGlobalFloat(CountId, 0f);
            globalsSet = false;
        }

        static float Luma(Color c) { return c.r * 0.299f + c.g * 0.587f + c.b * 0.114f; }
        static Color Hex(int h) { return new Color(((h >> 16) & 255) / 255f, ((h >> 8) & 255) / 255f, (h & 255) / 255f, 1f); }
    }
}
