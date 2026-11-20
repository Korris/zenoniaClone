using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// RO3-style light pictures of the Cleric's Heal and Divine Light (2026-11-20), replacing their painted choreographies. Render only: the heal / damage
    /// stays in the skill data (SkillRun, both at atMs 0 = the cast instant), so every piece here starts at 0 ms. Layers, sizes, colours and times
    /// follow the ChatGPT spec (ClericLightLook): Heal = light gathering on the saved ally (white flash, green ground halo + ring, a soft column and a
    /// holy cross behind them, rising sparkles), 550 ms, nothing left after; Divine Light = a holy circle opening on the ground exactly the strike radius
    /// (ring, turning rune, wave to the rim, upright cross + flash at the centre, outward sparkles, healing motes), 780 ms, everything on the ground,
    /// no tall pillar over a boss. Allies healed inside the circle get AllySpark.
    /// </summary>
    public static class ClericLightSkills
    {
        public const string HealId = "cleric.heal", DivineLightId = "cleric.divine-light";
        const float HealMs = 550f, DivineMs = 780f;

        /// <summary>True when this skill draws with the light FX (flag on, shaders ready).</summary>
        public static bool Handles(string skillId) { return (skillId == HealId || skillId == DivineLightId) && ClericLightLook.Active; }

        static bool OwnFor(HeroController a, HeroController b) { HeroController me = LocalPlayer.Hero; return me == null || a == me || b == me; }

        public static void Cast(SkillContext ctx)
        {
            if (!ctx.View || !ClericLightLook.Active) return;
            if (ctx.Def.Id == HealId) Heal(ctx.Ally != null ? ctx.Ally : ctx.Body.Hero, ctx.Body.Hero);
            else DivineLight(ctx);
        }

        /// <summary>Heal on `target`, cast by `caster` (the light gathers on the target, follows them for the 550 ms).</summary>
        public static void Heal(HeroController target, HeroController caster)
        {
            if (target == null) return;
            var fx = ClericLightFx.Create("Heal", target.Position, target, HealMs, OwnFor(target, caster));
            Color white = ClericLightLook.Spark, mint = ClericLightLook.Leaf;
            fx.Ground(ClericLightLook.GroundGlow, 0f, 340f, 10f, 26f, 0.9f)
              .Ground(ClericLightLook.Ring, 0f, 380f, 10f, 30f, 0.75f, 0.9f, 0f, 5)
              .Ground(ClericLightLook.GroundGlow, 280f, HealMs, 18f, 30f, 0.35f)
              .Upright(ClericLightLook.Column, ClericLightFx.Band.Behind, true, 30f, 400f, 15f, 10f, 30f, 36f, 0.85f, new Vector2(0f, -1f))
              .Upright(ClericLightLook.Cross, ClericLightFx.Band.Behind, false, 40f, 290f, 16f, 24f, 20f, 30f, 0.8f, new Vector2(0f, 12f), 1)
              .Upright(ClericLightLook.Flash, ClericLightFx.Band.Over, false, 0f, 130f, 5f, 16f, 5f, 16f, 0.75f, new Vector2(0f, 10f))
              .At(50f, f => f.Puff(true, f.Anchor + new Vector2(0f, 4f), 6, 7f, 24f, 2.4f, 3.6f, white, 0.45f))
              .At(140f, f => f.Puff(false, f.Anchor + new Vector2(0f, 2f), 6, 8f, 20f, 1.8f, 2.8f, new Color(mint.r, mint.g, mint.b, 0.9f), 0.4f));
        }

        static void DivineLight(SkillContext ctx)
        {
            SkillEffect strike = null;
            foreach (SkillEffect e in ctx.Def.Effects) if (e.Kind == EffectKind.Strike) { strike = e; break; }
            if (strike == null) return;
            float u = ctx.Units, r = strike.Radius * u, d = 2f * r;
            Vector2 c = strike.Range > 0f ? SkillAim.ClampToWalls(ctx.CastFrom, ctx.CastFrom + ctx.Dir * strike.Range * u) : ctx.CastFrom; // the strike centre (SkillEffects.Center)
            var fx = ClericLightFx.Create("DivineLight", c, null, DivineMs, OwnFor(ctx.Body.Hero, null));
            Color white = ClericLightLook.Spark, jade = ClericLightLook.Leaf, mint = ClericLightLook.Leaf;
            fx.Ground(ClericLightLook.GroundGlow, 0f, 600f, d * 0.7f, d * 1.05f, 0.85f, 1f)
              .Ground(ClericLightLook.Rune, 0f, 520f, d * 0.62f, d * 0.9f, 0.95f, 1f, 38f, 5)
              .Ground(ClericLightLook.Ring, 0f, 460f, d * 1.2f, d * 1.28f, 0.75f, 1f, 0f, 6)                // the area: the ring band sits at 78 % of the quad, so 1.28 d puts it on the radius
              .Ground(ClericLightLook.Ring, 60f, 430f, 8f, d * 1.28f, 0.6f, 1f, 0f, 7)                      // wave running out to the rim
              .Ground(ClericLightLook.GroundGlow, 430f, DivineMs, d, d, 0.3f, 1f)
              .Upright(ClericLightLook.Cross, ClericLightFx.Band.Over, false, 0f, 300f, 18f, 30f, 26f, 44f, 0.75f, new Vector2(0f, 10f))
              .Upright(ClericLightLook.Flash, ClericLightFx.Band.Over, false, 0f, 170f, 12f, 38f, 12f, 38f, 0.8f, new Vector2(0f, 6f), 1)
              .At(80f, f => f.Ring(true, f.Anchor + new Vector2(0f, 4f), 12, r * 1.6f, 2.6f, 4f, white, 0.38f))
              .At(100f, f => f.Puff(false, f.Anchor, 10, r * 0.8f, 18f, 2f, 3.2f, new Color(jade.r, jade.g, jade.b, 0.9f), 0.5f))
              .At(260f, f => f.Puff(true, f.Anchor, 5, r * 0.7f, 14f, 1.8f, 2.8f, new Color(mint.r, mint.g, mint.b, 0.9f), 0.45f));
            if (ctx.Body.IsLocal) { GameClock.HitStop(40f); CameraFollow.SkillShake(0.22f); } // the old choreography's punch, for the caster's screen only
        }

        /// <summary>A second healed ally (Divine Light circle): a short green flash at their feet, 180 ms (no extra circle per hero).</summary>
        public static void AllySpark(HeroController ally, HeroController caster)
        {
            if (ally == null || !ClericLightLook.Active) return;
            Color mint = ClericLightLook.Leaf;
            ClericLightFx.Create("AllySpark", ally.Position, ally, 260f, OwnFor(ally, caster))
                .Ground(ClericLightLook.GroundGlow, 0f, 260f, 10f, 16f, 0.8f)
                .Upright(ClericLightLook.Flash, ClericLightFx.Band.Over, false, 0f, 180f, 4f, 9f, 4f, 9f, 0.6f, new Vector2(0f, 9f))
                .At(20f, f => f.Puff(true, f.Anchor + new Vector2(0f, 4f), 3, 5f, 20f, 2f, 3f, new Color(mint.r, mint.g, mint.b, 0.9f), 0.35f));
        }
    }
}
