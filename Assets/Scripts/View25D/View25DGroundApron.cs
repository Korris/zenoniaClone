using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// World beyond the map edge on a 2.5D map. RO3 never shows the end of the ground, and the camera stays centred on the hero at every edge
    /// (camera loop 21/11/2026, docs/reference/willowbrook-25d/camera-loop/round-1-chatgpt.md items 2-3), so past all four edges lies a render-only
    /// forest border: a tiled forest-floor grass (<ground>-border.png from Tools/build-map-25d-ground.py ... border, graded to the map's own outer grass)
    /// fading in over the outer 1.5 tiles of the map, and trees, bushes and rocks scattered on it (no road, paving, water or mirrored clumps:
    /// the first version mirrored the border ground and trees, which repeated the ruin paving at the north edge).
    /// No collision, no monsters, no baked shadows (nothing there is reachable). Built with the map, only on a 2.5D map with the RO3 profile.
    /// </summary>
    public static class View25DGroundApron
    {
        const float ApronTiles = 26f;    // ground past each edge: covers the view at the widest zoom (hero 55 px) with the hero at a corner
        const float FeatherTiles = 2f;   // the border ground fades in over this much of the map's outer edge (soft seam; ChatGPT round 2: 1.5 -> 2)
        const float ForestTiles = 22f;   // trees / bushes up to this far out (ChatGPT: at least 20 m)
        const float SouthClearTiles = 6f; // south edge: only bushes and rocks this close (a tall tree there would stand between the camera and the hero)
        const float PortalClearTiles = 6f; // no tree this close to an edge portal (the exit and the road past it must read as a way out)
        const float TreeArea = 25f, BushArea = 10f; // m² per big tree / per bush or rock (ChatGPT: 20-30, 8-12)
        const float NearBandTiles = 8f, NearTreeArea = 35f, NearBushArea = 12f; // thinner in the first 8 m past the edge (ChatGPT round 2)
        static readonly string[] Trees = { "wfOakTree", "wfOakTreeB", "wfOakYoungTree", "wfPineTree", "wfPineTree", "wfBirchTree" }; // the map's own forest set
        static readonly string[] Smalls = { "wbxBush", "wbxBush", "wbxBushWhite", "wbxBushLilac", "wbxRock", "wbxRocks", "wbxRockTiny", "wfBushWide" }; // no stumps: 4 tiles wide, a field of them looked logged

        /// <summary>Real 2.5D maps (RO3 profile); the Hydra arena prototype keeps its own camera and painted walls, no apron.</summary>
        public static bool Wanted { get { return View25D.Active && View25D.Profile.Ro3Look; } }

        /// <summary>Border ground + border forest of the map being built.</summary>
        public static void Build(Transform parent, IGameMap map)
        {
            if (!Wanted) return;
            BuildGround(parent, map);
            BuildForest(parent, map);
        }

        static void BuildGround(Transform parent, IGameMap map)
        {
            Texture2D tex = Resources.Load<Texture2D>(map.GroundTexture + "-border");
            if (tex == null) { Debug.LogWarning("[ZenoniaProto] 2.5D border ground missing: Resources/" + map.GroundTexture + "-border.png"); return; }
            tex.wrapMode = TextureWrapMode.Repeat;
            Material m = View25DLighting.GroundMaterialFor(tex);
            if (m == null) return;
            float t = GameConfig.TileUnits, w = GameConfig.MapW, h = GameConfig.MapH, a = ApronTiles * t, f = FeatherTiles * t;
            var go = new GameObject("GroundApron");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = View25DApronMesh.Frame(w, h, a, f, 16f * t);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = m;
            mr.sortingOrder = GameConfig.GroundOrder + 1; // over the map's ground (its feather), under flat decor (+4) and shadows (+8)
        }

        /// <summary>Scatters border trees (1 per TreeArea m²) and bushes / rocks (1 per BushArea m²) in the band past every edge, jittered on a grid,
        /// seeded by the map id (same forest every visit). Varied type, flip and scale ±10 %, never mirrored copies of the map's own clumps.</summary>
        static void BuildForest(Transform parent, IGameMap map)
        {
            var rng = new System.Random(map.Id.GetHashCode() & 0x7fffffff);
            float tw = map.Width, th = map.Height;
            Scatter(parent, map, rng, Mathf.Sqrt(TreeArea), Trees, tw, th, true);
            Scatter(parent, map, rng, Mathf.Sqrt(BushArea), Smalls, tw, th, false);
        }

        static void Scatter(Transform parent, IGameMap map, System.Random rng, float cell, string[] types, float tw, float th, bool trees)
        {
            for (float ty = -ForestTiles; ty < th + ForestTiles; ty += cell)
                for (float tx = -ForestTiles; tx < tw + ForestTiles; tx += cell)
                {
                    // tile coords, y down like the map rows; jitter inside the cell
                    float x = tx + (float)rng.NextDouble() * cell, y = ty + (float)rng.NextDouble() * cell;
                    string type = types[rng.Next(types.Length)];
                    bool flip = rng.Next(2) == 0;
                    float scale = 0.9f + (float)rng.NextDouble() * 0.2f;
                    float outX = Mathf.Max(-x, x - tw), outY = Mathf.Max(-y, y - th), outside = Mathf.Max(outX, outY);
                    if (outside < (trees ? 1f : 0.4f)) continue; // inside the map, or touching the border row
                    if (trees && y > th && y - th < SouthClearTiles) continue;
                    if (trees && NearPortal(map, x, y)) continue;
                    if (outside < NearBandTiles && rng.NextDouble() > (trees ? TreeArea / NearTreeArea : BushArea / NearBushArea)) continue;
                    Place(parent, type, x, y, flip, scale);
                }
        }

        static bool NearPortal(IGameMap map, float x, float y)
        {
            foreach (MapPortal p in map.Portals)
            {
                float dx = p.Tx + 0.5f - x, dy = p.Ty + 0.5f - y;
                if (dx * dx + dy * dy < PortalClearTiles * PortalClearTiles) return true;
            }
            return false;
        }

        /// <summary>One border prop at tile (x, y) (y down): tall ones stand up like map props (lighting, sway, fade), low ones lie on the ground.</summary>
        static void Place(Transform parent, string type, float x, float y, bool flip, float scale)
        {
            PropDef d;
            Sprite s = MapScenery.PropSprite(type);
            if (s == null || !VillagePropDefs.TryGet(type, out d)) return;
            float t = GameConfig.TileUnits, wx = x * t, wy = GameConfig.MapH - y * t;
            SpriteRenderer sr = ProceduralSprites.Child(parent, type, s); // named by type: View25DLighting picks the tree material by name
            sr.flipX = flip;
            sr.transform.position = new Vector3(wx, wy, 0f);
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            if (d.FootH == 0 && d.H <= MapScenery.FlatDecorMaxTiles) { sr.sortingOrder = GameConfig.GroundOrder + 4; View25DLighting.ApplyGround(sr); return; }
            // far north the y-sort order would sink under the ground (OrderBase < GroundOrder): clamp, equal orders sort by view depth
            sr.sortingOrder = Mathf.Max(GameConfig.OrderBase(wy), GameConfig.GroundOrder + 12);
            View25D.StandProp(sr);
        }
    }
}
