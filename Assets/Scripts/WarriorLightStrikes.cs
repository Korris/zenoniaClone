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
            var fx = ClericLightFx.Create("FlameCleave", ctx.Feet, null, t + 1000f, ClericLightFx.DimFor(hero, null));
            Vector2 chest = new Vector2(0f, 9f);
            const float Sq = 0.6f;
            float mid = WarriorLightLook.ParamDeg(aim, Sq), a0 = mid + half, a1 = mid - half;
            if (Mathf.Sin(a1 * Mathf.Deg2Rad) > Mathf.Sin(a0 * Mathf.Deg2Rad)) { float s = a0; a0 = a1; a1 = s; }
            // The dominant shape (ChatGPT round 1, concept): a forward WALL of fire = 3 fire crescents stacked up the arc (ground level, +0.45 H,
            // +0.9 H, thinner as they rise), then a second sheet 50 ms later; the crescents sit above the low flames and the small hit flash.
            float n = WarriorLightSkills.Norm(mid, Sq), rc = r * 0.7f / n;
            // Round 2 (ChatGPT): one giant flame blade, not three separate arcs: a continuous red flame body behind the crescents (1.3 H wide,
            // ~50 %), the middle crescent widest and brightest, the outer two at ~60 %.
            ArcRibbonFx.Add(fx, ArcRibbonFx.Arc(chest + new Vector2(0f, 0.1f * H), rc, Sq, a0, a1), t, 95f, 330f, 1.3f * H, WarriorGrandFx.Pyre, 0.55f, RibbonSort.Over, 0, null, 6f,
                WarriorLightLook.TrailLayer("flame-body", ClericLightLook.A(ClericLightLook.Hex(0xE83D27), 0.6f), new Color(0.5f, 0.12f, 0.02f, 1f), 0f));
            for (int k = 0; k < 3; k++)
            {
                bool middle = k == 1;
                Vector2 lift = chest + new Vector2(0f, (k - 0.8f) * 0.42f * H);
                WarriorLightSkills.Slash(fx, ArcRibbonFx.Arc(lift, rc * (1f - 0.05f * k), Sq, a0, a1), t + k * 12f, 90f, 310f - k * 20f, (middle ? 0.62f : 0.4f) * H, middle ? p : WarriorGrandFx.Pyre, middle ? 1f : 0.6f, RibbonSort.Over, middle ? 5 : 3);
            }
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Arc(chest, r * 0.92f / n, Sq, a0, a1), t + 50f, 90f, 240f, 0.36f * H, WarriorGrandFx.Pyre, 0.8f, RibbonSort.Over, 1);
            float gm = WarriorLightLook.ParamDeg(aim, ClericLightFx.Squash);                                            // boundary of the real 3-tile arc on the ground
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Arc(Vector2.zero, r, ClericLightFx.Squash, gm + half, gm - half), t + 30f, 80f, 360f, 0.2f * H, p, 0.4f, RibbonSort.Ground, 3);
            WarriorGrandFx.Scorch(fx, t + 20f, 1150f, ctx.Dir * r * 0.55f, r * 0.5f, p);
            for (int i = 0; i < 3; i++)                                                                                // 3 low flames licking along the outer arc
            {
                float u = (i + 0.5f) / 3f, ang = aim + Mathf.Lerp(-half, half, u) * 0.8f, t0 = t + 50f + i * 25f;
                Vector2 at = WarriorLightSkills.Dir(ang) * r * 0.88f;
                at.y *= ClericLightFx.Squash;
                WarriorGrandFx.Pillar(fx, t0, 520f, at, 0.55f * H, (i == 1 ? 0.9f : 0.65f) * H, p, ClericLightFx.Band.Over, 1, i == 1 ? LayerKind.Main : LayerKind.Detail);
            }
            WarriorGrandFx.Impact(fx, t, chest + ctx.Dir * r * 0.55f, 1.3f * H, p, 24);
            WarriorGrandFx.Shock(fx, t, 380f, ctx.Dir * r * 0.15f, r * 0.25f, r, p, 0.45f);
            fx.At(t, f =>
            {
                WarriorGrandFx.Embers(f, f.Anchor + ctx.Dir * r * 0.5f, 24, r * 0.5f, p);
                UltimateFx.Debris(f.Anchor + ctx.Dir * r * 0.5f, f.Own ? 12 : 3, r * 0.3f, 90f);
                WarriorLightLook.Punch(ctx.Body.IsLocal, 65f, 0.4f, 0.4f, 0.1f);
            });
            WarriorLightSkills.SplitPulses(fx, ctx.Def.Id, t, e.Finisher, chest + ctx.Dir * r * 0.5f, p, 14f);
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
            var start = ClericLightFx.Create("ChargeStart", from, null, 700f, dim);
            WarriorGrandFx.Shock(start, 0f, 260f, Vector2.zero, 4f, 1.1f * H, p, 0.9f);
            WarriorGrandFx.Scorch(start, 0f, 600f, Vector2.zero, 0.45f * H, WarriorLightLook.Flame);
            start.At(0f, f => UltimateFx.Dust(from, f.Own ? 10 : 3, 8f));
            var run = ClericLightFx.Create("ChargeRun", from, hero, hitMs + 40f, dim);
            run.Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, 0f, hitMs, 7f, 10f, 7f, 10f, 0.9f, new Vector2(0f, 10f) + dir * 6f).Trail(p, 0.42f * H, 0.2f)
               .Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, 20f, hitMs, 3f, 3f, 3f, 3f, 0.6f, new Vector2(0f, 3f)).Trail(p, 0.22f * H, 0.16f).Kind(LayerKind.Detail);
            run.At(0f, f => MechanicLightBursts.Sparks(f, f.Anchor + new Vector2(0f, 4f), 10, 90f, WarriorLightSkills.AngleOf(-dir), 50f, p.Light, 0.3f, 1.8f));
            for (int i = 1; i <= 3; i++) run.At(hitMs * i / 4f, f => UltimateFx.Dust(f.Anchor, f.Own ? 3 : 1, 5f));
            bool finisher = strike != null && strike.Finisher;
            run.At(hitMs, f => Impact(f.Anchor, from, dir, halfW, dim, ctx.Body.IsLocal, p, ctx.Def.Id, finisher));
        }

        /// <summary>The line strike at the end of the dash (`at` = where the hero stopped, `from` = where he started).</summary>
        static void Impact(Vector2 at, Vector2 from, Vector2 dir, float halfW, float dim, bool local, ClericLightLook.Pal p, string skillId, bool finisher)
        {
            Vector2 lift = new Vector2(0f, 8f), tip = dir * 10f + lift;
            var fx = ClericLightFx.Create("ChargeHit", at, null, 1000f, dim);
            WarriorLightSkills.SplitPulses(fx, skillId, 0f, finisher, tip - dir * 14f, p, 14f);
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Line(from - at + lift, tip + dir * 8f), 0f, 70f, 220f, 0.45f * H, p, 1f, RibbonSort.Over, 1);
            float aimDeg = WarriorLightSkills.AngleOf(dir);                                                       // forward shock cone (ChatGPT round 1): a collision, not a disc
            fx.Upright(WarriorLightLook.Glyph(MechanicLightLook.Flare, p), ClericLightFx.Band.Over, false, 30f, 260f, 1.0f * H, 2.2f * H, 0.5f * H, 1.1f * H, 1f, tip + dir * 0.9f * H, 4).Turn(aimDeg)
              .Upright(WarriorLightLook.Glyph(MechanicLightLook.Flare, WarriorGrandFx.Hot), ClericLightFx.Band.Over, false, 30f, 180f, 0.5f * H, 1.2f * H, 0.25f * H, 0.5f * H, 1f, tip + dir * 0.5f * H, 5).Turn(aimDeg);
            WarriorGrandFx.Impact(fx, 30f, tip, 1.6f * H, p, 0);
            WarriorGrandFx.Shock(fx, 30f, 380f, dir * 10f, 6f, Mathf.Max(halfW + 10f, 1.6f * H), p, 0.35f);
            WarriorGrandFx.Scorch(fx, 30f, 850f, dir * 10f, Mathf.Max(halfW, 0.8f * H), WarriorLightLook.Flame);
            fx.At(40f, f =>
              {
                  MechanicLightBursts.Sparks(f, f.Anchor + tip, 26, 170f, WarriorLightSkills.AngleOf(dir), 80f, p.Light, 0.36f, 2f);
                  MechanicLightBursts.Sparks(f, f.Anchor + tip, 12, 220f, WarriorLightSkills.AngleOf(dir), 40f, WarriorGrandFx.Hot.Core, 0.22f, 1.6f);
                  WarriorGrandFx.Embers(f, f.Anchor + tip, 10, 8f, p);
                  UltimateFx.Debris(f.Anchor + dir * 10f, f.Own ? 16 : 4, 8f, 90f);
                  UltimateFx.Dust(f.Anchor + dir * 8f, f.Own ? 14 : 4, 12f);
                  WarriorLightLook.Punch(local, 70f, 0.45f, 0.5f, 0.09f);
              });
        }
    }
}
