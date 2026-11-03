using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// CPU bakes behind the enhance effect of a held weapon / offhand / companion (HeldEnhanceFx), made ONCE per art and shared by every band and
    /// hero: the bands only differ in how these are drawn, so reaching a new band costs no bake. The painted art itself is never touched
    /// (the old additive copy of the whole sprite washed it out to white, the stamped outline rings looked hard).
    ///   Shine  half-res mask for the SpriteShine shader: R how much light a pixel takes, G position along the weapon, A silhouette
    ///   Halo   half-res blurred silhouette, white so the renderer tints it; fades to zero before its padding ends, so it has no edge
    ///   Rim    thin ring just outside the silhouette, same mask format as Shine (HeldRimBaker)
    ///   Points where stars twinkle: the brightest spots of the art, well apart and biased away from the grip (sprite local units)
    /// Same pivot and world size as GearOverlays' sprite of the art, so both layers line up as children of it.
    /// </summary>
    public static class HeldGlowBaker
    {
        const int ShineScale = 2, HaloScale = 2;
        // Halo at half res, reaching 12 art px (~4-5 screen px at game zoom): a tight glow that hugs the weapon instead of a soft blob around it.
        const int HaloPad = 6, HaloBlurRadius = 2, HaloBlurPasses = 3; // half-res px; three box passes reach exactly radius * passes = HaloPad
        const int MaxPoints = 8, PointScan = 4;
        const float PointGapPx = 28f, SweepSlant = 0.15f;

        public struct Baked { public Sprite Shine, Halo, Aura, Inner; public Vector2[] Points; public Vector2 Crest, Tip, TipDir; public Color Glow; }
        static readonly Dictionary<string, Baked> Cache = new Dictionary<string, Baked>();
        static readonly Dictionary<Sprite, Baked> SpriteCache = new Dictionary<Sprite, Baked>();

        public static void ResetState() { Cache.Clear(); SpriteCache.Clear(); }

        /// <summary>Effect layers of the art the item draws with (enhanced art, or the base art when FeelConfig.EnhanceUsesBaseArt); Shine is null when the PNG is missing.</summary>
        public static Baked Get(string folder, OverlayEntry entry, bool enhancedArt = true)
        {
            string path = "Sprites/" + folder + "/" + entry.name + (enhancedArt ? "-enhanced" : "-base");
            Baked b;
            if (Cache.TryGetValue(path, out b)) return b;
            b = new Baked();
            Texture2D tex = SpriteSheet.Load(path);
            if (tex != null) b = Bake(tex, enhancedArt ? entry.enhancedGrip : entry.baseGrip, entry.glow);
            Cache[path] = b;
            return b;
        }

        /// <summary>Effect layers of art named by an anchor instead of the tier manifest (the mechanic's cannon per facing): pivot at its centre, as GearOverlays.Centered makes it.</summary>
        public static Baked Get(Sprite art, string glow)
        {
            Baked b;
            if (SpriteCache.TryGetValue(art, out b)) return b;
            Texture2D tex = art.texture;
            b = Bake(tex, new GripInfo { w = tex.width, h = tex.height, gripX = tex.width / 2, gripY = tex.height / 2 }, glow);
            SpriteCache[art] = b;
            return b;
        }

        static Baked Bake(Texture2D tex, GripInfo grip, string glow)
        {
            var b = new Baked();
            if (!ColorUtility.TryParseHtmlString(glow, out b.Glow)) b.Glow = new Color(0.35f, 0.48f, 1f);
            Color32[] src = SpriteSheet.ReadPixels(tex);
            b.Shine = BakeShine(src, tex.width, tex.height, grip);
            b.Halo = BakeHalo(src, tex.width, tex.height, grip);
            b.Aura = HeldRimBaker.BakeAura(src, tex.width, tex.height, grip);
            Color energy;
            bool found;
            b.Inner = HeldEnergyBaker.BakeInner(src, tex.width, tex.height, grip, out energy, out found);
            if (found) b.Glow = energy; // the art's own glow beats the manifest colour, which can lag behind a redrawn tier
            b.Crest = HeldEnergyBaker.CrestPoint(src, tex.width, tex.height, grip);
            float gx, gy, ax, ay, min, range;
            Axis(src, tex.width, tex.height, grip, out gx, out gy, out ax, out ay, out min, out range);
            b.TipDir = new Vector2(ax, ay); // sprite local, grip -> tip
            b.Tip = b.TipDir * ((min + range) / GearOverlays.Ppu); // the far end of the art on its long axis, where the +15 energy blade starts
            b.Points = PickPoints(src, tex.width, tex.height, grip);
            return b;
        }

        static float Luma(Color32 c) { return (c.r * 0.3f + c.g * 0.59f + c.b * 0.11f) / 255f; }

        static Sprite BakeShine(Color32[] src, int sw, int sh, GripInfo grip)
        {
            float gx, gy, ax, ay, min, range;
            Axis(src, sw, sh, grip, out gx, out gy, out ax, out ay, out min, out range);

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

        /// <summary>
        /// Long axis of the art in texture pixels (y up): grip (gx, gy), unit direction (ax, ay) towards the farthest opaque pixel, so light runs the
        /// long way of any art (blade, barrel, companion); min / range map a pixel's projection on it to 0..1 along the item.
        /// </summary>
        internal static void Axis(Color32[] src, int sw, int sh, GripInfo grip, out float gx, out float gy, out float ax, out float ay, out float min, out float range)
        {
            gx = grip.gripX; gy = sh - grip.gripY;
            float tipX = gx, tipY = gy + 1f, far = -1f;
            for (int y = 0; y < sh; y += 2)
                for (int x = 0; x < sw; x += 2)
                {
                    if (src[y * sw + x].a < 128) continue;
                    float dx = x - gx, dy = y - gy, d = dx * dx + dy * dy;
                    if (d > far) { far = d; tipX = x; tipY = y; }
                }
            ax = tipX - gx; ay = tipY - gy;
            float len = Mathf.Max(1f, Mathf.Sqrt(ax * ax + ay * ay));
            ax /= len; ay /= len;
            float lo = float.MaxValue, hi = float.MinValue;
            for (int y = 0; y < sh; y += 2)
                for (int x = 0; x < sw; x += 2)
                {
                    if (src[y * sw + x].a < 128) continue;
                    float along = (x - gx) * ax + (y - gy) * ay;
                    if (along < lo) lo = along;
                    if (along > hi) hi = along;
                }
            min = lo;
            range = Mathf.Max(1f, hi - lo);
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
