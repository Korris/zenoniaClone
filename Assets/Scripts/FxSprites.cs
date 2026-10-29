using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Generated FX textures: soft dot (flash quad / spark particle) and thin ring (shock wave). Both 1 world unit wide.</summary>
    public static class FxSprites
    {
        const int Size = 64;
        static Sprite dot, ring;
        static Texture2D dotTex;

        public static Texture2D DotTexture { get { Dot(); return dotTex; } }

        public static Sprite Dot()
        {
            if (dot != null) return dot;
            dotTex = Make(d => Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f));
            dot = Sprite.Create(dotTex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size, 0, SpriteMeshType.FullRect);
            return dot;
        }

        public static Sprite Ring()
        {
            if (ring != null) return ring;
            // Bell-shaped band centred at 85% of the radius, crisp enough to read as a shock ring.
            Texture2D tex = Make(d => Mathf.Clamp01(1f - Mathf.Abs(d - 0.85f) / 0.13f));
            ring = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size, 0, SpriteMeshType.FullRect);
            return ring;
        }

        /// <summary>Builds a white texture whose alpha is f(normalised distance from centre, 0 centre .. 1 edge).</summary>
        static Texture2D Make(System.Func<float, float> alphaOfDistance)
        {
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = (x + 0.5f - Size * 0.5f) / (Size * 0.5f);
                    float dy = (y + 0.5f - Size * 0.5f) / (Size * 0.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alphaOfDistance(d)) * 255f));
                }
            }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            return tex;
        }
    }
}
