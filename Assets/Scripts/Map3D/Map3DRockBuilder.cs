using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Generated rocks, logs and stumps of the 3D map (ChatGPT spec section 3): icosphere (subdivided twice) scaled unevenly, displaced by two noise
    /// octaves (10-12 % and 3 % of the radius), cut by 2-3 planes for a few broad flat faces (no potatoes), bottom sunk into the ground; smooth normals.
    /// Painted triplanar stone + moss on top come from the material (Map3DPainted, mode 1); vertex r = AO (0.72 at the ground contact), b = moss.
    /// Logs: a lying bark tube with capped ends; stumps: a short flared tube with a lighter cut top. Units: metres, y up (Map3DMeshKit converts).
    /// </summary>
    public static class Map3DRockBuilder
    {
        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();
        static List<Vector3> icoV;
        static List<int> icoT;

        static float R(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

        /// <summary>One rock of size (width x, height y, depth z) metres, variant seed.</summary>
        public static Mesh Rock(Vector3 size, int seed)
        {
            string key = "rock" + size.ToString("F1") + "_" + seed;
            Mesh m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            var kit = new Map3DMeshKit();
            AddRock(kit, Vector3.zero, size, seed);
            m = kit.Build("Map3DRock_" + seed);
            Cache[key] = m;
            return m;
        }

        /// <summary>A cluster of rocks (stream banks, rock runs): n stones of decreasing size along x.</summary>
        public static Mesh Cluster(Vector3 size, int n, int seed)
        {
            string key = "cluster" + size.ToString("F1") + "_" + n + "_" + seed;
            Mesh m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            var rng = new System.Random(seed);
            var kit = new Map3DMeshKit();
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0.5f : i / (float)(n - 1);
                float s = i == 0 ? 1f : R(rng, 0.45f, 0.8f);
                Vector3 c = new Vector3((t - 0.5f) * size.x * 0.75f, 0f, R(rng, -0.25f, 0.25f) * size.z);
                AddRock(kit, c, new Vector3(size.x / n * 1.5f * s, size.y * s, size.z * 0.8f * s), seed * 7 + i);
            }
            m = kit.Build("Map3DRockCluster_" + seed);
            Cache[key] = m;
            return m;
        }

        static void AddRock(Map3DMeshKit kit, Vector3 centre, Vector3 size, int seed)
        {
            Ico();
            var rng = new System.Random(seed);
            Vector3 o1 = new Vector3(R(rng, 0, 50), R(rng, 0, 50), R(rng, 0, 50)), o2 = new Vector3(R(rng, 0, 50), R(rng, 0, 50), R(rng, 0, 50));
            int planes = rng.Next(3, 5);
            var pn = new Vector3[planes]; var pd = new float[planes];
            for (int i = 0; i < planes; i++) { pn[i] = new Vector3(R(rng, -1, 1), R(rng, 0.2f, 1.2f), R(rng, -1, 1)).normalized; pd[i] = R(rng, 0.55f, 0.75f); }
            var p = new Vector3[icoV.Count];
            for (int i = 0; i < icoV.Count; i++)
            {
                Vector3 d = icoV[i];
                float r = 1f + 0.16f * (Noise3(d * 1.2f + o1) - 0.5f) * 2f + 0.035f * (Noise3(d * 4.2f + o2) - 0.5f) * 2f;
                Vector3 v = d * r;
                for (int k = 0; k < planes; k++) { float dd = Vector3.Dot(v, pn[k]); if (dd > pd[k]) v -= pn[k] * (dd - pd[k]) * 0.85f; }
                p[i] = v;
            }
            // to size: y up; the bottom 18 % sinks under the ground
            var w = new Vector3[p.Length];
            for (int i = 0; i < p.Length; i++)
                w[i] = centre + new Vector3(p[i].x * size.x * 0.5f, (p[i].y * 0.5f + 0.32f) * size.y, p[i].z * size.z * 0.5f);
            // smooth normals from the faces
            var n = new Vector3[w.Length];
            for (int t = 0; t < icoT.Count; t += 3)
            {
                Vector3 fn = Vector3.Cross(w[icoT[t + 1]] - w[icoT[t]], w[icoT[t + 2]] - w[icoT[t]]);
                n[icoT[t]] += fn; n[icoT[t + 1]] += fn; n[icoT[t + 2]] += fn;
            }
            int b = kit.Count;
            for (int i = 0; i < w.Length; i++)
            {
                float h01 = Mathf.Clamp01(w[i].y / Mathf.Max(0.1f, size.y * 0.8f));
                kit.Vert(w[i], n[i].normalized, Vector2.zero, new Color(Mathf.Lerp(0.72f, 1f, h01 * 1.4f), 0f, 0.38f, 0f)); // moss: a patchy top, not a green cap
            }
            for (int t = 0; t < icoT.Count; t += 3) kit.Tri(false, b + icoT[t], b + icoT[t + 2], b + icoT[t + 1]); // the table is clockwise from outside (Unity front); the kit flips solids
        }

        /// <summary>Lying log (length along x) or standing stump (log = false), bark strip 0 (oak).</summary>
        public static Mesh Wood(float length, float radius, bool log, int seed)
        {
            string key = (log ? "log" : "stump") + length.ToString("F1") + radius.ToString("F2") + "_" + seed;
            Mesh m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            var rng = new System.Random(seed);
            var kit = new Map3DMeshKit();
            if (log)
            {
                float y = radius * 0.8f;
                var path = new[] { new Vector3(-length * 0.5f, y, 0f), new Vector3(0f, y + R(rng, -0.04f, 0.04f), R(rng, -0.08f, 0.08f)), new Vector3(length * 0.5f, y * 0.95f, 0f) };
                var rad = new[] { radius, radius * 0.95f, radius * 0.85f };
                var col = new[] { new Color(0.8f, 0, 0.25f, 0), new Color(0.9f, 0, 0.3f, 0), new Color(0.8f, 0, 0.2f, 0) }; // light moss: the first build read as a green tube
                kit.Tube(path, rad, 9, 0f, 0.25f, 1.6f, col, true);
                kit.Tube(new[] { path[0] + Vector3.right * 0.02f, path[0] - Vector3.right * 0.01f }, new[] { radius * 1.0f, radius * 0.6f }, 9, 0f, 0.25f, 1.6f, new[] { new Color(0.6f, 0, 0, 0), new Color(0.5f, 0, 0, 0) }, true);
            }
            else
            {
                var path = new[] { new Vector3(0, -0.15f, 0), new Vector3(0, 0.05f, 0), new Vector3(0.02f, length, 0.01f) };
                var rad = new[] { radius * 1.6f, radius * 1.25f, radius };
                var col = new[] { new Color(0.72f, 0, 0.4f, 0), new Color(0.8f, 0, 0.3f, 0), new Color(1f, 0, 0.15f, 0) };
                kit.Tube(path, rad, 10, 0f, 0.25f, 1.6f, col, true);
            }
            m = kit.Build("Map3DWood_" + key);
            Cache[key] = m;
            return m;
        }

        static float Noise3(Vector3 p)
        {
            return (Mathf.PerlinNoise(p.x, p.y) + Mathf.PerlinNoise(p.y + 17.3f, p.z) + Mathf.PerlinNoise(p.z + 41.9f, p.x)) / 3f;
        }

        /// <summary>Unit icosphere, 2 subdivisions (162 vertices, 320 triangles); Cross(b - a, c - a) points outward.</summary>
        static void Ico()
        {
            if (icoV != null) return;
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            icoV = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0), new Vector3(0, -1, t), new Vector3(0, 1, t),
                new Vector3(0, -1, -t), new Vector3(0, 1, -t), new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            for (int i = 0; i < icoV.Count; i++) icoV[i] = icoV[i].normalized;
            icoT = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8, 3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            for (int s = 0; s < 2; s++)
            {
                var mid = new Dictionary<long, int>();
                var nt = new List<int>();
                for (int i = 0; i < icoT.Count; i += 3)
                {
                    int a = icoT[i], b = icoT[i + 1], c = icoT[i + 2];
                    int ab = Mid(mid, a, b), bc = Mid(mid, b, c), ca = Mid(mid, c, a);
                    nt.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                icoT = nt;
            }
        }

        static int Mid(Dictionary<long, int> cache, int a, int b)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            int i;
            if (cache.TryGetValue(key, out i)) return i;
            icoV.Add(((icoV[a] + icoV[b]) * 0.5f).normalized);
            cache[key] = icoV.Count - 1;
            return icoV.Count - 1;
        }
    }
}
