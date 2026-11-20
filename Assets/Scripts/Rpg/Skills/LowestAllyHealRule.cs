using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Who a LowestAlly heal (Cleric Heal) reaches. User 2026-11-20: "skill buff máu cho buff chính bản thân mình luôn": the heal always lands on the
    /// caster too. The target is the hurt ally (caster included) with the lowest HP fraction in range; when nobody is hurt the target is the caster, so
    /// the skill never refuses with "no target in range" any more. The caster is healed in addition to a different target. Engine-free (shared with
    /// the zone server through SkillAim / SkillSupport): changing it means rebuilding the server.
    /// </summary>
    public static class LowestAllyHealRule
    {
        /// <summary>At or above this HP fraction a hero counts as unhurt.</summary>
        public const float FullFrac = 0.999f;

        /// <summary>Index in `hpFrac` of the hurt hero with the lowest HP fraction (first one on a tie), else `caster` (nobody hurt, or an empty list).</summary>
        public static int PickTarget(IReadOnlyList<float> hpFrac, int caster)
        {
            int best = caster;
            float bestFrac = FullFrac;
            for (int i = 0; i < hpFrac.Count; i++)
                if (hpFrac[i] < bestFrac) { bestFrac = hpFrac[i]; best = i; }
            return best;
        }

        /// <summary>True when the caster gets a heal of its own besides the target (the target is somebody else).</summary>
        public static bool AlsoHealsCaster(int target, int caster) { return target != caster; }
    }
}
