using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Which split (SkillHitSplit) your own hit on a monster is drawn with. Neither the hit nor the online MonsterHit event says which skill it came
    /// from, so the client remembers the local hero's last damaging cast (SkillExecutor.Cast runs for it offline, when predicted online and when
    /// the server's SkillCast is replayed) and how long its hits keep landing. A hit inside that window is that skill's, unless the hero is in a
    /// basic-attack combo (a cast resets the combo, so a swing after the cast is a basic hit). Quiet hits (damage over time) never split.
    /// </summary>
    public static class SkillHitContext
    {
        /// <summary>Extra time after the skill's last step: projectile travel + server round trip + snapshot delay online.</summary>
        const float SlackMs = 900f;

        static string skillId;
        static float untilMs = -1f;

        static float NowMs { get { return Time.unscaledTime * 1000f; } }

        /// <summary>SkillExecutor: a skill was cast by `hero` (ignored unless it is the local hero and the skill deals damage).</summary>
        public static void NoteCast(HeroController hero, SkillDef def)
        {
            if (hero == null || hero != LocalPlayer.Hero || !SkillHitSplit.Damages(def)) return;
            skillId = def.Id;
            untilMs = NowMs + SkillHitSplit.ActiveMs(def) + SlackMs;
        }

        /// <summary>The split for a hit of the local hero landing now.</summary>
        public static HitSplit ForLocalHit(bool quiet)
        {
            if (quiet) return SkillHitSplit.None;
            HeroController h = LocalPlayer.Hero;
            bool inSkill = skillId != null && NowMs <= untilMs && (h == null || !h.IsBasicAttacking);
            return inSkill ? SkillHitSplit.For(skillId) : SkillHitSplit.Basic;
        }

        public static void ResetState() { skillId = null; untilMs = -1f; }
    }
}
