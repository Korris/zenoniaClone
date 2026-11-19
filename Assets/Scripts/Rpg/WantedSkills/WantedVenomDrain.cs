using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Rules of a draining wanted skill (WantedSkillDef.DrainPoisoned: Mire Hydra Queen's Venom Drain, ChatGPT V1 numbers 2026-11-19). Engine-free so
    /// dotnet tests hold them: who is hit, and how much the boss heals. Monster.WantedSkills.cs applies them on the authority.
    /// </summary>
    public static class WantedVenomDrain
    {
        /// <summary>Whether a hero standing at the strike instant `inShape` / `poisoned` is hit by `skill` (a non-drain skill hits anyone in its shape).</summary>
        public static bool Hits(WantedSkillDef skill, bool inShape, bool poisoned)
        {
            return inShape && (!skill.DrainPoisoned || poisoned);
        }

        /// <summary>Whether a hero whose poison has `poisonMsLeft` lets the drain start now (any poison does).</summary>
        public static bool CanStartOn(WantedSkillDef skill, float poisonMsLeft)
        {
            return skill.DrainPoisoned && poisonMsLeft > 0f;
        }

        /// <summary>
        /// When the drain starts, the poison of every poisoned hero inside is held at least this long (ms), so it is still there at the strike
        /// unless that hero leaves the circle. Without it the skill never landed: every Hydra poison lasts 4-5 s, the shared 3 s skill gap +
        /// 350 ms execute already eat most of it, and the 2 s telegraph the rest (playtests 2026-11-19: "0 poisoned inside" at every strike).
        /// Victims are still decided at the strike instant (Hits): poisoned and inside.
        /// </summary>
        public static int HoldPoisonMs(WantedSkillDef skill) { return skill.DrainPoisoned ? skill.TelegraphMs + HoldMarginMs : 0; }
        public const int HoldMarginMs = 300;

        /// <summary>HP the boss gets back from `hpTaken` (real HP the drained heroes lost, summed over the cast): DrainHealShare of it, at most
        /// DrainHealCap x `bossMaxHp`, never negative. 0 for a non-drain skill.</summary>
        public static float Heal(WantedSkillDef skill, float hpTaken, float bossMaxHp)
        {
            if (!skill.DrainPoisoned || hpTaken <= 0f || bossMaxHp <= 0f) return 0f;
            return Math.Min(hpTaken * skill.DrainHealShare, bossMaxHp * skill.DrainHealCap);
        }
    }
}
