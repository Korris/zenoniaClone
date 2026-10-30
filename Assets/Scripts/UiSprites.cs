using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Generated round UI art for the touch controls, matching the web HUD buttons (hud-renderer.js disc()): a metal rim with a vertical
    /// light-dark-light gradient around a dark centre, a thin accent ring, a plain white disc (mask / cooldown pie) and the D-pad cross.
    /// All 128px, anti-aliased, built once.
    /// </summary>
    public static partial class UiSprites
    {
        const int Size = 128;
        static Sprite disc, ring, circle, dpad, dpadArm;

        delegate Color Painter(float x, float y, float d); // x, y in -1..1 (y up), d = distance from the centre

        static Sprite Paint(Painter p)
        {
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float fx = (x + 0.5f) / Size * 2f - 1f, fy = (y + 0.5f) / Size * 2f - 1f;
                    px[y * Size + x] = p(fx, fy, Mathf.Sqrt(fx * fx + fy * fy));
                }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Coverage of an edge at distance d from `edge` (1 inside, 0 outside, smooth over about one pixel).</summary>
        static float Inside(float d, float edge) { return Mathf.Clamp01((edge - d) * Size * 0.5f + 0.5f); }

        static Color Hex(string hex) { Color c; ColorUtility.TryParseHtmlString(hex, out c); return c; }

        /// <summary>Web disc(): metal rim (#eef5f7 -> #728b9b -> #293944 -> #bdd0d8 top to bottom) around a #10202f centre.</summary>
        public static Sprite Disc()
        {
            if (disc != null) return disc;
            Color a = Hex("#eef5f7"), b = Hex("#728b9b"), c = Hex("#293944"), e = Hex("#bdd0d8"), centre = Hex("#10202f");
            disc = Paint((x, y, d) =>
            {
                float t = (1f - y) * 0.5f; // 0 at the top
                Color metal = t < 0.4f ? Color.Lerp(a, b, t / 0.4f) : t < 0.65f ? Color.Lerp(b, c, (t - 0.4f) / 0.25f) : Color.Lerp(c, e, (t - 0.65f) / 0.35f);
                float rim = 1f - Inside(d, 0.9f);
                Color col = Color.Lerp(new Color(centre.r, centre.g, centre.b, 0.93f), metal, rim);
                col.a *= Inside(d, 1f);
                return col;
            });
            return disc;
        }

        /// <summary>Thin white ring just inside the rim (tint = the web accent colour: bright when ready, grey when not).</summary>
        public static Sprite Ring()
        {
            if (ring != null) return ring;
            ring = Paint((x, y, d) => new Color(1f, 1f, 1f, Inside(d, 0.82f) * (1f - Inside(d, 0.79f))));
            return ring;
        }

        /// <summary>Plain white disc: icon mask and the radial cooldown fill.</summary>
        public static Sprite Circle()
        {
            if (circle != null) return circle;
            circle = Paint((x, y, d) => new Color(1f, 1f, 1f, Inside(d, 1f)));
            return circle;
        }

        static float Cross(float x, float y)
        {
            // Signed-ish distance to a plus shape with rounded arm ends: arm half-width 0.3, length 0.95.
            const float half = 0.3f, len = 0.95f, round = 0.12f;
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
            float horiz = Mathf.Max(ax - (len - round), 0f), vert = Mathf.Max(ay - half + round, 0f);
            float dh = Mathf.Sqrt(horiz * horiz + vert * vert) - round;
            horiz = Mathf.Max(ay - (len - round), 0f); vert = Mathf.Max(ax - half + round, 0f);
            float dv = Mathf.Sqrt(horiz * horiz + vert * vert) - round;
            return Mathf.Min(dh, dv);
        }

        static float Arrow(float x, float y)
        {
            // Small triangle pointing up near the top of the cross (repeated for the other arms by symmetry).
            float best = 0f;
            for (int i = 0; i < 4; i++)
            {
                float rx = i == 0 ? x : i == 1 ? -x : i == 2 ? y : -y, ry = i == 0 ? y : i == 1 ? -y : i == 2 ? -x : x;
                float py = (ry - 0.55f) / 0.22f;  // 0 at the base, 1 at the tip
                if (py < 0f || py > 1f) continue;
                best = Mathf.Max(best, Mathf.Clamp01((0.16f * (1f - py) - Mathf.Abs(rx)) * Size * 0.5f + 0.5f));
            }
            return best;
        }

        /// <summary>D-pad: translucent dark cross with a light rim and four arrow heads.</summary>
        public static Sprite DPad()
        {
            if (dpad != null) return dpad;
            dpad = Paint((x, y, d) =>
            {
                float c = Cross(x, y);
                float cover = Mathf.Clamp01(-c * Size * 0.5f + 0.5f);
                float rim = Mathf.Clamp01((c + 0.05f) * Size * 0.5f + 0.5f);
                Color col = Color.Lerp(new Color(0.06f, 0.13f, 0.2f, 0.78f), new Color(0.75f, 0.85f, 0.9f, 0.9f), rim);
                col = Color.Lerp(col, new Color(0.9f, 0.96f, 1f, 0.95f), Arrow(x, y));
                col.a *= cover;
                return col;
            });
            return dpad;
        }

        /// <summary>White up-arm of the cross (rotate for the others): pressed-direction highlight.</summary>
        public static Sprite DPadArm()
        {
            if (dpadArm != null) return dpadArm;
            dpadArm = Paint((x, y, d) =>
            {
                float c = Cross(x, y);
                float cover = Mathf.Clamp01(-c * Size * 0.5f + 0.5f) * Mathf.Clamp01((y - 0.28f) * Size * 0.5f);
                return new Color(1f, 1f, 1f, cover);
            });
            return dpadArm;
        }
    }
}
