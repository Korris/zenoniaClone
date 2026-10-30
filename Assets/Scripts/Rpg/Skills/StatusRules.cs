using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>Engine-free status rules: boss conversion, refresh math and conditional damage bonuses (unit-tested in ZenoniaSkills.Tests).</summary>
    public static class StatusRules
    {
        public const float BossSlowPct = 50f;     // freeze / stun on a boss become this slow
        public const float MaxSlowPct = 90f;      // movement never drops below 10 %

        /// <summary>
        /// What a status becomes on a boss: slow / chill value is halved, freeze and stun turn into a 50 % slow (same duration), damage over time and
        /// mark apply as is, knockback / launch are ignored (Kind None).
        /// </summary>
        public static StatusSpec ForBoss(StatusSpec s)
        {
            switch (s.Kind)
            {
                case StatusKind.Slow:
                case StatusKind.Chill: s.Value *= 0.5f; return s;
                case StatusKind.Freeze:
                case StatusKind.Stun: return new StatusSpec(StatusKind.Slow, BossSlowPct, s.Ms);
                case StatusKind.Knockback:
                case StatusKind.Launch: return default(StatusSpec);
                default: return s;
            }
        }

        /// <summary>Same kind refreshes to the longer duration and the stronger value (never shortens or weakens).</summary>
        public static void Refresh(ref float ms, ref float value, float newMs, float newValue)
        {
            ms = Math.Max(ms, newMs);
            value = Math.Max(value, newValue);
        }

        /// <summary>Move speed multiplier from slow percent (0..90 clamp).</summary>
        public static float SpeedFactor(float slowPct) { return 1f - Math.Min(MaxSlowPct, Math.Max(0f, slowPct)) / 100f; }

        /// <summary>Damage of one DoT tick: value % of the caster's ATK per second over the tick length.</summary>
        public static float DotTickDamage(float casterAtk, float pctPerSecond, int tickMs) { return casterAtk * pctPerSecond / 100f * tickMs / 1000f; }

        /// <summary>Incoming damage multiplier of a marked monster (Mark value = percent more damage).</summary>
        public static float MarkMultiplier(float markPct) { return 1f + Math.Max(0f, markPct) / 100f; }

        /// <summary>
        /// Conditional damage multiplier of a strike: BonusPct when the target carries any status in BonusVsMask, plus BehindBonusPct when the
        /// attacker is behind the target. Bonuses add up (they are percentages of the base).
        /// </summary>
        public static float ConditionalMultiplier(SkillEffect e, int targetStatusMask, bool behind)
        {
            float pct = 0f;
            if (e.BonusVsMask != 0 && (e.BonusVsMask & targetStatusMask) != 0) pct += e.BonusPct;
            if (behind) pct += e.BehindBonusPct;
            return 1f + pct / 100f;
        }
    }
}
