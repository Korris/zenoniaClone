using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The few shapes the RO Soft Fantasy look needs, painted in code (no PNGs): 9-sliced rounded rects (card, section, row, item cell, flat
    /// buttons, pill tabs, HUD plate) and the round close button. Colours are baked into the sprite so the windows' own Image.color tints
    /// (dimmed "can't afford" buttons, inactive tabs, rarity rings) still multiply on top. 1 sprite pixel = 1 canvas unit at multiplier 1.
    /// </summary>
    public static class RoSoftSprites
    {
        public enum Shape { Card, Section, Row, Cell, Primary, Secondary, Danger, Gold, TabOn, TabOff, Pill, SectionLocked, Disabled, Night, NightCell, Glass }

        static readonly Dictionary<Shape, Sprite> Cache = new Dictionary<Shape, Sprite>();
        static Sprite close;

        public static void ResetState() { Cache.Clear(); close = null; }

        public static bool IsButton(Shape s) { return s == Shape.Primary || s == Shape.Secondary || s == Shape.Danger || s == Shape.Gold || s == Shape.TabOn; }

        public static Sprite Get(Shape shape)
        {
            Sprite s;
            if (Cache.TryGetValue(shape, out s) && s != null) return s;
            s = Build(shape);
            Cache[shape] = s;
            return s;
        }

        /// <summary>The shape a sprite was painted as (false for any other sprite).</summary>
        public static bool ShapeOf(Sprite sprite, out Shape shape)
        {
            foreach (var kv in Cache) if (kv.Value == sprite) { shape = kv.Key; return true; }
            shape = Shape.Card;
            return false;
        }

        static Sprite Build(Shape shape)
        {
            Color w = RoSoftTheme.CloudWhite;
            switch (shape)
            {
                case Shape.Card: return RoundRect(A(w, RoSoftTheme.CardAlpha), Color.white, 1.5f, 20, 8, 0.16f, null);
                case Shape.Section: return RoundRect(A(Color.white, 0.94f), RoSoftTheme.Line, 1f, 16, 0, 0f, null);
                case Shape.Row: return RoundRect(Color.white, RoSoftTheme.Line, 1f, 12, 0, 0f, null);
                case Shape.Cell: return RoundRect(RoSoftTheme.Cell, RoSoftTheme.CellEdge, 1f, 10, 0, 0f, null);
                case Shape.Primary: return RoundRect(RoSoftTheme.Mint, RoSoftTheme.MintDark, 1f, 12, 0, 0f, RoSoftTheme.MintDark);
                case Shape.Secondary: return RoundRect(RoSoftTheme.Sky, RoSoftTheme.SkyDark, 1f, 12, 0, 0f, RoSoftTheme.SkyDark);
                case Shape.Danger: return RoundRect(RoSoftTheme.Coral, RoSoftTheme.CoralDark, 1f, 12, 0, 0f, RoSoftTheme.CoralDark);
                case Shape.Gold: return RoundRect(RoSoftTheme.Gold, RoSoftTheme.GoldDark, 1f, 12, 0, 0f, RoSoftTheme.GoldDark);
                case Shape.TabOn: return RoundRect(RoSoftTheme.Mint, RoSoftTheme.MintDark, 1f, 16, 0, 0f, null);
                case Shape.TabOff: return RoundRect(Color.white, RoSoftTheme.Line, 1f, 16, 0, 0f, null);
                case Shape.SectionLocked: return RoundRect(RoSoftTheme.MintMist, RoSoftTheme.Mint, 2f, 16, 0, 0f, null);
                case Shape.Disabled: return RoundRect(Hex("#E3E9EE"), Hex("#CDD7DF"), 1f, 12, 0, 0f, Hex("#CDD7DF"));
                case Shape.Pill: return RoundRect(A(Color.white, 0.92f), RoSoftTheme.Line, 1f, 16, 0, 0f, null);
                case Shape.Night: return RoundRect(A(RoSoftTheme.Night, RoSoftTheme.NightAlpha), A(RoSoftTheme.NightEdge, 0.9f), 1f, 14, 0, 0f, null);
                case Shape.NightCell: return RoundRect(A(RoSoftTheme.Night, 0.82f), A(RoSoftTheme.NightEdge, 0.9f), 1f, 10, 0, 0f, null);
                default: return RoundRect(A(w, 0.9f), Color.white, 1f, 12, 4, 0.12f, null); // Glass: toast / tooltip
            }
        }

        static Color A(Color c, float a) { c.a = a; return c; }
        static Color Hex(string h) { return RoSoftTheme.Hex(h); }

        /// <summary>Signed distance to a rounded rect of half size (hx, hy) and corner `r` centred on (cx, cy); negative inside.</summary>
        static float Sd(float x, float y, float cx, float cy, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(x - cx) - (hx - r), qy = Mathf.Abs(y - cy) - (hy - r);
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        static Color Over(Color under, Color top)
        {
            float a = top.a, outA = a + under.a * (1f - a);
            if (outA <= 0f) return Color.clear;
            Color c = (top * a + under * under.a * (1f - a)) / outA;
            c.a = outA;
            return c;
        }

        /// <summary>
        /// Rounded rect: `fill`, an `edgePx` outline, corner radius `r`, an optional soft drop shadow of `shadow` px (alpha `shadowA`, 2 px down)
        /// and an optional darker 2 px lip along the bottom (`lip`, the flat buttons of the style board). 9-slice border = shadow + r + 2.
        /// </summary>
        static Sprite RoundRect(Color fill, Color edge, float edgePx, int r, int shadow, float shadowA, Color? lip)
        {
            int b = shadow + r + 2, size = b * 2 + 4;
            float c = size * 0.5f, half = c - shadow;
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    Color col = Color.clear;
                    if (shadow > 0)
                    {
                        float sd = Sd(fx, fy, c, c + 2f, half, half, r);
                        float k = Mathf.Clamp01(1f - Mathf.Max(sd, 0f) / shadow);
                        col = new Color(0.12f, 0.2f, 0.27f, shadowA * k * k);
                    }
                    float d = Sd(fx, fy, c, c, half, half, r);
                    float cover = Mathf.Clamp01(0.5f - d);
                    if (cover > 0f)
                    {
                        Color body = fill;
                        if (lip.HasValue && Sd(fx, fy, c, c - 2f, half, half - 2f, r) > 0f && fy > c) body = lip.Value; // bottom lip
                        float e = Mathf.Clamp01(d + edgePx + 0.5f);                                                      // 1 on the outline
                        float bodyA = Mathf.Lerp(body.a, Mathf.Max(body.a, edge.a), e);
                        body = Color.Lerp(body, edge, e * edge.a);
                        body.a = bodyA * cover;
                        col = Over(col, body);
                    }
                    px[(size - 1 - y) * size + x] = col;
                }
            return Make(px, size, size, b);
        }

        /// <summary>Round close button: white disc, hairline rim, soft-ink X (48 px, drawn unsliced).</summary>
        public static Sprite Close()
        {
            if (close != null) return close;
            const int S = 48;
            float c = S * 0.5f, R = c - 3f;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float fx = x + 0.5f - c, fy = y + 0.5f - c, d = Mathf.Sqrt(fx * fx + fy * fy) - R;
                    Color col = new Color(0.12f, 0.2f, 0.27f, 0.18f * Mathf.Clamp01(1f - Mathf.Max(Mathf.Sqrt(fx * fx + (fy - 1.5f) * (fy - 1.5f)) - R, 0f) / 3f));
                    float cover = Mathf.Clamp01(0.5f - d);
                    if (cover > 0f)
                    {
                        Color body = Color.Lerp(Color.white, RoSoftTheme.Line, Mathf.Clamp01(d + 2f));
                        float arm = Mathf.Max(Mathf.Abs(fx), Mathf.Abs(fy)) <= 8.5f ? 1f : 0f;
                        float lineD = Mathf.Min(Mathf.Abs(fx - fy), Mathf.Abs(fx + fy)) * 0.7071f;
                        body = Color.Lerp(body, RoSoftTheme.InkSoft, arm * Mathf.Clamp01(1.9f - lineD));
                        body.a = cover;
                        col = Over(col, body);
                    }
                    px[(S - 1 - y) * S + x] = col;
                }
            close = Make(px, S, S, 0);
            return close;
        }

        static Sprite Make(Color[] px, int w, int h, int border)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }
    }
}
