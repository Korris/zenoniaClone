using System;

namespace ZenoniaProto.Rpg
{
    public struct BaseStats { public int MaxHp, MaxMp, Atk, Def; public float Speed; }
    public struct ExpResult { public int Level, Exp, LevelsGained; }

    /// <summary>EXP curve, level-ups and stat scaling (web systems/leveling-system.js + data/hero-stats-config.js).</summary>
    public static class Leveling
    {
        public const int MaxLevel = 30;
        public const double DeathExpPenalty = 0.1; // lose 10% of current-level EXP, never a level

        /// <summary>EXP required to go from `level` to `level + 1`.</summary>
        public static int ExpToNext(int level) { return (int)Math.Floor(50.0 * Math.Pow(level, 1.5)); }

        /// <summary>Base stats (before equipment) for a hero level of the given class.</summary>
        public static BaseStats StatsForLevel(int level, string classId)
        {
            ClassStatDef c = RpgClasses.Get(classId);
            int n = Math.Max(0, level - 1);
            return new BaseStats
            {
                MaxHp = RpgMath.Round(c.BaseHp + (double)c.GrowHp * n),
                MaxMp = RpgMath.Round(c.BaseMp + (double)c.GrowMp * n),
                Atk = RpgMath.Round(c.BaseAtk + (double)c.GrowAtk * n),
                Def = RpgMath.Round(c.BaseDef + (double)c.GrowDef * n),
                Speed = c.Speed,
            };
        }

        /// <summary>Adds EXP and resolves level-ups (several at once possible). At max level EXP is capped below the next threshold.</summary>
        public static ExpResult ApplyExpGain(int level, int exp, int amount)
        {
            long e = (long)exp + Math.Max(0, amount);
            int gained = 0;
            while (level < MaxLevel && e >= ExpToNext(level)) { e -= ExpToNext(level); level++; gained++; }
            if (level >= MaxLevel) e = Math.Min(e, ExpToNext(level) - 1);
            return new ExpResult { Level = level, Exp = (int)e, LevelsGained = gained };
        }

        /// <summary>Death penalty: EXP lost (10% of the current progress, floored).</summary>
        public static int DeathPenalty(int exp) { return (int)Math.Floor(exp * DeathExpPenalty); }

        public static float ProgressRatio(int level, int exp) { return Math.Max(0f, Math.Min(1f, exp / (float)ExpToNext(level))); }
    }
}
