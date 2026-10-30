namespace ZenoniaProto
{
    /// <summary>
    /// The single damage modifier hook every monster hit passes through (Monster.TakeHit, authority only). Phase 02: the mark multiplier.
    /// Phase 03 adds the hero-side parts here (Death Mark charges, Overcharge energy, buff multipliers) without touching combat code.
    /// </summary>
    public static class SkillHitHooks
    {
        public static int Modify(HeroController src, Monster target, int dmg)
        {
            float mul = target.Status.IncomingMul;
            return mul > 1f ? UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(dmg * mul)) : dmg;
        }
    }
}
