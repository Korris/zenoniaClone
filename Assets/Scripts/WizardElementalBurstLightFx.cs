using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Elemental Burst, RO3 Soft Fantasy V1 (2026-11-21; icon: a fire arm and an ice arm swirling into each other). SkillTable: circle of 2.5 tiles,
    /// 4 tiles ahead (clamped by walls like SkillEffects.Center), at 0 ms (400 %, +25 % on burning / frozen), cast lock 500 ms. Rule 8: fire orange +
    /// ice cyan, one white core. 950 ms:
    ///  0 ms   peak: flash at the staff and at the centre, the two swirl arms (fire / ice, opposite) drawing themselves in on the ground, flame petals
    ///         popping 1.5 H, 6 ice shards round them, a fire ring and an ice ring running out to the gameplay radius (the ice one stays on it),
    ///         embers + snow, a flash on every monster caught, one pulse per damage number, own screen punch;
    ///  250-950 arms and ring hold at ~55 % and fade, shards melt (dissolve).
    /// </summary>
    public static class WizardElementalBurstLightFx
    {
        const float Ms = 950f;

        public static void Cast(SkillContext ctx)
        {
            SkillEffect strike = WizardLightSkills.Effect(ctx.Def, EffectKind.Strike);
            if (strike == null) return;
            float u = ctx.Units, r = strike.Radius * u, d = 2f * r;
            Vector2 c = strike.Range > 0f ? SkillAim.ClampToWalls(ctx.Feet, ctx.Feet + ctx.Dir * strike.Range * u) : ctx.Feet; // SkillEffects.Center
            ClericLightLook.Pal fire = ElementLightLook.Fire, ice = ElementLightLook.Ice;
            HeroController caster = ctx.Body.Hero;
            float dim = ClericLightFx.DimFor(caster, null);
            Vector2 lift = new Vector2(0f, 8f), hand = caster.Position + new Vector2(ctx.Dir.x * 7f, 15f) - c;

            var fx = ClericLightFx.Create("ElementalBurst", c, null, Ms, dim);
            fx.Upright(ElementLightLook.M(ClericLightLook.Core, ice, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 0f, 140f, 4f, 10f, 4f, 10f, 0.75f, hand)
              .Ground(ElementLightLook.M(ClericLightLook.Band, ice, ClericLightLook.Role.Fill), 0f, 820f, d * 0.5f, d * 1.1f, 1f).Env(40f, 400f).Kind(LayerKind.Glow)
              .Ground(ElementLightLook.M(ElementLightLook.SwirlFire, fire, ClericLightLook.Role.Glyph), 0f, Ms, d * 0.7f, d * 0.95f, 0.9f, ClericLightFx.Squash, 0f, 6).Reveal(200f).Env(20f, 420f).Phases(0f, 250f, 1f, 0.55f)
              .Ground(ElementLightLook.M(ElementLightLook.SwirlIce, ice, ClericLightLook.Role.Glyph), 0f, Ms, d * 0.7f, d * 0.95f, 0.9f, ClericLightFx.Squash, 0f, 7).Reveal(200f).Env(20f, 420f).Phases(0f, 250f, 1f, 0.55f)
              .Ground(ElementLightLook.M(ClericLightLook.Band, fire, ClericLightLook.Role.Line), 0f, 300f, 8f, d / 0.78f * 0.92f, 0.8f, ClericLightFx.Squash, 0f, 8)
              .Ground(ElementLightLook.M(ClericLightLook.Band, ice, ClericLightLook.Role.Line), 50f, Ms, 8f, d / 0.78f, 0.85f, ClericLightFx.Squash, 0f, 9).Env(30f, 380f).Phases(50f, 300f, 1f, 0.5f)
              .Upright(ElementLightLook.M(ElementLightLook.FireBurst, fire, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, 0f, 440f, 16f, 44f, 16f, 44f, 0.92f, lift, 1).Pop(170f)
              .Upright(ElementLightLook.M(ClericLightLook.Core, ice, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 0f, 210f, 12f, 36f, 12f, 36f, 0.85f, lift, 3)
              .Ground(ElementLightLook.M(ElementLightLook.FrostRing, ice, ClericLightLook.Role.Line), 60f, Ms, d / 0.955f * 0.9f, d / 0.955f, 0.8f, ClericLightFx.Squash, -20f, 5).Reveal(160f).Env(30f, 380f).Phases(60f, 320f, 1f, 0.5f).Kind(LayerKind.Detail);

            Material shard = ElementLightLook.M(ElementLightLook.IceShard, ice, ClericLightLook.Role.Glyph);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f + 0.4f, t0 = 15f * i;
                Vector2 off = ClericLightFx.GroundRing(a, r * 0.55f);
                fx.Upright(shard, ClericLightFx.Band.Over, true, t0, 760f, 4.4f, 6f, 3f, 17f, 0.9f, off, -1).Floor().Pop(130f).Env(20f, 280f).Dissolve().Kind(LayerKind.Detail);
            }

            WizardLightSkills.Pulses(fx, ctx.Def.Id, 30f, ElementLightLook.M(ClericLightLook.Core, fire, ClericLightLook.Role.Flash), 10f, 24f, lift, 0.6f);
            Color ember = fire.Light, snow = ice.Light;
            fx.At(0f, f => f.Ring(true, c + lift, 14, r * 2.1f, 2f, 3.2f, ember, 0.45f))
              .At(40f, f => f.Ring(true, c + lift, 14, r * 1.7f, 2f, 3f, snow, 0.5f))
              .At(0f, f => WizardLightSkills.MonsterFlashes(c, r, f.Dim, ice, ClericLightLook.Star));
            WizardLightSkills.Punch(ctx, 0.3f, 60f);
        }
    }
}
