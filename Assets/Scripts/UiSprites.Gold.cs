using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Gold-trimmed dark-navy HUD art (procedural, 256px, anti-aliased): the button disc with a gold rim, the joystick base (gold ring + four
    /// diamond studs), the metal joystick knob with a gold rim, and the 9-sliced status panel frame. Matches the Bag / Settings button sprites.
    /// </summary>
    public static partial class UiSprites
    {
        const int Big = 256;
        static Sprite goldDisc, stickBase, stickKnob, panel;
        static readonly Color Outline = new Color(0.13f, 0.08f, 0.02f), Navy = new Color(0.05f, 0.09f, 0.16f);

        delegate Color PainterN(float x, float y, float d);

        static Texture2D MakeTexture(int n, PainterN p)
        {
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float fx = (x + 0.5f) / n * 2f - 1f, fy = (y + 0.5f) / n * 2f - 1f;
                    px[y * n + x] = p(fx, fy, Mathf.Sqrt(fx * fx + fy * fy));
                }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>Coverage (0..1) of a point with signed distance sd (negative inside, in -1..1 units) for a texture n px wide.</summary>
        static float Cov(float sd, int n) { return Mathf.Clamp01(-sd * n * 0.5f + 0.5f); }

        /// <summary>Composites src over dst (straight alpha) with an extra coverage factor.</summary>
        static Color Over(Color dst, Color src, float cov)
        {
            float a = src.a * cov, oa = a + dst.a * (1f - a);
            if (oa <= 0f) return new Color(0f, 0f, 0f, 0f);
            Color c = (src * a + dst * dst.a * (1f - a)) / oa;
            c.a = oa;
            return c;
        }

        /// <summary>Gold metal, t = 0 at the top (bright) to 1 at the bottom (deep bronze).</summary>
        static Color Gold(float t)
        {
            Color a = new Color(0.98f, 0.9f, 0.62f), b = new Color(0.86f, 0.64f, 0.26f), c = new Color(0.6f, 0.4f, 0.14f), e = new Color(0.42f, 0.27f, 0.09f);
            t = Mathf.Clamp01(t);
            return t < 0.3f ? Color.Lerp(a, b, t / 0.3f) : t < 0.7f ? Color.Lerp(b, c, (t - 0.3f) / 0.4f) : Color.Lerp(c, e, (t - 0.7f) / 0.3f);
        }

        static Sprite ToSprite(Texture2D tex, Vector4 border = default(Vector4))
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>HUD button disc: dark navy centre (d &lt; 0.8) inside a gold rim with dark outlines. Icons sit on the centre.</summary>
        public static Sprite GoldDisc()
        {
            if (goldDisc != null) return goldDisc;
            goldDisc = ToSprite(MakeTexture(Big, (x, y, d) =>
            {
                float t = (1f - y) * 0.5f;
                Color c = Over(new Color(0f, 0f, 0f, 0f), Outline, Cov(d - 1f, Big));
                c = Over(c, Gold(t), Cov(d - 0.95f, Big));
                c = Over(c, Outline, Cov(d - 0.82f, Big) * 0.9f);
                Color fill = Color.Lerp(new Color(0.1f, 0.16f, 0.26f), Navy, Mathf.Clamp01(t * 1.3f));
                fill.a = 0.94f;
                return Over(c, fill, Cov(d - 0.8f, Big));
            }));
            return goldDisc;
        }

        /// <summary>Floating joystick base: dark translucent disc, gold ring, four gold diamond studs (up / right / down / left) on the ring.</summary>
        public static Sprite JoystickBase()
        {
            if (stickBase != null) return stickBase;
            stickBase = ToSprite(MakeTexture(Big, (x, y, d) =>
            {
                Color c = new Color(0f, 0f, 0f, 0f);
                c = Over(c, Outline, Cov(d - 0.875f, Big));
                c = Over(c, Gold((1f - y) * 0.5f), Cov(d - 0.855f, Big));
                c = Over(c, Outline, Cov(d - 0.81f, Big) * 0.85f);
                c = Over(c, new Color(0.03f, 0.06f, 0.11f, 0.8f), Cov(d - 0.8f, Big));
                c = Over(c, new Color(0.9f, 0.7f, 0.3f, 0.35f), Cov(Mathf.Abs(d - 0.62f) - 0.006f, Big)); // faint inner guide ring
                for (int i = 0; i < 4; i++)
                {
                    float ox = i == 1 ? 0.875f : i == 3 ? -0.875f : 0f, oy = i == 0 ? 0.875f : i == 2 ? -0.875f : 0f;
                    float dx = x - ox, dy = y - oy, m = Mathf.Abs(dx) + Mathf.Abs(dy); // diamond: |dx| + |dy| = half-diagonal
                    float facet = Mathf.Clamp01(0.5f + (dy - dx) * 2.2f);              // light upper-left facet, darker lower-right
                    Color stud = Color.Lerp(Gold(0.6f), Gold(0f), facet);
                    c = Over(c, Outline, Cov(m - 0.132f, Big));
                    c = Over(c, stud, Cov(m - 0.118f, Big));
                }
                return c;
            }));
            return stickBase;
        }

        /// <summary>Polished steel joystick knob (glossy dark blue-grey sphere with a highlight) with a thin gold rim.</summary>
        public static Sprite JoystickKnob()
        {
            if (stickKnob != null) return stickKnob;
            stickKnob = ToSprite(MakeTexture(Big, (x, y, d) =>
            {
                Color c = Over(new Color(0f, 0f, 0f, 0f), Outline, Cov(d - 1f, Big));
                c = Over(c, Gold((1f - y) * 0.5f), Cov(d - 0.95f, Big));
                c = Over(c, Outline, Cov(d - 0.78f, Big) * 0.9f);
                float hx = x + 0.28f, hy = y - 0.32f, h = Mathf.Sqrt(hx * hx + hy * hy); // distance from the highlight
                Color steel = h < 0.55f ? Color.Lerp(new Color(0.72f, 0.8f, 0.88f), new Color(0.27f, 0.36f, 0.46f), h / 0.55f)
                                        : Color.Lerp(new Color(0.27f, 0.36f, 0.46f), new Color(0.08f, 0.12f, 0.19f), Mathf.Clamp01((h - 0.55f) / 0.7f));
                steel.a = 1f;
                return Over(c, steel, Cov(d - 0.76f, Big));
            }));
            return stickKnob;
        }

        /// <summary>
        /// 9-sliced HUD panel frame: translucent dark navy fill inside a rounded gold border (96px, border 24). Use Image.Type.Sliced with
        /// pixelsPerUnitMultiplier about 1.3 for a ~5 unit gold edge.
        /// </summary>
        public static Sprite Panel()
        {
            if (panel != null) return panel;
            const int n = 96;
            panel = ToSprite(MakeTexture(n, (x, y, d) =>
            {
                float px = Mathf.Abs(x) * n * 0.5f, py = Mathf.Abs(y) * n * 0.5f, half = n * 0.5f, r = 9f;
                float qx = px - (half - r), qy = py - (half - r);
                float sd = (Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r) * 2f / n; // rounded box
                Color c = Over(new Color(0f, 0f, 0f, 0f), Outline, Cov(sd, n));
                c = Over(c, Gold((1f - y) * 0.5f), Cov(sd + 1.5f * 2f / n, n));
                c = Over(c, Outline, Cov(sd + 5.5f * 2f / n, n) * 0.9f);
                return Over(c, new Color(0.04f, 0.08f, 0.15f, 0.84f), Cov(sd + 6.5f * 2f / n, n));
            }), new Vector4(24f, 24f, 24f, 24f));
            return panel;
        }
    }
}
