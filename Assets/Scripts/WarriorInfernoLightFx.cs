using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Picture of the Warrior's INFERNO CRASH (21/11: first "RO3 Soft Fantasy V1" pass, then the GRAND pass: the Warrior's most destructive skill,
    /// spec + concept in docs/reference/warrior-fx-grand/). Flame palette (deep orange-red, gold, white-yellow core, dark rim). Times from the skill
    /// data: the leap lands at `landMs` (SkillTable Leap 400 ms), the barrage / blasts are the strike steps (13 small every 35 ms from 450, 940,
    /// finisher 1100). InfernoCrashUltimate keeps the pose, the lift and the shakes.
    ///  - 0-300 take-off: scorch, a small shock, 3 flame tongues at the feet;
    ///  - 40-400 leap: a thick fire ribbon following the hero's arc; 290-400: a falling flame streak onto the landing spot;
    ///  - 400 landing: white-hot impact 2.6 H, fireball, thick shock to 1.15 r, scorch + glowing cracks lingering to the end, the war seal,
    ///    a central fire pillar 2.5 H and a ring of 10 flame spikes round the crater, rocks, dust; small hits: sparks, a pillar every third;
    ///  - 940: shock + impact; finisher: the largest impact (3.4 H), fireball, shocks to 1.4 r, 8 tall spikes, rocks; gone by ~1700 ms.
    /// </summary>
    public static class WarriorInfernoLightFx
    {
        const float H = WarriorLightLook.H, Lift = 3f;

        public static void Build(HeroController hero, string skillId, Vector2 takeOff, Vector2 landing, Vector2 dir, float r, float landMs, float liftUnits, List<float> strikes, List<bool> finishers, float totalMs)
        {
            if (!WarriorLightLook.Ready || hero == null) return;
            ClericLightLook.Pal p = WarriorLightLook.Flame;
            float dim = ClericLightFx.DimFor(hero, null), sq = ClericLightFx.Squash;

            // Take-off + the leap trail (offsets from the take-off spot)
            var up = ClericLightFx.Create("InfernoLeap", takeOff, null, landMs + 700f, dim);
            WarriorGrandFx.Shock(up, 0f, 260f, Vector2.zero, 4f, 1.2f * H, p, 0.9f);
            WarriorGrandFx.Scorch(up, 0f, 700f, Vector2.zero, 0.5f * H, p);
            for (int i = 0; i < 3; i++)
                WarriorGrandFx.Pillar(up, i * 30f, 300f, new Vector2((i - 1) * 7f, -1f), 0.45f * H, 0.9f * H, p, ClericLightFx.Band.Over, 2, LayerKind.Detail);
            Vector2 span = landing - takeOff;
            WarriorLightSkills.Slash(up, u => span * u + new Vector2(0f, 10f + Mathf.Sin(u * Mathf.PI) * liftUnits), 40f, landMs - 40f, 230f, 0.42f * H, p, 1f, RibbonSort.Over, 1, null, 6f, true);

            // Landing / crater (offsets from the landing spot)
            var fx = ClericLightFx.Create("InfernoCrater", landing, null, Mathf.Max(totalMs, 1700f), dim);
            Vector2 ring = new Vector2(0f, Lift);
            float side = dir.x < -0.1f ? 1f : -1f;
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Line(new Vector2(side * 14f, 56f), ring + new Vector2(0f, 2f)), landMs - 110f, 110f, 180f, 0.45f * H, p, 1f, RibbonSort.Over, 2);
            float seal = 2f * r / WarriorLightLook.SigilBand, crater = Mathf.Max(totalMs, 1700f) - landMs;
            fx.Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, p), landMs, totalMs, seal * 0.9f, seal, 0.9f, sq, 20f, 4).Reveal(150f).Env(0f, 320f);
            WarriorGrandFx.Impact(fx, landMs, ring + new Vector2(0f, 6f), 1.8f * H, p, 26);
            WarriorGrandFx.Fireball(fx, landMs, ring + new Vector2(0f, 8f), 1.35f * H, p);
            WarriorGrandFx.Shock(fx, landMs + 60f, 320f, Vector2.zero, r * 0.2f, r * 0.75f, p, 0.7f, 6);
            WarriorGrandFx.Shock(fx, landMs, 380f, Vector2.zero, r * 0.3f, r * 1.15f, p);
            WarriorGrandFx.Scorch(fx, landMs, crater, Vector2.zero, r * 0.8f, p);
            WarriorGrandFx.Pillar(fx, landMs, 620f, new Vector2(0f, -2f), 1f * H, 2.5f * H, p, ClericLightFx.Band.Over, 3);
            for (int i = 0; i < 10; i++)
                WarriorGrandFx.Pillar(fx, landMs + i * 12f + Random.Range(0f, 30f), 520f, Ground(i * 36f + Random.Range(-14f, 14f), r * Random.Range(0.45f, 0.78f)), Random.Range(0.45f, 0.65f) * H, Random.Range(1f, 1.8f) * H, p, ClericLightFx.Band.Over, 2, i % 2 == 0 ? LayerKind.Main : LayerKind.Detail);
            fx.At(landMs, f =>
            {
                UltimateFx.Debris(f.Anchor + ring, f.Own ? 34 : 8, r * 0.6f, 140f);
                UltimateFx.Dust(f.Anchor, f.Own ? 20 : 5, r * 0.7f);
            });

            // Barrage: sparks, a pillar every third; the two blasts
            int n = strikes.Count;
            for (int i = 0; i < n; i++)
            {
                float t = strikes[i];
                bool last = i == n - 1, blast = i == n - 2 || last;
                if (!blast)
                {
                    Vector2 at = Ground(Random.value * 360f, r * Random.Range(0.15f, 0.75f));
                    fx.At(t, f => MechanicLightBursts.Sparks(f, f.Anchor + at + new Vector2(0f, 4f), 4, 120f, 90f, 360f, p.Light, 0.3f, 1.8f));
                    if (i % 3 == 1) WarriorGrandFx.Pillar(fx, t, 340f, at, 0.45f * H, 1f * H, p, ClericLightFx.Band.Over, 1, LayerKind.Detail);
                    continue;
                }
                WarriorGrandFx.Impact(fx, t, ring + new Vector2(0f, 6f), (last ? 2.4f : 1.4f) * H, p, last ? 30 : 14);
                WarriorGrandFx.Shock(fx, t, last ? 480f : 320f, Vector2.zero, r * 0.4f, r * (last ? 1.4f : 1f), p, last ? 1f : 0.9f);
                if (!last) continue;
                WarriorLightSkills.SplitPulses(fx, skillId, t, finishers[i], ring + new Vector2(0f, 6f), p, 16f); // the finisher's extra damage numbers
                WarriorGrandFx.Fireball(fx, t, ring + new Vector2(0f, 8f), 1.2f * H, p);
                WarriorGrandFx.Pillar(fx, t, 640f, new Vector2(0f, -2f), 1.15f * H, 2f * H, p, ClericLightFx.Band.Over, 4); // one continuous central eruption (ChatGPT round 3)
                for (int k = 0; k < 8; k++)
                    WarriorGrandFx.Pillar(fx, t + k * 12f + Random.Range(0f, 25f), 560f, Ground(k * 45f + Random.Range(-15f, 15f), r * Random.Range(0.35f, 0.65f)), Random.Range(0.55f, 0.8f) * H, Random.Range(1.4f, 2.3f) * H, p, ClericLightFx.Band.Over, 3, k % 2 == 0 ? LayerKind.Main : LayerKind.Detail);
                fx.At(t, f => UltimateFx.Debris(f.Anchor + ring, f.Own ? 30 : 7, r * 0.7f, 160f));
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
