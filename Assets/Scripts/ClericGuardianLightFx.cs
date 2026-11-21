using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Guardian Spirit, RO3 Soft Fantasy V1 (spec 4.6 CLERIC_GUARDIAN_SPIRIT_V1, Basilica + Kyrie Eleison), replacing the painted choreography.
    /// The shield (SkillTable: 200 % of the caster's ATK, 8000 ms, allies within 4 tiles, applied at 0 ms) is skill data; every shielded hero gets,
    /// at once (SkillSupport -> ClericLightSkills.OnTarget -> Receive), 1000 ms, colours ivory #FFF2C7 + sage #D7EAD6:
    ///  1. a small sage seal at the feet (compass star, 1.25 H), 2. two soft light wings opening behind the shoulders (only now: never kept open,
    ///  they would cover other heroes on a boss), 3. a small ivory halo over the head, 4. a ring of light rising from the feet to the chest behind
    ///  the body, 5. an ivory flash behind the chest, 10 motes / sparks.
    /// After it ClericBuffStatusFx keeps only the small halo at ~25 % while the shield lasts; it flashes when the wearer is hit and the wings flash
    /// once when the shield breaks early (rule 10). Crowd: ClericLightFx.DimFor.
    /// </summary>
    public static class ClericGuardianLightFx
    {
        public const float Ms = 1000f, HaloY = 27f;

        /// <summary>The caster's cast: a thin ivory wave running out to the skill's ally radius (4 tiles) so the reach reads; the rest is per target.</summary>
        public static void Cast(SkillContext ctx)
        {
            float r = 4f * ctx.Units;
            foreach (Rpg.SkillEffect e in ctx.Def.Effects) if (e.Radius > 0f) { r = e.Radius * ctx.Units; break; }
            ClericLightFx.Create("GuardianWave", ctx.Feet, null, 520f, ClericLightFx.DimFor(ctx.Body.Hero, null))
                .Ground(ClericLightLook.M(ClericLightLook.Band, ClericLightLook.Spirit, ClericLightLook.Role.Line), 60f, 520f, 12f, 2f * r / 0.78f, 0.5f, ClericLightFx.Squash, 0f, 3).Kind(LayerKind.Detail);
        }

        public static void Receive(HeroController h, HeroController caster)
        {
            ClericLightLook.Pal ivory = ClericLightLook.Spirit, sage = ClericLightLook.Sage;
            var fx = ClericLightFx.Create("Guardian", h.Position, h, Ms, ClericLightFx.DimFor(caster, h));
            fx.Ground(ClericLightLook.M(ClericLightLook.Band, sage, ClericLightLook.Role.Fill), 0f, 800f, 14f, 26f, 1f).Kind(LayerKind.Glow)
              .Ground(ClericLightLook.M(ClericLightLook.Compass, sage, ClericLightLook.Role.Line), 0f, 900f, 16f, 26f, 0.9f, ClericLightFx.Squash, 24f, 5).Env(80f, 350f)
              .Upright(ClericLightLook.M(ClericLightLook.Band, ivory, ClericLightLook.Role.Line), ClericLightFx.Band.Behind, false, 60f, 700f, 22f, 26f, 8f, 9f, 0.8f, new Vector2(0f, 1f), -1).Rise(12f)
              .Upright(ClericLightLook.M(ClericLightLook.Wings, ivory, ClericLightLook.Role.Glyph), ClericLightFx.Band.Behind, false, 150f, 950f, 26f, 38f, 13f, 19f, 0.85f, new Vector2(0f, 16f), -2).Env(140f, 380f)
              .Upright(ClericLightLook.M(ClericLightLook.Band, ivory, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, 100f, Ms, 8f, 11f, 3.2f, 4.2f, 0.95f, new Vector2(0f, HaloY)).Env(100f, 300f)
              .Upright(ClericLightLook.M(ClericLightLook.Core, ivory, ClericLightLook.Role.Flash), ClericLightFx.Band.Behind, false, 0f, 180f, 6f, 13f, 6f, 13f, 0.65f, new Vector2(0f, 10f), 1)
              .At(60f, f => f.Puff(false, f.Anchor + new Vector2(0f, 2f), 6, 9f, 20f, 1.6f, 2.4f, ClericLightLook.A(sage.Main, 0.9f), 0.6f))
              .At(160f, f => ClericBlessingLightFx.SideSparks(f, 4, ivory.Light, 0.5f));
            ClericBuffStatusFx.Ensure(h, Ms - 200f);
        }
    }
}
