using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Picture of the Warrior's EARTH BREAK (21/11: first "RO3 Soft Fantasy V1" pass, then the GRAND pass after the user found the Warrior weak;
    /// spec + concept in docs/reference/warrior-fx-grand/). The timeline and every hit stay in EarthBreakUltimate (slam 1450, 8 small hits every
    /// 45 ms from it, 1850, finisher 2000, burns 2150-2450); this lays the matching shapes once at the cast. Earth palette (blazing gold-orange,
    /// dark rim) + Flame for the fire (rule 8: 2 main colours):
    ///  - 0-620 at the caster: a war seal draws itself in under him, a hot glow gathers on the blade (anticipation), motes drawn in;
    ///  - 450 / 600: two thick gold sword sweeps in front of him;
    ///  - 400-1450 at the target: the seal at the real radius draws itself in (telegraph), a warm pool grows;
    ///  - 850-1450: a HUGE great sword (3 H tall; since 21/11 the ChatGPT-painted sprite of WarriorPaintedSword with pulsing runes, a hot halo,
    ///    a white compression flash at contact, flame tongues + embers along the blade, burning away 2100-2500) drops onto the centre;
    ///  - 1450 slam: white-hot impact 2.6 H, thick shockwave to 1.2 R, scorch + glowing cracks that linger to the end, 6 flame pillars round the
    ///    blade, rocks / dust / sparks; small hits: sparks, a shock every third; 1850: shock; 2000 finisher: the largest impact (3.2 H), two
    ///    shocks to 1.45 R, 8 tall pillars, a fireball at the blade, rocks; 2150-2450 burns: the cracks flare, small pillars + embers.
    /// Shake / hit-stop stay in EarthBreakUltimate (local caster only).
    /// </summary>
    public static class WarriorEarthBreakLightFx
    {
        const float H = WarriorLightLook.H;

        /// <summary>The falling sword's own colours (round 2: in Blade colours its glow burned to flat orange on snow): deep crimson body,
        /// orange-gold edge light, pale gold fuller, near-black rim, LOW additive part. Its outline is the gold Earth palette.</summary>
        static readonly ClericLightLook.Pal Sword = new ClericLightLook.Pal("WarSword", 0xC41E1A, 0xFF7A2A, 0xFFE6A0, 0x380606, new Color(0.28f, 0.05f, 0.02f));

        public static void Build(HeroController hero, Vector2 caster, Vector2 center, Vector2 dir, float radius, float slamMs, float smallStepMs, int smalls,
            float penultMs, float finishMs, float[] burnAt, float totalMs)
        {
            if (!WarriorLightLook.Ready || hero == null) return;
            ClericLightLook.Pal p = WarriorLightLook.Earth, fire = WarriorLightLook.Flame;
            float dim = ClericLightFx.DimFor(hero, null), sq = ClericLightFx.Squash, aim = WarriorLightSkills.AngleOf(dir);
            float seal = 2f * radius / WarriorLightLook.SigilBand;

            // Caster: charge seal, blade glow, two forward sweeps
            var me = ClericLightFx.Create("EarthBreakCharge", caster, hero, 1050f, dim);
            me.Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, p), 0f, 620f, 1.6f * H / WarriorLightLook.SigilBand, 1.6f * H / WarriorLightLook.SigilBand, 0.95f, sq, 30f, 4)
                .Reveal(380f).Phases(380f, 460f, 0.5f, 0.7f).Env(0f, 140f);
            WarriorGrandFx.Charge(me, 120f, 450f, new Vector2(0f, 13f) + dir * 7f, 1.1f * H, p);
            const float Sq = 0.6f;
            float mid = WarriorLightLook.ParamDeg(aim, Sq), rr = Mathf.Min(radius, 40f) / WarriorLightSkills.Norm(mid, Sq);
            WarriorLightSkills.Slash(me, ArcRibbonFx.Arc(new Vector2(0f, 9f), rr, Sq, mid + 75f, mid - 75f), 450f, 160f, 260f, 0.42f * H, p, 1f, RibbonSort.Over, 1);
            WarriorLightSkills.Slash(me, ArcRibbonFx.Arc(new Vector2(0f, 9f), rr * 0.8f, Sq, mid - 70f, mid + 70f), 600f, 150f, 240f, 0.34f * H, p, 1f, RibbonSort.Over, 0);

            // Target: telegraph seal + pool, the falling sword
            var fx = ClericLightFx.Create("EarthBreak", center, null, totalMs, dim);
            float swordW = 1.5f * H, swordH = 3f * H, dropFrom = 95f, dropMs = (slamMs - 850f) / 0.37f; // PopCurve first reaches the ground at 37 % of the drop
            fx.Ground(WarriorLightLook.Fill(p), 400f, 1700f, radius * 1.1f, radius * 1.5f, 0.6f).Env(220f, 250f).Kind(LayerKind.Glow)
              .Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, p), 400f, 1750f, seal, seal, 0.95f, sq, 12f, 4)
                .Reveal(600f).Phases(slamMs - 400f, slamMs - 300f, 0.55f, 0.7f).Env(0f, 260f)
              .Upright(WarriorLightLook.Fill(p), ClericLightFx.Band.Over, true, 850f, finishMs + 250f, swordW * 2.2f, swordW * 2.4f, swordH * 1.1f, swordH * 1.15f, 1f, new Vector2(0f, -10f), 1)
                .Drop(dropFrom, dropMs).Env(90f, 260f).Kind(LayerKind.Glow);                                      // blazing aura behind the blade
            // The sword itself: the ChatGPT-painted greatsword (21/11, WarriorPaintedSword) standing through the hits and burning away with the
            // burns; the procedural mask sword only when that art / shader is missing.
            if (!WarriorPaintedSword.Lay(fx, 850f, totalMs, swordH, 8f, dropFrom, dropMs, slamMs, totalMs - finishMs - 100f, fire))
                fx.Upright(WarriorLightLook.Glyph(WarriorLightLook.GreatSword, p), ClericLightFx.Band.Over, true, 850f, finishMs + 250f, swordW * 1.14f, swordW * 1.14f, swordH * 1.05f, swordH * 1.05f, 0.9f, new Vector2(0f, -8f), 1)
                    .Drop(dropFrom, dropMs).Env(90f, 260f)                                                        // hot orange outline round the crimson blade
                  .Upright(WarriorLightLook.Glyph(WarriorLightLook.GreatSword, Sword), ClericLightFx.Band.Over, true, // crimson blade, white-hot fuller, gold aura (concept)
                    850f, finishMs + 250f, swordW, swordW, swordH, swordH, 1f, new Vector2(0f, -6f), 2)
                    .Drop(dropFrom, dropMs).Env(90f, 260f);
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Line(new Vector2(0f, dropFrom + swordH + 40f), new Vector2(0f, swordH * 0.6f)), slamMs - 200f, 200f, 160f, 0.35f * H, p, 0.9f, RibbonSort.Over, 0);

            // Slam
            WarriorGrandFx.Impact(fx, slamMs, new Vector2(0f, 6f), 1.8f * H, p, 24);
            WarriorGrandFx.Shock(fx, slamMs, 380f, Vector2.zero, radius * 0.3f, radius * 1.2f, p);
            WarriorGrandFx.Scorch(fx, slamMs, totalMs - slamMs, Vector2.zero, radius * 0.85f, fire);
            for (int k = 0; k < 6; k++)
                WarriorGrandFx.Pillar(fx, slamMs + k * 20f, 520f, Ground(k * 60f, radius * 0.45f), 0.6f * H, 1.4f * H, fire, ClericLightFx.Band.Over, 3, k % 2 == 0 ? LayerKind.Main : LayerKind.Detail);
            fx.At(slamMs, f =>
            {
                UltimateFx.Debris(f.Anchor, f.Own ? 30 : 7, radius * 0.6f, 120f);
                UltimateFx.Dust(f.Anchor, f.Own ? 18 : 5, radius * 0.8f);
            });
            for (int i = 0; i < smalls; i++)
            {
                float t = slamMs + i * smallStepMs;
                fx.At(t, f => MechanicLightBursts.Sparks(f, f.Anchor + new Vector2(0f, 8f), 4, 130f, 90f, 360f, p.Light, 0.28f, 1.8f));
                if (i % 3 == 2) WarriorGrandFx.Shock(fx, t, 240f, Vector2.zero, radius * 0.25f, radius * 0.85f, p, 0.7f, 6);
            }
            WarriorGrandFx.Shock(fx, penultMs, 300f, Vector2.zero, radius * 0.5f, radius, p, 0.9f, 6);

            // Finisher
            WarriorGrandFx.Impact(fx, finishMs, new Vector2(0f, 8f), 2.2f * H, p, 30);
            WarriorGrandFx.Shock(fx, finishMs, 480f, Vector2.zero, radius * 0.4f, radius * 1.45f, p);
            WarriorGrandFx.Shock(fx, finishMs + 50f, 380f, Vector2.zero, radius * 0.2f, radius * 0.95f, fire, 0.85f, 6);
            WarriorGrandFx.Fireball(fx, finishMs, new Vector2(0f, 10f), 0.9f * H, fire);
            for (int k = 0; k < 8; k++)
                WarriorGrandFx.Pillar(fx, finishMs + k * 15f, 560f, Ground(k * 45f + 22.5f, radius * 0.6f), 0.7f * H, (k % 2 == 0 ? 2f : 1.5f) * H, fire, ClericLightFx.Band.Over, 3, k % 2 == 0 ? LayerKind.Main : LayerKind.Detail);
            WarriorLightSkills.SplitPulses(fx, "warrior.earth-break", finishMs, true, new Vector2(0f, 8f), p, 16f); // the finisher's extra damage numbers
            fx.At(finishMs, f =>
            {
                UltimateFx.Debris(f.Anchor, f.Own ? 40 : 9, radius * 0.6f, 140f);
                UltimateFx.Dust(f.Anchor, f.Own ? 24 : 6, radius * 0.8f);
            });

            // Burns: the cracks flare, small pillars and embers
            foreach (float b in burnAt)
            {
                fx.Ground(WarriorLightLook.Line(WarriorLightLook.Crack, fire), b, b + 220f, radius * 1.7f, radius * 1.75f, 1f, sq, 0f, 4).Env(40f, 120f).Kind(LayerKind.Glow);
                WarriorGrandFx.Pillar(fx, b, 360f, Ground(Random.value * 360f, radius * Random.Range(0.2f, 0.7f)), 0.45f * H, 0.9f * H, fire, ClericLightFx.Band.Over, 2, LayerKind.Detail);
                fx.At(b, f => WarriorGrandFx.Embers(f, f.Anchor, 8, radius * 0.6f, fire));
            }
        }

        /// <summary>A point on the ground ellipse at `deg`, `dist` units from the centre.</summary>
        static Vector2 Ground(float deg, float dist)
        {
            Vector2 d = WarriorLightSkills.Dir(deg) * dist;
            d.y *= ClericLightFx.Squash;
            return d;
        }
    }
}
