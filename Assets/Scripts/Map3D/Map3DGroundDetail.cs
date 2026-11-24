using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Ground detail of the Taurus 3D maps from ChatGPT's RO3 look loop round 1 (24/11/2026, docs/reference/ro3-compare/rounds/r1/round-1-chatgpt.txt):
    /// - a TONE texture (R8, 4 texels per metre over the same rect as the splat control) that darkens the terrain shader's colour:
    ///   contact AO at the foot of trees, buildings, lamp posts, the well, benches, rocks ("dirt decal rất nhẹ ở chân cây, chân giếng, chân cột đèn",
    ///   "contact AO sát gốc"), at the foot of the cliffs ("shadow/AO tại chân vách"), and the Brambleback Nest ground 8-12 % darker;
    /// - faint compressed-dirt patches written into the control's dirt channel: under trees, around the camps, 2-3 around the nest's bramble heaps
    ///   ("vùng đất nén rất nhạt dưới cây và quanh camp", "2-3 mảng đất nén quanh các cụm gai").
    /// Visual only, data from the map's own prop table / spawn rects; other 3D maps get a neutral tone texture.
    /// </summary>
    public static class Map3DGroundDetail
    {
        public const int TonePx = 4; // texels per metre

        /// <summary>The map gets the round-1 ground detail (Taurus village and meadows).</summary>
        public static bool AppliesTo(IGameMap map) { return map.Id == TauWillowbrookVillageMap.MapId || map.Id == TauCloverMeadowsMap.MapId; }

        /// <summary>Extra dirt-channel weight (0..1) at a tile point: faint packed earth under trees, around camps and at the nest.</summary>
        public static float[] DirtPatches(IGameMap map, Map3DHeightField f, int w, int h, int px)
        {
            var add = new float[w * h];
            if (!AppliesTo(map)) return add;
            foreach (MapProp p in map.Props)
            {
                string t = p.Type.ToLowerInvariant();
                Vector2 c = Centre(p);
                if (t.Contains("tree") || t.Contains("oak")) Blob(add, w, h, px, f, c, 1.3f, 0.22f, 0.6f);
                else if (map.Id == TauCloverMeadowsMap.MapId && t.Contains("bramble") && c.x < 27f && c.y < 13f) Blob(add, w, h, px, f, c + new Vector2(0.6f, 0.9f), 1.8f, 0.45f, 0.8f);
            }
            if (map.Id == TauCloverMeadowsMap.MapId)
                foreach (FrostHollowMap.Zone z in MapSpawnTable.ZonesFor(map.Id) ?? new FrostHollowMap.Zone[0])
                {
                    Vector2 c = new Vector2(z.Tx + z.W * 0.5f, z.Ty + z.H * 0.5f);
                    Blob(add, w, h, px, f, c, Mathf.Min(z.W, z.H) * 0.45f, 0.20f, 1f); // a worn heart, the edges stay grass for the telegraphs (round 2: +dirt where paths meet camps)
                }
            if (map.Id == TauCloverMeadowsMap.MapId)
            {
                // round 2: C01 gets 2-3 larger faint packed-earth patches in the main view; the nest 2-3 trampled patches round the boss tile (8, 5)
                Blob(add, w, h, px, f, new Vector2(40.5f, 47f), 2.6f, 0.24f, 0.5f);
                Blob(add, w, h, px, f, new Vector2(46f, 52.5f), 2.2f, 0.22f, 0.5f);
                Blob(add, w, h, px, f, new Vector2(35f, 51f), 1.9f, 0.2f, 0.5f);
                Blob(add, w, h, px, f, new Vector2(6.5f, 6.5f), 1.8f, 0.42f, 0.6f);
                Blob(add, w, h, px, f, new Vector2(10.5f, 4f), 1.6f, 0.38f, 0.6f);
                Blob(add, w, h, px, f, new Vector2(12f, 8f), 1.5f, 0.36f, 0.6f);
            }
            return add;
        }

        /// <summary>Tone texture (darkness 0..1, linear R8) over the terrain rect.</summary>
        public static Texture2D Tone(IGameMap map, Map3DHeightField f)
        {
            int w = Mathf.RoundToInt((f.MapW + 2f * Map3DHeightField.Apron) * TonePx), h = Mathf.RoundToInt((f.MapH + 2f * Map3DHeightField.Apron) * TonePx);
            var dark = new float[w * h];
            if (AppliesTo(map))
            {
                foreach (MapProp p in map.Props) PropAo(dark, w, h, f, p);
                CliffFootAo(dark, w, h, f);
                if (map.Id == TauCloverMeadowsMap.MapId) NestTone(dark, w, h, f);
            }
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    byte v = (byte)(Mathf.Clamp01(dark[y * w + x]) * 255f);
                    px[(h - 1 - y) * w + x] = new Color32(v, v, v, 255); // row 0 = south, like the control
                }
            var tex = new Texture2D(w, h, TextureFormat.R8, true, true) { name = "Map3DTone", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        static Vector2 Centre(MapProp p)
        {
            PropDef d;
            float fw = VillagePropDefs.TryGet(p.Type, out d) ? Mathf.Max(1, d.FootW) : 1f;
            return new Vector2(p.Tx + fw * 0.5f, p.Ty + 0.5f);
        }

        /// <summary>Contact darkness under a prop by kind (radius m, strength): trees 1.5 / 0.26, buildings 2.6 / 0.24, small posts 0.8 / 0.22.</summary>
        static void PropAo(float[] dark, int w, int h, Map3DHeightField f, MapProp p)
        {
            string t = p.Type.ToLowerInvariant();
            Vector2 c = Centre(p);
            PropDef d;
            bool has = VillagePropDefs.TryGet(p.Type, out d);
            if (t.Contains("tree") || t.Contains("oak")) { Blob(dark, w, h, TonePx, f, c, 1.5f, 0.28f, 0.5f); return; }
            if (t.Contains("house") || t.Contains("hall") || t.Contains("tower") || t.Contains("tent") || t.Contains("stall") || t.Contains("forge") || t.Contains("windmill") || t.Contains("gate"))
            {
                float r = has ? Mathf.Max(1.6f, d.FootW * 0.55f) : 2.6f;
                Blob(dark, w, h, TonePx, f, c, r, 0.29f, 0.45f); // round 2: wall-foot AO +12 %; round 3: +7 %
                return;
            }
            if (t.Contains("wall") || t.Contains("fence")) { Blob(dark, w, h, TonePx, f, c, has ? Mathf.Max(0.8f, d.FootW * 0.5f) : 1f, 0.16f, 0.4f); return; }
            if (t.Contains("lamp") || t.Contains("well") || t.Contains("bench") || t.Contains("barrel") || t.Contains("sign") || t.Contains("planter") || t.Contains("crate")
                || t.Contains("boulder") || t.Contains("rock") || t.Contains("column") || t.Contains("post") || t.Contains("statue") || t.Contains("stone"))
                Blob(dark, w, h, TonePx, f, c, has ? Mathf.Clamp(d.W * 0.45f, 0.7f, 1.6f) : 0.8f, 0.25f, 0.4f); // round 2: column / bench foot AO +12 %
        }

        /// <summary>Walkable ground at the foot of a rise darkens with the height of the rise within 1.5 m (cliff / bank foot AO).</summary>
        static void CliffFootAo(float[] dark, int w, int h, Map3DHeightField f)
        {
            const float R = 1.5f;
            for (int y = 0; y < h; y += 1)
                for (int x = 0; x < w; x++)
                {
                    float tx = f.MinTx + (x + 0.5f) / TonePx, ty = f.MinTy + (y + 0.5f) / TonePx;
                    float here = f.HeightAt(tx, ty);
                    if (here > 0.4f) continue;
                    float rise = Mathf.Max(Mathf.Max(f.HeightAt(tx + R, ty), f.HeightAt(tx - R, ty)), Mathf.Max(f.HeightAt(tx, ty + R), f.HeightAt(tx, ty - R))) - here;
                    float a = Mathf.Clamp01((rise - 0.35f) / 1.6f) * 0.24f;
                    int k = y * w + x;
                    if (a > dark[k]) dark[k] = a;
                }
        }

        /// <summary>Brambleback Nest (north-west clearing, x &lt; 26, y &lt; 12): ground ~10 % darker, soft 3 m edge.</summary>
        static void NestTone(float[] dark, int w, int h, Map3DHeightField f)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float tx = f.MinTx + (x + 0.5f) / TonePx, ty = f.MinTy + (y + 0.5f) / TonePx;
                    float inX = 1f - Mathf.Clamp01((tx - 23f) / 3f), inY = 1f - Mathf.Clamp01((ty - 10f) / 3f);
                    float a = inX * inY * 0.14f; // round 2: 3-5 % darker again
                    int k = y * w + x;
                    dark[k] = Mathf.Max(dark[k], dark[k] * 0.5f + a); // adds to the contact AO without doubling it
                }
        }

        /// <summary>Soft radial blob (strength `s`, radius `r` m, inner `core` share at full strength) taken as max into a grid of `px` texels per metre.</summary>
        static void Blob(float[] g, int w, int h, int px, Map3DHeightField f, Vector2 c, float r, float s, float core)
        {
            int x0 = Mathf.FloorToInt((c.x - r - f.MinTx) * px), x1 = Mathf.CeilToInt((c.x + r - f.MinTx) * px);
            int y0 = Mathf.FloorToInt((c.y - r - f.MinTy) * px), y1 = Mathf.CeilToInt((c.y + r - f.MinTy) * px);
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(h - 1, y1); y++)
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(w - 1, x1); x++)
                {
                    float tx = f.MinTx + (x + 0.5f) / px, ty = f.MinTy + (y + 0.5f) / px;
                    float d = new Vector2(tx - c.x, (ty - c.y) * 1.15f).magnitude / r;   // a little wider than deep
                    if (d >= 1f) continue;
                    float n = Mathf.PerlinNoise(tx * 1.7f + 13f, ty * 1.7f + 7f);         // ragged edge
                    float a = s * (1f - Mathf.Clamp01((d - core) / (1f - core))) * (0.75f + 0.5f * n);
                    int k = y * w + x;
                    if (a > g[k]) g[k] = a;
                }
        }
    }
}
