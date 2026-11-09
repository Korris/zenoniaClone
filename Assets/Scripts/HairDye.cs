using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Code-side hair dye for layered heads (HeroHairLayers): the hair art is painted in neutral grays (outline #242424, shadow #555555,
    /// base #999999, highlight #E0E0E0); every near-gray pixel is remapped by its lightness onto a ramp built from the dye colour.
    /// Coloured pixels (a cyan lock, a navy ribbon) keep their own colour. Results are cached per sprite and colour.
    /// </summary>
    public static class HairDye
    {
        const int GrayChroma = 30; // max - min channel spread (0-255) still counted as gray hair
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>The sprite dyed with a colour (alpha ignored); the source sprite itself when dye is null or the source is null.</summary>
        public static Sprite Apply(Sprite src, Color32? dye)
        {
            if (src == null || !dye.HasValue) return src;
            Color32 d = dye.Value;
            string key = src.texture.GetInstanceID() + "|" + src.rect + "|" + d.r + "," + d.g + "," + d.b;
            Sprite done;
            if (Cache.TryGetValue(key, out done) && done != null) return done;

            Color32[] px = SpriteSheet.ReadPixels(src.texture);
            int texW = src.texture.width;
            var r = new RectInt((int)src.rect.x, (int)src.rect.y, (int)src.rect.width, (int)src.rect.height);
            var outPx = new Color32[r.width * r.height];
            for (int y = 0; y < r.height; y++)
                for (int x = 0; x < r.width; x++)
                {
                    Color32 c = px[(r.y + y) * texW + r.x + x];
                    outPx[y * r.width + x] = c.a == 0 ? c : Remap(c, d);
                }
            var tex = new Texture2D(r.width, r.height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = src.texture.filterMode };
            tex.SetPixels32(outPx);
            tex.Apply(false, true);
            Vector2 pivot = new Vector2(src.pivot.x / r.width, src.pivot.y / r.height);
            done = Sprite.Create(tex, new Rect(0, 0, r.width, r.height), pivot, src.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            Cache[key] = done;
            return done;
        }

        /// <summary>Gray lightness L onto the dye ramp: dark outline, dye at the base tone, toward white at the highlight.</summary>
        static Color32 Remap(Color32 c, Color32 d)
        {
            int max = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            if (max - min > GrayChroma) return c; // a coloured accent: keep it
            float l = (c.r + c.g + c.b) / (3f * 255f);
            Color dc = d;
            Color o;
            const float baseL = 0.6f; // #999999
            if (l <= baseL) o = Color.Lerp(dc * 0.16f, dc, Mathf.Pow(l / baseL, 0.9f)); // outline -> shadow -> base
            else o = Color.Lerp(dc, Color.white, (l - baseL) / (1f - baseL) * 0.75f); // base -> highlight
            o.a = 1f;
            Color32 o32 = o;
            o32.a = c.a;
            return o32;
        }
    }
}
