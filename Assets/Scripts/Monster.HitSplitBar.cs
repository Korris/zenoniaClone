using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Cosmetic HP bar of the multi-hit numbers (SkillHitSplit, 21/11): while the parts of your last split hit are still popping, the bar shows
    /// the HP as if only the parts already shown had landed, so it steps down with each number. The real hp, the kill and the server are not
    /// touched; a newer split hit replaces the pending one (the bar catches up at once), a single-number hit leaves it. Wanted / boss bars read the real hp.
    /// </summary>
    public partial class Monster
    {
        int[] barParts;
        float barPartsFromMs, barPartsDelayMs, barPartsIntervalMs;

        void ShowBarParts(int[] parts, float delayMs, float intervalMs)
        {
            if (parts == null) return; // a single number (or a burn chip) lands at once: the parts still pending keep stepping (21/11)
            barParts = parts;
            barPartsFromMs = Time.unscaledTime * 1000f;
            barPartsDelayMs = delayMs;
            barPartsIntervalMs = intervalMs;
        }

        /// <summary>HP the bar draws: real hp plus the parts not shown yet (never above full).</summary>
        public float BarHp
        {
            get
            {
                if (barParts == null || state == State.Dying) return hp;
                float elapsed = Time.unscaledTime * 1000f - barPartsFromMs, pending = 0f;
                for (int i = barParts.Length - 1; i >= 0; i--)
                {
                    if (barPartsDelayMs + i * barPartsIntervalMs <= elapsed) break;
                    pending += barParts[i];
                }
                if (pending <= 0f) { barParts = null; return hp; }
                return Mathf.Min(def.Hp, hp + pending);
            }
        }
    }
}
