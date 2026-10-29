using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Loads PNGs from Resources and slices them into cached runtime Sprites (no editor sprite import needed).</summary>
    public static class SpriteSheet
    {
        static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
        static readonly Dictionary<string, Sprite[][]> Grids = new Dictionary<string, Sprite[][]>();

        /// <summary>Loads a texture (path relative to Resources, no extension). Returns null (and logs) when missing.</summary>
        public static Texture2D Load(string resourcePath)
        {
            Texture2D tex;
            if (Textures.TryGetValue(resourcePath, out tex) && tex != null) return tex;
            tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null)
            {
                Debug.LogError("[ZenoniaProto] Missing texture Resources/" + resourcePath + ".png");
                return null;
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = tex.mipmapCount > 1 ? FilterMode.Trilinear : FilterMode.Bilinear;
            Textures[resourcePath] = tex;
            return tex;
        }

        /// <summary>Slices a sheet into [row][col] sprites (row 0 = top row of the PNG). Null when the texture is missing.</summary>
        public static Sprite[][] Grid(string resourcePath, int cellW, int cellH, float ppu, Vector2 pivot)
        {
            string key = resourcePath + "|" + cellW + "x" + cellH + "|" + ppu + "|" + pivot.x + "," + pivot.y;
            Sprite[][] grid;
            if (Grids.TryGetValue(key, out grid) && grid != null) return grid;
            Texture2D tex = Load(resourcePath);
            if (tex == null || cellW <= 0 || cellH <= 0) return null;
            int cols = tex.width / cellW;
            int rows = tex.height / cellH;
            grid = new Sprite[rows][];
            for (int r = 0; r < rows; r++)
            {
                grid[r] = new Sprite[cols];
                for (int c = 0; c < cols; c++)
                {
                    // Unity texture space has y=0 at the bottom, PNG row 0 is the top.
                    var rect = new Rect(c * cellW, tex.height - (r + 1) * cellH, cellW, cellH);
                    grid[r][c] = Sprite.Create(tex, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
                }
            }
            Grids[key] = grid;
            return grid;
        }

        /// <summary>Slices a sheet with a known column/row count (cell size derived from the texture).</summary>
        public static Sprite[][] GridByCount(string resourcePath, int cols, int rows, float ppu, Vector2 pivot)
        {
            Texture2D tex = Load(resourcePath);
            if (tex == null) return null;
            return Grid(resourcePath, tex.width / cols, tex.height / rows, ppu, pivot);
        }

        /// <summary>Single sprite covering the whole texture.</summary>
        public static Sprite Whole(string resourcePath, float ppu, Vector2 pivot)
        {
            Texture2D tex = Load(resourcePath);
            if (tex == null) return null;
            Sprite[][] grid = Grid(resourcePath, tex.width, tex.height, ppu, pivot);
            return grid != null && grid.Length > 0 ? grid[0][0] : null;
        }

        /// <summary>Safe row lookup; null when the grid / row is missing.</summary>
        public static Sprite[] Row(Sprite[][] grid, int row)
        {
            if (grid == null || row < 0 || row >= grid.Length) return null;
            return grid[row];
        }

        /// <summary>Reads all pixels of any texture (readable or not) via a temporary RenderTexture. Bottom-left origin.</summary>
        public static Color32[] ReadPixels(Texture2D tex)
        {
            if (tex.isReadable) return tex.GetPixels32();
            RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            RenderTexture prev = RenderTexture.active;
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            copy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            Color32[] px = copy.GetPixels32();
            Object.Destroy(copy);
            return px;
        }
    }
}
