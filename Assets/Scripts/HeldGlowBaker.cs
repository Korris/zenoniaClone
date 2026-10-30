using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Enhance glow of a held weapon / offhand, a port of the web held-weapon-glow-baker.js: crisp stepped outline rings of the art's
    /// silhouette in the tier's glow colour (manifest "glow"), baked ONCE per (art, band) on the CPU. The blade itself keeps its painted
    /// colours (the old additive copy of the whole sprite washed it out to white).
    ///   band 1 (+7)  thin colour rim            band 2 (+9)  colour edge + white inner line
    ///   band 3 (+11) adds two outer rings       band 4 (+13) as 3, but the outer rings are a separate aura sprite that pulses
    /// Sprites are Pad px larger on every side, pivot on the grip, same 100 ppu as GearOverlays.
    /// </summary>
    public static class HeldGlowBaker
    {
        public const int Pad = 12;
        const int Steps = 12;

        struct Ring { public float R, A, White; public Ring(float r, float a, float white = 0f) { R = r; A = a; White = white; } }
        static readonly Ring[] Inner1 = { new Ring(2f, 0.55f, 0.5f) };
        static readonly Ring[] Inner2 = { new Ring(3f, 0.85f), new Ring(1f, 0.9f, 1f) };
        static readonly Ring[] Outer = { new Ring(8f, 0.2f), new Ring(5f, 0.4f) };

        public struct Baked { public Sprite Body, Aura; }
        static readonly Dictionary<string, Baked> Cache = new Dictionary<string, Baked>();

        public static void ResetState() { Cache.Clear(); }

        /// <summary>Glowing enhanced art for the band (1..4); Body is null when the PNG is missing.</summary>
        public static Baked Get(string folder, OverlayEntry entry, int band)
        {
            band = Mathf.Clamp(band, 1, 4);
            string path = "Sprites/" + folder + "/" + entry.name + "-enhanced";
            string key = path + "|" + band;
            Baked b;
            if (Cache.TryGetValue(key, out b)) return b;
            b = new Baked();
            Texture2D tex = SpriteSheet.Load(path);
            if (tex != null)
            {
                Color glow;
                if (!ColorUtility.TryParseHtmlString(entry.glow, out glow)) glow = new Color(0.35f, 0.48f, 1f);
                Color32[] src = SpriteSheet.ReadPixels(tex);
                int w = tex.width + Pad * 2, h = tex.height + Pad * 2;
                Ring[] inner = band == 1 ? Inner1 : Inner2;
                var body = new Color[w * h];
                if (band == 3) Rings(body, w, h, src, tex.width, tex.height, Outer, glow);
                Rings(body, w, h, src, tex.width, tex.height, inner, glow);
                Art(body, w, src, tex.width, tex.height);
                b.Body = MakeSprite(body, w, h, entry.enhancedGrip);
                if (band >= 4)
                {
                    var aura = new Color[w * h];
                    Rings(aura, w, h, src, tex.width, tex.height, Outer, glow);
                    // The aura is additive: keep it OUTSIDE the silhouette, or it brightens the blade itself to white (with bloom).
                    for (int y = 0; y < tex.height; y++)
                        for (int x = 0; x < tex.width; x++)
                            if (src[y * tex.width + x].a > 0) aura[(y + Pad) * w + x + Pad].a = 0f;
                    b.Aura = MakeSprite(aura, w, h, entry.enhancedGrip);
                }
            }
            Cache[key] = b;
            return b;
        }

        /// <summary>Source-over of `top` (alpha k) onto `dst`.</summary>
        static Color Over(Color dst, Color top, float k)
        {
            float a = top.a * k, outA = a + dst.a * (1f - a);
            if (outA <= 0f) return new Color(0f, 0f, 0f, 0f);
            Color c = (top * a + dst * dst.a * (1f - a)) / outA;
            c.a = outA;
            return c;
        }

        /// <summary>For each ring: the silhouette tinted in the glow colour (mixed toward white), stamped at Steps offsets around a circle.</summary>
        static void Rings(Color[] dst, int w, int h, Color32[] src, int sw, int sh, Ring[] rings, Color glow)
        {
            foreach (Ring ring in rings)
            {
                Color tint = Color.Lerp(glow, Color.white, ring.White);
                tint.a = 1f;
                for (int s = 0; s < Steps; s++)
                {
                    float ang = s * Mathf.PI * 2f / Steps;
                    int ox = Mathf.RoundToInt(Mathf.Cos(ang) * ring.R), oy = Mathf.RoundToInt(Mathf.Sin(ang) * ring.R);
                    for (int y = 0; y < sh; y++)
                    {
                        int ty = y + Pad + oy;
                        if (ty < 0 || ty >= h) continue;
                        for (int x = 0; x < sw; x++)
                        {
                            byte a = src[y * sw + x].a;
                            if (a == 0) continue;
                            int tx = x + Pad + ox;
                            if (tx < 0 || tx >= w) continue;
                            int i = ty * w + tx;
                            dst[i] = Over(dst[i], tint, ring.A * (a / 255f));
                        }
                    }
                }
            }
        }

        static void Art(Color[] dst, int w, Color32[] src, int sw, int sh)
        {
            for (int y = 0; y < sh; y++)
                for (int x = 0; x < sw; x++)
                {
                    Color32 c = src[y * sw + x];
                    if (c.a == 0) continue;
                    int i = (y + Pad) * w + x + Pad;
                    dst[i] = Over(dst[i], (Color)c, 1f);
                }
        }

        static Sprite MakeSprite(Color[] px, int w, int h, GripInfo grip)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(px);
            tex.Apply(true, true);
            // Grip is in PNG pixels from the top-left of the unpadded art; textures run bottom-up.
            var pivot = new Vector2((grip.gripX + Pad) / (float)w, (h - (grip.gripY + Pad)) / (float)h);
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, GearOverlays.Ppu, 0, SpriteMeshType.FullRect);
        }
    }
}
