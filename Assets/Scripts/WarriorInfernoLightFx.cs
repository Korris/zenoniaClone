using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Picture of the Warrior's INFERNO CRASH in "RO3 Soft Fantasy V1" (2026-11-21; replaces the 24 painted frames of fx/infernocrash). Flame palette
    /// (soft orange, gold highlight). Times from the skill data: the leap lands at `landMs` (SkillTable Leap 400 ms), the barrage / blasts are the
    /// strike steps (13 small every 35 ms from 450, 940, finisher 1100). InfernoCrashUltimate keeps the pose, the lift and the shakes.
    ///  - 0-260 take-off: 3 small flame tongues + a warm pool at the feet;
    ///  - 40-400 leap: a fire Mesh Trail following the hero's arc in the air (same path as the pose lift);
    ///  - 290-400: a falling flame slash onto the landing spot;
    ///  - 400 landing: flash, the war seal on the crater radius drawing itself in fast, cracks, pool, a wave, 8 flame tongues round the crater,
    ///    sparks, rocks, dust; each small hit an ember pair (a small tongue every third); 940: wave + flash;
    ///  - finisher: the largest wave, flash, 6 tall tongues, sparks, rocks; everything gone by ~1300 ms.
    /// </summary>
    public static class WarriorInfernoLightFx
    {
        const float H = WarriorLightLook.H, Lift = 3f;

        public static void Build(HeroController hero, string skillId, Vector2 takeOff, Vector2 landing, Vector2 dir, float r, float landMs, float liftUnits, List<float> strikes, List<bool> finishers, float totalMs)
        {
            if (!WarriorLightLook.Ready || hero == null) return;
            ClericLightLook.Pal p = WarriorLightLook.Flame;
            float dim = ClericLightFx.DimFor(hero, null), sq = ClericLightFx.Squash, wave = 2f / WarriorLightLook.BandRing;
            Material tongue = WarriorLightLook.Glyph(WarriorLightLook.FlameTongue, p);

            // Take-off + the leap trail (offsets from the take-off spot)
            var up = ClericLightFx.Create("InfernoLeap", takeOff, null, landMs + 300f, dim);
            up.Ground(WarriorLightLook.Fill(p), 0f, 300f, 14f, 26f, 0.8f).Kind(LayerKind.Glow);
            for (int i = 0; i < 3; i++)
                up.Upright(tongue, ClericLightFx.Band.Over, true, i * 30f, 260f + i * 30f, 3f, 5f, 4f, 11f, 0.8f, new Vector2((i - 1) * 6f, -1f), 2).Pop(120f).Dissolve();
            Vector2 span = landing - takeOff;
            WarriorLightSkills.Slash(up, u => span * u + new Vector2(0f, 10f + Mathf.Sin(u * Mathf.PI) * liftUnits), 40f, landMs - 40f, 210f, 0.24f * H, p, 0.95f, RibbonSort.Over, 1, null, 6f, true);

            // Landing / crater (offsets from the landing spot)
            var fx = ClericLightFx.Create("InfernoCrater", landing, null, totalMs, dim);
            Vector2 ring = new Vector2(0f, Lift);
            float side = dir.x < -0.1f ? 1f : -1f;
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Line(new Vector2(side * 12f, 46f), ring + new Vector2(0f, 2f)), landMs - 110f, 110f, 170f, 0.26f * H, p, 1f, RibbonSort.Over, 2);
            float seal = 2f * r / WarriorLightLook.SigilBand;
            fx.Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, landMs, landMs + 170f, 18f, 36f, 13f, 26f, 0.85f, ring + new Vector2(0f, 4f), 4)
              .Ground(WarriorLightLook.Fill(p), landMs, totalMs, r * 1.3f, r * 2f, 0.85f).Env(60f, 350f).Kind(LayerKind.Glow)
              .Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, p), landMs, totalMs, seal * 0.9f, seal, 0.78f, sq, 20f, 4).Reveal(150f).Env(0f, 320f)
              .Ground(WarriorLightLook.Line(WarriorLightLook.Crack, p), landMs, totalMs, r * 1.5f, r * 1.6f, 0.8f, sq, 0f, 5).Env(40f, 350f).Dissolve()
              .Ground(WarriorLightLook.Wave(p), landMs, landMs + 350f, r * 0.3f, r * 1.15f * wave, 0.85f, sq, 0f, 6);
            for (int i = 0; i < 8; i++)
            {
                Vector2 at = Ground(i * 45f + 22f, r * 0.62f);
                float t0 = landMs + i * 15f;
                fx.Upright(tongue, ClericLightFx.Band.Over, true, t0, t0 + 460f, 4f, 7f, 5f, 16f, 0.85f, at, 2).Pop(150f).Dissolve().Kind(i % 2 == 0 ? LayerKind.Main : LayerKind.Detail);
            }
            fx.At(landMs, f =>
            {
                f.Ring(true, f.Anchor + ring, 16, 95f, 1.8f, 2.8f, p.Light, 0.4f);
                UltimateFx.Debris(f.Anchor + ring, f.Own ? 28 : 7, r * 0.6f, 130f);
                UltimateFx.Dust(f.Anchor, f.Own ? 18 : 5, r * 0.7f);
            });

            // Barrage: ember pairs, a small tongue every third; the two blasts
            int n = strikes.Count;
            for (int i = 0; i < n; i++)
            {
                float t = strikes[i];
                bool last = i == n - 1, blast = i == n - 2 || last;
                if (!blast)
                {
                    Vector2 at = Ground(Random.value * 360f, r * Random.Range(0.15f, 0.75f));
                    fx.At(t, f => f.Puff(true, f.Anchor + at, 2, 4f, 34f, 1.3f, 2.1f, p.Light, 0.3f));
                    if (i % 3 == 1) fx.Upright(tongue, ClericLightFx.Band.Over, true, t, t + 300f, 3f, 5f, 4f, 11f, 0.75f, at, 1).Pop(110f).Dissolve().Kind(LayerKind.Detail);
                    continue;
                }
                fx.Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, t, t + (last ? 220f : 160f), last ? 20f : 12f, last ? 40f : 24f, last ? 15f : 9f, last ? 30f : 18f, last ? 0.9f : 0.7f, ring + new Vector2(0f, 4f), 4)
                  .Ground(WarriorLightLook.Wave(p), t, t + (last ? 450f : 300f), r * 0.4f, r * (last ? 1.35f : 1f) * wave, last ? 0.9f : 0.7f, sq, 0f, 7);
                if (!last) continue;
                WarriorLightSkills.SplitPulses(fx, skillId, t, finishers[i], ring + new Vector2(0f, 6f), p, 13f); // the finisher's extra damage numbers
                for (int k = 0; k < 6; k++)
                    fx.Upright(tongue, ClericLightFx.Band.Over, true, t + k * 12f, t + 420f + k * 12f, 5f, 8f, 6f, 20f, 0.9f, Ground(k * 60f, r * 0.45f), 3).Pop(140f).Dissolve();
                fx.At(t, f =>
                {
                    f.Ring(true, f.Anchor + ring, 18, 120f, 1.8f, 3f, p.Light, 0.45f);
                    UltimateFx.Debris(f.Anchor + ring, f.Own ? 20 : 5, r * 0.7f, 140f);
                });
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
