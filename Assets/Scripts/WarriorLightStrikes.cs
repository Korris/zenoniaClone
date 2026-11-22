using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Flame Cleave and Charging Strike, "RO trailer style" (21/11 evening, WarriorLightSkills dispatches here). Render only; impacts sit on the
    /// SkillTable strike times (Flame Cleave 0 ms, Charging Strike 250 ms = the end of the 250 ms dash).
    /// </summary>
    public static class WarriorLightStrikes
    {
        const float H = WarriorLightLook.H;

        /// <summary>
        /// Flame Cleave (one 300 % arc of 140 degrees, 3 tiles, burn, at 0 ms), one of the Warrior's two real Fire skills: a big honey-gold sword
        /// arc (Warrior Fire palette, translucent) over a honey ground fan whose ivory rim sits on the real reach, a lighter outer arc 40 ms later,
        /// 5 small honey flame tongues along the arc living &lt; 200-250 ms (fire &lt;= 20-30 % of the area), a few honey embers, a star where the
        /// blade bites, a thin shock ring, the stop / shake. No red floor, no scorch.
        /// </summary>
        public static void FlameCleave(SkillContext ctx)
        {
            SkillEffect e = ctx.Def.Effects[0];
            ClericLightLook.Pal p = WarriorLightLook.Flame;
            HeroController hero = ctx.Body.Hero;
            float r = e.Radius * ctx.Units, aim = WarriorLightSkills.AngleOf(ctx.Dir), half = e.AngleDeg * 0.5f, t = e.AtMs;
            var fx = ClericLightFx.Create("FlameCleave", ctx.Feet, null, t + 900f, ClericLightFx.DimFor(hero, null));
            Vector2 chest = new Vector2(0f, 9f);
            const float Sq = 0.6f;
            float mid = WarriorLightLook.ParamDeg(aim, Sq), a0 = mid + half, a1 = mid - half;
            if (Mathf.Sin(a1 * Mathf.Deg2Rad) > Mathf.Sin(a0 * Mathf.Deg2Rad)) { float s = a0; a0 = a1; a1 = s; }
            float n = WarriorLightSkills.Norm(mid, Sq);
            WarriorGrandFx.Sector(fx, t, 85f, 420f, 260f, Vector2.zero, aim, e.AngleDeg, r, p, 0.3f, 0.68f);
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Arc(chest, r * 0.7f / n, Sq, a0, a1), t, 90f, 300f, 0.5f * H, p, 1f, RibbonSort.Over, 2);
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Arc(chest, r * 0.9f / n, Sq, a0, a1), t + 40f, 90f, 220f, 0.24f * H, WarriorLightLook.Blade, 0.75f, RibbonSort.Over, 1);
            for (int i = 0; i < 5; i++)                                                                                // small honey flames along the arc, short
            {
                float u = (i + 0.5f) / 5f, ang = aim + Mathf.Lerp(-half, half, u) * 0.85f, t0 = t + 40f + i * 18f;
                Vector2 at = WarriorGrandFx.OnGround(ang, r * Random.Range(0.62f, 0.85f));
                WarriorGrandFx.Lick(fx, t0, 220f, at, 0.42f * H, (i == 2 ? 0.75f : 0.55f) * H, ClericLightFx.Band.Over, 1, i == 2 ? LayerKind.Main : LayerKind.Detail);
            }
            WarriorGrandFx.Impact(fx, t, chest + ctx.Dir * r * 0.55f, 1.3f * H, p, 10, ClericLightFx.Band.Over, chest.y);
            WarriorGrandFx.Shock(fx, t, 340f, ctx.Dir * r * 0.1f, r * 0.25f, r, p, 0.5f);
            fx.At(t, f =>
            {
                WarriorGrandFx.Embers(f, f.Anchor + ctx.Dir * r * 0.5f, f.Own ? 12 : 3, r * 0.45f, p);
                WarriorLightLook.Punch(ctx.Body.IsLocal, 60f, 0.35f, 0.35f, 0.05f);
            });
            WarriorLightSkills.SplitPulses(fx, ctx.Def.Id, t, e.Finisher, chest + ctx.Dir * r * 0.5f, p, 14f);
        }

        /// <summary>
        /// Charging Strike (dash 5 tiles in 250 ms, then a 300 % line strike 5 x 2 tiles along the path at 250 ms), steel / ivory: a small steel ring
        /// where the hero pushes off, a steel-ivory spearhead of force leading him and two wind ribbons behind (Mesh Trail); at 250 ms a faint gold
        /// ground band over the 5 x 2-tile hit corridor, a thin steel line, a tapered steel spearhead (1.15 x 0.65 H, ivory only at the tip) with two
        /// broken trailing streaks, a 1.5 H star at the tip, a thin ground ring, sparkles thrown forward, the biggest stop / shake of the normal skills.
        /// </summary>
        public static void ChargingStrike(SkillContext ctx)
        {
            ClericLightLook.Pal p = WarriorLightLook.Blade, steel = WarriorLightLook.Steel;
            HeroController hero = ctx.Body.Hero;
            SkillEffect strike = null;
            foreach (SkillEffect x in ctx.Def.Effects) if (x.Kind == EffectKind.Strike) { strike = x; break; }
            float hitMs = strike != null ? strike.AtMs : 250f, halfW = (strike != null ? strike.Width : 2f) * ctx.Units * 0.5f;
            float dim = ClericLightFx.DimFor(hero, null);
            Vector2 from = ctx.CastFrom, dir = ctx.Dir;
            float aimDeg = WarriorLightSkills.AngleOf(dir);
            var start = ClericLightFx.Create("ChargeStart", from, null, 600f, dim);
            WarriorGrandFx.Shock(start, 0f, 260f, Vector2.zero, 4f, 1.1f * H, steel, 0.7f);
            start.At(0f, f => UltimateFx.Dust(from, f.Own ? 6 : 2, 8f));
            var run = ClericLightFx.Create("ChargeRun", from, hero, hitMs + 40f, dim);
            run.Upright(WarriorLightLook.Glyph(MechanicLightLook.Flare, steel), ClericLightFx.Band.Over, false, 0f, hitMs, 0.8f * H, 0.9f * H, 0.25f * H, 0.3f * H, 0.8f,
                    new Vector2(0f, 10f) + dir * 0.55f * H).Turn(aimDeg)                                    // spearhead of force leading the hero
               .Upright(WarriorLightLook.Flash(steel), ClericLightFx.Band.Over, false, 0f, hitMs, 7f, 9f, 7f, 9f, 0.7f, new Vector2(0f, 10f) + dir * 6f).Trail(steel, 0.42f * H, 0.2f)
               .Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, 20f, hitMs, 3f, 3f, 3f, 3f, 0.55f, new Vector2(0f, 3f)).Trail(p, 0.22f * H, 0.16f).Kind(LayerKind.Detail);
            run.At(0f, f => MechanicLightBursts.Sparks(f, f.Anchor + new Vector2(0f, 4f), 6, 90f, WarriorLightSkills.AngleOf(-dir), 50f, steel.Light, 0.28f, 1.6f));
            for (int i = 1; i <= 3; i++) run.At(hitMs * i / 4f, f => UltimateFx.Dust(f.Anchor, f.Own ? 2 : 1, 5f));
            bool finisher = strike != null && strike.Finisher;
            run.At(hitMs, f => Impact(f.Anchor, from, dir, halfW, dim, ctx.Body.IsLocal, p, steel, ctx.Def.Id, finisher));
        }

        /// <summary>The line strike at the end of the dash (`at` = where the hero stopped, `from` = where he started).</summary>
        static void Impact(Vector2 at, Vector2 from, Vector2 dir, float halfW, float dim, bool local, ClericLightLook.Pal p, ClericLightLook.Pal steel, string skillId, bool finisher)
        {
            Vector2 lift = new Vector2(0f, 8f), tip = dir * 10f + lift;
            var fx = ClericLightFx.Create("ChargeHit", at, null, 900f, dim);
            WarriorLightSkills.SplitPulses(fx, skillId, 0f, finisher, tip - dir * 14f, p, 14f);
            ArcRibbonFx.PathFn path = ArcRibbonFx.Line3(ArcRibbonFx.P(from - at, 1f, lift.y * 0.4f), ArcRibbonFx.P(dir * 18f, 1f, lift.y)); // from the start to just past the tip, rising to chest height
            // V4 review (78): the beam read as a projectile. The 5 x 2 corridor is a faint ground band (#D5C378 ~0.15), the force is a tapered
            // steel-ivory spearhead at the tip with two broken trailing streaks; only the spearhead tip is ivory.
            Vector2 back = from - at;
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Line(back, dir * 10f), 0f, 70f, 260f, halfW * 2f, p, 0.24f, RibbonSort.Ground, 0); // V5 review: lane 0.11-0.13
            WarriorLightSkills.Slash(fx, path, 10f, 70f, 160f, 0.14f * H, steel, 0.7f, RibbonSort.Over, 1);
            Vector2 side = new Vector2(-dir.y, dir.x) * 0.28f * H;
            for (int s = -1; s <= 1; s += 2)                                                                        // two broken trailing streaks
            {
                Vector2 a0 = tip - dir * 2.2f * H + side * s, a1 = tip - dir * 0.6f * H + side * s * 0.6f;
                WarriorLightSkills.Slash(fx, ArcRibbonFx.Line(a0, a1), 20f + (s + 1) * 15f, 60f, 140f, 0.1f * H, steel, 0.8f, RibbonSort.Over, 2);
            }
            float aimDeg = WarriorLightSkills.AngleOf(dir);
            fx.Upright(WarriorLightLook.Glyph(MechanicLightLook.Flare, steel), ClericLightFx.Band.Over, false, 20f, 230f, 0.8f * H, 1.15f * H, 0.45f * H, 0.65f * H, 0.75f, tip + dir * 0.45f * H, 4).Turn(aimDeg).Floor(lift.y)
              .Upright(WarriorLightLook.Flash(WarriorGrandFx.Hot), ClericLightFx.Band.Over, false, 20f, 110f, 0.2f * H, 0.3f * H, 0.2f * H, 0.3f * H, 0.7f, tip + dir * 0.9f * H, 5).Floor(lift.y);
            WarriorGrandFx.Impact(fx, 30f, tip, 1.5f * H, p, 0);
            WarriorGrandFx.Shock(fx, 30f, 360f, dir * 10f, 6f, Mathf.Max(halfW + 8f, 1.4f * H), steel, 0.55f);
            fx.At(40f, f =>
              {
                  MechanicLightBursts.Sparks(f, f.Anchor + tip, 14, 170f, WarriorLightSkills.AngleOf(dir), 70f, p.Light, 0.32f, 1.8f);
                  MechanicLightBursts.Sparks(f, f.Anchor + tip, 6, 210f, WarriorLightSkills.AngleOf(dir), 40f, steel.Light, 0.22f, 1.5f);
                  UltimateFx.Debris(f.Anchor + dir * 10f, f.Own ? 6 : 2, 8f, 90f);
                  UltimateFx.Dust(f.Anchor + dir * 8f, f.Own ? 8 : 2, 12f);
                  WarriorLightLook.Punch(local, 70f, 0.42f, 0.45f, 0.05f);
              });
        }
    }
}
