using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// RO3 Soft Fantasy V1 pictures of the Cleric skills other than the two ally buffs (ClericBuffFx: Spirit Blessing, Divine Aegis), 2026-11-21,
    /// spec docs/reference/ro3-skill-style/ro3-acolyte-priest-skill-style-chatgpt.md section 4. Render only: heals, damage, shields stay in the skill
    /// data (SkillRun); every piece is timed on the SkillTable step it shows (Heal / Divine Light / Purification at 0 ms, Sanctuary pulses every
    /// 1000 ms from 0, Divine Ascension heal 0 ms + strike 100 ms, Guardian Spirit shield at 0 ms).
    ///  - Heal (4.1, 650 ms): ClericHealLightFx below; also on the caster (LowestAllyHealRule), the caster's share smaller.
    ///  - Divine Light (4.4, 620 ms): Judex-style holy ray onto the strike centre + star, see DivineLight();
    ///  - Sanctuary / Guardian Spirit / Purification / Divine Ascension: ClericSanctuaryLightFx, ClericGuardianLightFx, ClericPurifyLightFx, ClericAscensionLightFx.
    /// SkillSupport routes every healed / buffed hero here (OnTarget). `--cleric-fx legacy` keeps the old procedural Heal / Divine Light; the other four
    /// have no painted art any more (removed 2026-11-21) and draw these light FX in legacy mode too.
    /// </summary>
    public static class ClericLightSkills
    {
        public const string HealId = "cleric.heal", DivineLightId = "cleric.divine-light", SanctuaryId = "cleric.sanctuary", GuardianId = "cleric.guardian-spirit",
            PurifyId = "cleric.purification", AscensionId = "cleric.divine-ascension";
        const float HealMs = 650f, DivineMs = 620f;

        /// <summary>True when this skill draws with the light FX (flag on, shaders ready).</summary>
        public static bool Handles(string skillId)
        {
            if (skillId == HealId || skillId == DivineLightId) return ClericLightLook.Active;
            return (skillId == SanctuaryId || skillId == GuardianId || skillId == PurifyId || skillId == AscensionId) && ClericLightLook.Drawable; // no painted art left: legacy draws these too
        }

        public static void Cast(SkillContext ctx)
        {
            if (!ctx.View || !Handles(ctx.Def.Id)) return;
            HeroController caster = ctx.Body.Hero;
            switch (ctx.Def.Id)
            {
                case HealId:
                    HeroController target = ctx.Ally != null ? ctx.Ally : caster;
                    Heal(target, caster, false);
                    if (target != caster) Heal(caster, caster, true); // LowestAllyHealRule: the caster is always healed too
                    break;
                case DivineLightId: DivineLight(ctx); break;
                case SanctuaryId: ClericSanctuaryLightFx.Cast(ctx); break;
                case GuardianId: ClericGuardianLightFx.Cast(ctx); break;
                case PurifyId: ClericPurifyLightFx.Cast(ctx); break;
                case AscensionId: ClericAscensionLightFx.Cast(ctx); break;
            }
        }

        /// <summary>A hero healed / buffed by one of these skills (SkillSupport, on every rendering side, at the step's time).</summary>
        public static void OnTarget(SkillContext ctx, HeroController h)
        {
            if (h == null || !Handles(ctx.Def.Id)) return;
            HeroController caster = ctx.Body.Hero;
            switch (ctx.Def.Id)
            {
                case DivineLightId: if (h != caster) AllySpark(h, caster, ClericLightLook.Heal); break;
                case SanctuaryId: ClericSanctuaryLightFx.Tick(h, caster); break;
                case GuardianId: ClericGuardianLightFx.Receive(h, caster); break;
                case AscensionId: if (h != caster) AllySpark(h, caster, ClericLightLook.Ascend); break;
            }
        }

        /// <summary>
        /// Heal on `target` (spec 4.1 CLERIC_HEAL_V1, 650 ms, follows the target): Heal_Core = a soft white-green light at the chest (behind the hero,
        /// never over the face), Heal_Ring = a ring opening on the ground 0.4 H -> 1.2 H, Heal_Rays = 4 short rays rising behind the body, 8 green-yellow
        /// motes rising beside it; a faint pool under the feet. `self` = the caster's own share when the heal also went to somebody else: 80 %, 2 rays.
        /// </summary>
        public static void Heal(HeroController target, HeroController caster, bool self)
        {
            if (target == null) return;
            ClericLightLook.Pal p = ClericLightLook.Heal;
            float k = self ? 0.8f : 1f;
            var fx = ClericLightFx.Create("Heal", target.Position, target, HealMs, ClericLightFx.DimFor(caster, target));
            fx.Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Fill), 0f, 520f, 14f * k, 26f * k, 1f).Kind(LayerKind.Glow)
              .Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Arc), 0f, 480f, 8f / 0.78f * k, 25f / 0.78f * k, 0.95f, ClericLightFx.Squash, 0f, 5).Arc(10f)
              .Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Behind, false, 0f, 220f, 4f * k, 9f * k, 4f * k, 9f * k, 0.85f, new Vector2(0f, 10f), 1);
            float[] xs = self ? new[] { -5f, 5f } : new[] { -7.5f, -2.5f, 2.5f, 7.5f };
            for (int i = 0; i < xs.Length; i++)
            {
                float t0 = 40f + i * 45f, h = (i % 2 == 0 ? 15f : 20f) * k;
                fx.Upright(ClericLightLook.M(ClericLightLook.Streak, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Behind, true, t0, t0 + 380f, 2.6f, 2.2f, h * 0.6f, h, 0.85f, new Vector2(xs[i] * k, 1f), 2).Rise(5f);
            }
            Color mote = Color.Lerp(p.Main, p.Core, 0.5f);
            fx.At(60f, f => ClericBlessingLightFx.SideSparks(f, self ? 5 : 8, mote, 0.5f));
        }

        /// <summary>
        /// Divine Light (spec 4.4, Holy Light + Judex): the strike (radius 2 tiles, 3 tiles ahead) lands at 0 ms, so no flying bolt that would arrive
        /// after the damage numbers: a short holy ray drops onto the strike centre at once (rule 1: holy attack = ray + hit flash), a 4-point star
        /// 1.1 H at the centre, a thin gold seal ring exactly on the strike radius with a wave running out to it, a small flash at the staff, 5 gold
        /// sparks. Allies healed inside get AllySpark. 620 ms, everything gone after.
        /// </summary>
        static void DivineLight(SkillContext ctx)
        {
            SkillEffect strike = null;
            foreach (SkillEffect e in ctx.Def.Effects) if (e.Kind == EffectKind.Strike) { strike = e; break; }
            if (strike == null) return;
            ClericLightLook.Pal p = ClericLightLook.Holy;
            float u = ctx.Units, r = strike.Radius * u;
            Vector2 c = SkillAim.PlacedCentre(ctx, strike, ctx.CastFrom); // the strike centre (SkillEffects.Center)
            Vector2 hand = ctx.Body.Hero.Position + new Vector2(ctx.Dir.x * 6f, 16f) - c, lift = new Vector2(0f, 8f);
            var fx = ClericLightFx.Create("DivineLight", c, null, DivineMs, ClericLightFx.DimFor(ctx.Body.Hero, null));
            fx.Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 0f, 160f, 3f, 8f, 3f, 8f, 0.8f, hand)
              .Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Fill), 0f, 520f, r * 1.2f, r * 1.8f, 1f).Kind(LayerKind.Glow)
              .Ground(ClericLightLook.M(ClericLightLook.RuneRing, p, ClericLightLook.Role.Line), 0f, DivineMs, 2f * r / 0.955f * 0.85f, 2f * r / 0.955f, 0.85f, ClericLightFx.Squash, 20f, 5).Env(60f, 260f)
              .Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Line), 40f, 420f, 8f, 2f * r / 0.78f, 0.7f, ClericLightFx.Squash, 0f, 6).Kind(LayerKind.Detail)
              .Upright(ClericLightLook.M(ClericLightLook.Streak, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, true, 0f, 220f, 12f, 8f, 40f, 34f, 0.9f, new Vector2(0f, -2f))
              .Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 0f, 150f, 8f, 18f, 8f, 18f, 0.75f, lift, 1)
              .Upright(ClericLightLook.M(ClericLightLook.Star, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, 0f, 300f, 14f, 23f, 14f, 23f, 0.95f, lift, 2)
              .At(30f, f => f.Ring(true, f.Anchor + lift, 5, r * 1.4f, 1.8f, 2.6f, p.Light, 0.32f));
            if (ctx.Body.IsLocal) { GameClock.HitStop(40f); CameraFollow.SkillShake(0.18f); } // the old choreography's punch, for the caster's screen only
        }

        /// <summary>A healed ally of a circle skill (Divine Light, Divine Ascension): a small ring + flash at their feet, 260 ms, no extra circle.</summary>
        public static void AllySpark(HeroController ally, HeroController caster, ClericLightLook.Pal p)
        {
            if (ally == null || !ClericLightLook.Drawable) return;
            Color mote = p.Light;
            ClericLightFx.Create("AllySpark", ally.Position, ally, 280f, ClericLightFx.DimFor(caster, ally))
                .Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Line), 0f, 260f, 8f, 20f, 0.8f)
                .Upright(ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Behind, false, 0f, 170f, 4f, 9f, 4f, 9f, 0.6f, new Vector2(0f, 9f))
                .At(20f, f => f.Puff(true, f.Anchor + new Vector2(0f, 4f), 3, 5f, 20f, 1.6f, 2.4f, mote, 0.35f));
        }
    }
}
