using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Forest on the terrain past the map edges of a 3D map (the 3D counterpart of View25DGroundApron; RO3 never shows the end of the world and the
    /// camera turns 360 deg): generated trees 1 per 30 m², bushes 1 per 22 m², rocks 1 per 60 m², jittered on a grid and seeded by the map id, kept
    /// 2.5 m away from every walkable tile / portal corridor and, within 6 m past the south edge, only bushes and rocks (a tall tree there stands
    /// between the default camera and the hero). Render only: no collision, no monsters.
    /// </summary>
    public static class Map3DBorderForest
    {
        const float TreeArea = 30f, BushArea = 22f, RockArea = 60f, SouthClear = 6f, KeepOff = 2.5f;
        static readonly Map3DTreeBuilder.Kind[] Trees =
        {
            Map3DTreeBuilder.Kind.Oak, Map3DTreeBuilder.Kind.Oak, Map3DTreeBuilder.Kind.Pine, Map3DTreeBuilder.Kind.Pine, Map3DTreeBuilder.Kind.Pine,
            Map3DTreeBuilder.Kind.OakYoung, Map3DTreeBuilder.Kind.Birch,
        };
        static readonly Map3DTreeBuilder.Kind[] Bushes = { Map3DTreeBuilder.Kind.Bush, Map3DTreeBuilder.Kind.Bush, Map3DTreeBuilder.Kind.BushLilac, Map3DTreeBuilder.Kind.BushWhite };

        public static void Build(Transform parent, IGameMap map, Map3DHeightField f)
        {
            var root = new GameObject("Map3DBorderForest").transform;
            root.SetParent(parent, false);
            var rng = new System.Random(map.Id.GetHashCode() & 0x7fffffff);
            Scatter(root, f, rng, TreeArea, 0);
            Scatter(root, f, rng, BushArea, 1);
            Scatter(root, f, rng, RockArea, 2);
        }

        static void Scatter(Transform root, Map3DHeightField f, System.Random rng, float area, int what)
        {
            float cell = Mathf.Sqrt(area), a = Map3DHeightField.Apron - 2f;
            for (float ty = -a; ty < f.MapH + a; ty += cell)
                for (float tx = -a; tx < f.MapW + a; tx += cell)
                {
                    float x = tx + (float)rng.NextDouble() * cell, y = ty + (float)rng.NextDouble() * cell;
                    int hash = rng.Next() & 0x7fffffff;
                    float outside = Mathf.Max(Mathf.Max(-x, x - f.MapW), Mathf.Max(-y, y - f.MapH));
                    if (outside < 0.6f) continue;                          // inside the map: its own prop table
                    if (f.DistanceToOpenAt(x, y) < KeepOff) continue;      // away from corridors / walkable edge tiles
                    if (what == 0 && y > f.MapH && y - f.MapH < SouthClear) continue;
                    if (what == 2)
                    {
                        Vector3 p = Map3D.World(x, y, f.HeightAt(x, y)); p.z += Map3DTerrain.Sink;
                        float s = 0.8f + (hash % 100) / 60f;
                        Map3DBatcher.Add(Map3DRockBuilder.Rock(new Vector3(1.4f, 0.9f, 1.1f), hash % 6), Map3DAssets.Rock, Map3DAssets.Rock, p, Quaternion.AngleAxis(hash % 360, Vector3.forward), s, false);
                        continue;
                    }
                    Map3DTreeBuilder.Kind k = what == 0 ? Trees[hash % Trees.Length] : Bushes[hash % Bushes.Length];
                    Map3DProps.PlaceKind(root, what == 0 ? "BorderTree" : "BorderBush", k, x, y, f, hash, outside < 6f); // only the first 6 m cast shadows
                }
        }
    }
}
