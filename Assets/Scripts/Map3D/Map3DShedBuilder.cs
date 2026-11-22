using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Farm pieces of the 3D map, replacing the flat sprites that skewed under the turning camera (user 22/11): an open-front plank shed with a gable
    /// shingle roof (ridge along x, the open side faces south), barrels, a leaning board pair; a haystack; reeds as crossed alpha cards of the painted
    /// reed sprite. Wood textures from the ChatGPT wood atlas (Map3DWoodBuilder.Beam / cells). Units: metres, y up, z north, origin on the ground.
    /// </summary>
    public static class Map3DShedBuilder
    {
        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        /// <summary>Shed `w` x `d` m: corner posts, plank back / side walls, stepped gable boards, shingle roof with 0.3 m overhang, ridge beam, floor,
        /// two barrels at the front-left corner and two loose boards leaning on the east wall.</summary>
        public static Mesh Shed(float w, float d, int seed)
        {
            string key = "shed" + w + d + seed;
            Mesh m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            var rng = new System.Random(seed);
            var kit = new Map3DMeshKit();
            float hx = w * 0.5f, hz = d * 0.5f, eave = 1.95f, ridge = 2.95f;
            Quaternion id = Quaternion.identity;
            foreach (float x in new[] { -hx, hx })
                foreach (float z in new[] { -hz, hz })
                    Map3DWoodBuilder.Beam(kit, new Vector3(x, eave * 0.5f, z), new Vector3(0.1f, eave * 0.5f + 0.04f, 0.1f), id, Map3DWoodBuilder.Grain, 1f, rng, 0.65f, 0.3f);
            Map3DWoodBuilder.Beam(kit, new Vector3(0f, eave * 0.5f, hz - 0.02f), new Vector3(hx - 0.06f, eave * 0.5f - 0.02f, 0.035f), id, Map3DWoodBuilder.Planks, 1.6f, rng, 0.72f);
            foreach (float x in new[] { -hx + 0.02f, hx - 0.02f })
            {
                Map3DWoodBuilder.Beam(kit, new Vector3(x, eave * 0.5f, 0f), new Vector3(0.035f, eave * 0.5f - 0.02f, hz - 0.06f), id, Map3DWoodBuilder.Planks, 1.6f, rng, 0.72f);
                for (int k = 0; k < 5; k++) // gable: boards stepping up to the ridge
                {
                    float t = (k + 0.5f) / 5f;
                    Map3DWoodBuilder.Beam(kit, new Vector3(x, eave + (ridge - eave) * t, 0f), new Vector3(0.03f, (ridge - eave) / 10f + 0.01f, hz * (1f - t) + 0.05f), id, Map3DWoodBuilder.Planks, 1.6f, rng, 0.9f);
                }
            }
            foreach (float z in new[] { -hz, hz }) // wall plates under the eaves
                Map3DWoodBuilder.Beam(kit, new Vector3(0f, eave - 0.02f, z), new Vector3(hx + 0.12f, 0.08f, 0.09f), id, Map3DWoodBuilder.Grain, 1.2f, rng, 0.85f);
            Map3DWoodBuilder.Beam(kit, new Vector3(-hx + 0.45f, eave - 0.4f, -hz), new Vector3(0.45f, 0.05f, 0.04f), Quaternion.AngleAxis(40f, Vector3.forward), Map3DWoodBuilder.Grain, 1f, rng, 0.85f); // knee braces
            Map3DWoodBuilder.Beam(kit, new Vector3(hx - 0.45f, eave - 0.4f, -hz), new Vector3(0.45f, 0.05f, 0.04f), Quaternion.AngleAxis(-40f, Vector3.forward), Map3DWoodBuilder.Grain, 1f, rng, 0.85f);
            Map3DWoodBuilder.Beam(kit, new Vector3(0f, 0.035f, 0f), new Vector3(hx - 0.08f, 0.035f, hz - 0.08f), id, Map3DWoodBuilder.Planks, 1.6f, rng, 0.7f); // floor
            // roof: one slab of shingles per side, 0.3 m past the walls on every edge
            float run = hz + 0.3f, rise = (ridge - eave) * run / hz, slope = Mathf.Atan2(rise, run) * Mathf.Rad2Deg, half = Mathf.Sqrt(run * run + rise * rise) * 0.5f;
            foreach (float s in new[] { -1f, 1f }) // the north slab is turned 180 deg so its shingle tabs also hang downhill
            {
                Vector3 c = new Vector3(0f, ridge - rise * 0.5f + 0.07f, s * run * 0.5f);
                Map3DWoodBuilder.Beam(kit, c, new Vector3(hx + 0.32f, 0.05f, half), Quaternion.AngleAxis(s * slope, Vector3.right) * Quaternion.AngleAxis(s > 0f ? 180f : 0f, Vector3.up), Map3DWoodBuilder.Shingles, 1.3f, rng, 0.8f, 0.07f); // moss only in patches: the painted shingles carry their own
            }
            Map3DWoodBuilder.Beam(kit, new Vector3(0f, ridge + 0.1f, 0f), new Vector3(hx + 0.38f, 0.07f, 0.1f), id, Map3DWoodBuilder.Grain, 1.2f, rng, 0.85f); // ridge beam
            Barrel(kit, new Vector3(-hx + 0.25f, 0f, -hz - 0.4f), 0.32f, 0.82f, rng);
            Barrel(kit, new Vector3(-hx + 0.95f, 0f, -hz - 0.5f), 0.28f, 0.7f, rng);
            Map3DWoodBuilder.Beam(kit, new Vector3(hx + 0.3f, 0.85f, -0.4f), new Vector3(0.025f, 0.9f, 0.13f), Quaternion.AngleAxis(16f, Vector3.forward), Map3DWoodBuilder.Grain, 1f, rng, 0.75f);
            Map3DWoodBuilder.Beam(kit, new Vector3(hx + 0.26f, 0.8f, -0.12f), new Vector3(0.025f, 0.85f, 0.12f), Quaternion.AngleAxis(13f, Vector3.forward) * Quaternion.AngleAxis(6f, Vector3.up), Map3DWoodBuilder.Grain, 1f, rng, 0.75f);
            m = kit.Build("Map3DShed");
            Cache[key] = m;
            return m;
        }

        /// <summary>Barrel standing at p: bulging 12-sided stave tube with a lid (stave grain) and two darker hoops.</summary>
        static void Barrel(Map3DMeshKit kit, Vector3 p, float r, float h, System.Random rng)
        {
            var path = new[] { p, p + Vector3.up * h * 0.5f, p + Vector3.up * h };
            Color c = Map3DWoodBuilder.Col(0.85f, Map3DWoodBuilder.Grain), top = Map3DWoodBuilder.Col(1f, Map3DWoodBuilder.Grain); // one cell per face: alpha must not blend between cells
            kit.Tube(path, new[] { r * 0.88f, r * 1.04f, r * 0.9f }, 12, Map3DWoodBuilder.R(rng, 0f, 1f), 2.5f, 0.8f, new[] { c, c, top }, true);
            Color hoop = Map3DWoodBuilder.Col(0f, Map3DWoodBuilder.Grain);
            foreach (float t in new[] { 0.2f, 0.8f })
            {
                float rr = Mathf.Lerp(r * 0.88f, r * 1.04f, 1f - Mathf.Abs(t - 0.5f) * 2f) + 0.012f;
                kit.Tube(new[] { p + Vector3.up * (h * t - 0.03f), p + Vector3.up * (h * t + 0.03f) }, new[] { rr, rr }, 12, 0f, 2f, 3f, new[] { hoop, hoop }, false);
            }
        }

        /// <summary>Haystack: a lumpy straw dome (thatch cell, fibres running up) with a loose skirt at the foot.</summary>
        public static Mesh Haystack(float radius, float height, int seed)
        {
            string key = "hay" + radius + height + seed;
            Mesh m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            var rng = new System.Random(seed);
            var kit = new Map3DMeshKit();
            float[] t = { 0f, 0.12f, 0.35f, 0.6f, 0.82f, 0.96f, 1f };
            float[] rad = { 1.08f, 1.0f, 0.93f, 0.74f, 0.45f, 0.16f, 0.02f };
            var path = new Vector3[t.Length]; var rr = new float[t.Length]; var cols = new Color[t.Length];
            Vector3 lean = new Vector3(Map3DWoodBuilder.R(rng, -0.08f, 0.08f), 0f, Map3DWoodBuilder.R(rng, -0.08f, 0.08f));
            for (int k = 0; k < t.Length; k++)
            {
                path[k] = Vector3.up * height * t[k] + lean * t[k] * t[k];
                rr[k] = radius * rad[k] * Map3DWoodBuilder.R(rng, 0.97f, 1.03f);
                cols[k] = Map3DWoodBuilder.Col(Mathf.Lerp(0.7f, 1f, Mathf.Clamp01(t[k] * 2f)), Map3DWoodBuilder.Thatch);
            }
            kit.Tube(path, rr, 16, 0f, 4f, 1.1f, cols, false);
            m = kit.Build("Map3DHaystack");
            Cache[key] = m;
            return m;
        }

        /// <summary>Reed clump: three crossed upright cards (60 deg apart) of the painted reed sprite, base rooted, tips swaying.</summary>
        public static Mesh Reeds(float w, float h, int seed)
        {
            string key = "reeds" + w + h + seed;
            Mesh m;
            if (Cache.TryGetValue(key, out m) && m != null) return m;
            var rng = new System.Random(seed);
            var kit = new Map3DMeshKit();
            float yaw = Map3DWoodBuilder.R(rng, 0f, Mathf.PI);
            for (int c = 0; c < 3; c++)
            {
                float a = yaw + c * Mathf.PI / 3f;
                Vector3 right = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * w * 0.5f, up = Vector3.up * h * 0.5f;
                Vector3 outN = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                var nrm = new[] { (Vector3.up + outN * 0.4f).normalized, (Vector3.up + outN * 0.4f).normalized, (Vector3.up * 1.4f + outN * 0.3f).normalized, (Vector3.up * 1.4f + outN * 0.3f).normalized };
                float var = (float)rng.NextDouble();
                var col = new[] { new Color(0.75f, 0f, var, 0f), new Color(0.75f, 0f, var, 0f), new Color(1f, 1f, var, 0f), new Color(1f, 1f, var, 0f) };
                bool flip = c == 1; // mirror one card so the three do not repeat the same silhouette
                kit.Card(up, right, up, flip ? new Vector4(1f, 0f, 0f, 1f) : new Vector4(0f, 0f, 1f, 1f), nrm, col);
            }
            m = kit.Build("Map3DReeds");
            Cache[key] = m;
            return m;
        }
    }
}
