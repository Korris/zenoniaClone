using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Every Wizard skill in RO3 Soft Fantasy V1 (2026-11-21, user: "làm lại hết skill của warrior, wizard, mechanic đổi qua kiểu mới"): Particle System
    /// + mesh trail + shader quads on the Cleric light toolkit (ClericLightFx timeline, ElementLightLook palettes), no painted frames. Render only:
    /// damage, statuses and shields stay in the skill data; every picture is timed on the SkillTable step it shows.
    ///  - Arcane Bolt / Fireball / Arcane Lance: the real SkillBolt flight carries the picture (WizardBoltLightFx via SkillBoltArt);
    ///  - Mana Shield: WizardManaShieldLightFx (cast bubble + maintained rings round the body); Frost Nova: WizardFrostNovaLightFx;
    ///  - Elemental Burst: WizardElementalBurstLightFx; Astral Cataclysm: WizardAstralLightFx; Ice Storm (legacy timeline): IceStormLightFx;
    ///  - basic attack: WizardAttackLightFx (ComboProjectile, HeroFx).
    /// Multi-hit numbers (SkillHitSplit, client only): each impact flashes once per number (Pulses), in step with them.
    /// </summary>
    public static class WizardLightSkills
    {
        public const string ArcaneBoltId = "wizard.arcane-bolt", FireballId = "wizard.fireball", ArcaneLanceId = "wizard.arcane-lance", ManaShieldId = "wizard.mana-shield",
            FrostNovaId = "wizard.frost-nova", ElementalBurstId = "wizard.elemental-burst", AstralId = "wizard.astral-cataclysm";

        /// <summary>True when this skill draws with the light FX (renders FX, shaders and masks ready).</summary>
        public static bool Handles(string skillId)
        {
            switch (skillId)
            {
                case ArcaneBoltId: case FireballId: case ArcaneLanceId: case ManaShieldId: case FrostNovaId: case ElementalBurstId: case AstralId:
                    return ElementLightLook.Active;
                default: return false;
            }
        }

        /// <summary>Cast start (SkillExecutor, every rendering side). Bolts draw from SkillBolt; the rest start here.</summary>
        public static void Cast(SkillContext ctx)
        {
            if (!ctx.View || !Handles(ctx.Def.Id)) return;
            switch (ctx.Def.Id)
            {
                case ManaShieldId: WizardManaShieldLightFx.Cast(ctx); break;
                case FrostNovaId: WizardFrostNovaLightFx.Cast(ctx); break;
                case ElementalBurstId: WizardElementalBurstLightFx.Cast(ctx); break;
                case AstralId: WizardAstralLightFx.Cast(ctx); break;
            }
        }

        /// <summary>First effect of `kind` in the skill (null if none).</summary>
        public static SkillEffect Effect(SkillDef def, EffectKind kind)
        {
            foreach (SkillEffect e in def.Effects) if (e.Kind == kind) return e;
            return null;
        }

        /// <summary>
        /// One flash per damage number of the skill's multi-hit split (SkillHitSplit.For): `flash` quads of `d0 -> d1` units at `lift` over `fx`'s anchor,
        /// starting `t0` ms into the FX, StartDelayMs + i x IntervalMs apart, shrinking a little each time. Another player's cast: one flash only (rule 12).
        /// </summary>
        public static void Pulses(ClericLightFx fx, string skillId, float t0, Material flash, float d0, float d1, Vector2 lift, float alpha = 0.7f)
        {
            HitSplit s = SkillHitSplit.For(skillId);
            int n = fx.Own ? Mathf.Clamp(s.Hits, 1, SkillHitSplit.MaxHits) : 1;
            for (int i = 0; i < n; i++)
            {
                float t = t0 + s.StartDelayMs + i * s.IntervalMs, k = 1f - 0.12f * i;
                fx.Upright(flash, ClericLightFx.Band.Over, false, t, t + 110f, d0 * k, d1 * k, d0 * k, d1 * k, alpha * (i == 0 ? 1f : 0.8f), lift, 3 + i);
            }
        }

        /// <summary>A small flash + glyph on every live monster within `r` of `centre` (at most `max`, none for strangers' casts): who got hit.</summary>
        public static void MonsterFlashes(Vector2 centre, float r, float dim, ClericLightLook.Pal p, string glyphMask, int max = 10)
        {
            if (ClericLightFx.TierOf(dim) == FxTier.Far) return;
            int n = 0;
            foreach (Monster m in Monster.All)
            {
                if (m == null || m.IsDying || (m.Feet - centre).sqrMagnitude > r * r) continue;
                if (n++ >= max) break;
                ClericLightFx.Create("WizardHit", m.Center, null, 300f, dim)
                    .Upright(ElementLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 0f, 170f, 5f, 13f, 5f, 13f, 0.75f, Vector2.zero)
                    .Upright(ElementLightLook.M(glyphMask, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, 0f, 280f, 7f, 13f, 7f, 13f, 0.9f, Vector2.zero, 1);
            }
        }

        /// <summary>Own screen punch of a big hit (choreographies used to bring it; SkillEffects skips it for skills that draw their own body).</summary>
        public static void Punch(SkillContext ctx, float shake, float hitStopMs)
        {
            if (!ctx.Body.IsLocal) return;
            if (shake > 0f) CameraFollow.SkillShake(shake);
            if (hitStopMs > 0f) GameClock.HitStop(hitStopMs);
        }
    }
}
