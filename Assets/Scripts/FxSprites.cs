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

        static Texture2D chunkTex, shardTex;

        /// <summary>Irregular rock chunk (lit from the upper left), white-grey so particle colour tints it.</summary>
        public static Texture2D ChunkTexture
        {
            get
            {
                if (chunkTex != null) return chunkTex;
                var px = new Color32[Size * Size];
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float dx = (x + 0.5f - Size * 0.5f) / (Size * 0.5f), dy = (y + 0.5f - Size * 0.5f) / (Size * 0.5f);
                        float a = Mathf.Atan2(dy, dx);
                        float edge = 0.78f + 0.14f * Mathf.Sin(a * 3f + 0.7f) + 0.08f * Mathf.Sin(a * 5f + 2f);
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float lit = Mathf.Clamp01(0.62f + 0.38f * (-dx * 0.7f + dy * 0.7f));
                        byte v = (byte)Mathf.RoundToInt(255f * lit);
                        px[y * Size + x] = new Color32(v, v, v, d < edge ? (byte)255 : (byte)0);
                    }
                chunkTex = Finish(px);
                return chunkTex;
            }
        }

        /// <summary>Thin bright diamond (ice shard / spark), pointing along +X.</summary>
        public static Texture2D ShardTexture
        {
            get
            {
                if (shardTex != null) return shardTex;
                var px = new Color32[Size * Size];
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float dx = (x + 0.5f - Size * 0.5f) / (Size * 0.5f), dy = (y + 0.5f - Size * 0.5f) / (Size * 0.5f);
                        float d = Mathf.Abs(dx) + Mathf.Abs(dy) * 3.2f;
                        float a = Mathf.Clamp01((1f - d) / 0.2f);
                        px[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                    }
                shardTex = Finish(px);
                return shardTex;
            }
        }

        static Texture2D Finish(Color32[] px)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            return tex;
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
