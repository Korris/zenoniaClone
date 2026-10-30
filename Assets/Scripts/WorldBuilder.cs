using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Ground of the current map: the web's single painted backdrop (36x24 tiles, 64 source px per 16-unit tile) as one sprite, its scenery (props,
    /// portals, villagers: MapScenery) and an optional F4 debug overlay that tints the solid tiles so collision can be compared with the painting.
    /// Build() runs once at start-up, Rebuild() after MapRegistry.Current changed (MapTransfer); everything lives under one "Map_&lt;id&gt;" object.
    /// </summary>
    public static class WorldBuilder
    {
        static SpriteRenderer solidOverlay;
        static Transform parent, mapRoot;
        static bool overlayOn;

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
            mapRoot = new GameObject("Map_" + map.Id).transform;
            mapRoot.SetParent(parent, false);
            BuildGround(map);
            MapScenery.Build(mapRoot, map);
            BuildSolidOverlay(map);
        }

        static void BuildGround(IGameMap map)
        {
            Texture2D tex = SpriteSheet.Load(map.GroundTexture);
            Sprite sprite = tex != null
                ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, tex.width / GameConfig.MapW, 0, SpriteMeshType.FullRect)
                : ProceduralSprites.White(false);
            SpriteRenderer sr = ProceduralSprites.Child(mapRoot, "Ground", sprite);
            sr.sortingOrder = GameConfig.GroundOrder;
            if (tex == null)
            {
                sr.color = new Color(0.82f, 0.9f, 0.95f);
                sr.transform.localScale = new Vector3(GameConfig.MapW, GameConfig.MapH, 1f);
                sr.transform.position = new Vector3(GameConfig.MapW * 0.5f, GameConfig.MapH * 0.5f, 0f);
            }
            else sr.transform.position = Vector3.zero;
        }

        /// <summary>F4: show / hide the collision tint (kept across map changes).</summary>
        public static void ToggleSolidOverlay()
        {
            overlayOn = !overlayOn;
            if (solidOverlay != null) solidOverlay.enabled = overlayOn;
        }

        static void BuildSolidOverlay(IGameMap map)
        {
            const int W = TileMapBase.W, H = TileMapBase.H;
            var px = new Color32[W * H];
            for (int ty = 0; ty < H; ty++)
                for (int tx = 0; tx < W; tx++)
                    px[(H - 1 - ty) * W + tx] = map.IsSolidTile(tx, ty) ? new Color32(255, 40, 40, 110) : new Color32(0, 0, 0, 0);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 1f / TileMapBase.Tile, 0, SpriteMeshType.FullRect);
            solidOverlay = ProceduralSprites.Child(mapRoot, "SolidOverlay", s);
            solidOverlay.sortingOrder = FeelConfig.FxOrder - 100;
            solidOverlay.enabled = overlayOn;
        }
    }
}
