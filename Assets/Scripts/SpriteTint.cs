using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Runtime recolour of painted FX art (web tinted-sprite-cache): keeps the strength of every pixel but swaps the hue for a tint, while
    /// the near-white (white-hot core) pixels stay white, so a warm slash becomes a purple one and a blue orb becomes holy green-gold (a plain colour
    /// multiply would just darken it).
    /// </summary>
    public static class SpriteTint
    {
        /// <summary>New texture with the same alpha and luminance, coloured by tint.</summary>
        public static Texture2D Colorize(Texture2D src, Color tint)
        {
            Color32[] px = SpriteSheet.ReadPixels(src);
            for (int i = 0; i < px.Length; i++)
            {
                Color32 c = px[i];
                float bright = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) / 255f;   // overall strength of the pixel
                float white = Mathf.Min(c.r, Mathf.Min(c.g, c.b)) / 255f;    // how close to a white-hot core (saturated pixels have little)
                float hot = Smooth(0.88f, 1f, white); // only the near-white centre stays white (the painted FX art is cyan-white, so most pixels carry the tint)
                float k = Mathf.Min(1f, bright * 1.1f);
                px[i] = new Color32(
                    (byte)(255f * Mathf.Lerp(tint.r * k, 1f, hot)),
                    (byte)(255f * Mathf.Lerp(tint.g * k, 1f, hot)),
                    (byte)(255f * Mathf.Lerp(tint.b * k, 1f, hot)), c.a);
            }
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, true);
            tex.SetPixels32(px);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply(true, true);
            return tex;
        }

        /// <summary>HLSL-style smoothstep (Mathf.SmoothStep interpolates values, not an edge range).</summary>
        static float Smooth(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Slices a texture into [row][col] sprites (row 0 = top), like SpriteSheet.GridByCount but for an in-memory texture.</summary>
        public static Sprite[][] Slice(Texture2D tex, int cols, int rows, float ppu, Vector2 pivot)
        {
            int cw = tex.width / cols, ch = tex.height / rows;
            var grid = new Sprite[rows][];
            for (int r = 0; r < rows; r++)
            {
                grid[r] = new Sprite[cols];
                for (int c = 0; c < cols; c++)
                    grid[r][c] = Sprite.Create(tex, new Rect(c * cw, tex.height - (r + 1) * ch, cw, ch), pivot, ppu, 0, SpriteMeshType.FullRect);
            }
            return grid;
        }
    }
}
