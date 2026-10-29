using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Frost Hollow ground: the web's single painted backdrop (36x24 tiles, 64 source px per 16-unit tile) as one sprite, plus an
    /// optional F4 debug overlay that tints the solid tiles (T / C / w) so collision can be compared with the painting.
    /// </summary>
    public static class WorldBuilder
    {
        static SpriteRenderer solidOverlay;

        public static void Build(Transform parent)
        {
            Texture2D tex = SpriteSheet.Load("Sprites/tiles/frost-hollow-ground");
            Sprite sprite = tex != null
                ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, tex.width / GameConfig.MapW, 0, SpriteMeshType.FullRect)
                : ProceduralSprites.White(false);
            SpriteRenderer sr = ProceduralSprites.Child(parent, "Ground", sprite);
            sr.sortingOrder = GameConfig.GroundOrder;
            if (tex == null)
            {
                sr.color = new Color(0.82f, 0.9f, 0.95f);
                sr.transform.localScale = new Vector3(GameConfig.MapW, GameConfig.MapH, 1f);
                sr.transform.position = new Vector3(GameConfig.MapW * 0.5f, GameConfig.MapH * 0.5f, 0f);
            }
            else sr.transform.position = Vector3.zero;
            BuildSolidOverlay(parent);
        }

        /// <summary>F4: show / hide the collision tint.</summary>
        public static void ToggleSolidOverlay()
        {
            if (solidOverlay != null) solidOverlay.enabled = !solidOverlay.enabled;
        }

        static void BuildSolidOverlay(Transform parent)
        {
            var px = new Color32[FrostHollowMap.TilesW * FrostHollowMap.TilesH];
            for (int ty = 0; ty < FrostHollowMap.TilesH; ty++)
            {
                for (int tx = 0; tx < FrostHollowMap.TilesW; tx++)
                {
                    bool solid = FrostHollowMap.IsSolidTile(tx, ty);
                    px[(FrostHollowMap.TilesH - 1 - ty) * FrostHollowMap.TilesW + tx] = solid ? new Color32(255, 40, 40, 110) : new Color32(0, 0, 0, 0);
                }
            }
            var tex = new Texture2D(FrostHollowMap.TilesW, FrostHollowMap.TilesH, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 1f / FrostHollowMap.Tile, 0, SpriteMeshType.FullRect);
            solidOverlay = ProceduralSprites.Child(parent, "SolidOverlay", s);
            solidOverlay.sortingOrder = FeelConfig.FxOrder - 100;
            solidOverlay.enabled = false;
        }
    }
}
