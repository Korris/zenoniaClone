using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Static prop shadows of a map, made from its prop placement (IGameMap.Props + VillagePropDefs) by rules keyed on the prop type and laid out
    /// along FakeSun (spec 3): tree = small root ellipse + soft crown shadow of 4 merged blobs shifted down-right (variant by position hash);
    /// house / tent / stall = wall-foot band + short cast; rock, boulder, pillar = contact (+ short cast when tall); low walk-through decor = light contact;
    /// fence = thin band. Baked once per map (and again when FakeSun.Version moves) into a cache texture, 1 px per world unit, red channel = shadow
    /// alpha, overlaps combined by max (no stacking). Cliffs painted into the ground get nothing yet. Not hand-tuned per map: every map uses the same rules.
    /// </summary>
    public static class StaticPropShadows
    {
        enum Shape { None, Tree, House, Rock, Decor, Fence }

        const float Ppu = 1f;            // cache texels per world unit (shadows are soft: 1 is plenty)
        const float FlatDecorMaxTiles = 3f; // same rule as MapScenery: walk-through and this low = ground decoration

        static float[] buf;
        static int texW, texH;

        /// <summary>Bakes the shadows of `map`'s props into a new R8 texture covering the whole map (MapW x MapH units), or null when the map has none.</summary>
        public static Texture2D Bake(IGameMap map)
        {
            texW = Mathf.Clamp(Mathf.CeilToInt(GameConfig.MapW * Ppu), 1, 2048);
            texH = Mathf.Clamp(Mathf.CeilToInt(GameConfig.MapH * Ppu), 1, 2048);
            buf = new float[texW * texH];
            int stamped = 0;
            foreach (MapProp p in map.Props)
            {
                PropDef d;
                if (!VillagePropDefs.TryGet(p.Type, out d)) continue;
                Shape s = Classify(p.Type, d);
                if (s == Shape.None) continue;
                Vector2 basePt = new Vector2((p.Tx + d.FootW * 0.5f) * GameConfig.TileUnits, GameConfig.MapH - (p.Ty + 1) * GameConfig.TileUnits); // MapScenery's sprite pivot
                StampProp(s, basePt, d.W * GameConfig.TileUnits, d.H * GameConfig.TileUnits, Hash(p.Tx, p.Ty));
                stamped++;
            }
            if (stamped == 0) { buf = null; return null; }
            var px = new byte[texW * texH];
            for (int i = 0; i < px.Length; i++) px[i] = (byte)Mathf.RoundToInt(Mathf.Clamp01(buf[i]) * 255f);
            buf = null;
            var tex = new Texture2D(texW, texH, TextureFormat.R8, false) { name = "PropShadows_" + map.Id, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            tex.SetPixelData(px, 0);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>Shadow rule of a prop type, from its name and size (every map's naming: wb*, wbx*, fh*, bw*, sb*, sg*, village props).</summary>
        static Shape Classify(string type, PropDef d)
        {
            string t = type.ToLowerInvariant();
            if (Has(t, "bridge", "palisade", "flower", "lupine", "grasstuft", "fern")) return Shape.None; // flat on the ground or a long diagonal fence painted as one image
            if (Has(t, "fence")) return Shape.Fence;
            if (Has(t, "fallen", "log")) return Shape.Rock;
            if (d.FootH == 0 && d.H <= FlatDecorMaxTiles) return Shape.Decor;
            if (Has(t, "oak", "pine", "tree", "acacia", "palm", "juniper", "giantmushroom", "mangrove")) return Shape.Tree;
            if (Has(t, "house", "cottage", "lodge", "workshop", "residence", "shop", "forge", "inn", "shed", "shrine", "tent", "stall", "wagon", "barricade", "tower", "haystack")) return Shape.House;
            return Shape.Rock; // rocks, boulders, pillars, steles, statues, posts, wells, crates...
        }

        static bool Has(string t, params string[] keys)
        {
            foreach (string k in keys) if (t.Contains(k)) return true;
            return false;
        }

        static void StampProp(Shape s, Vector2 b, float w, float h, int hash)
        {
            float feather = FakeSun.Feather;
            switch (s)
            {
                case Shape.Tree:
                {
                    float crown = Mathf.Min(w * 0.85f, h * 1.1f); // crown diameter
                    float root = Mathf.Clamp(crown * 0.16f, 4f, 11f);
                    Ellipse(b + FakeSun.Along(1.5f), root, root * 0.45f, 0f, FakeSun.TreeRootAlpha, 0.25f);
                    // Crown shadow: ~70 % of the crown, centre 28 % of the crown down-right of the trunk, 4 soft blobs merged (spec 3.1).
                    Vector2 c = b + FakeSun.Along(crown * 0.28f * (FakeSun.LengthFactor / 0.3f));
                    float rot = Mathf.Atan2(FakeSun.Direction.y, FakeSun.Direction.x) * Mathf.Rad2Deg * 0.5f; // leans along the sun
                    float j1 = Jitter(hash, 0), j2 = Jitter(hash, 1), j3 = Jitter(hash, 2);
                    float a = FakeSun.TreeCrownAlpha;
                    Ellipse(c, crown * 0.30f, crown * 0.17f, rot + j1 * 15f, a, feather);
                    Ellipse(c + new Vector2(-crown * (0.17f + 0.04f * j2), crown * 0.03f), crown * 0.20f, crown * 0.13f, j2 * 25f, a, feather);
                    Ellipse(c + new Vector2(crown * (0.18f + 0.04f * j3), -crown * 0.02f), crown * 0.21f, crown * 0.12f, j3 * 25f, a, feather);
                    Ellipse(c + new Vector2(crown * 0.05f * j1, -crown * (0.08f + 0.03f * j2)), crown * 0.19f, crown * 0.11f, j1 * 30f, a, feather);
                    break;
                }
                case Shape.House:
                {
                    // Wall-foot band (0.28-0.38) + a short cast toward the lower right, 15-30 % of the height (spec 3.2). Low alphas: the art has painted shading.
                    Ellipse(b + new Vector2(0f, h * 0.03f), w * 0.48f, Mathf.Max(4f, h * 0.07f), 0f, FakeSun.HouseFootAlpha, 0.35f);
                    float len = h * 0.22f * (FakeSun.LengthFactor / 0.3f);
                    Vector2 cast = b + FakeSun.Along(len * 0.6f);
                    for (int i = -1; i <= 1; i++)
                        Ellipse(cast + new Vector2(w * 0.28f * i, 0f), w * 0.26f, h * 0.08f + len * 0.35f, 0f, FakeSun.HouseCastAlpha, feather);
                    break;
                }
                case Shape.Rock:
                {
                    float rx = w * 0.42f;
                    Ellipse(b + FakeSun.Along(1.5f), rx, Mathf.Max(3f, rx * 0.4f), 0f, FakeSun.RockContactAlpha, feather);
                    if (h >= 2f * GameConfig.TileUnits) // big rock / pillar: short cast, 15-25 % of its height
                        Ellipse(b + FakeSun.Along(h * 0.2f * (FakeSun.LengthFactor / 0.3f)), w * 0.36f, h * 0.14f, 0f, FakeSun.RockCastAlpha, feather);
                    break;
                }
                case Shape.Decor:
                {
                    float rx = w * 0.36f;
                    Ellipse(b + FakeSun.Along(1f) + new Vector2(0f, -1f), rx, Mathf.Max(2.5f, rx * 0.38f), 0f, FakeSun.DecorContactAlpha, feather);
                    break;
                }
                case Shape.Fence:
                    Ellipse(b + FakeSun.Along(3f), w * 0.46f, 3f, 0f, FakeSun.FenceAlpha, 0.4f);
                    break;
            }
        }

        /// <summary>Max-combines a feathered ellipse (radii in world units, rotation in degrees) into the cache buffer.</summary>
        static void Ellipse(Vector2 c, float rx, float ry, float rotDeg, float alpha, float feather)
        {
            if (rx <= 0.5f || ry <= 0.5f || alpha <= 0f) return;
            float r = Mathf.Max(rx, ry);
            int x0 = Mathf.Max(0, Mathf.FloorToInt((c.x - r) * Ppu)), x1 = Mathf.Min(texW - 1, Mathf.CeilToInt((c.x + r) * Ppu));
            int y0 = Mathf.Max(0, Mathf.FloorToInt((c.y - r) * Ppu)), y1 = Mathf.Min(texH - 1, Mathf.CeilToInt((c.y + r) * Ppu));
            float cs = Mathf.Cos(rotDeg * Mathf.Deg2Rad), sn = Mathf.Sin(rotDeg * Mathf.Deg2Rad);
            float f = Mathf.Max(0.05f, feather);
            for (int y = y0; y <= y1; y++)
            {
                float wy = (y + 0.5f) / Ppu - c.y;
                int row = y * texW;
                for (int x = x0; x <= x1; x++)
                {
                    float wx = (x + 0.5f) / Ppu - c.x;
                    float u = (wx * cs + wy * sn) / rx, v = (-wx * sn + wy * cs) / ry;
                    float d = Mathf.Sqrt(u * u + v * v);
                    if (d >= 1f) continue;
                    float k = Mathf.Clamp01((1f - d) / f);
                    float a = alpha * k * k * (3f - 2f * k);
                    if (a > buf[row + x]) buf[row + x] = a;
                }
            }
        }

        static int Hash(int x, int y) { unchecked { int h = x * 73856093 ^ y * 19349663; h ^= h >> 13; return h * 1274126177; } }
        /// <summary>-1..1 from the prop's hash, channel n: picks one of many crown-shadow variants by position.</summary>
        static float Jitter(int hash, int n) { return ((hash >> (n * 8)) & 0xFF) / 127.5f - 1f; }
    }
}
