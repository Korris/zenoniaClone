using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Flame Cleave and Charging Strike in "RO3 Soft Fantasy V1" (2026-11-21, WarriorLightSkills dispatches here). Render only; impacts sit on the
    /// SkillTable strike times (Flame Cleave 0 ms, Charging Strike 250 ms = the end of the 250 ms dash).
    /// </summary>
    public static class WarriorLightStrikes
    {
        const float H = WarriorLightLook.H;

        /// <summary>
        /// Flame Cleave (one 300 % arc of 140 degrees, 3 tiles, burn, at 0 ms): a wide fire ribbon (Flame palette, 0.22 H) sweeping the arc in 80 ms
        /// with a thinner gold one just outside it, a scorched pool on the ground, 5 flame tongues licking up along the arc (pop in, dissolve out
        /// by ~500 ms), embers rising, a flash where the blade bites, and the stop / shake / punch for the caster.
        /// </summary>
        public static void FlameCleave(SkillContext ctx)
        {
            SkillEffect e = ctx.Def.Effects[0];
            ClericLightLook.Pal p = WarriorLightLook.Flame;
            HeroController hero = ctx.Body.Hero;
            float r = e.Radius * ctx.Units, aim = WarriorLightSkills.AngleOf(ctx.Dir), half = e.AngleDeg * 0.5f, t = e.AtMs;
            var fx = ClericLightFx.Create("FlameCleave", ctx.Feet, null, t + 700f, ClericLightFx.DimFor(hero, null));
            Vector2 chest = new Vector2(0f, 9f);
            const float Sq = 0.6f;
            float mid = WarriorLightLook.ParamDeg(aim, Sq), a0 = mid + half, a1 = mid - half;
            if (Mathf.Sin(a1 * Mathf.Deg2Rad) > Mathf.Sin(a0 * Mathf.Deg2Rad)) { float s = a0; a0 = a1; a1 = s; }
            float n = WarriorLightSkills.Norm(mid, Sq);
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Arc(chest, r * 0.62f / n, Sq, a0, a1), t, 80f, 240f, 0.28f * H, p, 1f, RibbonSort.Over, 1);
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Arc(chest, r * 0.86f / n, Sq, a0, a1), t + 25f, 80f, 190f, 0.15f * H, p, 0.85f, RibbonSort.Over, 0);
            float gm = WarriorLightLook.ParamDeg(aim, ClericLightFx.Squash);                                            // boundary of the real 3-tile arc on the ground
            ArcRibbonFx.Add(fx, ArcRibbonFx.Arc(Vector2.zero, r, ClericLightFx.Squash, gm + half, gm - half), t + 30f, 80f, 320f, 2.6f, p, 0.85f, RibbonSort.Ground, 3);
            fx.Ground(WarriorLightLook.Fill(p), t, t + 650f, r * 0.9f, r * 1.4f, 0.9f, ClericLightFx.Squash, 0f, 3, ctx.Dir * r * 0.5f).Env(40f, 320f).Kind(LayerKind.Glow);
            for (int i = 0; i < 7; i++)
            {
                float ang = aim + Mathf.Lerp(-half, half, (i + 0.5f) / 7f) * 0.85f, dist = r * (i % 2 == 0 ? 0.55f : 0.8f), t0 = t + 30f + i * 18f;
                Vector2 at = WarriorLightSkills.Dir(ang) * dist;
                at.y *= ClericLightFx.Squash;
                float w = i % 2 == 0 ? 9f : 6.5f;
                fx.Upright(WarriorLightLook.Glyph(WarriorLightLook.FlameTongue, p), ClericLightFx.Band.Over, true, t0, t0 + 460f, w * 0.6f, w, w * 0.8f, w * 2.5f, 0.9f, at, 2)
                  .Pop(170f).Dissolve().Kind(i % 2 == 1 ? LayerKind.Detail : LayerKind.Main);
                fx.At(t0 + 60f, f => f.Puff(false, f.Anchor + at + new Vector2(0f, 4f), 2, 3f, 26f, 1.3f, 2f, p.Light, 0.5f));
            }
            fx.Upright(WarriorLightLook.Spark(p), ClericLightFx.Band.Over, false, t, t + 220f, 12f, 26f, 12f, 26f, 0.95f, chest + ctx.Dir * r * 0.5f, 3)
              .Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, t, t + 190f, 12f, 30f, 9f, 22f, 0.75f, chest + ctx.Dir * r * 0.5f, 2)
              .Ground(WarriorLightLook.Wave(p), t, t + 340f, r * 0.3f, 2f * r / WarriorLightLook.BandRing, 0.75f, ClericLightFx.Squash, 0f, 6)                   // shockwave to the real radius
              .At(t, f =>
              {
                  f.Ring(true, f.Anchor + chest + ctx.Dir * r * 0.5f, 14, 80f, 1.6f, 2.8f, p.Light, 0.35f);
                  UltimateFx.Debris(f.Anchor + ctx.Dir * r * 0.5f, f.Own ? 8 : 2, r * 0.3f, 80f);
              });
            fx.At(t, f => WarriorLightLook.Punch(ctx.Body.IsLocal, 40f, 0.24f, 0.3f, 0.06f));
            WarriorLightSkills.SplitPulses(fx, ctx.Def.Id, t, e.Finisher, chest + ctx.Dir * r * 0.5f, p, 10f);
        }

        /// <summary>
        /// Charging Strike (dash 5 tiles in 250 ms, then a 300 % line strike 5 x 2 tiles along the path at 250 ms): dust and a small wave where the
        /// hero pushes off, two short wind ribbons (Mesh Trail) behind his chest and feet during the dash, dust puffs on the way; at 250 ms a
        /// straight coral ribbon sweeps the whole path in 70 ms, a 4-point flash at the tip, a wave ring and sparks thrown forward, rocks, the
        /// biggest stop / shake of the normal skills.
        /// </summary>
        public static void ChargingStrike(SkillContext ctx)
        {
            ClericLightLook.Pal p = WarriorLightLook.Blade;
            HeroController hero = ctx.Body.Hero;
            SkillEffect strike = null;
            foreach (SkillEffect x in ctx.Def.Effects) if (x.Kind == EffectKind.Strike) { strike = x; break; }
            float hitMs = strike != null ? strike.AtMs : 250f, halfW = (strike != null ? strike.Width : 2f) * ctx.Units * 0.5f;
            float dim = ClericLightFx.DimFor(hero, null);
            Vector2 from = ctx.CastFrom, dir = ctx.Dir;
            ClericLightFx.Create("ChargeStart", from, null, 260f, dim)
                .Ground(WarriorLightLook.Wave(p), 0f, 220f, 8f, 26f, 0.7f)
                .At(0f, f => UltimateFx.Dust(from, f.Own ? 8 : 3, 8f));
            var run = ClericLightFx.Create("ChargeRun", from, hero, hitMs + 40f, dim);
            run.Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, 0f, hitMs, 3f, 4f, 3f, 4f, 0.5f, new Vector2(0f, 10f) + dir * 5f).Trail(p, 0.18f * H, 0.16f)
               .Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, 20f, hitMs, 2f, 2f, 2f, 2f, 0.3f, new Vector2(0f, 3f)).Trail(p, 0.1f * H, 0.12f).Kind(LayerKind.Detail);
            for (int i = 1; i <= 3; i++) run.At(hitMs * i / 4f, f => UltimateFx.Dust(f.Anchor, f.Own ? 3 : 1, 5f));
            bool finisher = strike != null && strike.Finisher;
            run.At(hitMs, f => Impact(f.Anchor, from, dir, halfW, dim, ctx.Body.IsLocal, p, ctx.Def.Id, finisher));
        }

        /// <summary>The line strike at the end of the dash (`at` = where the hero stopped, `from` = where he started).</summary>
        static void Impact(Vector2 at, Vector2 from, Vector2 dir, float halfW, float dim, bool local, ClericLightLook.Pal p, string skillId, bool finisher)
        {
            Vector2 lift = new Vector2(0f, 8f), tip = dir * 10f + lift;
            var fx = ClericLightFx.Create("ChargeHit", at, null, 620f, dim);
            WarriorLightSkills.SplitPulses(fx, skillId, 0f, finisher, tip - dir * 14f, p, 10f);
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Line(from - at + lift, tip), 0f, 70f, 190f, 0.27f * H, p, 1f, RibbonSort.Over, 1);
            fx.Upright(WarriorLightLook.Spark(p), ClericLightFx.Band.Over, false, 30f, 270f, 14f, 30f, 14f, 30f, 0.95f, tip, 3)
              .Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, 30f, 220f, 12f, 30f, 10f, 24f, 0.75f, tip, 2)
              .Ground(WarriorLightLook.Wave(p), 30f, 380f, 10f, 2f * Mathf.Max(halfW + 10f, 26f) / WarriorLightLook.BandRing, 0.85f, ClericLightFx.Squash, 0f, 6, dir * 10f)
              .Ground(WarriorLightLook.Line(WarriorLightLook.Crack, p), 30f, 600f, 30f, 32f, 0.6f, ClericLightFx.Squash, 0f, 5, dir * 10f).Env(40f, 260f).Dissolve().Kind(LayerKind.Detail)
              .At(40f, f =>
              {
                  for (int k = 0; k < f.Count(16); k++)
                  {
                      Vector2 d = WarriorLightSkills.Dir(WarriorLightSkills.AngleOf(dir) + Random.Range(-40f, 40f));
                      f.Emit(true, f.Anchor + tip, d * Random.Range(45f, 85f), Random.Range(1.6f, 2.6f), p.Light, Random.Range(0.25f, 0.4f));
                  }
                  UltimateFx.Debris(f.Anchor + dir * 10f, f.Own ? 12 : 3, 8f, 80f);
                  UltimateFx.Dust(f.Anchor + dir * 8f, f.Own ? 12 : 4, 12f);
                  WarriorLightLook.Punch(local, 55f, 0.38f, 0.5f, 0.06f);
              });
        }
    }
}
