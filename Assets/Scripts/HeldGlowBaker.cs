using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// CPU bakes behind the enhance effect of a held weapon / offhand / companion (HeldEnhanceFx), made ONCE per art and shared by every band and
    /// hero: the bands only differ in how these are drawn, so reaching a new band costs no bake. The painted art itself is never touched
    /// (the old additive copy of the whole sprite washed it out to white, the stamped outline rings looked hard).
    ///   Shine  half-res mask for the SpriteShine shader: R how much light a pixel takes, G position along the weapon, A silhouette
    ///   Halo   quarter-res blurred silhouette, white so the renderer tints it; fades to zero before its padding ends, so it has no edge
    ///   Points where stars twinkle: the brightest spots of the art, well apart and biased away from the grip (sprite local units)
    /// Same pivot and world size as GearOverlays' sprite of the art, so both layers line up as children of it.
    /// </summary>
    public static class HeldGlowBaker
    {
        const int ShineScale = 2, HaloScale = 4;
        const int HaloPad = 12, HaloBlurRadius = 4, HaloBlurPasses = 3; // quarter-res px; three box passes reach exactly radius * passes = HaloPad
        const int MaxPoints = 8, PointScan = 4;
        const float PointGapPx = 28f, SweepSlant = 0.15f;

        public struct Baked { public Sprite Shine, Halo; public Vector2[] Points; public Color Glow; }
        static readonly Dictionary<string, Baked> Cache = new Dictionary<string, Baked>();

        public static void ResetState() { Cache.Clear(); }

        /// <summary>Effect layers of the enhanced art; Shine is null when the PNG is missing.</summary>
        public static Baked Get(string folder, OverlayEntry entry)
        {
            string path = "Sprites/" + folder + "/" + entry.name + "-enhanced";
            Baked b;
            if (Cache.TryGetValue(path, out b)) return b;
            b = new Baked();
            Texture2D tex = SpriteSheet.Load(path);
            if (tex != null)
            {
                if (!ColorUtility.TryParseHtmlString(entry.glow, out b.Glow)) b.Glow = new Color(0.35f, 0.48f, 1f);
                Color32[] src = SpriteSheet.ReadPixels(tex);
                b.Shine = BakeShine(src, tex.width, tex.height, entry.enhancedGrip);
                b.Halo = BakeHalo(src, tex.width, tex.height, entry.enhancedGrip);
                b.Points = PickPoints(src, tex.width, tex.height, entry.enhancedGrip);
            }
            Cache[path] = b;
            return b;
        }

        static float Luma(Color32 c) { return (c.r * 0.3f + c.g * 0.59f + c.b * 0.11f) / 255f; }

        static Sprite BakeShine(Color32[] src, int sw, int sh, GripInfo grip)
        {
            // Sweep axis: grip -> farthest opaque pixel, so the light runs the long way of any art (blade, barrel, companion).
            float gx = grip.gripX, gy = sh - grip.gripY, tipX = gx, tipY = gy + 1f, far = -1f;
            for (int y = 0; y < sh; y += 2)
                for (int x = 0; x < sw; x += 2)
                {
                    if (src[y * sw + x].a < 128) continue;
                    float dx = x - gx, dy = y - gy, d = dx * dx + dy * dy;
                    if (d > far) { far = d; tipX = x; tipY = y; }
                }
            float ax = tipX - gx, ay = tipY - gy, len = Mathf.Max(1f, Mathf.Sqrt(ax * ax + ay * ay));
            ax /= len; ay /= len;
            float min = float.MaxValue, max = float.MinValue;
            for (int y = 0; y < sh; y += 2)
                for (int x = 0; x < sw; x += 2)
                {
                    if (src[y * sw + x].a < 128) continue;
                    float along = (x - gx) * ax + (y - gy) * ay;
                    if (along < min) min = along;
                    if (along > max) max = along;
                }
            float range = Mathf.Max(1f, max - min);

            int w = (sw + ShineScale - 1) / ShineScale, h = (sh + ShineScale - 1) / ShineScale;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int a = 0;
                    float lum = 0f;
                    for (int oy = 0; oy < ShineScale; oy++)
                        for (int ox = 0; ox < ShineScale; ox++)
                        {
                            int sx = x * ShineScale + ox, sy = y * ShineScale + oy;
                            if (sx >= sw || sy >= sh) continue;
                            Color32 c = src[sy * sw + sx];
                            a += c.a;
                            lum += Luma(c) * c.a;
                        }
                    lum = a > 0 ? lum / a : 0f;
                    // Bright metal takes more light, but the near-white pixels take less: they have no headroom left and would clip to white.
                    float weight = Mathf.Lerp(0.5f, 1f, lum) * (1f - 0.55f * lum * lum * lum * lum);
                    float dx = x * ShineScale + 1f - gx, dy = y * ShineScale + 1f - gy;
                    float along = (dx * ax + dy * ay - min) / range, cross = (dy * ax - dx * ay) / range; // cross: slants the band like light on a bevel
                    px[y * w + x] = new Color32((byte)Mathf.RoundToInt(weight * 255f), (byte)Mathf.RoundToInt(Mathf.Clamp01(along + SweepSlant * cross) * 255f), 0, (byte)(a / (ShineScale * ShineScale)));
                }
            // linear: R / G are data, never colour-converted.
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true, true) { filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            // Grip is in PNG pixels from the top-left; an odd art size leaves one empty full-res column / row at the far end, hence w * scale.
            var pivot = new Vector2(grip.gripX / (float)(w * ShineScale), (sh - grip.gripY) / (float)(h * ShineScale));
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, GearOverlays.Ppu / ShineScale, 0, SpriteMeshType.FullRect);
        }

        static Sprite BakeHalo(Color32[] src, int sw, int sh, GripInfo grip)
        {
            int w = (sw + HaloScale - 1) / HaloScale + HaloPad * 2, h = (sh + HaloScale - 1) / HaloScale + HaloPad * 2;
            var a = new float[w * h];
            var b = new float[w * h];
            float perPixel = 1f / (255f * HaloScale * HaloScale);
            for (int y = 0; y < sh; y++)
                for (int x = 0; x < sw; x++)
                    a[(y / HaloScale + HaloPad) * w + x / HaloScale + HaloPad] += src[y * sw + x].a * perPixel;
            for (int pass = 0; pass < HaloBlurPasses; pass++)
            {
                BoxPass(a, b, w, h, 1, w);
                BoxPass(b, a, h, w, w, 1);
            }
            float peak = 0.001f;
            for (int i = 0; i < a.Length; i++) if (a[i] > peak) peak = a[i];
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a[i] / peak) * 255f)); // same strength on a thin dagger as on a shield
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var pivot = new Vector2((grip.gripX + HaloPad * HaloScale) / (float)(w * HaloScale), (sh - grip.gripY + HaloPad * HaloScale) / (float)(h * HaloScale));
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, GearOverlays.Ppu / HaloScale, 0, SpriteMeshType.FullRect);
        }

        /// <summary>One running-sum box blur along `lines` lines of `count` samples (stride between samples, lineStride between lines).</summary>
        static void BoxPass(float[] from, float[] to, int count, int lines, int stride, int lineStride)
        {
            float norm = 1f / (HaloBlurRadius * 2 + 1);
            for (int l = 0; l < lines; l++)
            {
                int start = l * lineStride;
                float sum = 0f;
                for (int i = 0; i <= HaloBlurRadius && i < count; i++) sum += from[start + i * stride];
                for (int i = 0; i < count; i++)
                {
                    to[start + i * stride] = sum * norm;
                    int add = i + HaloBlurRadius + 1, sub = i - HaloBlurRadius;
                    if (add < count) sum += from[start + add * stride];
                    if (sub >= 0) sum -= from[start + sub * stride];
                }
            }
        }

        static Vector2[] PickPoints(Color32[] src, int sw, int sh, GripInfo grip)
        {
            var points = new List<Vector2>(MaxPoints);
            float gx = grip.gripX, gy = sh - grip.gripY, reach = Mathf.Max(sw, sh), gap = PointGapPx / GearOverlays.Ppu;
            for (int pick = 0; pick < MaxPoints; pick++)
            {
                float best = -1f;
                var bestPoint = Vector2.zero;
                for (int y = 0; y < sh; y += PointScan)
                    for (int x = 0; x < sw; x += PointScan)
                    {
                        Color32 c = src[y * sw + x];
                        if (c.a < 250) continue;
                        float dx = x - gx, dy = y - gy;
                        float score = Luma(c) + 0.25f * Mathf.Sqrt(dx * dx + dy * dy) / reach;
                        if (score <= best) continue;
                        var p = new Vector2(dx / GearOverlays.Ppu, dy / GearOverlays.Ppu);
                        bool near = false;
                        for (int i = 0; i < points.Count && !near; i++) near = (points[i] - p).sqrMagnitude < gap * gap;
                        if (near) continue;
                        best = score;
                        bestPoint = p;
                    }
                if (best < 0f) break;
                points.Add(bestPoint);
            }
            return points.ToArray();
        }
    }
}
