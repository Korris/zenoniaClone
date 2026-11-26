using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Entry point of the data-driven skills: resolves aim / target and starts the timeline (or the legacy Earth Break / Ice Storm / Rocket Barrage / Blade Cyclone adapter).
    /// It does NOT check learned / cooldown / MP / cast lock: HeroController.TryCastSkill does, then calls Cast.
    /// Returns false (nothing happened, nothing to charge) when the skill needs a target and none is in range.
    /// </summary>
    public static class SkillExecutor
    {
        /// <summary>Casts skill `skillIndex` (0-7) of the hero's class at its learned rank (min 1; `rankOverride` &gt; 0 forces one). `salt` (the cast's input step, identical on server and clients) seeds the rain pattern.</summary>
        public static bool Cast(HeroController hero, int skillIndex, int rankOverride = 0, int salt = 0)
        {
            Vector2? used;
            return Cast(hero, skillIndex, rankOverride, salt, null, out used);
        }

        /// <summary>`ground` = the point picked for a Ground skill (null = auto pick); `used` = where it really landed (the authority's clamped point,
        /// which the SkillCast event carries to every viewer), null for other skills.</summary>
        public static bool Cast(HeroController hero, int skillIndex, int rankOverride, int salt, Vector2? ground, out Vector2? used)
        {
            used = null;
            if (hero == null || hero.IsDead) return false;
            return Cast(new HeroSkillBody(hero), skillIndex, rankOverride, salt, ground, out used);
        }

        public static bool Cast(ISkillBody body, int skillIndex, int rankOverride = 0, int salt = 0)
        {
            Vector2? used;
            return Cast(body, skillIndex, rankOverride, salt, null, out used);
        }

        public static bool Cast(ISkillBody body, int skillIndex, int rankOverride, int salt, Vector2? ground, out Vector2? used)
        {
            used = null;
            SkillDef def = SkillTable.Get(body.ClassId, skillIndex);
            if (def == null) { Debug.LogWarning("[ZenoniaProto] no skill " + skillIndex + " for class " + body.ClassId); return false; }
            int rank = rankOverride > 0 ? rankOverride : Mathf.Max(1, body.Hero.Rpg != null ? body.Hero.Rpg.Skills[skillIndex] : 1);
            SkillCaps.Prune();
            SkillHitContext.NoteCast(body.Hero, def); // multi-hit damage numbers: your hits in this skill's window use its SkillHitSplit
            if (def.Legacy != LegacySkill.None)
            {
                if (GroundTargetRules.IsGround(def))
                {
                    used = SkillGroundAim.Point(def, body, ground, NetMode.IsAuthority);
                    Vector2 to = used.Value - body.Feet;
                    if (NetMode.IsAuthority && to.sqrMagnitude > 1f) body.Face(to.normalized);
                }
                return CastLegacy(body, def, rank, used);
            }
            SkillContext ctx = SkillAim.Resolve(def, rank, body, salt, ground);
            if (ctx == null) return false;
            used = ctx.Ground;
            SkillRun.Begin(ctx);
            if (def.Id == InfernoCrashUltimate.SkillId) InfernoCrashUltimate.Cast(ctx);
            else if (def.Id == BerserkerAwakeningFx.SkillId) BerserkerAwakeningFx.Cast(ctx);
            else if (def.Id == NightfallExecutionUltimate.SkillId) NightfallExecutionUltimate.Cast(ctx);
            else if (AssassinSkillArt.Handles(def.Id)) AssassinSkillArt.Cast(ctx);
            else if (ClericBuffFx.Handles(def.Id)) ClericBuffFx.Cast(ctx);
            else if (ClericLightSkills.Handles(def.Id)) ClericLightSkills.Cast(ctx);
            else if (WarriorLightSkills.Handles(def.Id)) WarriorLightSkills.Cast(ctx);
            else if (WizardLightSkills.Handles(def.Id)) WizardLightSkills.Cast(ctx); // bolts draw from SkillBolt (SkillBoltArt)
            else if (MechanicLightSkills.Handles(def.Id)) MechanicLightSkills.Cast(ctx);
            else if (ArcherLightSkills.Handles(def.Id)) ArcherLightSkills.Cast(ctx); // bolts draw from SkillBolt (SkillBoltArt)
            else SkillChoreoCastHook.Play(ctx);
            return true;
        }

        /// <summary>Earth Break / Ice Storm / Rocket Barrage / Blade Cyclone keep their hand-made timelines; the rank only scales their damage.</summary>
        static bool CastLegacy(ISkillBody body, SkillDef def, int rank, Vector2? ground)
        {
            float scale = def.DamageMul(rank);
            if (def.Legacy == LegacySkill.EarthBreak) EarthBreakUltimate.Cast(body.Hero, scale, ground);
            else if (def.Legacy == LegacySkill.IceStorm) IceStormUltimate.Cast(body.Hero, scale, ground);
            else if (def.Legacy == LegacySkill.RocketBarrage) { RocketBarrageUltimate.Cast(body.Hero, scale); MechanicRocketPose.Begin(body.Hero); }
            else if (def.Legacy == LegacySkill.BladeCyclone) BladeCycloneUltimate.Cast(body.Hero, scale);
            return true;
        }

        /// <summary>Loads the painted art of a class's skills ahead of the first cast (call when the hero spawns).</summary>
        public static void Preload(string classId) { SkillVfx.Preload(classId); if (classId == "assassin") AssassinSkillArt.Preload(); if (classId == "mechanic") { DeployedTurret.Preload(); MechanicLightLook.Load(); } if (classId == ArcherLightSkills.ClassId) ArcherLightLook.Load(); }
    }
}
