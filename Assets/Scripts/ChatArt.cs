using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Procedural chat sprites: the speech-bubble glyph of the chat button and the left-to-right fade strip behind collapsed lines.</summary>
    public static class ChatArt
    {
        const int Size = 64;
        static Sprite bubble, fade;
        static Font textFont;

        /// <summary>
        /// Font of the chat text. The browser build has no OS fonts behind Unity's built-in one, so Vietnamese letters like ọ ư ờ vanished
        /// from the chat there; it uses the bundled Inter (SIL OFL, Resources/Fonts/Inter-Regular.ttf, full Vietnamese). Elsewhere the usual UI font.
        /// </summary>
        public static Font TextFont()
        {
            if (textFont != null) return textFont;
            if (Application.platform == RuntimePlatform.WebGLPlayer) textFont = Resources.Load<Font>("Fonts/Inter-Regular");
            if (textFont == null) textFont = UiKit.Font;
            return textFont;
        }

        /// <summary>White speech bubble (rounded body, tail bottom-left) with three dark dots cut out; tint it with Image.color.</summary>
        public static Sprite Bubble()
        {
            if (bubble != null) return bubble;
            var tex = NewTex(Size, Size);
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float body = Box(fx, fy, 6f, 18f, 58f, 56f, 12f);
                    float tail = Tail(fx, fy);
                    float a = Mathf.Max(body, tail);
                    float dots = 0f;
                    for (int d = 0; d < 3; d++) dots = Mathf.Max(dots, Mathf.Clamp01(4.6f - Vector2.Distance(new Vector2(fx, fy), new Vector2(20f + d * 12f, 37f)) + 0.5f));
                    byte v = (byte)(255f * (1f - dots * 0.85f));
                    px[y * Size + x] = new Color32(v, v, v, (byte)(255f * a));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            bubble = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            return bubble;
        }

        /// <summary>Horizontal strip, opaque on the left, fading to clear on the right (tint with Image.color).</summary>
        public static Sprite Fade()
        {
            if (fade != null) return fade;
            const int w = 64;
            var tex = NewTex(w, 4);
            var px = new Color32[w * 4];
            for (int x = 0; x < w; x++)
            {
                float t = x / (w - 1f), a = t < 0.55f ? 1f : 1f - (t - 0.55f) / 0.45f;
                for (int y = 0; y < 4; y++) px[y * w + x] = new Color32(255, 255, 255, (byte)(255f * a * a));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            fade = Sprite.Create(tex, new Rect(0, 0, w, 4), new Vector2(0.5f, 0.5f), 100f);
            return fade;
        }

        static Texture2D NewTex(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        }

        static float Box(float x, float y, float x0, float y0, float x1, float y1, float r)
        {
            float cx = Mathf.Clamp(x, x0 + r, x1 - r), cy = Mathf.Clamp(y, y0 + r, y1 - r);
            float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            return Mathf.Clamp01(r - d + 0.5f);
        }

        /// <summary>Triangle (12,24) (26,22) (8,6): the tail under the body, pointing down-left.</summary>
        static float Tail(float x, float y)
        {
            Vector2 a = new Vector2(12f, 24f), b = new Vector2(28f, 22f), c = new Vector2(8f, 5f), p = new Vector2(x, y);
            float d1 = Edge(p, a, b), d2 = Edge(p, b, c), d3 = Edge(p, c, a);
            bool neg = d1 < 0f || d2 < 0f || d3 < 0f, pos = d1 > 0f || d2 > 0f || d3 > 0f;
            return neg && pos ? 0f : 1f;
        }

        static float Edge(Vector2 p, Vector2 a, Vector2 b) { return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y); }
    }
}
