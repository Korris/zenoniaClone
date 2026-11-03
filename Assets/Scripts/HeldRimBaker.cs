using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Band just OUTSIDE an enhanced item's silhouette, baked once per art from a chamfer distance field
    /// (a 250 px blade is ~95 screen px at 720p, so one art px is ~0.4 screen px): R closeness to the art (1 at its edge), G position along
    /// the item, A 1 inside the band. Two shaders draw it: SpriteRim as a crisp rim whose width grows per band (EnhanceLook.RimWidth),
    /// SpriteAura as animated energy tongues flowing towards the tip (the +15 signature).
    /// The painted art itself is never touched.
    /// </summary>
    public static class HeldRimBaker
    {
        const float AuraPx = 22f;   // art px reach of the +15 aura (~8 screen px)
        const int Pad = 23;         // room for the aura around an art that touches its own texture edge

        public static Sprite BakeAura(Color32[] src, int sw, int sh, GripInfo grip)
        {
            float gx, gy, ax, ay, min, range;
            HeldGlowBaker.Axis(src, sw, sh, grip, out gx, out gy, out ax, out ay, out min, out range);
            int w = sw + Pad * 2, h = sh + Pad * 2;
            float[] dist = Distance(src, sw, sh, w, h);
            const float reach = AuraPx;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float d = dist[y * w + x];
                    if (d <= 0f || d >= reach) continue; // outside only: the painted art stays untouched
                    int sx = x - Pad, sy = y - Pad;
                    float along = Mathf.Clamp01(((sx - gx) * ax + (sy - gy) * ay - min) / range);
                    float r = 1f - d / reach, a = Mathf.Clamp01((reach - d) / 2f);
                    px[y * w + x] = new Color32((byte)Mathf.RoundToInt(r * 255f), (byte)Mathf.RoundToInt(along * 255f), 0, (byte)Mathf.RoundToInt(a * 255f));
                }
            // linear: R / G are data, never colour-converted.
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, true, true) { filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            var pivot = new Vector2((grip.gripX + Pad) / (float)w, (sh - grip.gripY + Pad) / (float)h);
            return Sprite.Create(tex, new Rect(0, 0, w, h), pivot, GearOverlays.Ppu, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Two-pass chamfer distance (art px) from every padded pixel to the nearest opaque art pixel; 0 on the art.</summary>
        static float[] Distance(Color32[] src, int sw, int sh, int w, int h)
        {
            const float Diag = 1.4142f, Far = 1e6f;
            var d = new float[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int sx = x - Pad, sy = y - Pad;
                    bool on = sx >= 0 && sy >= 0 && sx < sw && sy < sh && src[sy * sw + sx].a >= 128;
                    d[y * w + x] = on ? 0f : Far;
                }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    float v = d[i];
                    if (x > 0) v = Mathf.Min(v, d[i - 1] + 1f);
                    if (y > 0)
                    {
                        v = Mathf.Min(v, d[i - w] + 1f);
                        if (x > 0) v = Mathf.Min(v, d[i - w - 1] + Diag);
                        if (x < w - 1) v = Mathf.Min(v, d[i - w + 1] + Diag);
                    }
                    d[i] = v;
                }
            for (int y = h - 1; y >= 0; y--)
                for (int x = w - 1; x >= 0; x--)
                {
                    int i = y * w + x;
                    float v = d[i];
                    if (x < w - 1) v = Mathf.Min(v, d[i + 1] + 1f);
                    if (y < h - 1)
                    {
                        v = Mathf.Min(v, d[i + w] + 1f);
                        if (x < w - 1) v = Mathf.Min(v, d[i + w + 1] + Diag);
                        if (x > 0) v = Mathf.Min(v, d[i + w - 1] + Diag);
                    }
                    d[i] = v;
                }
            return d;
        }
    }
}
