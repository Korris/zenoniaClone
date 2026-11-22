using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Wooden props of the 3D map (user 22/11: "cây cầu khi tôi xoay ở nhiều góc độ thì nó bị méo": the bridge, fences, signpost and barricade were flat
    /// sprites that skew when the camera turns). Built board by board from textured boxes on the ChatGPT wood atlas (Resources/Map3D/wood.png, 2x2:
    /// <see cref="Planks"/> horizontal planks, <see cref="Grain"/> beam side grain, <see cref="Shingles"/> roof shingles, <see cref="Thatch"/> straw),
    /// drawn by Map3DPainted _Mode 2 (cell from vertex alpha, uv tiling inside the cell). Shed / haystack / barrels: Map3DShedBuilder.
    /// Units: metres, y up, x east, z north (Map3DMeshKit converts). Origin = footprint centre on the ground (the bridge: deck top at 0 = walkable plane).
    /// </summary>
    public static class Map3DWoodBuilder
    {
        public const int Planks = 0, Grain = 1, Shingles = 2, Thatch = 3;
        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();
        internal static float R(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

        /// <summary>Vertex colour of a wood surface: r = AO, b = moss amount, a = atlas cell.</summary>
        internal static Color Col(float ao, int cell, float moss = 0f) { return new Color(ao, 0f, moss, cell / 3f); }

        /// <summary>Box centred at c with half sizes h, turned by q, every face uv-mapped at `repeat` metres per texture tile. Beam grain runs along the
        /// longest axis (v); planks and shingles run along it too (u). AO darkens the lower half toward `aoBottom`.</summary>
        internal static void Beam(Map3DMeshKit kit, Vector3 c, Vector3 h, Quaternion q, int cell, float repeat, System.Random rng, float aoBottom = 0.8f, float moss = 0f)
        {
            int L = h.x >= h.y && h.x >= h.z ? 0 : h.y >= h.z ? 1 : 2;
            bool alongU = cell != Grain && cell != Thatch;
            Vector2 off = new Vector2(R(rng, 0f, 1f), R(rng, 0f, 1f));
            Vector3[] n = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (Vector3 f in n)
            {
                Vector3 u = Mathf.Abs(f.y) > 0.5f ? Vector3.forward : Vector3.up, r = Vector3.Cross(u, f);
                Vector3 fc = Vector3.Scale(f, h), ru = Vector3.Scale(r, h), uu = Vector3.Scale(u, h);
                Vector3[] p = { fc - ru - uu, fc + ru - uu, fc + ru + uu, fc - ru + uu };
                int na = Mathf.Abs(f.x) > 0.5f ? 0 : Mathf.Abs(f.y) > 0.5f ? 1 : 2;
                int a1 = na == L ? (L == 0 ? 1 : 0) : L;            // first in-face axis: the long one when the face holds it
                int a2 = 3 - na - a1;
                int b = kit.Count;
                for (int k = 0; k < 4; k++)
                {
                    float s1 = p[k][a1] / repeat, s2 = p[k][a2] / repeat;
                    Vector2 t = alongU ? new Vector2(s1, s2) : new Vector2(s2, s1);
                    kit.Vert(c + q * p[k], q * f, t + off, Col(p[k].y > 0f ? 1f : aoBottom, cell, moss));
                }
                kit.Tri(false, b, b + 2, b + 1); kit.Tri(false, b, b + 3, b + 2);
            }
        }

        static Quaternion Yaw(float deg) { return Quaternion.AngleAxis(deg, Vector3.up); }

        /// <summary>Plank footbridge `length` m along x, deck `width` m: boards across the span (top at 0), two stringers, piles into the stream bed,
        /// end sills and a post-and-rail railing on both sides over the middle `railLength` m (the bank ends stay open, heroes step on from the side).</summary>
        public static Mesh Bridge(float length, float width, float railLength, int seed)
        {
            string key = "bridge" + length + width + railLength + seed;
            Mesh m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            var rng = new System.Random(seed);
            var kit = new Map3DMeshKit();
            const float Board = 0.27f, Gap = 0.03f;
            int boards = Mathf.FloorToInt(length / (Board + Gap));
            float x0 = -boards * (Board + Gap) * 0.5f + (Board + Gap) * 0.5f;
            for (int k = 0; k < boards; k++)
            {
                float hw = width * 0.5f + R(rng, -0.06f, 0.05f);
                Beam(kit, new Vector3(x0 + k * (Board + Gap), -0.04f + R(rng, -0.008f, 0.004f), R(rng, -0.04f, 0.04f)), new Vector3(Board * 0.5f, 0.04f, hw),
                    Yaw(R(rng, -2.5f, 2.5f)), Grain, 0.9f, rng, 0.75f);
            }
            float zs = width * 0.5f - 0.18f;
            foreach (float z in new[] { -zs, zs })
                Beam(kit, new Vector3(0f, -0.2f, z), new Vector3(length * 0.5f + 0.15f, 0.12f, 0.09f), Quaternion.identity, Grain, 1.2f, rng, 0.7f);
            foreach (float x in new[] { -length * 0.26f, length * 0.26f })
            {
                Beam(kit, new Vector3(x, -0.36f, 0f), new Vector3(0.08f, 0.07f, width * 0.5f + 0.05f), Quaternion.identity, Grain, 1f, rng, 0.7f); // cross beam
                foreach (float z in new[] { -zs, zs })
                    Beam(kit, new Vector3(x, -0.62f, z), new Vector3(0.1f, 0.5f, 0.1f), Yaw(R(rng, 0f, 20f)), Grain, 1f, rng, 0.6f, 0.4f); // pile into the bed
            }
            foreach (float x in new[] { -length * 0.5f - 0.05f, length * 0.5f + 0.05f })
                Beam(kit, new Vector3(x, -0.17f, 0f), new Vector3(0.14f, 0.13f, width * 0.5f + 0.2f), Quaternion.identity, Grain, 1f, rng, 0.65f, 0.5f); // sill on the bank
            int posts = Mathf.Max(2, Mathf.RoundToInt(railLength / 1.25f) + 1);
            float zr = width * 0.5f - 0.04f;
            foreach (float z in new[] { -zr, zr })
            {
                for (int k = 0; k < posts; k++)
                {
                    float x = -railLength * 0.5f + railLength * k / (posts - 1);
                    Beam(kit, new Vector3(x, 0.32f, z), new Vector3(0.065f, 0.55f, 0.065f), Quaternion.AngleAxis(R(rng, -2f, 2f), Vector3.forward), Grain, 0.8f, rng, 0.7f);
                }
                Beam(kit, new Vector3(0f, 0.8f, z), new Vector3(railLength * 0.5f + 0.1f, 0.045f, 0.055f), Quaternion.AngleAxis(R(rng, -0.6f, 0.6f), Vector3.forward), Grain, 1.2f, rng, 0.85f);
                Beam(kit, new Vector3(0f, 0.42f, z), new Vector3(railLength * 0.5f, 0.035f, 0.04f), Quaternion.AngleAxis(R(rng, -0.8f, 0.8f), Vector3.forward), Grain, 1.2f, rng, 0.85f);
            }
            m = kit.Build("Map3DBridge");
            Cache[key] = m;
            return m;
        }

        /// <summary>Post-and-rail fence `length` m along x (posts ~1.3 m apart, two rails on the south face); broken: the far half of the top rail hangs down.</summary>
        public static Mesh Fence(float length, bool broken, int seed)
        {
            string key = "fence" + length.ToString("F1") + broken + seed;
            Mesh m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            var rng = new System.Random(seed);
            var kit = new Map3DMeshKit();
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 1.3f) + 1);
            for (int k = 0; k < posts; k++)
            {
                float x = -length * 0.5f + length * k / (posts - 1), hgt = R(rng, 0.98f, 1.08f);
                Beam(kit, new Vector3(x, hgt * 0.5f - 0.05f, 0f), new Vector3(0.075f, hgt * 0.5f, 0.075f),
                    Quaternion.AngleAxis(R(rng, -3f, 3f), Vector3.forward) * Yaw(R(rng, -8f, 8f)), Grain, 0.8f, rng, 0.7f, 0.3f);
            }
            float half = length * 0.5f + 0.08f;
            Beam(kit, new Vector3(0f, 0.42f, -0.1f), new Vector3(half, 0.055f, 0.03f), Quaternion.AngleAxis(R(rng, -1f, 1f), Vector3.forward), Grain, 1.2f, rng, 0.85f);
            if (!broken) Beam(kit, new Vector3(0f, 0.78f, -0.1f), new Vector3(half, 0.055f, 0.03f), Quaternion.AngleAxis(R(rng, -1f, 1f), Vector3.forward), Grain, 1.2f, rng, 0.85f);
            else
            {
                Beam(kit, new Vector3(-half * 0.5f, 0.78f, -0.1f), new Vector3(half * 0.5f, 0.055f, 0.03f), Quaternion.identity, Grain, 1.2f, rng, 0.85f);
                Beam(kit, new Vector3(half * 0.45f, 0.45f, -0.16f), new Vector3(half * 0.48f, 0.05f, 0.03f), Quaternion.AngleAxis(-24f, Vector3.forward), Grain, 1.2f, rng, 0.8f); // fallen half
            }
            m = kit.Build("Map3DFence");
            Cache[key] = m;
            return m;
        }

        /// <summary>Signpost: a 1.6 m post with two arrow boards pointing different ways.</summary>
        public static Mesh Signpost(int seed)
        {
            string key = "sign" + seed;
            Mesh m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            var rng = new System.Random(seed);
            var kit = new Map3DMeshKit();
            Beam(kit, new Vector3(0f, 0.8f, 0f), new Vector3(0.065f, 0.82f, 0.065f), Quaternion.AngleAxis(R(rng, -2f, 2f), Vector3.forward), Grain, 0.8f, rng, 0.7f, 0.3f);
            Beam(kit, new Vector3(0f, 1.38f, 0f), new Vector3(0.36f, 0.09f, 0.025f), Yaw(20f) * Quaternion.Euler(0f, 0f, -3f), Planks, 0.9f, rng, 0.9f);
            Beam(kit, new Vector3(0f, 1.12f, 0f), new Vector3(0.32f, 0.085f, 0.025f), Yaw(-35f) * Quaternion.Euler(0f, 0f, 4f), Planks, 0.9f, rng, 0.9f);
            m = kit.Build("Map3DSignpost");
            Cache[key] = m;
            return m;
        }

        /// <summary>Road barricade `length` m along x: two heavy posts, two rails, an X brace and two props behind (+z).</summary>
        public static Mesh Barricade(float length, int seed)
        {
            string key = "barricade" + length.ToString("F1") + seed;
            Mesh m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            var rng = new System.Random(seed);
            var kit = new Map3DMeshKit();
            float hx = length * 0.5f;
            foreach (float x in new[] { -hx, hx })
            {
                Beam(kit, new Vector3(x, 0.85f, 0f), new Vector3(0.12f, 0.9f, 0.12f), Quaternion.AngleAxis(R(rng, -2f, 2f), Vector3.forward), Grain, 1f, rng, 0.65f, 0.4f);
                Beam(kit, new Vector3(x, 0.62f, 0.55f), new Vector3(0.06f, 0.78f, 0.06f), Quaternion.AngleAxis(-42f, Vector3.right), Grain, 1f, rng, 0.7f);
            }
            Beam(kit, new Vector3(0f, 0.38f, -0.14f), new Vector3(hx + 0.1f, 0.09f, 0.05f), Quaternion.identity, Grain, 1.2f, rng, 0.8f);
            Beam(kit, new Vector3(0f, 1.42f, -0.14f), new Vector3(hx + 0.14f, 0.09f, 0.05f), Quaternion.AngleAxis(R(rng, -1f, 1f), Vector3.forward), Grain, 1.2f, rng, 0.85f);
            float ang = Mathf.Atan2(1.04f, length) * Mathf.Rad2Deg, len = Mathf.Sqrt(length * length + 1.04f * 1.04f) * 0.5f - 0.05f;
            Beam(kit, new Vector3(0f, 0.9f, -0.2f), new Vector3(len, 0.075f, 0.04f), Quaternion.AngleAxis(ang, Vector3.forward), Grain, 1.2f, rng, 0.8f);
            Beam(kit, new Vector3(0f, 0.9f, -0.25f), new Vector3(len, 0.075f, 0.04f), Quaternion.AngleAxis(-ang, Vector3.forward), Grain, 1.2f, rng, 0.8f);
            m = kit.Build("Map3DBarricade");
            Cache[key] = m;
            return m;
        }
    }
}
