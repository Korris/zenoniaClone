using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Picture of the Warrior's BLADE CYCLONE in "RO3 Soft Fantasy V1" (2026-11-21; replaces the 24 painted frames of fx/bladecyclone). Follows the
    /// caster like the skill. Hits and their times stay in BladeCycloneUltimate (16 small every 100 ms from 500, 2100, finisher 2250); this lays the
    /// matching shapes once at the cast, Blade palette only:
    ///  - ground: soft pool, the war seal exactly on the radius drawing itself in (Reveal 450 ms, then ~55 %, slow spin), a thin ring with 3 bright
    ///    arcs running round (rule 6 boundary);
    ///  - 0-500 cast: 3 short streaks rising behind the hero, motes drawn in;
    ///  - 450-1300 spin: 3 Mesh Trail ribbons orbiting at 0.5 / 0.66 / 0.82 r (2.5 turns / s, far half behind the hero, near half in front);
    ///  - 1250-2250 peak: 4 wider ribbons at 3.5 turns / s;
    ///  - every small hit: 2 sparks thrown off the ring; every 4th and the strong one: a wave ring to the edge;
    ///  - 2250 finisher: flash, the largest wave (1.35 r), one last ribbon slashing round at the edge, sparks, rocks, dust; fades by 2750.
    /// Shake / hit-stop / punch / flash and the loop sound stay in BladeCycloneUltimate (local caster only).
    /// </summary>
    public static class WarriorCycloneLightFx
    {
        const float H = WarriorLightLook.H, Lift = 5f;

        public static void Build(HeroController hero, float r, float[] waveAt, float castMs, float spinFrom, float spinTo, float peakFrom, float peakTo, float totalMs)
        {
            if (!WarriorLightLook.Ready || hero == null) return;
            ClericLightLook.Pal p = WarriorLightLook.Blade;
            float sq = ClericLightFx.Squash, wave = 2f / WarriorLightLook.BandRing;
            Vector2 ring = new Vector2(0f, Lift);
            var fx = ClericLightFx.Create("BladeCyclone", hero.Position, hero, totalMs, ClericLightFx.DimFor(hero, null));
            fx.Ground(WarriorLightLook.Fill(p), 0f, totalMs, r * 1.4f, r * 2f, 0.85f).Env(150f, 350f).Kind(LayerKind.Glow)
              .Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, p), 0f, totalMs, 2f * r / WarriorLightLook.SigilBand, 2f * r / WarriorLightLook.SigilBand, 0.78f, sq, 25f, 4)
                .Reveal(450f).Phases(450f, 560f, 0.45f, 0.55f).Env(0f, 400f)
              .Ground(ClericLightLook.M(ClericLightLook.Band, p, ClericLightLook.Role.Arc), spinFrom, totalMs, r * wave * 0.98f, r * wave, 0.6f, sq, 0f, 5).Env(120f, 400f).Arc(-8f, 0f, 3).Kind(LayerKind.Detail);
            float[] xs = { -6f, 0f, 6f };
            for (int i = 0; i < xs.Length; i++)
                fx.Upright(ClericLightLook.M(ClericLightLook.Streak, p, ClericLightLook.Role.Glyph), ClericLightFx.Band.Behind, true, i * 50f, castMs - 50f + i * 30f, 3f, 2.4f, 14f, 22f, 0.8f, new Vector2(xs[i], 2f), 1).Rise(6f);
            for (int i = 0; i < 4; i++) fx.At(i * 100f, f => In(f, r * 0.7f, 3, p.Light));

            float start = Random.value * 360f;
            for (int k = 0; k < 3; k++) Orbit(fx, hero, ring, r * (0.5f + 0.16f * k), start + k * 120f, spinFrom - 40f, spinTo + 40f, 2.5f, 140f, 0.17f * H, p);
            for (int k = 0; k < 4; k++) Orbit(fx, hero, ring, r * (0.45f + 0.15f * k), start + k * 90f + 45f, peakFrom, peakTo, 3.5f, 130f, 0.22f * H, p);

            int n = waveAt.Length;
            for (int i = 0; i < n; i++)
            {
                float t = waveAt[i];
                bool last = i == n - 1, penult = i == n - 2;
                if (!last) fx.At(t, f => Throw(f, r * 0.8f, penult ? 6 : 2, p.Light));
                if (penult || !last && i % 4 == 3)
                    fx.Ground(WarriorLightLook.Wave(p), t, t + 240f, r * 0.5f, r * wave, penult ? 0.8f : 0.55f, sq, 0f, 6).Kind(penult ? LayerKind.Main : LayerKind.Detail);
            }
            float fin = n > 0 ? waveAt[n - 1] : peakTo;
            fx.Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, fin, fin + 200f, 14f, 30f, 10f, 22f, 0.8f, new Vector2(0f, 9f), 3)
              .Ground(WarriorLightLook.Wave(p), fin, fin + 420f, r * 0.4f, r * 1.35f * wave, 0.9f, sq, 0f, 7)
              .At(fin, f =>
              {
                  f.Ring(true, f.Anchor + ring, 16, 100f, 1.8f, 2.8f, p.Light, 0.4f);
                  UltimateFx.Debris(f.Anchor + ring, f.Own ? 22 : 6, r * 0.6f, 110f);
                  UltimateFx.Dust(f.Anchor, f.Own ? 14 : 4, r * 0.7f);
              });
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Arc(ring, r * 0.95f, sq, start, start - 360f), fin, 200f, 280f, 0.28f * H, p, 1f, RibbonSort.Split, 1, hero, Lift);
            WarriorLightSkills.SplitPulses(fx, "warrior.blade-cyclone", fin, true, ring + new Vector2(0f, 6f), p, 12f); // the finisher's extra damage numbers
        }

        /// <summary>One ribbon orbiting the hero clockwise (seen from above) between `from` and `to` ms at `turnsPerSec`.</summary>
        static void Orbit(ClericLightFx fx, HeroController hero, Vector2 centre, float rx, float a0, float from, float to, float turnsPerSec, float tailMs, float width, ClericLightLook.Pal p)
        {
            float span = turnsPerSec * 360f * (to - from) / 1000f;
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Arc(centre, rx, ClericLightFx.Squash, a0, a0 - span), from, to - from, tailMs, width, p, 0.88f, RibbonSort.Split, 0, hero, centre.y, true);
        }

        /// <summary>Sparks thrown outward from random points of the ring at `dist`.</summary>
        static void Throw(ClericLightFx f, float dist, int n, Color c)
        {
            n = f.Count(n);
            for (int i = 0; i < n; i++)
            {
                Vector2 d = WarriorLightSkills.Dir(Random.value * 360f), e = new Vector2(d.x, d.y * ClericLightFx.Squash);
                f.Emit(true, f.Anchor + new Vector2(0f, Lift) + e * dist, e * Random.Range(40f, 70f), Random.Range(1.5f, 2.4f), c, Random.Range(0.22f, 0.35f));
            }
        }

        /// <summary>Motes drawn in from `dist` toward the hero (the cast gathering).</summary>
        static void In(ClericLightFx f, float dist, int n, Color c)
        {
            n = f.Count(n);
            for (int i = 0; i < n; i++)
            {
                Vector2 d = WarriorLightSkills.Dir(Random.value * 360f), e = new Vector2(d.x, d.y * ClericLightFx.Squash);
                f.Emit(false, f.Anchor + new Vector2(0f, Lift) + e * dist, -e * dist * 2.5f, Random.Range(1.5f, 2.3f), c, 0.35f);
            }
        }
    }
}
