using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// World beyond the map edge on a 2.5D map (21/11/2026, camera vs RO3): the tilted camera sees past the north border (and the upper corners past the sides;
    /// View25DCamera.Clamp keeps only the screen centre line inside the map so the hero stays centred), where the flat fog-coloured background used to read
    /// as a horizon line behind the tree wall ("a stage with a backdrop", ChatGPT).
    /// RO3 never shows the end of the ground, so: every border chunk of the ground painting is mirrored across its edge (seamless, no new texture memory,
    /// half a map deep, lit and fogged like the ground) and the trees of the north / west / east border bands are mirrored beyond them as a forest continuation.
    /// Visual only: no collision, no shadows baked for them (they stand outside every reachable tile). Built with the map, only on a 2.5D map with the RO3 profile.
    /// </summary>
    public static class View25DGroundApron
    {
        /// <summary>Trees whose trunk is this many tiles or fewer from the north / west / east edge are mirrored beyond it.</summary>
        const int BorderTreeBandTiles = 9;

        /// <summary>Real 2.5D maps (RO3 profile); the Hydra arena prototype keeps its own camera and painted walls, no apron.</summary>
        static bool Wanted { get { return View25D.Active && View25D.Profile.Ro3Look; } }

        /// <summary>Mirrors the ground chunk (row r, col c of rows x cols, bottom-left at origin, chunkW x chunkH) across every map edge it touches (and the corners).</summary>
        public static void MirrorGroundChunk(Transform parent, Sprite sprite, Vector2 origin, float chunkW, float chunkH, int r, int c, int rows, int cols)
        {
            if (sprite == null || !Wanted) return;
            float mapW = GameConfig.MapW, mapH = GameConfig.MapH;
            for (int sy = -1; sy <= 1; sy++)
                for (int sx = -1; sx <= 1; sx++)
                {
                    if (sx == 0 && sy == 0) continue;
                    // row 0 is the top (north) chunk row; mirror north only from row 0, south only from the last row, same for the columns
                    if ((sy == 1 && r != 0) || (sy == -1 && r != rows - 1) || (sx == -1 && c != 0) || (sx == 1 && c != cols - 1)) continue;
                    // a chunk [a, a + w] mirrored across edge e lies on [2e - a - w, 2e - a]: with scale -1 the sprite (pivot bottom-left) grows from 2e - a backwards
                    float px = sx == 0 ? origin.x : sx < 0 ? -origin.x : 2f * mapW - origin.x;
                    float py = sy == 0 ? origin.y : sy < 0 ? -origin.y : 2f * mapH - origin.y;
                    SpriteRenderer sr = ProceduralSprites.Child(parent, "GroundApron_" + sx + "_" + sy + "_r" + r + "c" + c, sprite);
                    sr.sortingOrder = GameConfig.GroundOrder;
                    sr.transform.position = new Vector3(px, py, 0f);
                    sr.transform.localScale = new Vector3(sx == 0 ? 1f : -1f, sy == 0 ? 1f : -1f, 1f);
                    View25DLighting.ApplyGround(sr);
                }
        }

        /// <summary>Mirrors the standing trees of the north, west and east border bands across those edges (and both north corners): same type, flipped
        /// left-right, one tile farther out so the copy never stands on the border row, y-sorted like any prop. The south edge needs none (hard camera clamp).</summary>
        public static void MirrorBorderTrees(Transform parent, IGameMap map)
        {
            if (!Wanted) return;
            float mapW = GameConfig.MapW, mapH = GameConfig.MapH, t = GameConfig.TileUnits;
            foreach (MapProp p in map.Props)
            {
                PropDef d;
                if (!VillagePropDefs.TryGet(p.Type, out d)) continue;
                bool north = p.Ty <= BorderTreeBandTiles, west = p.Tx <= BorderTreeBandTiles, east = p.Tx + d.FootW >= map.Width - BorderTreeBandTiles;
                if (!north && !west && !east) continue;
                if (StaticPropShadows.Classify(p.Type, d) != StaticPropShadows.Shape.Tree) continue;
                Sprite s = MapScenery.PropSprite(p.Type);
                if (s == null) continue;
                float x = (p.Tx + d.FootW * 0.5f) * t, y = mapH - (p.Ty + 1) * t;
                float my = 2f * mapH - y + t, wx = -x - t, ex = 2f * mapW - x + t;
                if (north) Mirror(parent, p.Type, s, x, my);
                if (west) Mirror(parent, p.Type, s, wx, y);
                if (east) Mirror(parent, p.Type, s, ex, y);
                if (north && west) Mirror(parent, p.Type, s, wx, my);
                if (north && east) Mirror(parent, p.Type, s, ex, my);
            }
        }

        static void Mirror(Transform parent, string type, Sprite s, float x, float y)
        {
            SpriteRenderer sr = ProceduralSprites.Child(parent, type, s); // named by type: View25DLighting picks the tree material by name
            sr.flipX = true;
            sr.sortingOrder = GameConfig.OrderBase(y);
            sr.transform.position = new Vector3(x, y, 0f);
            View25D.StandProp(sr);
        }
    }
}
