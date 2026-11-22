using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Grass clumps on the 3D terrain (ChatGPT round 1, P0-1: "the terrain is empty, grass reads as a flat green plane: 4-7 clumps per 10 m²").
    /// Two crossed upright cards per clump (0.5-0.65 m wide, 0.3-0.42 m tall) from the ChatGPT grass-tuft atlas (Resources/Map3D/leaf-grass.png),
    /// normals pointing up so they light like the ground, base rooted (vertex g = 0) and tips swaying. On grass only (not on roads, paving, water),
    /// half as dense on the raised banks, a 6 m band past the map edge; seeded per map. Merged per cell (Map3DBatcher), no shadow casting.
    /// </summary>
    public static class Map3DGrass
    {
        const float PerM2 = 0.36f; // round 2: 2000 clumps read as repeating noise -> ~1300

        public static void Build(IGameMap map, Map3DHeightField f)
        {
            if (Resources.Load<Texture2D>("Map3D/leaf-grass") == null) return;
            var rng = new System.Random((map.Id.GetHashCode() & 0x7fffffff) ^ 0x5151);
            Material mat = Map3DAssets.Leaves("grass");
            var kit = new Map3DMeshKit();
            int clumps = 0;
            const float Chunk = 8f; // metres: one merged-cell piece per 8 x 8 m keeps culling useful
            for (float cy = -6f; cy < f.MapH + 6f; cy += Chunk)
                for (float cx = -6f; cx < f.MapW + 6f; cx += Chunk)
                {
                    kit = new Map3DMeshKit();
                    int n = Mathf.RoundToInt(Chunk * Chunk * PerM2);
                    for (int i = 0; i < n; i++)
                    {
                        float tx = cx + (float)rng.NextDouble() * Chunk, ty = cy + (float)rng.NextDouble() * Chunk;
                        bool inside = tx >= 0f && ty >= 0f && tx < f.MapW && ty < f.MapH;
                        if (inside && (f.Splat(tx, ty, 0) > 0.25f || f.Splat(tx, ty, 2) > 0.2f)) continue;
                        if (f.Splat(tx, ty, 3) > 0.03f) continue;
                        float road = inside ? f.Splat(tx, ty, 0) : 0f;                        // thin out toward the road edge (round 2: 15-25 % within 0.6 m, ~50 % within 1.2 m)
                        if (road > 0.08f && rng.NextDouble() > 0.2) continue;
                        if (road > 0.01f && rng.NextDouble() > 0.5) continue;
                        float d = f.DistanceToOpenAt(tx, ty);
                        if (d > 0.5f && rng.NextDouble() < 0.5) continue;
                        Clump(kit, rng, new Vector3(tx - cx, f.HeightAt(tx, ty), -(ty - cy)));
                        clumps++;
                    }
                    if (kit.Count == 0) continue;
                    Mesh m = kit.Build("Map3DGrass");
                    Vector3 o = Map3D.World(cx, cy, 0f); o.z += Map3DTerrain.Sink;
                    Map3DBatcher.Add(m, mat, mat, o, Quaternion.identity, 1f, false);
                }
            Debug.Log("[ZenoniaProto] Map3D grass: " + clumps + " clumps");
        }

        static readonly int[] Flowers = { 2, 7, 8, 14 }, Plain = { 0, 1, 3, 4, 5, 6, 9, 10, 11, 12, 13, 15 }; // cells of the ChatGPT tuft atlas

        /// <summary>One clump at p (local metres, y up, z north).</summary>
        static void Clump(Map3DMeshKit kit, System.Random rng, Vector3 p)
        {
            float sc = 0.75f + (float)rng.NextDouble() * 0.5f;                                  // scale 0.75-1.25
            float w = 0.34f * sc, h = 0.19f * sc;
            float yaw = (float)rng.NextDouble() * Mathf.PI;
            int cell = rng.NextDouble() < 0.10 ? Flowers[rng.Next(Flowers.Length)] : Plain[rng.Next(Plain.Length)]; // 10 % with flowers
            var nrm = new Vector3[4]; var col = new Color[4];
            float var = (float)rng.NextDouble();
            for (int k = 0; k < 4; k++) { nrm[k] = Vector3.up; bool top = k >= 2; col[k] = new Color(top ? 1f : 0.8f, top ? 1f : 0f, var, 0f); }
            for (int c = 0; c < 3; c++)
            {
                // three cards in a star, each leaning 35 deg outward: seen from the 52 deg camera a tuft spreads instead of reading as a thin sprig
                float a = yaw + c * Mathf.PI / 3f;
                Vector3 right = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * w;
                Vector3 outward = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) * (c % 2 == 0 ? 1f : -1f);
                float tilt = 0.57f + ((float)rng.NextDouble() - 0.5f) * 0.28f;                    // 35 deg +- 8
                Vector3 up = (Vector3.up * Mathf.Sqrt(1f - tilt * tilt) + outward * tilt) * h;
                kit.Card(p + up - Vector3.up * 0.02f, right, up, Map3DMeshKit.Cell(cell, 0f, 0.56f), nrm, col); // tufts fill the lower ~55 % of their cell
            }
        }
    }
}
