using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Small padlock sprite drawn procedurally (white, tinted by the Image): rounded body + shackle ring. Built once.</summary>
    public static class LockGlyph
    {
        const int Size = 48;
        static Sprite sprite;

        public static Sprite Get()
        {
            if (sprite != null) return sprite;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    // body: x 11..37, y 7..26 with 4 px rounded corners; shackle: ring centred (24, 26) outer r 10.5 / inner r 6.5, upper half only
                    float a = Mathf.Max(Box(x + 0.5f, y + 0.5f, 11f, 7f, 37f, 26f, 4f), Shackle(x + 0.5f, y + 0.5f));
                    px[y * Size + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            return sprite;
        }

        static float Box(float x, float y, float x0, float y0, float x1, float y1, float r)
        {
            float cx = Mathf.Clamp(x, x0 + r, x1 - r), cy = Mathf.Clamp(y, y0 + r, y1 - r);
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            return Mathf.Clamp01(r - d + 0.5f);
        }

        static float Shackle(float x, float y)
        {
            if (y < 25f) return 0f;
            float d = Mathf.Sqrt((x - 24f) * (x - 24f) + (y - 26f) * (y - 26f));
            return Mathf.Clamp01(Mathf.Min(10.5f - d, d - 6.5f) + 0.5f);
        }
    }
}
