using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Willowbrook Village's town wall as real 3D (user 24/11 23:50: "khi xoay camera cái tường cũng xoay theo": the tauWallLeft / tauWallRight
    /// sprites stood as billboards and turned with the 360 deg camera). Same design as the painted wall sprite: a cream ashlar curtain wall with a
    /// protruding top course, big merlons at the joints and the middle, a pilaster at each joint, a bottom course, three blind arches with voussoirs
    /// on each face and ivy clumps. Stone / timber = the Guild Hall's ChatGPT atlas A (Map3DAssets.TownStone, warmed to the sprite's cream),
    /// ivy = the Taurus leaf cards. Also the stone curb ring round the Great Oak (tauOakPlanterRing). Gate towers and the arch: Map3DGateBuilder.
    /// Units: metres, y up, x along the wall (east), z north, origin on the ground at the footprint centre. Visual only: rows / solids unchanged.
    /// </summary>
    public static class Map3DTownWallBuilder
    {
        public const int Stone = Map3DGuildHallBuilder.Stone, Timber = Map3DGuildHallBuilder.Timber;
        public const float Rep = 1.8f;       // stone texture repeat (m): ~0.45 m courses like the sprite
        public const float HalfDepth = 0.42f; // wall 0.84 m thick (ChatGPT round 1: "tường quá đồ sộ"), centred on the line between the two solid rows
        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        public static bool HasArt { get { return Map3DGuildHallBuilder.HasArt; } }

        static void Box(Map3DMeshKit k, Vector3 c, Vector3 h, float ao) { Map3DGuildHallBuilder.Box(k, c, h, Stone, Rep, ao); }

        /// <summary>One wall segment `length` m long (stone in submesh 0, ivy leaf cards in 1 / 2). Merlon and pilaster only at the WEST joint and the
        /// middle, so segments laid end to end never double a block.</summary>
        public static Mesh Wall(float length, int seed)
        {
            string key = "wall" + length.ToString("F2") + "_" + seed;
            Mesh m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            var k = new Map3DMeshKit();
            var rng = new System.Random(seed * 31 + 5);
            float hl = length * 0.5f + 0.01f, hz = HalfDepth;
            Box(k, new Vector3(0f, -0.35f, 0f), new Vector3(hl, 0.55f, hz + 0.06f), 0.7f);    // footing into the bank
            Box(k, new Vector3(0f, 0.35f, 0f), new Vector3(hl, 0.15f, hz + 0.04f), 0.8f);     // bottom course
            Box(k, new Vector3(0f, 1.0f, 0f), new Vector3(hl, 0.5f, hz), 0.88f);               // recessed body
            Box(k, new Vector3(0f, 1.68f, 0f), new Vector3(hl, 0.18f, hz + 0.07f), 0.95f);    // protruding top course (total 2.25 m with merlons)
            foreach (float x in new[] { -length * 0.5f, 0f })
            {
                float j = (float)rng.NextDouble() * 0.04f;
                Box(k, new Vector3(x, 2.07f + j * 0.5f, 0f), new Vector3(0.34f, 0.21f + j * 0.5f, hz + 0.03f), 1f); // merlon
            }
            Box(k, new Vector3(-length * 0.5f, 1.0f, 0f), new Vector3(0.3f, 0.5f, hz + 0.05f), 0.88f);           // joint pilaster
            float bay = (length - 0.64f) / 3f;
            for (int s = -1; s <= 1; s += 2)
                for (int a = 0; a < 3; a++) BlindArch(k, -length * 0.5f + 0.32f + bay * (a + 0.5f), s, Mathf.Min(0.42f, bay * 0.34f));
            for (int s = -1; s <= 1; s += 2)
                for (int c = 0; c < 2; c++) Ivy(k, rng, (c == 0 ? -0.35f : 0.3f) * length + (float)(rng.NextDouble() - 0.5) * 0.6f, s);
            m = k.Build("Map3DTownWall_" + key);
            Cache[key] = m;
            return m;
        }

        /// <summary>Blind arch on face s (-1 south, +1 north) centred at x: darker recess (stepped top) + a ring of 7 voussoirs and two jambs, proud of the body.</summary>
        static void BlindArch(Map3DMeshKit k, float x, int s, float w)
        {
            float hz = HalfDepth, spring = 0.95f, r = w + 0.06f;
            Box(k, new Vector3(x, 0.725f, s * (hz + 0.006f)), new Vector3(w, 0.225f, 0.012f), 0f);
            Box(k, new Vector3(x, spring + 0.08f, s * (hz + 0.006f)), new Vector3(w * 0.86f, 0.08f, 0.012f), 0f);
            Box(k, new Vector3(x, spring + 0.21f, s * (hz + 0.006f)), new Vector3(w * 0.6f, 0.06f, 0.012f), 0f);
            Box(k, new Vector3(x, spring + 0.3f, s * (hz + 0.006f)), new Vector3(w * 0.3f, 0.04f, 0.012f), 0f);
            for (int v = 0; v < 7; v++)
            {
                float t = Mathf.PI * v / 6f;
                Vector3 c = new Vector3(x + Mathf.Cos(t) * r, spring + Mathf.Sin(t) * r, s * (hz + 0.035f));
                Map3DGuildHallBuilder.Box(k, c, new Vector3(0.09f, 0.1f, 0.035f), Quaternion.AngleAxis(t * Mathf.Rad2Deg, Vector3.forward), Stone, Rep, 1f);
            }
            foreach (float side in new[] { -1f, 1f })
                Box(k, new Vector3(x + side * r, 0.72f, s * (hz + 0.03f)), new Vector3(0.08f, 0.22f, 0.03f), 0.9f);
        }

        /// <summary>Ivy clump on face s at x: 3-4 leaf cards flat on the wall, hanging from the top course (the sprite's two yellow-green vines).</summary>
        static void Ivy(Map3DMeshKit k, System.Random rng, float x, int s)
        {
            int n = 3 + rng.Next(2);
            Vector3 outN = new Vector3(0f, 0f, s);
            for (int i = 0; i < n; i++)
            {
                float sz = 0.34f + (float)rng.NextDouble() * 0.18f;
                Vector3 c = new Vector3(x + (float)(rng.NextDouble() - 0.5) * 0.5f, 1.72f - i * 0.34f - (float)rng.NextDouble() * 0.1f, s * (HalfDepth + 0.1f + i * 0.004f));
                var nrm = new[] { (outN + Vector3.up * 0.5f).normalized, (outN + Vector3.up * 0.5f).normalized, (outN + Vector3.up * 0.8f).normalized, (outN + Vector3.up * 0.8f).normalized };
                float var = (float)rng.NextDouble(), tint = (float)rng.NextDouble();
                var col = new[] { new Color(0.8f, 0.02f, var, tint), new Color(0.8f, 0.02f, var, tint), new Color(1f, 0.05f, var, tint), new Color(1f, 0.05f, var, tint) };
                k.Card(c, new Vector3(sz * -s, 0f, 0f), new Vector3(0f, sz, 0f), Map3DMeshKit.Cell(rng.Next(16)), nrm, col);
            }
        }

        /// <summary>Stone curb ring (tauOakPlanterRing): `n` dressed blocks round a circle of outer radius `r`, 0.42 m high, a cap course on top.</summary>
        public static Mesh Ring(float r, int n)
        {
            string key = "ring" + r.ToString("F2") + n;
            Mesh m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            var k = new Map3DMeshKit();
            float mid = r - 0.2f, seg = 2f * Mathf.PI * mid / n;
            for (int i = 0; i < n; i++)
            {
                float a = (i + 0.5f) * 2f * Mathf.PI / n;
                Vector3 c = new Vector3(Mathf.Cos(a) * mid, 0f, Mathf.Sin(a) * mid);
                Quaternion q = Quaternion.AngleAxis(-a * Mathf.Rad2Deg + 90f, Vector3.up); // box x along the tangent
                bool cap = i % 2 == 0;
                Map3DGuildHallBuilder.Box(k, c + Vector3.up * 0.06f, new Vector3(seg * 0.5f + 0.01f, 0.3f, 0.2f), q, Stone, Rep, 0.75f);
                Map3DGuildHallBuilder.Box(k, c + Vector3.up * (cap ? 0.42f : 0.39f), new Vector3(seg * 0.5f - 0.02f, cap ? 0.07f : 0.05f, 0.23f), q, Stone, Rep, 1f);
            }
            m = k.Build("Map3DTownRing_" + key);
            Cache[key] = m;
            return m;
        }

        // ---- shared primitives (also Map3DGateBuilder) ----

        /// <summary>Quad q0..q3 (counter-clockwise from outside, any start) facing n; uv per corner; colour = AO + atlas cell.</summary>
        internal static void Quad(Map3DMeshKit k, Vector3 q0, Vector3 q1, Vector3 q2, Vector3 q3, Vector3 n, Vector2 t0, Vector2 t1, Vector2 t2, Vector2 t3, Color c)
        {
            if (Vector3.Dot(Vector3.Cross(q1 - q0, q2 - q0), n) < 0f) { Vector3 tq = q1; q1 = q3; q3 = tq; Vector2 tt = t1; t1 = t3; t3 = tt; }
            int b = k.Count;
            k.Vert(q0, n, t0, c); k.Vert(q1, n, t1, c); k.Vert(q2, n, t2, c); k.Vert(q3, n, t3, c);
            k.Tri(false, b, b + 2, b + 1); k.Tri(false, b, b + 3, b + 2);
        }

        /// <summary>Vertical cylinder (no caps) at centre c from y0 to y1, radius r, atlas cell tiled in world metres (rep).</summary>
        internal static void Cylinder(Map3DMeshKit k, Vector3 c, float y0, float y1, float r, int sides, int cell, float rep, float aoBottom)
        {
            for (int s = 0; s < sides; s++)
            {
                float a0 = 2f * Mathf.PI * s / sides, a1 = 2f * Mathf.PI * (s + 1) / sides, am = (a0 + a1) * 0.5f;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 n = new Vector3(Mathf.Cos(am), 0f, Mathf.Sin(am));
                float u0 = a0 * r / rep, u1 = a1 * r / rep;
                int b = k.Count;
                Color lo = Map3DGuildHallBuilder.Col(aoBottom, cell), hi = Map3DGuildHallBuilder.Col(1f, cell);
                // per-vertex smooth normals: round tower, not facets
                k.Vert(c + d0 * r + Vector3.up * y0, d0, new Vector2(u0, y0 / rep), lo);
                k.Vert(c + d1 * r + Vector3.up * y0, d1, new Vector2(u1, y0 / rep), lo);
                k.Vert(c + d1 * r + Vector3.up * y1, d1, new Vector2(u1, y1 / rep), hi);
                k.Vert(c + d0 * r + Vector3.up * y1, d0, new Vector2(u0, y1 / rep), hi);
                if (Vector3.Dot(Vector3.Cross(d1 - d0, Vector3.up), n) > 0f) { k.Tri(false, b, b + 2, b + 1); k.Tri(false, b, b + 3, b + 2); }
                else { k.Tri(false, b, b + 1, b + 2); k.Tri(false, b, b + 2, b + 3); }
            }
        }

        /// <summary>Cone roof: eave ring radius r at height y0 (with a short drip skirt), apex at y1; shingles tiled along the eave and up the slope.</summary>
        internal static void Cone(Map3DMeshKit k, Vector3 c, float y0, float y1, float r, int sides, int cell, float rep)
        {
            float h = y1 - y0, slant = Mathf.Sqrt(h * h + r * r);
            Vector3 apex = c + Vector3.up * y1;
            for (int s = 0; s < sides; s++)
            {
                float a0 = 2f * Mathf.PI * s / sides, a1 = 2f * Mathf.PI * (s + 1) / sides, am = (a0 + a1) * 0.5f;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)), dm = new Vector3(Mathf.Cos(am), 0f, Mathf.Sin(am));
                Vector3 n = (dm * h + Vector3.up * r).normalized;
                Vector3 e0 = c + d0 * r + Vector3.up * y0, e1 = c + d1 * r + Vector3.up * y0;
                float u0 = a0 * r / rep, u1 = a1 * r / rep;
                Quad(k, e0, e1, apex, apex, n, new Vector2(u0, 0f), new Vector2(u1, 0f), new Vector2((u0 + u1) * 0.5f, slant / rep), new Vector2((u0 + u1) * 0.5f, slant / rep), Map3DGuildHallBuilder.Col(1f, cell));
                Quad(k, e0 - Vector3.up * 0.14f, e1 - Vector3.up * 0.14f, e1, e0, dm, new Vector2(u0, -0.14f / rep), new Vector2(u1, -0.14f / rep), new Vector2(u1, 0f), new Vector2(u0, 0f), Map3DGuildHallBuilder.Col(0.8f, cell));
                Quad(k, e0 - Vector3.up * 0.14f, e1 - Vector3.up * 0.14f, c + Vector3.up * (y0 - 0.14f), c + Vector3.up * (y0 - 0.14f), Vector3.down, Vector2.zero, new Vector2(0.3f, 0f), new Vector2(0.15f, 0.3f), new Vector2(0.15f, 0.3f), Map3DGuildHallBuilder.Col(0.4f, cell));
            }
        }
    }
}
