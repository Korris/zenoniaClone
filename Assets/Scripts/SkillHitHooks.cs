using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The single damage modifier hook every monster hit passes through (Monster.TakeHit, authority only): the mark multiplier, Death Mark charges
    /// (only the marker's own hits count and spend them) and the attacker's NextHitsPct buff. Damage over time (quiet hits) benefits from a mark
    /// but never spends charges or buffs. Overcharge energy (phase 11) joins here.
    /// </summary>
    public static class SkillHitHooks
    {
        public static int Modify(HeroController src, Monster target, int dmg, bool damageOverTime = false)
        {
            MonsterStatus st = target.Status;
            float mul = st.IncomingMul;
            if (mul > 1f && st.MarkCharges > 0) // Death Mark: a charged mark belongs to its caster
            {
                if (src != null && ReferenceEquals(st.MarkSource, src)) { if (!damageOverTime) st.ConsumeMarkCharge(); }
                else mul = 1f;
            }
            if (!damageOverTime && src != null) mul *= HeroBuffs.ConsumeNextHit(src);
            return mul > 1f ? UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(dmg * mul)) : dmg;
        }
    }
}
