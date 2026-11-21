using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Picture of the Warrior's BLADE CYCLONE (21/11: first "RO3 Soft Fantasy V1" pass, then the GRAND pass: a TALL helical tornado of sword
    /// ribbons, unlike the flat Whirlwind; spec + concept in docs/reference/warrior-fx-grand/). Follows the caster like the skill. Hits and their
    /// times stay in BladeCycloneUltimate (16 small every 100 ms from 500, 2100, finisher 2250); this lays the matching shapes once at the cast:
    ///  - ground: warm pool, the war seal on the radius drawing itself in, a thick boundary ring with 3 bright arcs running round;
    ///  - 0-500 cast: anticipation glow on the blade, 3 flame streaks rising behind the hero, motes drawn in;
    ///  - 450-1300 spin: 3 ribbons orbiting at rising heights (a helix: low / mid / high, 2.5 turns / s, far half behind the hero);
    ///  - 1250-2250 peak: 4 thicker ribbons at 3.5 turns / s, stacked up to ~1.6 H;
    ///  - every small hit: sparks thrown off the ring; every 4th and the strong one: a thick shock to the edge;
    ///  - 2250 finisher: white-hot impact behind the hero, the largest shock (1.35 r), one last thick ribbon slashing round at the edge, a
    ///    scorch, sparks, rocks, dust; fades by ~3000.
    /// Shake / hit-stop / punch / flash and the loop sound stay in BladeCycloneUltimate (local caster only).
    /// </summary>
    public static class WarriorCycloneLightFx
    {
        const float H = WarriorLightLook.H, Lift = 5f;

        public static void Build(HeroController hero, float r, float[] waveAt, float castMs, float spinFrom, float spinTo, float peakFrom, float peakTo, float totalMs)
        {
            if (!WarriorLightLook.Ready || hero == null) return;
            ClericLightLook.Pal p = WarriorLightLook.Blade;
            float sq = ClericLightFx.Squash;
            Vector2 ring = new Vector2(0f, Lift);
            var fx = ClericLightFx.Create("BladeCyclone", hero.Position, hero, totalMs + 400f, ClericLightFx.DimFor(hero, null));
            fx.Ground(WarriorLightLook.Fill(p), 0f, totalMs, 2f * H, 2.6f * H, 0.3f).Env(150f, 350f).Kind(LayerKind.Glow)
              .Ground(WarriorLightLook.Line(WarriorLightLook.Sigil, p), 0f, totalMs, 2f * r / WarriorLightLook.SigilBand, 2f * r / WarriorLightLook.SigilBand, 0.5f, sq, 25f, 4)
                .Reveal(450f).Phases(450f, 560f, 0.55f, 0.7f).Env(0f, 400f)
              .Ground(WarriorLightLook.M(WarriorLightLook.Shock, p, ClericLightLook.Role.Arc), spinFrom, totalMs, 2f * r / WarriorLightLook.ShockBand * 0.95f, 2f * r / WarriorLightLook.ShockBand, 0.15f, sq, 0f, 5).Env(120f, 400f).Arc(-8f, 0f, 3);
            WarriorGrandFx.Charge(fx, 60f, castMs, new Vector2(0f, 12f), 1f * H, p);
            float[] xs = { -7f, 0f, 7f };
            for (int i = 0; i < xs.Length; i++)
                WarriorGrandFx.Pillar(fx, i * 60f, castMs, new Vector2(xs[i], 2f), 0.45f * H, 1.3f * H, p, ClericLightFx.Band.Behind, 1, LayerKind.Detail);

            float start = Random.value * 360f;
            for (int k = 0; k < 3; k++) Orbit(fx, hero, ring + new Vector2(0f, k * 0.7f * H), (0.95f + 0.15f * k) * H, (k % 2 == 0 ? 0.3f : -0.3f) * H, start + k * 120f, spinFrom - 40f, spinTo + 40f, 2.5f, 170f, 0.3f * H, p);
            for (int k = 0; k < 4; k++) Orbit(fx, hero, ring + new Vector2(0f, k * 0.55f * H), (0.9f + 0.13f * k) * H, (k % 2 == 0 ? 0.35f : -0.35f) * H, start + k * 90f + 45f, peakFrom, peakTo, 3.5f, 160f, (0.34f + 0.03f * k) * H, p);

            int n = waveAt.Length;
            for (int i = 0; i < n; i++)
            {
                float t = waveAt[i];
                bool last = i == n - 1, penult = i == n - 2;
                if (!last) fx.At(t, f => MechanicLightBursts.Sparks(f, f.Anchor + ring, penult ? 18 : 5, 150f, 90f, 360f, p.Light, 0.3f, 1.8f));
                if (penult || !last && i % 4 == 3)
                    WarriorGrandFx.Shock(fx, t, 260f, Vector2.zero, r * 0.45f, r, p, penult ? 1f : 0.75f, 6);
            }
            float fin = n > 0 ? waveAt[n - 1] : peakTo;
            WarriorGrandFx.Impact(fx, fin, new Vector2(0f, 9f), 1.6f * H, p, 26, ClericLightFx.Band.Behind);
            WarriorGrandFx.Shock(fx, fin, 450f, Vector2.zero, r * 0.4f, r * 1.35f, p, 0.7f);
            WarriorGrandFx.Scorch(fx, fin, 700f, Vector2.zero, r * 0.55f, WarriorLightLook.Flame);
            fx.At(fin, f =>
            {
                UltimateFx.Debris(f.Anchor + ring, f.Own ? 26 : 6, r * 0.6f, 120f);
                UltimateFx.Dust(f.Anchor, f.Own ? 16 : 4, r * 0.7f);
            });
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Arc(ring, r * 0.95f, sq, start, start - 360f), fin, 200f, 300f, 0.45f * H, p, 1f, RibbonSort.Split, 1, hero, Lift);
            WarriorLightSkills.SplitPulses(fx, "warrior.blade-cyclone", fin, true, ring + new Vector2(0f, 6f), p, 14f); // the finisher's extra damage numbers
        }

        /// <summary>One ribbon orbiting the hero clockwise (seen from above) between `from` and `to` ms at `turnsPerSec` on a TILTED ring: `centre.y`
        /// lifts it (tornado level), `tilt` raises one side and lowers the other, so stacked rings with opposite tilts cross like a helix (ChatGPT
        /// round 2: Blade Cyclone must read as a standing twisted column, Whirlwind as a flat disc).</summary>
        static void Orbit(ClericLightFx fx, HeroController hero, Vector2 centre, float rx, float tilt, float a0, float from, float to, float turnsPerSec, float tailMs, float width, ClericLightLook.Pal p)
        {
            float span = turnsPerSec * 360f * (to - from) / 1000f, sq = ClericLightFx.Squash;
            ArcRibbonFx.PathFn path = u =>
            {
                float a = Mathf.Lerp(a0, a0 - span, u) * Mathf.Deg2Rad;
                return centre + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * rx * sq + Mathf.Cos(a) * tilt);
            };
            WarriorLightSkills.Slash(fx, path, from, to - from, tailMs, width, p, 1f, RibbonSort.Split, 0, hero, centre.y, true);
        }
    }
}
