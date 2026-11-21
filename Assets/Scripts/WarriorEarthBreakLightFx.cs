using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Picture of the Warrior's EARTH BREAK, "Golden Knight Judgment" (RO trailer style, 21/11 evening; concept
    /// docs/reference/warrior-fx-ro-trailer/concept-earth-break-chatgpt.png). The Warrior's ultimate standard: weight and force, not fire. The
    /// timeline and every hit stay in EarthBreakUltimate (slam 1450, 8 small hits every 45 ms from it, 1850, finisher 2000, burns 2150-2450);
    /// this lays the matching shapes once at the cast. Earth Break palette (#887C5A / #C6B37D / #F3E1A8 / #FFFFFF) + Knight gold:
    ///  - 0-620 at the caster: a gold seal draws itself in under him, a soft glow gathers on the blade, two translucent gold sweeps (450 / 600);
    ///  - 400-1450 at the target: a thin golden rune circle on the REAL radius draws itself in (telegraph), a faint gold ground light;
    ///  - 850-1450: the ChatGPT-painted giant sword (WarriorPaintedSword, regraded steel-ivory blade + gold runes) drops onto the centre with a
    ///    light streak ahead of it, lands EXACTLY at 1450 ms;
    ///  - 1450 slam: ivory contact flash, a 1.8 H star, a tall translucent golden light column round the sword (5 H), a thin shock ring to the
    ///    radius, short earth cracks with gold cores (fade by the end); small hits: sparkles, a thin ring every third; 1850: ring;
    ///  - 2000 finisher: a 2.2 H white-gold star, thin rings to 1.2 R, 4 low gold-white light columns round the circle, the rune circle flares;
    ///  - 2150-2450 burns: the cracks pulse gold; the sword dissolves into golden light over the end.
    /// No fire pillars, fireball, embers, scorch or red. Shake / hit-stop stay in EarthBreakUltimate (local caster only).
    /// </summary>
    public static class WarriorEarthBreakLightFx
    {
        const float H = WarriorLightLook.H;

        public static void Build(HeroController hero, Vector2 caster, Vector2 center, Vector2 dir, float radius, float slamMs, float smallStepMs, int smalls,
            float penultMs, float finishMs, float[] burnAt, float totalMs)
        {
            if (!WarriorLightLook.Ready || hero == null) return;
            ClericLightLook.Pal p = WarriorLightLook.Earth, gold = WarriorLightLook.Blade;
            float dim = ClericLightFx.DimFor(hero, null), sq = ClericLightFx.Squash, aim = WarriorLightSkills.AngleOf(dir);
            float seal = 2f * radius / WarriorLightLook.SigilBand;

            // Caster: charge seal, blade glow, two forward sweeps
            var me = ClericLightFx.Create("EarthBreakCharge", caster, hero, 1050f, dim);
            me.Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, gold), 0f, 620f, 1.6f * H / WarriorLightLook.SigilBand, 1.6f * H / WarriorLightLook.SigilBand, 0.8f, sq, 30f, 4)
                .Reveal(380f).Phases(380f, 460f, 0.5f, 0.7f).Env(0f, 140f);
            WarriorGrandFx.Charge(me, 120f, 450f, new Vector2(0f, 13f) + dir * 7f, 1f * H, gold);
            const float Sq = 0.6f;
            float mid = WarriorLightLook.ParamDeg(aim, Sq), rr = Mathf.Min(radius, 40f) / WarriorLightSkills.Norm(mid, Sq);
            WarriorLightSkills.Slash(me, ArcRibbonFx.Arc(new Vector2(0f, 9f), rr, Sq, mid + 75f, mid - 75f), 450f, 160f, 260f, 0.36f * H, gold, 0.9f, RibbonSort.Over, 1);
            WarriorLightSkills.Slash(me, ArcRibbonFx.Arc(new Vector2(0f, 9f), rr * 0.8f, Sq, mid - 70f, mid + 70f), 600f, 150f, 240f, 0.28f * H, gold, 0.85f, RibbonSort.Over, 0);

            // Target: rune circle + faint ground light, the falling sword
            var fx = ClericLightFx.Create("EarthBreak", center, null, totalMs, dim);
            float swordH = 2.7f * H, dropFrom = 95f, dropMs = (slamMs - 850f) / 0.37f; // PopCurve first reaches the ground at 37 % of the drop
            fx.Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, p), 400f, totalMs, seal, seal, 0.68f, sq, 10f, 4)
                .Reveal(600f).Phases(slamMs - 400f, slamMs + 120f, 0.6f, 0.75f).Env(0f, 320f);
            WarriorGrandFx.Sector(fx, 400f, 600f, totalMs - 400f, 360f, Vector2.zero, 90f, 360f, radius, p, 0.12f, 0.3f, false, 0.2f, 2);
            if (!WarriorPaintedSword.Lay(fx, 850f, totalMs, swordH, 8f, dropFrom, dropMs, slamMs, totalMs - finishMs + 50f, gold)) // V3 review: dissolve from ~1950 (60-75 % by 2350)
                fx.Upright(WarriorLightLook.Glyph(WarriorLightLook.GreatSword, p), ClericLightFx.Band.Over, true, 850f, finishMs + 250f, 1.4f * H, 1.4f * H, swordH, swordH, 0.9f, new Vector2(0f, -8f), 2)
                    .Drop(dropFrom, dropMs).Env(90f, 260f);
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Line(new Vector2(0f, dropFrom + swordH + 40f), new Vector2(0f, swordH * 0.6f)), slamMs - 200f, 200f, 160f, 0.3f * H, gold, 0.8f, RibbonSort.Over, 0);

            // Slam (exactly at slamMs = 1450)
            WarriorGrandFx.Impact(fx, slamMs, new Vector2(0f, 6f), 1.8f * H, p, 14);
            WarriorGrandFx.Column(fx, slamMs, finishMs - slamMs + 260f, new Vector2(0f, -4f), 1.1f * H, 5f * H, gold, ClericLightFx.Band.Over, 1, LayerKind.Main, 0.2f); // r1: 0.5 / 1.5 H washed the slam white
            WarriorGrandFx.Shock(fx, slamMs, 380f, Vector2.zero, radius * 0.3f, radius, p, 0.75f);
            fx.Ground(WarriorLightLook.Line(WarriorLightLook.Crack, p), slamMs, totalMs, radius * 1.3f, radius * 1.4f, 0.85f, sq, 0f, 3).Env(40f, 380f).Kind(LayerKind.Main);
            fx.At(slamMs, f =>
            {
                UltimateFx.Debris(f.Anchor, f.Own ? 14 : 4, radius * 0.5f, 110f);
                UltimateFx.Dust(f.Anchor, f.Own ? 12 : 4, radius * 0.7f);
            });
            for (int i = 0; i < smalls; i++)
            {
                float t = slamMs + i * smallStepMs;
                fx.At(t, f => MechanicLightBursts.Sparks(f, f.Anchor + new Vector2(0f, 8f), 3, 120f, 90f, 360f, p.Light, 0.26f, 1.6f));
                if (i % 3 == 2) WarriorGrandFx.Shock(fx, t, 240f, Vector2.zero, radius * 0.25f, radius * 0.8f, p, 0.5f, 6);
            }
            WarriorGrandFx.Shock(fx, penultMs, 300f, Vector2.zero, radius * 0.5f, radius, p, 0.65f, 6);

            // Finisher (2000)
            WarriorGrandFx.Impact(fx, finishMs, new Vector2(0f, 8f), 1.8f * H, p, 16);
            WarriorGrandFx.Shock(fx, finishMs, 460f, Vector2.zero, radius * 0.4f, radius * 1.2f, p, 0.8f);
            WarriorGrandFx.Shock(fx, finishMs + 50f, 380f, Vector2.zero, radius * 0.2f, radius * 0.9f, gold, 0.6f, 6);
            WarriorGrandFx.Sector(fx, finishMs - 10f, 1f, 420f, 360f, Vector2.zero, 90f, 360f, radius, p, 0.14f, 0.8f, false, 0.3f, 3);
            for (int k = 0; k < 4; k++)
                WarriorGrandFx.Column(fx, finishMs + k * 25f, 520f, WarriorGrandFx.OnGround(k * 90f + 45f + aim, radius * 0.8f), 0.6f * H, 2.2f * H, gold,
                    ClericLightFx.Band.Over, 2, k % 2 == 0 ? LayerKind.Main : LayerKind.Detail, 0.45f);
            WarriorLightSkills.SplitPulses(fx, "warrior.earth-break", finishMs, true, new Vector2(0f, 8f), p, 16f); // the finisher's extra damage numbers
            fx.At(finishMs, f =>
            {
                UltimateFx.Debris(f.Anchor, f.Own ? 16 : 4, radius * 0.6f, 130f);
                UltimateFx.Dust(f.Anchor, f.Own ? 14 : 4, radius * 0.8f);
            });

            // Burns: the cracks pulse gold, a few sparkles
            foreach (float b in burnAt)
            {
                fx.Ground(WarriorLightLook.Line(WarriorLightLook.Crack, gold), b, b + 200f, radius * 1.3f, radius * 1.35f, 0.7f, sq, 0f, 4).Env(40f, 120f).Kind(LayerKind.Glow);
                fx.At(b, f => f.Puff(true, f.Anchor, f.Count(4), radius * 0.5f, 26f, 1.4f, 2.2f, p.Light, 0.5f));
            }
        }
    }
}
