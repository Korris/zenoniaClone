using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Rocks and reeds along the sunken creek of Clover Meadows (ChatGPT RO3 loop round 2, "Outdoor Depth Solution V4.0" A: "đá lộ ở mép mỗi 3-5 m",
    /// "cụm lau / reeds: 6-10 cụm / 20 m chiều dài"). Render only and only inside the water tiles (Map3DHeightField.CreekDistance > 0), so nothing
    /// lands on walkable ground: one bank rock per 4 x 4 m cell (80 %) on the upper bank band, reed clumps near the waterline in 2.5 m cells (~45 %
    /// per bank cell = ~7-9 per 20 m of each bank). Seeded by the map id; meshes merged by Map3DBatcher like every other 3D prop.
    /// </summary>
    public static class Map3DCreekDressing
    {
        public static void Build(Transform parent, IGameMap map, Map3DHeightField f)
        {
            if (f.CreekDistance(0f, 0f) < 0f) return; // no sunken creek on this map
            var rng = new System.Random((map.Id.GetHashCode() & 0x7fffffff) ^ 0x2C2C);
            int rocks = Scatter(parent, f, rng, 4f, 0.25f, 0.7f, 0.8, true);
            int reeds = Map3DAssets.Reeds != null ? Scatter(parent, f, rng, 2.0f, 0.75f, 1.2f, 0.48, false) : 0; // round 3: groups of 2-4 with 3-5 m open bank between (ReedGroup), ~18 % fewer
            Debug.Log("[ZenoniaProto] Map3D creek dressing: " + rocks + " rocks, " + reeds + " reed clumps");
        }

        /// <summary>Reeds grow in groups: a 1-D noise along the creek (x) opens 3-5 m of bare bank between clumps of 2-4.</summary>
        static bool ReedGroup(float tx, float ty) { return Mathf.PerlinNoise(tx * 0.22f + 17.3f, ty * 0.05f + 4.1f) > 0.5f; }

        /// <summary>For each cell x cell m square, one random point of the band [dMin, dMax] m from the dry bank, kept with probability p.</summary>
        static int Scatter(Transform parent, Map3DHeightField f, System.Random rng, float cell, float dMin, float dMax, double p, bool rock)
        {
            int n = 0;
            for (float cy = 0f; cy < f.MapH; cy += cell)
                for (float cx = 0f; cx < f.MapW; cx += cell)
                {
                    float bx = 0f, by = 0f; int found = 0;
                    for (int s = 0; s < 24; s++) // reservoir pick among band points of the cell
                    {
                        float tx = cx + (float)rng.NextDouble() * cell, ty = cy + (float)rng.NextDouble() * cell;
                        float d = f.CreekDistance(tx, ty);
                        if (d < dMin || d > dMax) continue;
                        found++;
                        if (rng.Next(found) == 0) { bx = tx; by = ty; }
                    }
                    if (found == 0 || rng.NextDouble() > p) continue;
                    if (!rock && !ReedGroup(bx, by)) continue;
                    int hash = rng.Next() & 0x7fffffff;
                    Vector3 pos = Map3D.World(bx, by, f.HeightAt(bx, by)); pos.z += Map3DTerrain.Sink;
                    Quaternion rot = Quaternion.AngleAxis(hash % 360, Vector3.forward);
                    if (rock)
                    {
                        float s = 0.55f + (hash % 100) / 160f; // 0.55-1.2
                        Vector3 size = (hash >> 7) % 4 == 0 ? new Vector3(1.6f, 0.45f, 1.0f) : new Vector3(1.2f, 0.7f, 0.9f); // round 3: ~25 % flat slabs, the rest boulders
                        Map3DBatcher.Add(Map3DRockBuilder.Rock(size, hash % 6), Map3DAssets.Rock, Map3DAssets.Rock, pos, rot, s, true);
                    }
                    else Map3DBatcher.Add(Map3DShedBuilder.Reeds(1.2f, 1.4f, hash % 4), Map3DAssets.Reeds, Map3DAssets.Reeds, pos, rot, 0.75f + (hash % 30) / 100f, false);
                    n++;
                }
            return n;
        }
    }
}
