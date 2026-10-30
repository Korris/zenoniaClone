using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Entry point of the data-driven skills: resolves aim / target and starts the timeline (or the legacy Earth Break / Ice Storm adapter).
    /// It does NOT check learned / cooldown / MP / cast lock: HeroController.TryCastSkill does, then calls Cast.
    /// Returns false (nothing happened, nothing to charge) when the skill needs a target and none is in range.
    /// </summary>
    public static class SkillExecutor
    {
        /// <summary>Casts skill `skillIndex` (0-7) of the hero's class at its learned rank (min 1; `rankOverride` &gt; 0 forces one). `salt` (the cast's input step, identical on server and clients) seeds the rain pattern.</summary>
        public static bool Cast(HeroController hero, int skillIndex, int rankOverride = 0, int salt = 0)
        {
            if (hero == null || hero.IsDead) return false;
            return Cast(new HeroSkillBody(hero), skillIndex, rankOverride, salt);
        }

        public static bool Cast(ISkillBody body, int skillIndex, int rankOverride = 0, int salt = 0)
        {
            SkillDef def = SkillTable.Get(body.ClassId, skillIndex);
            if (def == null) { Debug.LogWarning("[ZenoniaProto] no skill " + skillIndex + " for class " + body.ClassId); return false; }
            int rank = rankOverride > 0 ? rankOverride : Mathf.Max(1, body.Hero.Rpg != null ? body.Hero.Rpg.Skills[skillIndex] : 1);
            SkillCaps.Prune();
            if (def.Legacy != LegacySkill.None) return CastLegacy(body, def, rank);
            SkillContext ctx = SkillAim.Resolve(def, rank, body, salt);
            if (ctx == null) return false;
            SkillRun.Begin(ctx);
            return true;
        }

        /// <summary>Earth Break / Ice Storm keep their hand-made timelines; the rank only scales their damage.</summary>
        static bool CastLegacy(ISkillBody body, SkillDef def, int rank)
        {
            float scale = def.DamageMul(rank);
            if (def.Legacy == LegacySkill.EarthBreak) EarthBreakUltimate.Cast(body.Hero, scale);
            else if (def.Legacy == LegacySkill.IceStorm) IceStormUltimate.Cast(body.Hero, scale);
            return true;
        }

        /// <summary>Loads the painted art of a class's skills ahead of the first cast (call when the hero spawns).</summary>
        public static void Preload(string classId) { SkillVfx.Preload(classId); }
    }
}
