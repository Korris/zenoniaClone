using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Frost Nova, RO3 Soft Fantasy V1 (2026-11-21; icon: snowflake over a ring of ice crystals). SkillTable: circle of 3 tiles round the caster at
    /// 0 ms (280 %, freeze 2 s), so the burst is on the cast frame (no wind-up that would trail the numbers). Ice palette, 900 ms, rule 4 beats:
    ///  0 ms   peak: white-cyan flash at the feet, a frost wave running out to the radius in 260 ms, the frost ring (crystal teeth) drawing itself in
    ///         exactly on the gameplay radius (rule 3), a snowflake emblem 1.3 H on the ground popping in;
    ///  0-220  10 ice crystals stand up round the caster one after another (geometry over particles, rule 2), 14 snow motes, a flash on every
    ///         monster caught, one pulse per damage number (SkillHitSplit);
    ///  220-900 everything rests at ~55 % and melts away (crystals dissolve), the ring last.
    /// Crowd: rule 12 via ClericLightFx (Detail = the crystals' second row and the inner snowflake for strangers).
    /// </summary>
    public static class WizardFrostNovaLightFx
    {
        const float Ms = 900f;
        const int Crystals = 10;

        public static void Cast(SkillContext ctx)
        {
            SkillEffect strike = WizardLightSkills.Effect(ctx.Def, EffectKind.Strike);
            float r = (strike != null ? strike.Radius : 3f) * ctx.Units, d = 2f * r, ring = d / 0.955f;
            ClericLightLook.Pal p = ElementLightLook.Ice;
            HeroController caster = ctx.Body.Hero;
            float dim = ClericLightFx.DimFor(caster, null);
            var fx = ClericLightFx.Create("FrostNova", ctx.Feet, null, Ms, dim);
            Material shard = ElementLightLook.M(ElementLightLook.IceShard, p, ClericLightLook.Role.Glyph);

            fx.Ground(ElementLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Fill), 0f, 820f, d * 0.4f, d * 1.05f, 1f).Env(40f, 420f).Kind(LayerKind.Glow)
              .Ground(ElementLightLook.M(ElementLightLook.FrostRing, p, ClericLightLook.Role.Arc), 0f, Ms, ring * 0.92f, ring, 0.9f, ClericLightFx.Squash, 0f, 6).Reveal(180f).Arc(4f, 0f, 2).Env(20f, 380f).Phases(0f, 220f, 1f, 0.6f)
              .Ground(ElementLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Line), 0f, 300f, 8f, d / 0.78f, 0.9f, ClericLightFx.Squash, 0f, 7)
              .Ground(ElementLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Line), 110f, 420f, 8f, d / 0.78f * 1.12f, 0.5f, ClericLightFx.Squash, 0f, 7).Kind(LayerKind.Detail) // second, fainter wave just past the edge
              .Ground(ElementLightLook.M(ElementLightLook.Snowflake, p, ClericLightLook.Role.Glyph), 0f, 820f, 18f, 36f, 0.9f, ClericLightFx.Squash, 30f, 8).Pop(200f).Env(30f, 380f).Phases(0f, 200f, 1f, 0.55f)
              .Upright(ElementLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 0f, 180f, 8f, 28f, 8f, 28f, 0.8f, new Vector2(0f, 4f), 2);

            // Ice crystals standing up round the caster (0.62 / 0.82 of the radius, alternating), staggered over 220 ms, melting away. They sort with
            // the caster (far ones behind him, near ones in front), so this part follows him; the ground part stays where the nova went off.
            var body = ClericLightFx.Create("FrostNovaCrystals", ctx.Feet, caster, Ms, dim);
            float a0 = Random.value * Mathf.PI * 2f;
            for (int i = 0; i < Crystals; i++)
            {
                float a = a0 + i * Mathf.PI * 2f / Crystals, rr = r * (i % 2 == 0 ? 0.62f : 0.82f), t0 = 20f * i, h = i % 2 == 0 ? 19f : 15f;
                Vector2 off = new Vector2(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr * ClericLightFx.Squash);
                ClericLightFx.Band band = off.y > 0f ? ClericLightFx.Band.Behind : ClericLightFx.Band.Front; // far crystals behind the caster, near ones in front
                body.Upright(shard, band, true, t0, Ms - 60f, 4.8f, 6.4f, h * 0.2f, h, 0.92f, off, -1).Pop(140f).Env(20f, 300f).Dissolve()
                  .Kind(i % 2 == 0 ? LayerKind.Main : LayerKind.Detail);
            }

            WizardLightSkills.Pulses(fx, ctx.Def.Id, 30f, ElementLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash), 10f, 22f, new Vector2(0f, 10f), 0.6f);
            Color snow = p.Light, mote = ElementLightLook.A(p.Main, 0.9f);
            Vector2 feet = ctx.Feet;
            fx.At(0f, f => f.Ring(true, feet + new Vector2(0f, 6f), 14, r * 1.9f, 2f, 3f, snow, 0.45f))
              .At(80f, f => f.Puff(false, feet, 14, r * 0.85f, 10f, 1.6f, 2.6f, mote, 0.8f))
              .At(0f, f => WizardLightSkills.MonsterFlashes(feet, r, f.Dim, p, ElementLightLook.Snowflake));
            WizardLightSkills.Punch(ctx, 0.22f, 45f);
        }
    }
}
