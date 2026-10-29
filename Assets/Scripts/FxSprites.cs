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

        static Texture2D chunkTex, shardTex, starTex, pillarTex;
        static Sprite star, pillar;

        /// <summary>Vertical light pillar, 1 unit wide x 4 tall, pivot bottom-centre: bright narrow core, soft sides, fading out at the top.</summary>
        public static Sprite Pillar()
        {
            if (pillar != null) return pillar;
            const int w = 32, h = 128;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = Mathf.Abs((x + 0.5f - w * 0.5f) / (w * 0.5f)), v = (y + 0.5f) / h;
                    float side = Mathf.Pow(Mathf.Clamp01(1f - u), 1.4f);
                    float top = 1f - Mathf.Pow(v, 3f);
                    float foot = Mathf.Clamp01(v / 0.04f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(side * top * foot) * 255f));
                }
            pillarTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            pillarTex.SetPixels32(px);
            pillarTex.filterMode = FilterMode.Bilinear;
            pillarTex.wrapMode = TextureWrapMode.Clamp;
            pillarTex.Apply(false, true);
            pillar = Sprite.Create(pillarTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), w, 0, SpriteMeshType.FullRect);
            return pillar;
        }

        /// <summary>Eight-point arcane star (4 long spikes on the axes + 4 on the diagonals), 1 world unit wide.</summary>
        public static Sprite Star()
        {
            if (star != null) return star;
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float dx = (x + 0.5f - Size * 0.5f) / (Size * 0.5f), dy = (y + 0.5f - Size * 0.5f) / (Size * 0.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy), a = Mathf.Atan2(dy, dx);
                    float spike = Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 4f)), 6f);             // 8 spikes
                    float longSpike = Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2f)), 14f) * 0.35f; // 4 of them reach further
                    float r = 0.16f + 0.62f * spike + longSpike;
                    px[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01((r - d) / 0.12f) * 255f));
                }
            starTex = Finish(px);
            star = Sprite.Create(starTex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size, 0, SpriteMeshType.FullRect);
            return star;
        }

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
