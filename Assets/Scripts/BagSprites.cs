using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Art for the bag window, matching the web bag-widgets.js: the painted frame (Resources/Sprites/ui/bag-frame, a copy of the web
    /// custom/bag-frame-v2.png) and generated 9-sliced sprites for panels, item slots, buttons (steel / blue / gold), glows, the lock and the
    /// portrait backdrop. Built once and cached.
    /// </summary>
    public static class BagSprites
    {
        public enum Tone { Steel, Blue, Gold }

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static void ResetState() { Cache.Clear(); }

        delegate Color Painter(float x, float y); // pixel centre, top-left origin

        static Sprite Paint(string key, int w, int h, int border, Painter p)
        {
            Sprite s;
            if (Cache.TryGetValue(key, out s) && s != null) return s;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[(h - 1 - y) * w + x] = p(x + 0.5f, y + 0.5f);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            Cache[key] = s;
            return s;
        }

        static Color Hex(string hex) { return LootSprites.Hex(hex); }

        /// <summary>Signed distance to a rounded rect inset by `inset` pixels (negative inside).</summary>
        static float RoundRect(float x, float y, int w, int h, float inset, float radius)
        {
            float hx = w * 0.5f - inset, hy = h * 0.5f - inset;
            float qx = Mathf.Abs(x - w * 0.5f) - (hx - radius), qy = Mathf.Abs(y - h * 0.5f) - (hy - radius);
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        static float Cover(float d) { return Mathf.Clamp01(0.5f - d); }                       // 1 inside, AA over one pixel
        static float Line(float d, float width = 1f) { return Mathf.Clamp01(width * 0.5f + 0.5f - Mathf.Abs(d)); }

        /// <summary>Source-over blend of `top` (alpha scaled by `k`) onto `under`.</summary>
        static Color Over(Color under, Color top, float k = 1f)
        {
            float a = top.a * k, outA = a + under.a * (1f - a);
            if (outA <= 0f) return Color.clear;
            Color c = (top * a + under * under.a * (1f - a)) / outA;
            c.a = outA;
            return c;
        }

        static Color Stops(float t, Color a, float ta, Color b, float tb, Color c, float tc, Color d)
        {
            if (t < ta) return a;
            if (t < tb) return Color.Lerp(a, b, (t - ta) / (tb - ta));
            if (t < tc) return Color.Lerp(b, c, (t - tb) / (tc - tb));
            return Color.Lerp(c, d, (t - tc) / (1f - tc));
        }

        /// <summary>Web bagPanel(): translucent blue gradient, #536b80 border and a dark inner line.</summary>
        public static Sprite Panel()
        {
            Color a = Hex("#253d57a8"), b = Hex("#182d4390"), c = Hex("#102235b8"), edge = Hex("#536b80"), inner = Hex("#07111f90");
            const int S = 32;
            return Paint("panel", S, S, 8, (x, y) =>
            {
                float d = RoundRect(x, y, S, S, 0.5f, 5f);
                Color col = Stops(y / S, a, 0f, a, 0.01f, b, 0.5f, c);
                col.a *= Cover(d);
                col = Over(col, inner, Line(RoundRect(x, y, S, S, 2.5f, 4f)));
                return Over(col, edge, Line(d));
            });
        }

        /// <summary>Web bagButton(): vertical tone gradient, border (#a1edff when selected), top highlight.</summary>
        public static Sprite Button(Tone tone, bool selected = false)
        {
            string[] c = tone == Tone.Gold ? new[] { "#f4d597", "#ad772d", "#9b672b", "#d5aa5e" }
                : tone == Tone.Blue ? new[] { "#91dfff", "#155aa5", "#18324f", "#456885" } : new[] { "#607c9b", "#233b57", "#18324f", "#456885" };
            Color c0 = Hex(c[0]), c1 = Hex(c[1]), c2 = Hex(c[2]), c3 = Hex(c[3]), inner = Hex("#07111f90"), hi = Hex("#ffffff38");
            Color edge = Hex(selected ? "#a1edff" : "#9babb9");
            const int W = 48, H = 32;
            return Paint("btn" + tone + selected, W, H, 9, (x, y) =>
            {
                float d = RoundRect(x, y, W, H, 0.5f, 5f);
                Color col = Stops(y / H, c0, 0f, c1, 0.18f, c2, 0.8f, c3);
                col.a = Cover(d);
                col = Over(col, inner, Line(RoundRect(x, y, W, H, 2.5f, 4f)));
                if (x > 4f && x < W - 4f && Mathf.Abs(y - 3.5f) < 0.6f) col = Over(col, hi);
                return Over(col, edge, Line(d));
            });
        }

        /// <summary>Web bagSlot() backing: dark blue gradient with a dark and a light inner frame (the outer border is SlotEdge, tinted).</summary>
        public static Sprite SlotBack()
        {
            Color a = Hex("#253e58"), b = Hex("#0d1d30"), c = Hex("#1e3951"), inner = Hex("#07111f90"), light = Hex("#b4c8da55");
            const int S = 64;
            return Paint("slotBack", S, S, 16, (x, y) =>
            {
                float t = (x + y) / (2f * S); // the web gradient runs corner to corner
                Color col = t < 0.45f ? Color.Lerp(a, b, t / 0.45f) : Color.Lerp(b, c, (t - 0.45f) / 0.55f);
                col.a = Cover(RoundRect(x, y, S, S, 0.5f, 5f));
                col = Over(col, inner, Line(RoundRect(x, y, S, S, 2.5f, 4f)));
                return Over(col, light, Line(RoundRect(x, y, S, S, 3.5f, 0f)));
            });
        }

        /// <summary>White 1px rounded border (tint = rarity colour, or #ffe7a5 when selected).</summary>
        public static Sprite SlotEdge()
        {
            const int S = 64;
            return Paint("slotEdge", S, S, 16, (x, y) => new Color(1f, 1f, 1f, Line(RoundRect(x, y, S, S, 0.5f, 5f))));
        }

        /// <summary>The two corner brackets of a slot (top-left, bottom-right), white.</summary>
        public static Sprite SlotBrackets()
        {
            const int S = 64;
            return Paint("slotBrackets", S, S, 16, (x, y) =>
            {
                float a = 0f;
                if (x < 14f && y < 14f) a = Mathf.Max(Line(x - 3.5f, 1.25f) * (y >= 3f && y <= 12f ? 1f : 0f), Line(y - 3.5f, 1.25f) * (x >= 3f && x <= 13f ? 1f : 0f));
                float rx = S - x, ry = S - y;
                if (rx < 14f && ry < 14f) a = Mathf.Max(Line(rx - 3.5f, 1.25f) * (ry >= 3f && ry <= 12f ? 1f : 0f), Line(ry - 3.5f, 1.25f) * (rx >= 3f && rx <= 13f ? 1f : 0f));
                return new Color(1f, 1f, 1f, a);
            });
        }

        /// <summary>Soft white halo around a rounded rect (canvas shadowBlur), drawn 10px larger than its target.</summary>
        public static Sprite Glow()
        {
            const int S = 64;
            return Paint("glow", S, S, 20, (x, y) =>
            {
                float d = RoundRect(x, y, S, S, 10f, 5f);
                return new Color(1f, 1f, 1f, d < 0f ? 0.7f : Mathf.Clamp01(1f - d / 10f) * 0.7f);
            });
        }

        /// <summary>Padlock for locked slots (white; tint #43596d).</summary>
        public static Sprite Lock()
        {
            const int S = 32;
            return Paint("lock", S, S, 0, (x, y) =>
            {
                float body = Mathf.Max(Mathf.Abs(x - 16f) - 6f, Mathf.Abs(y - 19f) - 5f); // 12 x 10 box under the shackle
                float dx = x - 16f, dy = y - 14f, r = Mathf.Sqrt(dx * dx + dy * dy);
                float shackle = dy <= 0f ? Line(r - 4.5f, 2.5f) : 0f;
                return new Color(1f, 1f, 1f, Mathf.Max(Cover(body), shackle));
            });
        }

        /// <summary>Downward triangle under the selected tab (#519de7, #a8eeff edge).</summary>
        public static Sprite TabArrow()
        {
            Color fill = Hex("#519de7"), edge = Hex("#a8eeff");
            const int W = 20, H = 10;
            return Paint("tabArrow", W, H, 0, (x, y) =>
            {
                float half = (1f - y / H) * (W * 0.5f - 1f), d = Mathf.Abs(x - W * 0.5f) - half;
                Color col = fill;
                col.a = Cover(d) * (y < H - 0.5f ? 1f : 0f);
                return Over(col, edge, Line(d, 2f) * (y < H ? 1f : 0f));
            });
        }

        /// <summary>Web bagPortraitBackdrop(): radial blue light and an isometric stone dais (218 x 306).</summary>
        public static Sprite PortraitBackdrop()
        {
            const int W = 218, H = 306;
            Color g0 = Hex("#98c9f14d"), g1 = Hex("#73a8d11c"), g2 = Hex("#619ad000"), dais = Hex("#0b1b2a66"), t0 = Hex("#52718a18"), t1 = Hex("#7892ab14"), seam = Hex("#0c1f3240");
            return Paint("backdrop", W, H, 0, (x, y) =>
            {
                float gx = x - W * 0.5f, gy = y - H * 0.44f, r = Mathf.Sqrt(gx * gx + gy * gy), t = Mathf.Clamp01((r - 8f) / (W * 0.72f - 8f));
                Color col = t < 0.6f ? Color.Lerp(g0, g1, t / 0.6f) : Color.Lerp(g1, g2, (t - 0.6f) / 0.4f);
                float ex = (x - W * 0.5f) / (W * 0.46f), ey = (y - H * 0.85f) / (H * 0.10f);
                if (ex * ex + ey * ey <= 1f)
                {
                    col = Over(col, dais);
                    float u = (x + 20f) / 22f + (y - H * 0.73f) / 7f, v = (x + 20f) / 22f - (y - H * 0.73f) / 7f; // diamond tile lattice
                    int cu = Mathf.FloorToInt(u * 0.5f), cv = Mathf.FloorToInt(v * 0.5f);
                    col = Over(col, ((cu + cv) & 1) == 1 ? t0 : t1);
                    float fu = Mathf.Abs(u * 0.5f - Mathf.Round(u * 0.5f)), fv = Mathf.Abs(v * 0.5f - Mathf.Round(v * 0.5f));
                    if (fu < 0.03f || fv < 0.03f) col = Over(col, seam);
                }
                return col;
            });
        }

        // ---- the painted frame ----

        public const int FrameTitleRows = 180; // web: the top 180px rail is drawn into the 60px title bar, the rest stretches over the body

        static Sprite FramePart(string key, bool title)
        {
            Sprite s;
            if (Cache.TryGetValue(key, out s) && s != null) return s;
            Texture2D tex = SpriteSheet.Load("Sprites/ui/bag-frame");
            if (tex == null) return null;
            Rect r = title ? new Rect(0, tex.height - FrameTitleRows, tex.width, FrameTitleRows) : new Rect(0, 0, tex.width, tex.height - FrameTitleRows);
            s = Sprite.Create(tex, r, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Cache[key] = s;
            return s;
        }

        public static Sprite FrameTitle() { return FramePart("frameTitle", true); }
        public static Sprite FrameBody() { return FramePart("frameBody", false); }

        /// <summary>The leather bag painted in the frame's top-left corner (Inventory tab icon).</summary>
        public static Sprite BagIcon()
        {
            Sprite s;
            if (Cache.TryGetValue("bagIcon", out s) && s != null) return s;
            Texture2D tex = SpriteSheet.Load("Sprites/ui/bag-frame");
            if (tex == null) return null;
            s = Sprite.Create(tex, new Rect(58, tex.height - 158, 148, 122), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Cache["bagIcon"] = s;
            return s;
        }
    }
}
