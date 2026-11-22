using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Picture of the Warrior's INFERNO CRASH, "RO trailer style" (21/11 evening): the Warrior's main real Fire skill, read as a high-level Magnum
    /// Break + ground slam, NOT a dark lava blast. Warrior Fire palette (honey #E0A35A / #F6D586 / core #FFF9DE, outer #B67C50), translucent,
    /// ivory-gold stronger than orange at the peak, fire &lt;= 20-30 % of the area, no red floor, no red rune. Times from the skill data: the leap
    /// lands at `landMs` (SkillTable Leap 400 ms), the barrage / blasts are the strike steps (13 small every 35 ms from 450, 940, finisher 1100).
    /// InfernoCrashUltimate keeps the pose, the lift and the shakes.
    ///  - 0-300 take-off: a thin honey ring, dust; 40-400 leap: a translucent honey ribbon following the hero's arc; 290-400 a falling streak;
    ///  - 400 landing: a honey ground burst (RoSoftSector full circle, ivory rim on the real radius) spreading low, a pale gold seal, a 1.8 H
    ///    star, a honey burst (no smoke), thin shock rings to 1.1 r, 6 small low flame tongues round the crater, a few embers;
    ///  - barrage: sparkles, a small flame every fourth; 940: star + ring; finisher: 2.3 H star, honey burst, rings to 1.35 r, the ground light
    ///    flares; gone by ~1700 ms.
    /// </summary>
    public static class WarriorInfernoLightFx
    {
        const float H = WarriorLightLook.H, Lift = 3f;

        public static void Build(HeroController hero, string skillId, Vector2 takeOff, Vector2 landing, Vector2 dir, float r, float landMs, float liftUnits, List<float> strikes, List<bool> finishers, float totalMs)
        {
            if (!WarriorLightLook.Ready || hero == null) return;
            ClericLightLook.Pal p = WarriorLightLook.Flame, gold = WarriorLightLook.Blade;
            float dim = ClericLightFx.DimFor(hero, null), sq = ClericLightFx.Squash;

            // Take-off + the leap trail (offsets from the take-off spot)
            var up = ClericLightFx.Create("InfernoLeap", takeOff, null, landMs + 600f, dim);
            WarriorGrandFx.Shock(up, 0f, 260f, Vector2.zero, 4f, 1.2f * H, p, 0.7f);
            up.At(0f, f => UltimateFx.Dust(takeOff, f.Own ? 6 : 2, 8f));
            Vector2 span = landing - takeOff;
            WarriorLightSkills.Slash(up, u => ArcRibbonFx.P(span * u, 1f, 10f + Mathf.Sin(u * Mathf.PI) * liftUnits), 40f, landMs - 40f, 230f, 0.36f * H, p, 0.9f, RibbonSort.Over, 1, null, 6f, true);

            // Landing / crater (offsets from the landing spot)
            float end = Mathf.Max(totalMs, 1700f);
            var fx = ClericLightFx.Create("InfernoCrater", landing, null, end, dim);
            Vector2 ring = new Vector2(0f, Lift);
            float side = dir.x < -0.1f ? 1f : -1f;
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Line3(ArcRibbonFx.P(new Vector2(side * 14f, 0f), 1f, 56f), ArcRibbonFx.P(Vector2.zero, 1f, ring.y + 2f)), landMs - 110f, 110f, 180f, 0.36f * H, p, 0.9f, RibbonSort.Over, 2);
            float seal = 2f * r / WarriorLightLook.SigilBand;
            fx.Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, gold), landMs, totalMs, seal * 0.9f, seal, 0.7f, sq, 20f, 4).Reveal(150f).Env(0f, 320f);
            WarriorGrandFx.Sector(fx, landMs, 1f, end - landMs - 100f, 420f, Vector2.zero, 90f, 360f, r, p, 0.18f, 0.36f, false, 0.15f, 2); // V4 review: circumference -30 %
            WarriorGrandFx.Impact(fx, landMs, ring + new Vector2(0f, 6f), 1.8f * H, p, 12);
            WarriorGrandFx.HoneyBurst(fx, landMs, ring + new Vector2(0f, 8f), 1.1f * H);
            WarriorGrandFx.Shock(fx, landMs + 60f, 320f, Vector2.zero, r * 0.2f, r * 0.75f, gold, 0.55f, 6);
            WarriorGrandFx.Shock(fx, landMs, 380f, Vector2.zero, r * 0.3f, r * 1.1f, p, 0.75f);
            for (int i = 0; i < 6; i++)
                WarriorGrandFx.Lick(fx, landMs + i * 15f + Random.Range(0f, 25f), 300f, WarriorGrandFx.OnGround(i * 60f + Random.Range(-15f, 15f), r * Random.Range(0.5f, 0.78f)),
                    Random.Range(0.42f, 0.55f) * H, Random.Range(0.45f, 0.6f) * H, ClericLightFx.Band.Over, 2, i % 2 == 0 ? LayerKind.Main : LayerKind.Detail, 0.9f);
            fx.At(landMs, f =>
            {
                UltimateFx.Debris(f.Anchor + ring, f.Own ? 14 : 4, r * 0.5f, 120f);
                UltimateFx.Dust(f.Anchor, f.Own ? 10 : 3, r * 0.6f);
                WarriorGrandFx.Embers(f, f.Anchor + ring, f.Own ? 10 : 3, r * 0.5f, p);
            });

            // Barrage: sparkles, a small flame every fourth; the two blasts
            int n = strikes.Count;
            for (int i = 0; i < n; i++)
            {
                float t = strikes[i];
                bool last = i == n - 1, blast = i == n - 2 || last;
                if (!blast)
                {
                    Vector2 at = WarriorGrandFx.OnGround(Random.value * 360f, r * Random.Range(0.15f, 0.75f));
                    fx.At(t, f => MechanicLightBursts.Sparks(f, f.Anchor + at + new Vector2(0f, 4f), 3, 110f, 90f, 360f, p.Light, 0.26f, 1.6f));
                    if (i % 4 == 1) WarriorGrandFx.Lick(fx, t, 240f, at, 0.38f * H, 0.6f * H, ClericLightFx.Band.Over, 1, LayerKind.Detail);
                    continue;
                }
                WarriorGrandFx.Impact(fx, t, ring + new Vector2(0f, 6f), (last ? 2.3f : 1.4f) * H, p, last ? 14 : 8);
                WarriorGrandFx.Shock(fx, t, last ? 460f : 320f, Vector2.zero, r * 0.4f, r * (last ? 1.35f : 1f), p, last ? 0.44f : 0.5f); // V5 review: finisher ring 0.42-0.46
                if (!last) continue;
                WarriorLightSkills.SplitPulses(fx, skillId, t, finishers[i], ring + new Vector2(0f, 6f), p, 16f); // the finisher's extra damage numbers
                WarriorGrandFx.HoneyBurst(fx, t, ring + new Vector2(0f, 8f), 1.2f * H);
                WarriorGrandFx.Shock(fx, t + 40f, 380f, Vector2.zero, r * 0.25f, r, gold, 0.6f, 6);
                WarriorGrandFx.Sector(fx, t - 10f, 1f, 420f, 340f, Vector2.zero, 90f, 360f, r * 1.1f, p, 0.14f, 0.45f, false, 0.35f, 3);
                fx.At(t, f => UltimateFx.Debris(f.Anchor + ring, f.Own ? 14 : 4, r * 0.6f, 140f));
            }
        }
    }
}
