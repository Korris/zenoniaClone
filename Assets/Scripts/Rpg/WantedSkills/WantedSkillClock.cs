namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Per-cooldown clock of one wanted boss's 4 skills, plus the shared gap after any skill (design doc: "giữa hai skill phải có khoảng trống để
    /// gây sát thương và hồi máu" -- a few seconds of plain attacks). Engine-free so the decision (which skill fires next) is unit-tested without
    /// Unity; Monster.WantedSkills.cs only resolves the geometry and applies the hit.
    /// </summary>
    public sealed class WantedSkillClock
    {
        /// <summary>Below this HP fraction the enrage skill (table index 3) joins the pool.</summary>
        public const float EnrageHpFraction = 0.3f;
        /// <summary>Minimum time after any skill before the next one may start, so a few plain attacks land in between.</summary>
        public const float SkillGapMs = 3000f;

        /// <summary>Indexed by WantedSkillDef.Id (4 skills per boss, Mire Hydra Queen 5 with Venom Drain); room for a few more.</summary>
        public readonly float[] CooldownLeft = new float[8];
        public float GapLeft;

        public void Tick(float dtMs)
        {
            for (int i = 0; i < CooldownLeft.Length; i++) CooldownLeft[i] = System.Math.Max(0f, CooldownLeft[i] - dtMs);
            GapLeft = System.Math.Max(0f, GapLeft - dtMs);
        }

        /// <summary>rate &lt; 1 = the boss is awakened (MonsterDef.EnrageRate): the skill's cooldown and the shared gap shrink to that fraction.</summary>
        public void OnSkillUsed(WantedSkillDef skill, float rate = 1f)
        {
            CooldownLeft[skill.Id] = skill.CooldownMs * rate;
            GapLeft = SkillGapMs * rate;
        }

        /// <summary>A boss that walked home and healed starts its fight again: every cooldown is ready (the once-only Awakening too).</summary>
        public void Reset() { System.Array.Clear(CooldownLeft, 0, CooldownLeft.Length); GapLeft = 0f; }
    }

    /// <summary>Picks the next skill off cooldown (pure function: `roll` in [0,1) selects among several ready skills, for repeatable tests).</summary>
    public static class WantedSkillPicker
    {
        /// <summary>Index into `skills` to fire now, or -1 when none is ready (the boss keeps using its plain attack).</summary>
        public static int Pick(WantedSkillDef[] skills, WantedSkillClock clock, float hpFraction, float roll)
        {
            if (skills == null || clock.GapLeft > 0f) return -1;
            int ready = 0;
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i].Enrage && hpFraction >= WantedSkillClock.EnrageHpFraction) continue;
                if (clock.CooldownLeft[skills[i].Id] <= 0f) ready++;
            }
            if (ready == 0) return -1;
            int pick = System.Math.Min(ready - 1, (int)(System.Math.Max(0f, System.Math.Min(0.999999f, roll)) * ready));
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i].Enrage && hpFraction >= WantedSkillClock.EnrageHpFraction) continue;
                if (clock.CooldownLeft[skills[i].Id] > 0f) continue;
                if (pick == 0) return i;
                pick--;
            }
            return -1; // unreachable
        }
    }
}
