using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Shaping options of a ClericLightFx layer (2026-11-21, richer Spirit Blessing / Divine Aegis receive burst, RO3 rule 4 "strong" skill):
    ///  - Reveal: a ground ring / sigil draws itself in clockwise (LightBlob _Reveal) over its first `ms`, a bright head on the sweep front;
    ///  - Pop: the size goes W0 -> W1 over `ms` with a small overshoot (ease-out back) and then holds W1: an emblem popping in;
    ///  - Drop: the layer starts `dy` units higher and falls onto its place over `ms`, dipping ~10 % past it and settling (a dome dropping);
    ///  - Phases: brightness in RO3's four phases on top of the envelope: lead-in at `lead` (25-45 %) until `burstMs`, a 70 ms rise to 100 %,
    ///    held until `peakEndMs`, then 120 ms down to `hold` (45-70 %) until the envelope fades it out.
    /// All times are ms from the layer's own start.
    /// </summary>
    public sealed partial class ClericLightFx
    {
        public ClericLightFx Reveal(float ms) { if (layers.Count > 0) Last.RevealMs = Mathf.Max(1f, ms); return this; }

        public ClericLightFx Pop(float ms) { if (layers.Count > 0) Last.PopMs = Mathf.Max(1f, ms); return this; }

        public ClericLightFx Drop(float dy, float ms)
        {
            if (layers.Count == 0) return this;
            Layer l = Last;
            l.Offset += new Vector2(0f, dy); l.Move = new Vector2(0f, -dy); l.DropMs = Mathf.Max(1f, ms);
            return this;
        }

        public ClericLightFx Phases(float burstMs, float peakEndMs, float lead, float hold)
        {
            if (layers.Count == 0) return this;
            Layer l = Last;
            l.PhBurst = Mathf.Max(0f, burstMs); l.PhPeakEnd = Mathf.Max(burstMs + 70f, peakEndMs); l.PhLead = lead; l.PhHold = hold;
            return this;
        }

        static float PhaseGain(Layer l, float local)
        {
            if (l.PhBurst < 0f) return 1f;
            if (local < l.PhBurst) return l.PhLead;
            if (local < l.PhBurst + 70f) return Mathf.Lerp(l.PhLead, 1f, (local - l.PhBurst) / 70f);
            if (local < l.PhPeakEnd) return 1f;
            return Mathf.Lerp(1f, l.PhHold, Mathf.SmoothStep(0f, 1f, (local - l.PhPeakEnd) / 120f));
        }

        /// <summary>Ease-out back on 0..1 (peaks ~1.1 near 0.6, ends at 1); 1 past the end.</summary>
        static float PopCurve(float u)
        {
            if (u >= 1f) return 1f;
            float g = Mathf.Clamp01(u) - 1f;
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * g * g * g + c1 * g * g;
        }
    }
}
