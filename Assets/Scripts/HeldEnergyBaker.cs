using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Energy layers of an enhanced item, baked once per art (HeldGlowBaker calls it):
    ///   Energy colour  the art's own glowing hue (most saturated bright hue, gold trim excluded), so a blue hammer glows blue
    ///                  even when its tier manifest still names an older colour. None when the art has no such hue (a black axe).
    ///   Inner mask     the pixels painted in that hue (lightning lines, frost veins, a molten core), SpriteShine format:
    ///                  R strength (quiet at the grip), G position along the item, A mask. +9 lights them, +11 runs a flow along them.
    ///   Crest point    beside the upper third of the blade, just outside the silhouette: where the +15 floating gem hovers.
    /// Tuned on the ten Warrior swords (prototype sheets 2026-11-03).
    /// </summary>
    public static class HeldEnergyBaker
    {
        const float GoldLo = 18f / 360f, GoldHi = 62f / 360f; // metal trim hues: never "energy"
        const int HueBins = 36;
        const float MinShare = 0.03f;                         // fewer energy pixels than this share of the art: no energy layer
        const float CrestAlong = 0.68f, CrestBand = 0.06f, CrestGapPx = 14f;

        public static Sprite BakeInner(Color32[] src, int sw, int sh, GripInfo grip, out Color energy, out bool found)
        {
            energy = Color.white;
            found = false;
            int n = sw * sh, opaque = 0, candidates = 0;
            var hue = new float[n];
            var sat = new float[n];
            var val = new float[n];
            var hist = new float[HueBins];
            for (int i = 0; i < n; i++)
            {
                if (src[i].a < 128) continue;
                opaque++;
                Color.RGBToHSV(src[i], out hue[i], out sat[i], out val[i]);
                if (!Candidate(hue[i], sat[i], val[i])) continue;
                candidates++;
                hist[Mathf.Min(HueBins - 1, (int)(hue[i] * HueBins))] += sat[i] * val[i];
            }
            if (opaque == 0 || candidates < MinShare * opaque) return null;

            int peak = 0;
            for (int k = 1; k < HueBins; k++) if (hist[k] > hist[peak]) peak = k;
            // Circular mean of the candidates near the peak bin: the energy hue.
            float centre = (peak + 0.5f) / HueBins, cx = 0f, cy = 0f;
            for (int i = 0; i < n; i++)
            {
                if (src[i].a < 128 || !Candidate(hue[i], sat[i], val[i]) || HueDeg(hue[i], centre) > 20f) continue;
                float w = sat[i] * val[i], ang = hue[i] * Mathf.PI * 2f;
                cx += Mathf.Cos(ang) * w; cy += Mathf.Sin(ang) * w;
            }
            float h = Mathf.Repeat(Mathf.Atan2(cy, cx) / (Mathf.PI * 2f), 1f);
            energy = Color.HSVToRGB(h, 0.85f, 1f);
            found = true;

            float gx, gy, ax, ay, min, range;
            HeldGlowBaker.Axis(src, sw, sh, grip, out gx, out gy, out ax, out ay, out min, out range);
            var px = new Color32[n];
            for (int y = 0; y < sh; y++)
                for (int x = 0; x < sw; x++)
                {
                    int i = y * sw + x;
                    if (src[i].a < 128 || Gold(hue[i])) continue;
                    float m = (1f - Smooth(18f, 35f, HueDeg(hue[i], h))) * Smooth(0.45f, 0.7f, sat[i]) * Smooth(0.55f, 0.85f, val[i]);
                    if (m <= 0.01f) continue;
                    float along = Mathf.Clamp01(((x - gx) * ax + (y - gy) * ay - min) / range);
                    float strength = 0.25f + 0.75f * Smooth(0.15f, 0.45f, along); // the grip wrap shares the hue but must stay quiet
                    px[i] = new Color32((byte)Mathf.RoundToInt(strength * 255f), (byte)Mathf.RoundToInt(along * 255f), 0, (byte)Mathf.RoundToInt(m * 255f));
                }
            // linear: R / G are data, never colour-converted.
            var tex = new Texture2D(sw, sh, TextureFormat.RGBA32, true, true) { filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            var pivot = new Vector2(grip.gripX / (float)sw, (sh - grip.gripY) / (float)sh);
            return Sprite.Create(tex, new Rect(0, 0, sw, sh), pivot, GearOverlays.Ppu, 0, SpriteMeshType.FullRect);
        }

        /// <summary>Where the +15 gem hovers, in sprite local units (like HeldGlowBaker points): on the blade axis at its upper third,
        /// pushed sideways past the widest opaque pixel of that stretch.</summary>
        public static Vector2 CrestPoint(Color32[] src, int sw, int sh, GripInfo grip)
        {
            float gx, gy, ax, ay, min, range;
            HeldGlowBaker.Axis(src, sw, sh, grip, out gx, out gy, out ax, out ay, out min, out range);
            float side = 0f;
            for (int y = 0; y < sh; y += 2)
                for (int x = 0; x < sw; x += 2)
                {
                    if (src[y * sw + x].a < 128) continue;
                    float dx = x - gx, dy = y - gy, along = (dx * ax + dy * ay - min) / range;
                    if (Mathf.Abs(along - CrestAlong) > CrestBand) continue;
                    side = Mathf.Max(side, dx * -ay + dy * ax); // distance to the left of the grip -> tip axis
                }
            float a = min + CrestAlong * range, c = side + CrestGapPx;
            return new Vector2((a * ax - c * ay) / GearOverlays.Ppu, (a * ay + c * ax) / GearOverlays.Ppu);
        }

        static bool Gold(float h) { return h > GoldLo && h < GoldHi; }
        static bool Candidate(float h, float s, float v) { return s > 0.5f && v > 0.6f && !Gold(h); }
        static float HueDeg(float a, float b) { float d = Mathf.Abs(a - b); return Mathf.Min(d, 1f - d) * 360f; }
        static float Smooth(float e0, float e1, float x) { float t = Mathf.Clamp01((x - e0) / (e1 - e0)); return t * t * (3f - 2f * t); }
    }
}
