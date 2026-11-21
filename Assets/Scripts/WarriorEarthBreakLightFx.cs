using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Picture of the Warrior's EARTH BREAK in "RO3 Soft Fantasy V1" (2026-11-21; replaces the 24 painted frames of fx/earthbreak). The timeline and
    /// every hit stay in EarthBreakUltimate (slam 1450, 8 small hits every 45 ms from it, 1850, finisher 2000, burns 2150-2450); this lays the
    /// matching shapes once at the cast. Earth palette (amber gold, brown rim) + Flame for the burns (rule 8: 2 main colours):
    ///  - 0-560 at the caster: a war seal draws itself in under him (Reveal, ~40 % until 380 ms, then full), motes drawn into the blade;
    ///  - 450 / 600: two gold Mesh Trail sweeps in front of him (the old "golden sweep");
    ///  - 400-1450 at the target: the seal at the real radius draws itself in (telegraph), a soft pool grows;
    ///  - 850-1450: a big light sword (tip down) drops onto the centre and lands at the slam (RO3 5.4: a big light sword earns a ground contact);
    ///  - 1450 slam: flash, cracks on the ground (dissolve out), a wave to 1.2 R, sparks, rocks, dust; small hits: sparks at the blade, a small
    ///    wave every third; 1850: wave; 2000 finisher: the largest wave, flash, 4 light streaks rising round the sword, rocks, dust;
    ///  - 2150-2450 burns: the cracks glow orange and embers rise, 4 times.
    /// Shake / hit-stop stay in EarthBreakUltimate (local caster only).
    /// </summary>
    public static class WarriorEarthBreakLightFx
    {
        const float H = WarriorLightLook.H;

        public static void Build(HeroController hero, Vector2 caster, Vector2 center, Vector2 dir, float radius, float slamMs, float smallStepMs, int smalls,
            float penultMs, float finishMs, float[] burnAt, float totalMs)
        {
            if (!WarriorLightLook.Ready || hero == null) return;
            ClericLightLook.Pal p = WarriorLightLook.Earth, fire = WarriorLightLook.Flame;
            float dim = ClericLightFx.DimFor(hero, null), sq = ClericLightFx.Squash, aim = WarriorLightSkills.AngleOf(dir);
            float seal = 2f * radius / WarriorLightLook.SigilBand, wave = 2f / WarriorLightLook.BandRing;

            // Caster: charge seal, gather, two forward sweeps
            var me = ClericLightFx.Create("EarthBreakCharge", caster, hero, 1050f, dim);
            me.Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, p), 0f, 620f, 1.5f * H / WarriorLightLook.SigilBand, 1.5f * H / WarriorLightLook.SigilBand, 0.8f, sq, 30f, 4)
                .Reveal(380f).Phases(380f, 460f, 0.4f, 0.6f).Env(0f, 140f)
              .Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, 280f, 560f, 4f, 10f, 4f, 10f, 0.7f, new Vector2(0f, 13f) + dir * 6f).Phases(100f, 180f, 0.4f, 0.5f);
            for (int i = 0; i < 4; i++) me.At(i * 110f, f => Converge(f, 22f, 4, p.Light));
            const float Sq = 0.6f;
            float mid = WarriorLightLook.ParamDeg(aim, Sq), rr = Mathf.Min(radius, 40f) / WarriorLightSkills.Norm(mid, Sq);
            WarriorLightSkills.Slash(me, ArcRibbonFx.Arc(new Vector2(0f, 9f), rr, Sq, mid + 75f, mid - 75f), 450f, 160f, 240f, 0.27f * H, p, 1f, RibbonSort.Over, 1);
            WarriorLightSkills.Slash(me, ArcRibbonFx.Arc(new Vector2(0f, 9f), rr * 0.8f, Sq, mid - 70f, mid + 70f), 600f, 150f, 220f, 0.2f * H, p, 0.9f, RibbonSort.Over, 0);

            // Target: telegraph seal + pool, the sword
            var fx = ClericLightFx.Create("EarthBreak", center, null, totalMs, dim);
            fx.Ground(WarriorLightLook.Fill(p), 400f, 1700f, radius * 1.2f, radius * 1.9f, 0.85f).Env(220f, 250f).Kind(LayerKind.Glow)
              .Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, p), 400f, 1750f, seal, seal, 0.72f, sq, 12f, 4)
                .Reveal(600f).Phases(slamMs - 400f, slamMs - 300f, 0.5f, 0.55f).Env(0f, 260f)
              .Upright(WarriorLightLook.Glyph(WarriorLightLook.GreatSword, p), ClericLightFx.Band.Over, true, 850f, finishMs + 250f, 13f, 13f, 39f, 39f, 0.95f, new Vector2(0f, -3f), 2)
                .Drop(58f, (slamMs - 850f) / 0.37f).Env(90f, 260f)                                                    // PopCurve (ease-out back) first reaches the ground at 37 % of the drop, dips ~6 u in, settles
              .Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, 850f, slamMs + 120f, 8f, 16f, 8f, 16f, 0.35f, new Vector2(0f, 16f), 1).Kind(LayerKind.Glow);

            // Slam
            fx.Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, slamMs, slamMs + 170f, 16f, 34f, 12f, 26f, 0.85f, new Vector2(0f, 6f), 4)
              .Ground(WarriorLightLook.Line(WarriorLightLook.Crack, p), slamMs, totalMs, radius * 1.7f, radius * 1.75f, 0.8f, sq, 0f, 5).Env(60f, 420f).Dissolve()
              .Ground(WarriorLightLook.Wave(p), slamMs, slamMs + 350f, radius * 0.3f, radius * 1.2f * wave, 0.85f, sq, 0f, 6)
              .At(slamMs, f =>
              {
                  f.Ring(true, f.Anchor + new Vector2(0f, 6f), 14, 90f, 1.8f, 2.8f, p.Light, 0.4f);
                  UltimateFx.Debris(f.Anchor, f.Own ? 24 : 6, radius * 0.6f, 110f);
                  UltimateFx.Dust(f.Anchor, f.Own ? 16 : 5, radius * 0.8f);
              });
            for (int i = 0; i < smalls; i++)
            {
                float t = slamMs + i * smallStepMs;
                bool ring = i % 3 == 2;
                fx.At(t, f => f.Puff(true, f.Anchor + new Vector2(0f, 3f), 2, 6f, 30f, 1.4f, 2.2f, p.Light, 0.3f));
                if (ring) fx.Ground(WarriorLightLook.Wave(p), t, t + 220f, radius * 0.25f, radius * wave * 0.85f, 0.55f, sq, 0f, 6).Kind(LayerKind.Detail);
            }
            fx.Ground(WarriorLightLook.Wave(p), penultMs, penultMs + 280f, radius * 0.6f, radius * wave, 0.7f, sq, 0f, 6);

            // Finisher
            fx.Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, finishMs, finishMs + 220f, 20f, 40f, 15f, 30f, 0.9f, new Vector2(0f, 8f), 4)
              .Ground(WarriorLightLook.Wave(p), finishMs, finishMs + 450f, radius * 0.4f, radius * 1.45f * wave, 0.9f, sq, 0f, 7)
              .Ground(WarriorLightLook.Wave(p), finishMs + 40f, finishMs + 360f, radius * 0.2f, radius * 0.9f * wave, 0.6f, sq, 0f, 6).Kind(LayerKind.Detail);
            for (int k = 0; k < 4; k++)
            {
                Vector2 at = WarriorLightSkills.Dir(45f + k * 90f) * radius * 0.35f;
                at.y *= sq;
                fx.Upright(ClericLightLook.M(ClericLightLook.Streak, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, true, finishMs + k * 25f, finishMs + 420f + k * 25f, 3f, 2.4f, 22f, 32f, 0.8f, at, 3).Rise(6f).Kind(LayerKind.Detail);
            }
            WarriorLightSkills.SplitPulses(fx, "warrior.earth-break", finishMs, true, new Vector2(0f, 8f), p, 14f); // the finisher's extra damage numbers
            fx.At(finishMs, f =>
            {
                f.Ring(true, f.Anchor + new Vector2(0f, 6f), 24, 120f, 1.8f, 3.2f, p.Light, 0.45f);
                UltimateFx.Debris(f.Anchor, f.Own ? 34 : 8, radius * 0.6f, 120f);
                UltimateFx.Dust(f.Anchor, f.Own ? 22 : 6, radius * 0.8f);
            });

            // Burns: the cracks glow orange, embers rise
            foreach (float b in burnAt)
            {
                fx.Ground(WarriorLightLook.Line(WarriorLightLook.Crack, fire), b, b + 200f, radius * 1.7f, radius * 1.75f, 0.55f, sq, 0f, 6).Env(40f, 120f).Kind(LayerKind.Glow);
                fx.At(b, f => f.Puff(false, f.Anchor, 6, radius * 0.6f, 30f, 1.4f, 2.2f, fire.Light, 0.6f));
            }
        }

        /// <summary>`n` motes drawn in toward the anchor from `dist` units away (a charge).</summary>
        static void Converge(ClericLightFx f, float dist, int n, Color c)
        {
            n = f.Count(n);
            for (int i = 0; i < n; i++)
            {
                Vector2 d = WarriorLightSkills.Dir(Random.value * 360f);
                Vector2 from = f.Anchor + new Vector2(0f, 8f) + new Vector2(d.x, d.y * ClericLightFx.Squash) * dist;
                f.Emit(false, from, -new Vector2(d.x, d.y * ClericLightFx.Squash) * dist * 3f, Random.Range(1.5f, 2.3f), c, 0.3f);
            }
        }
    }
}
