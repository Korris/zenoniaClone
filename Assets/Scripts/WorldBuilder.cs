using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Ground of the current map: the painted backdrop (64 source px per 16-unit tile) as one sprite, or for a big map several chunk sprites (GroundTexture-r{row}c{col}), its scenery (props,
    /// portals, villagers: MapScenery) and an optional F4 debug overlay that tints the solid tiles so collision can be compared with the painting.
    /// Build() runs once at start-up, Rebuild() after MapRegistry.Current changed (MapTransfer); everything lives under one "Map_&lt;id&gt;" object.
    /// </summary>
    public static class WorldBuilder
    {
        static SpriteRenderer solidOverlay;
        static Transform parent, mapRoot;
        static bool overlayOn;
        static IGameMap builtMap;

        public static void Build(Transform root)
        {
            parent = root;
            Rebuild();
        }

        /// <summary>Destroys the previous map's ground / scenery / NPCs and builds the current map's.</summary>
        public static void Rebuild()
        {
            if (parent == null) return;
            if (mapRoot != null) Object.Destroy(mapRoot.gameObject);
            IGameMap map = MapRegistry.Current;
            if (builtMap != null && builtMap.Id != map.Id) UnloadGround(builtMap); // the big backdrops of the map left behind would stay cached forever
            builtMap = map;
            mapRoot = new GameObject("Map_" + map.Id).transform;
            mapRoot.SetParent(parent, false);
            BuildGround(map);
            MapScenery.Build(mapRoot, map);
            BuildSolidOverlay(map);
        }

        /// <summary>Frees the backdrop textures (every chunk) and the minimap picture of a map that is no longer shown.</summary>
        static void UnloadGround(IGameMap map)
        {
            int cols = map.GroundChunkCols, rows = map.GroundChunkRows;
            if (cols * rows > 1)
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++) SpriteSheet.Unload(ChunkPath(map, r, c));
            else SpriteSheet.Unload(map.GroundTexture);
            SpriteSheet.Unload(map.GroundTexture + "-minimap");
        }

        static string ChunkPath(IGameMap map, int row, int col) { return map.GroundTexture + "-r" + row + "c" + col; }

        /// <summary>One "Ground" sprite for a 1x1 map, else a "Ground_r{row}c{col}" sprite per chunk laid out top-down (row 0 = top, y up).</summary>
        static void BuildGround(IGameMap map)
        {
            int cols = Mathf.Max(1, map.GroundChunkCols), rows = Mathf.Max(1, map.GroundChunkRows);
            float chunkW = GameConfig.MapW / cols, chunkH = GameConfig.MapH / rows;
            bool chunked = cols * rows > 1;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    Texture2D tex = SpriteSheet.Load(chunked ? ChunkPath(map, r, c) : map.GroundTexture);
                    Sprite sprite = tex != null
                        ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, tex.width / chunkW, 0, SpriteMeshType.FullRect)
                        : ProceduralSprites.White(false);
                    SpriteRenderer sr = ProceduralSprites.Child(mapRoot, chunked ? "Ground_r" + r + "c" + c : "Ground", sprite);
                    sr.sortingOrder = GameConfig.GroundOrder;
                    Vector2 origin = new Vector2(c * chunkW, GameConfig.MapH - (r + 1) * chunkH);
                    if (tex == null)
                    {
                        sr.color = new Color(0.82f, 0.9f, 0.95f);
                        sr.transform.localScale = new Vector3(chunkW, chunkH, 1f);
                        sr.transform.position = new Vector3(origin.x + chunkW * 0.5f, origin.y + chunkH * 0.5f, 0f);
                    }
                    else sr.transform.position = new Vector3(origin.x, origin.y, 0f);
                }
        }

        /// <summary>F4: show / hide the collision tint (kept across map changes).</summary>
        public static void ToggleSolidOverlay()
        {
            overlayOn = !overlayOn;
            if (solidOverlay != null) solidOverlay.enabled = overlayOn;
        }

        static void BuildSolidOverlay(IGameMap map)
        {
            int W = map.Width, H = map.Height;
            var px = new Color32[W * H];
            for (int ty = 0; ty < H; ty++)
                for (int tx = 0; tx < W; tx++)
                    px[(H - 1 - ty) * W + tx] = map.IsSolidTile(tx, ty) ? new Color32(255, 40, 40, 110) : new Color32(0, 0, 0, 0);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 1f / GameConfig.TileUnits, 0, SpriteMeshType.FullRect);
            solidOverlay = ProceduralSprites.Child(mapRoot, "SolidOverlay", s);
            solidOverlay.sortingOrder = FeelConfig.FxOrder - 100;
            solidOverlay.enabled = overlayOn;
        }
    }
}
