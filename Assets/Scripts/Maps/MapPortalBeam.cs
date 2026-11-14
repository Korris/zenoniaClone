using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Map exit marker, option "B" the user picked from animated mock-ups (13/11/2026, Tools/mock-portal-options-gif.py): a soft column of cool white light
    /// rising from a bright pool on the ground, with motes drifting up through it. Everything is procedural (same shapes and timing as the mock-up) and
    /// moves continuously, nothing blinks. Portal tiles with the same target that touch are one exit; its beam stands on their middle, moved one tile
    /// in from the map edge. Sizes are in world units (16 per tile; the mock-up's 64 px tile = 4 px per unit).
    /// </summary>
    public static class MapPortalBeam
    {
        static readonly Color Light = new Color(0.72f, 0.88f, 1f);   // a touch bluer than the mock-up so it still reads on snow
        const float ColumnW = 30f, ColumnH = 55f;          // mock-up 120 x 220 px
        const float PoolW = 37.5f, PoolH = 15f;            // mock-up 150 x 60 px
        const float ColumnAlpha = 0.85f;
        const int Motes = 14;
        const float MoteSize = 4.5f, MoteRise = 47.5f, MoteSpread = 22.5f, MotePeriod = 2f;
        static Sprite column, pool, mote;

        /// <summary>Beams for every exit (MapPortalHaze uses BuildBeam only for exits away from the map edge).</summary>
        public static void Build(Transform parent, IGameMap map)
        {
            var done = new HashSet<MapPortal>();
            foreach (MapPortal p in map.Portals)
            {
                if (done.Contains(p)) continue;
                List<MapPortal> run = Run(map, p);
                float cx = 0f, cy = 0f;
                foreach (MapPortal q in run) { done.Add(q); cx += q.Tx + 0.5f; cy += q.Ty + 0.5f; }
                cx /= run.Count; cy /= run.Count;   // tiles, y down
                if (cx < 1f) cx += 1f; else if (cx > map.Width - 1) cx -= 1f;
                if (cy < 1f) cy += 1f; else if (cy > map.Height - 1) cy -= 1f;
                Vector3 feet = new Vector3(cx * GameConfig.TileUnits, GameConfig.MapH - cy * GameConfig.TileUnits, 0f);
                BuildBeam(parent, feet, p.TargetMapId);
            }
        }

        internal static void BuildBeam(Transform parent, Vector3 feet, string target)
        {
            var go = new GameObject("PortalBeam_" + target);
            go.transform.SetParent(parent, false);
            go.transform.position = feet;
            SpriteRenderer poolSr = Child(go.transform, "Pool", Pool(), new Vector3(0f, -2f, 0f), new Vector3(PoolW, PoolH, 1f), GameConfig.GroundOrder + 2);
            poolSr.color = Light;
            int order = GameConfig.OrderBase(feet.y);   // y-sorted like a prop: a hero in front covers it, one behind is covered
            SpriteRenderer col = Child(go.transform, "Column", Column(), new Vector3(0f, ColumnH * 0.5f - 2.5f, 0f), new Vector3(ColumnW, ColumnH, 1f), order);
            col.color = new Color(Light.r, Light.g, Light.b, ColumnAlpha);
            var anim = go.AddComponent<Motion>();
            anim.Motes = new Transform[Motes]; anim.MoteRenderers = new SpriteRenderer[Motes];
            anim.Phase = new float[Motes]; anim.OffsetX = new float[Motes];
            var rng = new System.Random(target.GetHashCode());
            for (int i = 0; i < Motes; i++)
            {
                SpriteRenderer m = Child(go.transform, "Mote", Mote(), Vector3.zero, new Vector3(MoteSize, MoteSize, 1f), order + 1);
                anim.Motes[i] = m.transform; anim.MoteRenderers[i] = m;
                anim.Phase[i] = (float)rng.NextDouble();
                anim.OffsetX[i] = ((float)rng.NextDouble() - 0.5f) * MoteSpread;
            }
        }

        static SpriteRenderer Child(Transform parent, string name, Sprite s, Vector3 pos, Vector3 scale, int order)
        {
            SpriteRenderer sr = ProceduralSprites.Child(parent, name, s);
            sr.transform.localPosition = pos;
            sr.transform.localScale = scale;
            sr.sortingOrder = order;
            return sr;
        }

        internal static List<MapPortal> Run(IGameMap map, MapPortal p)
        {
            var run = new List<MapPortal> { p };
            for (int i = 0; i < run.Count; i++)
                foreach (MapPortal q in map.Portals)
                    if (q.TargetMapId == p.TargetMapId && !run.Contains(q) && Mathf.Abs(q.Tx - run[i].Tx) + Mathf.Abs(q.Ty - run[i].Ty) == 1) run.Add(q);
            return run;
        }

        // Square textures -> 1 x 1 world-unit sprites before scaling (a non-square one would come out half as wide), white, shaped by alpha so the renderer colour tints them.

        /// <summary>Column: brightest along the middle (falloff^1.5 sideways), fading out towards the top (bottom = ground).</summary>
        static Sprite Column()
        {
            return column ?? (column = Make(64, 64, (u, v) => Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(u - 0.5f) * 2f), 1.5f) * Mathf.Pow(Mathf.Clamp01(1f - v), 1.2f)));
        }

        /// <summary>Pool: elliptical spot, hard bright core fading to its rim.</summary>
        static Sprite Pool()
        {
            return pool ?? (pool = Make(64, 64, (u, v) =>
            {
                float d = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) * 4f + (v - 0.5f) * (v - 0.5f) * 4f);
                return Mathf.Min(Mathf.Clamp01(1f - d) * 1.6f, 1f);
            }));
        }

        /// <summary>Mote: small round glow, squared falloff.</summary>
        internal static Sprite Mote()
        {
            return mote ?? (mote = Make(16, 16, (u, v) =>
            {
                float d = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2f;
                float a = Mathf.Clamp01(1f - d); return a * a;
            }));
        }

        internal static Sprite Make(int w, int h, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(255f * alpha((x + 0.5f) / w, (y + 0.5f) / h)));
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Mathf.Max(w, h), 0, SpriteMeshType.FullRect);
        }

        /// <summary>Motes rise through the column and fade in and out (sin of their phase); the column and pool stay steady.</summary>
        sealed class Motion : MonoBehaviour
        {
            public Transform[] Motes;
            public SpriteRenderer[] MoteRenderers;
            public float[] Phase, OffsetX;
            void Update()
            {
                float t = Time.time / MotePeriod;
                for (int i = 0; i < Motes.Length; i++)
                {
                    float ph = (t + Phase[i]) % 1f;
                    Motes[i].localPosition = new Vector3(OffsetX[i] + Mathf.Sin((ph + i) * 6f) * 1.5f, 1.25f + ph * MoteRise, 0f);
                    Color c = MoteRenderers[i].color; c.a = Mathf.Sin(ph * Mathf.PI); MoteRenderers[i].color = c;
                }
            }
        }
    }
}
