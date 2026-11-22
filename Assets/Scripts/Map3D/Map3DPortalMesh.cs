using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Meshes of the 3D warp gate (Map3DPortals, 22/11/2026, user: "làm cổng dịch chuyển 3D luôn đi"). Written in METRES, y up, through Map3DMeshKit
    /// (x east, y up, z north; Build converts to game space). The gate faces north / south (ring in the x-y plane); Map3DPortals turns it for side exits.
    ///  - Arch: a broken stone ring of 13 wedge blocks (keystone bigger) open at the bottom, standing on two plinths with footing slabs and a little rubble.
    ///    Vertex colour r = AO, g = rune (front / back faces of every other block carry a glyph: uv.x = (glyph + u) / RuneGlyphs, uv.y = v), b = moss.
    ///  - Vortex: a disc filling the ring (uv = -1..1 across it), Map3DPortalGlow mode 0.
    ///  - Column: an open cylinder rising through the ring (uv.x round, uv.y 0..1 up), mode 1.
    ///  - Pool: a flat disc on the ground (uv = -1..1), mode 2 (glow + turning rune circle).
    /// </summary>
    public static class Map3DPortalMesh
    {
        public const float RingCentre = 1.62f;  // height of the ring centre (m); the hero is ~1.25 m tall
        public const float RingRadius = 1.38f;  // mean radius of the stone ring
        const float BlockHalfR = 0.19f, BlockHalfZ = 0.27f;
        public const float VortexRadius = RingRadius - BlockHalfR + 0.04f;
        public const int RuneGlyphs = 8;

        static Mesh arch, vortex, column, pool;

        static Color C(float ao, float rune, float moss) { return new Color(ao, rune, moss, 0f); }

        /// <summary>Stone arch (opaque, Map3DPortalStone).</summary>
        public static Mesh Arch()
        {
            if (arch != null) return arch;
            var kit = new Map3DMeshKit();
            var rng = new System.Random(4242);
            Vector3 centre = new Vector3(0f, RingCentre, 0f);
            const int blocks = 13;
            const float from = -38f, to = 218f; // open arc at the bottom: the ring ends on the plinths
            float step = (to - from) / blocks;
            for (int k = 0; k < blocks; k++)
            {
                float a = from + step * (k + 0.5f);
                bool key = k == blocks / 2;
                float rad = a * Mathf.Deg2Rad;
                float jitter = ((float)rng.NextDouble() - 0.5f) * 0.03f;
                Vector3 c = centre + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * (RingRadius + jitter + (key ? 0.05f : 0f));
                Quaternion q = Quaternion.AngleAxis(a, Vector3.forward) * Quaternion.AngleAxis(((float)rng.NextDouble() - 0.5f) * 3f, Vector3.up);
                float halfT = RingRadius * Mathf.Sin(step * 0.5f * Mathf.Deg2Rad) * (key ? 1.05f : 0.95f);
                Vector3 h = new Vector3(BlockHalfR * (key ? 1.35f : 1f), halfT, BlockHalfZ * (key ? 1.15f : 1f));
                float ao = Mathf.Lerp(0.8f, 1f, Mathf.Clamp01((c.y - 0.4f) / 2.2f));
                Block(kit, c, h, q, ao, key || k % 2 == 1 ? (key ? 0 : rng.Next(1, RuneGlyphs)) : -1, Mathf.Sin(rad) > 0.6f ? 0.7f : 0.25f);
            }
            // plinths under both ends of the ring, footing slabs, rubble
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * (RingRadius * Mathf.Cos(from * Mathf.Deg2Rad) + 0.02f);
                Block(kit, new Vector3(x, 0.36f, 0f), new Vector3(0.3f, 0.36f, 0.34f), Quaternion.AngleAxis(s * 2f, Vector3.up), 0.85f, -1, 0.5f);
                Block(kit, new Vector3(x + s * 0.03f, 0.75f, 0f), new Vector3(0.34f, 0.05f, 0.37f), Quaternion.identity, 1f, -1, 0.6f);
                Block(kit, new Vector3(x + s * 0.06f, 0.05f, 0.02f), new Vector3(0.46f, 0.07f, 0.48f), Quaternion.AngleAxis(s * 7f, Vector3.up), 0.72f, -1, 0.8f);
                Block(kit, new Vector3(x + s * 0.62f, 0.07f, 0.35f), new Vector3(0.13f, 0.08f, 0.11f), Quaternion.Euler(4f, 30f * s, 6f), 0.8f, -1, 0.6f);
                Block(kit, new Vector3(x + s * 0.5f, 0.05f, -0.42f), new Vector3(0.09f, 0.06f, 0.08f), Quaternion.Euler(-5f, 55f * s, 3f), 0.8f, -1, 0.6f);
            }
            return arch = kit.Build("Map3DPortalArch");
        }

        /// <summary>Box centred at c with half sizes h turned by q (y-up metres). glyph >= 0: the two faces along local z (front / back of the ring)
        /// carry that rune (vertex g = 1, uv in the glyph's cell of the strip); other faces uv 0, g 0.</summary>
        static void Block(Map3DMeshKit kit, Vector3 c, Vector3 h, Quaternion q, float ao, int glyph, float moss)
        {
            Vector3[] n = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (Vector3 f in n)
            {
                Vector3 u = Mathf.Abs(f.y) > 0.5f ? Vector3.forward : Vector3.up, r = Vector3.Cross(u, f);
                Vector3 fc = Vector3.Scale(f, h), ru = Vector3.Scale(r, h), uu = Vector3.Scale(u, h);
                Vector3[] p = { fc - ru - uu, fc + ru - uu, fc + ru + uu, fc - ru + uu };
                Vector2[] t = { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
                bool rune = glyph >= 0 && Mathf.Abs(f.z) > 0.5f;
                int b = kit.Count;
                for (int k = 0; k < 4; k++)
                {
                    // glyph inset 15 % so it never touches the block edge
                    Vector2 uv = rune ? new Vector2((glyph + 0.15f + t[k].x * 0.7f) / RuneGlyphs, 0.15f + t[k].y * 0.7f) : Vector2.zero;
                    float a = p[k].y < 0f ? ao * 0.92f : ao;
                    kit.Vert(c + q * p[k], q * f, uv, C(a, rune ? 1f : 0f, f.y > 0.5f ? moss : moss * 0.2f));
                }
                kit.Tri(false, b, b + 2, b + 1); kit.Tri(false, b, b + 3, b + 2);
            }
        }

        /// <summary>Disc in the ring plane (normal z), radius VortexRadius, centred at the ring centre; uv = position / radius.</summary>
        public static Mesh Vortex()
        {
            if (vortex != null) return vortex;
            var kit = new Map3DMeshKit();
            Disc(kit, new Vector3(0f, RingCentre, 0f), Vector3.right, Vector3.up, VortexRadius, 40);
            return vortex = kit.Build("Map3DPortalVortex");
        }

        /// <summary>Flat disc on the ground (radius r m), uv = position / r.</summary>
        public static Mesh Pool(float r)
        {
            if (pool != null) return pool;
            var kit = new Map3DMeshKit();
            Disc(kit, Vector3.zero, Vector3.right, Vector3.forward, r, 40);
            return pool = kit.Build("Map3DPortalPool");
        }

        static void Disc(Map3DMeshKit kit, Vector3 c, Vector3 ax, Vector3 ay, float r, int seg)
        {
            Vector3 nrm = Vector3.Cross(ax, ay).normalized;
            int centre = kit.Vert(c, nrm, Vector2.zero, Color.white);
            for (int s = 0; s <= seg; s++)
            {
                float a = s / (float)seg * Mathf.PI * 2f;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                kit.Vert(c + (ax * d.x + ay * d.y) * r, nrm, d, Color.white);
            }
            for (int s = 0; s < seg; s++) kit.Tri(false, centre, centre + 1 + s, centre + 2 + s); // double-sided shader: winding does not matter
        }

        /// <summary>Open cylinder of radius r, height hgt (m) from the ground; uv.x round (0..1), uv.y 0..1 up.</summary>
        public static Mesh Column(float r, float hgt)
        {
            if (column != null) return column;
            var kit = new Map3DMeshKit();
            var path = new[] { Vector3.zero, new Vector3(0f, hgt * 0.5f, 0f), new Vector3(0f, hgt, 0f) };
            kit.Tube(path, new[] { r * 1.15f, r, r * 0.8f }, 20, 0f, 1f, hgt, new[] { Color.white, Color.white, Color.white }, false);
            return column = kit.Build("Map3DPortalColumn");
        }

        static Texture2D runes;

        /// <summary>Rune strip: RuneGlyphs glyphs of 64 x 64 side by side (white, shape in alpha), strokes between the nodes of a 3 x 3 grid, seeded.</summary>
        public static Texture2D Runes
        {
            get
            {
                if (runes != null) return runes;
                const int S = 64, W = S * RuneGlyphs;
                var a = new float[W * S];
                var rng = new System.Random(77);
                for (int g = 0; g < RuneGlyphs; g++)
                {
                    int strokes = 3 + rng.Next(3);
                    int prev = rng.Next(9);
                    for (int k = 0; k < strokes; k++)
                    {
                        int next = rng.Next(9);
                        if (next == prev) next = (next + 4) % 9;
                        Line(a, W, S, g * S, Node(prev), Node(next));
                        prev = rng.NextDouble() < 0.6 ? next : rng.Next(9); // mostly a continuous stroke, sometimes a new one
                    }
                }
                var px = new Color32[W * S];
                for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a[i]) * 255f));
                runes = new Texture2D(W, S, TextureFormat.RGBA32, true) { name = "Map3DPortalRunes", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
                runes.SetPixels32(px);
                runes.Apply(true, true);
                return runes;
            }
        }

        static Vector2 Node(int i) { return new Vector2(12f + (i % 3) * 20f, 12f + (i / 3) * 20f); }

        static void Line(float[] a, int w, int s, int x0, Vector2 p, Vector2 q)
        {
            const float half = 3.2f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    Vector2 pt = new Vector2(x + 0.5f, y + 0.5f), d = q - p;
                    float t = Mathf.Clamp01(Vector2.Dot(pt - p, d) / Mathf.Max(1e-3f, d.sqrMagnitude));
                    float dist = Vector2.Distance(pt, p + d * t);
                    float v = Mathf.Clamp01(half + 0.8f - dist) ;
                    int i = y * w + x0 + x;
                    a[i] = Mathf.Max(a[i], v);
                }
        }
    }
}
