using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Builds the ground once into a single texture (no per-tile renderers, no seams): a grass field from the web game's
    /// seamless 2x2 grass macro with a dirt road band (auto-tile edges) across the middle.
    /// Source tiles: 64px = 16 world units (ppu 4).
    /// </summary>
    public static class WorldBuilder
    {
        const int Tile = 64;
        const int PathRow = 15; // vertical tile index of the road's centre row

        public static void Build(Transform parent)
        {
            Texture2D grass = SpriteSheet.Load("Sprites/tiles/grass");
            Texture2D path = SpriteSheet.Load("Sprites/tiles/path");
            int w = GameConfig.TilesW * Tile, h = GameConfig.TilesH * Tile;
            var ground = new Color32[w * h];
            if (grass == null)
            {
                var fallback = new Color32(86, 150, 60, 255);
                for (int i = 0; i < ground.Length; i++) ground[i] = fallback;
            }
            else
            {
                Color32[] g = SpriteSheet.ReadPixels(grass);
                Color32[] p = path != null ? SpriteSheet.ReadPixels(path) : null;
                for (int ty = 0; ty < GameConfig.TilesH; ty++)
                {
                    for (int tx = 0; tx < GameConfig.TilesW; tx++)
                    {
                        // Macro order: cell = (ty % 2) * 2 + (tx % 2), laid out along the first row of grass.png.
                        int cell = (ty % 2) * 2 + (tx % 2);
                        CopyTile(g, grass.width, grass.height, cell, 0, ground, w, tx * Tile, ty * Tile, false);
                        if (p == null) continue;
                        if (ty == PathRow) CopyTile(p, path.width, path.height, tx % 2 + 2 * (ty % 2), 3, ground, w, tx * Tile, ty * Tile, false);   // dirt macro row
                        else if (ty == PathRow + 1) CopyTile(p, path.width, path.height, 1, 0, ground, w, tx * Tile, ty * Tile, true);            // top edge (grass fringe)
                        else if (ty == PathRow - 1) CopyTile(p, path.width, path.height, 1, 2, ground, w, tx * Tile, ty * Tile, true);            // bottom edge
                    }
                }
            }

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(ground);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.zero, Tile / (float)GameConfig.TileUnits, 0, SpriteMeshType.FullRect);
            SpriteRenderer sr = ProceduralSprites.Child(parent, "Ground", sprite);
            sr.sortingOrder = GameConfig.GroundOrder;
            sr.transform.position = Vector3.zero;
        }

        /// <summary>Copies one 64px cell (col,row from the top-left of the PNG) into the ground at (dx,dy) bottom-left origin.</summary>
        static void CopyTile(Color32[] src, int srcW, int srcH, int col, int rowFromTop, Color32[] dst, int dstW, int dx, int dy, bool blend)
        {
            int sx0 = col * Tile;
            int sy0 = srcH - (rowFromTop + 1) * Tile;
            if (sx0 < 0 || sy0 < 0 || sx0 + Tile > srcW) return; // cell outside the sheet: skip
            for (int y = 0; y < Tile; y++)
            {
                int si = (sy0 + y) * srcW + sx0;
                int di = (dy + y) * dstW + dx;
                for (int x = 0; x < Tile; x++)
                {
                    Color32 s = src[si + x];
                    if (!blend || s.a == 255) { dst[di + x] = new Color32(s.r, s.g, s.b, 255); continue; }
                    if (s.a == 0) continue;
                    Color32 d = dst[di + x];
                    int a = s.a;
                    dst[di + x] = new Color32(
                        (byte)((s.r * a + d.r * (255 - a)) / 255),
                        (byte)((s.g * a + d.g * (255 - a)) / 255),
                        (byte)((s.b * a + d.b * (255 - a)) / 255), 255);
                }
            }
        }
    }
}
