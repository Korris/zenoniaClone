using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Ruin pieces of the 3D map (ChatGPT round 1, P1-7: "replace at least 2 pillars and 1 wall with meshes, ivory stone #B8AF9C, AO at the foot x0.72"):
    /// a column = plinth box + a 10-sided shaft with a slight taper and lean + a capital ring (the short one is broken off, its top tilted);
    /// a low wall = two courses of blocks with gaps and jitter; a corner wall = an L of the same. Painted triplanar stone (Map3DAssets.Ruin).
    /// Units: metres, y up (Map3DMeshKit converts).
    /// </summary>
    public static class Map3DRuinBuilder
    {
        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();
        static float R(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }
        static Color Ao(float a) { return new Color(a, 0f, 0.22f, 0f); } // a little moss only (round 2: flat tops went fully green)

        public static Mesh Pillar(bool tall, int seed)
        {
            string key = (tall ? "pillarT" : "pillarS") + seed;
            Mesh m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            var rng = new System.Random(seed);
            var kit = new Map3DMeshKit();
            float h = tall ? R(rng, 2.5f, 2.8f) : R(rng, 1.1f, 1.5f), r = 0.3f;
            kit.Box(new Vector3(0f, 0.12f, 0f), new Vector3(0.48f, 0.2f, 0.48f), R(rng, 0f, 0.3f), Ao(0.85f), Ao(0.72f));
            Vector3 lean = new Vector3(R(rng, -0.06f, 0.06f), 0f, R(rng, -0.06f, 0.06f));
            var path = new[] { new Vector3(0f, 0.3f, 0f), new Vector3(0f, 0.3f, 0f) + lean * 0.5f + Vector3.up * h * 0.5f, new Vector3(0f, 0.3f + h, 0f) + lean };
            kit.Tube(path, new[] { r * 1.05f, r, r * 0.92f }, 10, 0f, 1f, 1f, new[] { Ao(0.78f), Ao(0.95f), Ao(1f) }, !tall);
            if (tall)
            {
                Vector3 top = path[2];
                kit.Tube(new[] { top, top + Vector3.up * 0.16f }, new[] { r * 1.25f, r * 1.4f }, 10, 0f, 1f, 1f, new[] { Ao(0.9f), Ao(1f) }, false);
                kit.Box(top + Vector3.up * 0.24f, new Vector3(0.44f, 0.09f, 0.44f), R(rng, 0f, 0.4f), Ao(1f), Ao(0.85f));
            }
            else kit.Box(new Vector3(0.55f, 0.13f, 0.3f), new Vector3(0.28f, 0.13f, 0.2f), R(rng, 0f, 3f), Ao(0.95f), Ao(0.75f)); // fallen chunk at the foot
            m = kit.Build("Map3DPillar_" + key);
            Cache[key] = m;
            return m;
        }

        /// <summary>Low wall `length` metres along x (corner: an L, the second arm along z).</summary>
        public static Mesh Wall(float length, bool corner, int seed)
        {
            string key = "wall" + length.ToString("F1") + corner + seed;
            Mesh m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            var rng = new System.Random(seed);
            var kit = new Map3DMeshKit();
            Course(kit, rng, new Vector3(-length * 0.5f, 0f, 0f), Vector3.right, length);
            if (corner) Course(kit, rng, new Vector3(-length * 0.5f, 0f, 0f), Vector3.forward, length * 0.7f);
            m = kit.Build("Map3DWall_" + key);
            Cache[key] = m;
            return m;
        }

        static void Course(Map3DMeshKit kit, System.Random rng, Vector3 start, Vector3 dir, float length)
        {
            float yaw = Mathf.Atan2(dir.x, dir.z) - Mathf.PI * 0.5f;
            for (int row = 0; row < 3; row++)
            {
                float x = row % 2 == 0 ? 0f : 0.3f, rowLen = length * (row == 2 ? 0.55f : 1f);
                while (x < rowLen - 0.2f)
                {
                    float w = R(rng, 0.5f, 0.75f);
                    if (rng.NextDouble() > (row == 0 ? 0.04 : 0.22))
                    {
                        Vector3 c = start + dir * (x + w * 0.5f) + new Vector3(0f, 0.22f + row * 0.42f, 0f);
                        float hy = 0.2f * R(rng, 0.9f, 1.05f);
                        kit.Box(c, new Vector3(w * 0.48f, hy, 0.28f), yaw + R(rng, -0.06f, 0.06f), Ao(row == 0 ? 0.85f : 1f), Ao(row == 0 ? 0.72f : 0.86f));
                    }
                    x += w + 0.03f;
                }
            }
        }
    }
}
