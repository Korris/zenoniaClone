using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Picture of the Warrior's BLADE CYCLONE, "RO trailer style" (21/11 evening; concept docs/reference/warrior-fx-ro-trailer/concept-blade-cyclone-chatgpt.png):
    /// the Warrior's AoE standard: force spreading wide while the hero and the monsters in the middle stay clearly visible. Follows the caster like
    /// the skill. Hits and their times stay in BladeCycloneUltimate (16 small every 100 ms from 500, 2100, finisher 2250); this lays the matching
    /// shapes once at the cast:
    ///  - ground: a faint pale-gold full-circle light with a thin ivory rim exactly on the real radius;
    ///  - 0-500 cast: a soft glow on the blade, one champagne arc forming round him;
    ///  - 450-2250: THREE wide translucent champagne arcs (~200 degrees each, broken, tilted differently) orbiting at 0.5 / 0.75 / 0.97 r, hollow
    ///    centre, faster from 1250; every 4th small hit a small flash on the ring; 2100: a star + a thin shock ring;
    ///  - 2250 finisher: a 1.6 H white-gold star behind the hero, a thin shock ring to 1.2 r, one last wide arc slashing round at the edge; fades by ~2800.
    /// No pillars, scorch, embers or red. Shake / hit-stop / punch / flash and the loop sound stay in BladeCycloneUltimate (local caster only).
    /// </summary>
    public static class WarriorCycloneLightFx
    {
        const float H = WarriorLightLook.H, Lift = 5f;

        public static void Build(HeroController hero, float r, float[] waveAt, float castMs, float spinFrom, float spinTo, float peakFrom, float peakTo, float totalMs)
        {
            if (!WarriorLightLook.Ready || hero == null) return;
            ClericLightLook.Pal p = WarriorLightLook.Blade, steel = WarriorLightLook.Steel;
            float sq = ClericLightFx.Squash;
            Vector2 ring = new Vector2(0f, Lift);
            var fx = ClericLightFx.Create("BladeCyclone", hero.Position, hero, totalMs + 400f, ClericLightFx.DimFor(hero, null));
            WarriorGrandFx.Sector(fx, 0f, 420f, totalMs + 100f, 420f, Vector2.zero, 90f, 360f, r, WarriorLightLook.Earth, 0.12f, 0.24f, false, 0.25f, 2); // V3 review: boundary -30-35 %, #C6B37D
            WarriorGrandFx.Charge(fx, 60f, castMs, new Vector2(0f, 12f), 0.9f * H, p);
            float start = Random.value * 360f;
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Arc(ring, r * 0.6f, sq, start + 60f, start - 240f), 120f, castMs - 120f, 260f, 0.3f * H, p, 0.8f, RibbonSort.Split, 0, hero, Lift);

            // Three orbiting arcs (V3 review: they merged into one ring): distinct inclinations (inner +20, middle -15, outer +35 degrees against
            // the ground: tilt = rx x tan), each ~200 degrees made of two ~85 degree pieces with a ~30 degree clear break; slow until the peak, then faster.
            float[] rad = { 0.5f, 0.75f, 0.97f }, incl = { 20f, -15f, 35f }, wid = { 0.32f, 0.4f, 0.36f };
            const float Piece = 85f, Gap = 30f;
            for (int k = 0; k < 3; k++)
            {
                float rx = r * rad[k], tilt = rx * Mathf.Tan(incl[k] * Mathf.Deg2Rad) * 0.6f, a0 = start + k * 120f;
                for (int piece = 0; piece < 2; piece++)
                {
                    float off = piece * (Piece + Gap);
                    float tail1 = Piece / (1.5f * 360f) * 1000f, tail2 = Piece / (2.1f * 360f) * 1000f;
                    float a1 = Orbit(fx, hero, ring, rx, tilt, a0 + off, spinFrom - 30f + k * 40f, peakFrom + 40f, 1.5f, tail1, wid[k] * H, p, 0.85f);
                    Orbit(fx, hero, ring, rx, tilt, a1, peakFrom, peakTo + 40f, 2.1f, tail2, wid[k] * H * 1.08f, p, 0.95f);
                }
            }
            Orbit(fx, hero, ring, r * 1.05f, 0.1f * H, start + 200f, peakFrom, peakTo, 2.1f, 220f, 0.14f * H, steel, 0.5f); // outer steel air

            int n = waveAt.Length;
            for (int i = 0; i < n; i++)
            {
                float t = waveAt[i];
                bool last = i == n - 1, penult = i == n - 2;
                if (last) continue;
                if (penult)
                {
                    WarriorGrandFx.Impact(fx, t, ring + new Vector2(0f, 6f), 1.1f * H, p, 8, ClericLightFx.Band.Behind);
                    WarriorGrandFx.Shock(fx, t, 280f, Vector2.zero, r * 0.45f, r, p, 0.6f, 6);
                    continue;
                }
                if (i % 4 == 3)
                {
                    Vector2 spot = ring + WarriorGrandFx.OnGround(Random.value * 360f, r * Random.Range(0.55f, 0.95f));
                    WarriorGrandFx.Impact(fx, t, spot + new Vector2(0f, 6f), 0.6f * H, p, 2);
                }
                else fx.At(t, f => MechanicLightBursts.Sparks(f, f.Anchor + ring, 3, 130f, 90f, 360f, p.Light, 0.26f, 1.6f));
            }
            float fin = n > 0 ? waveAt[n - 1] : peakTo;
            WarriorGrandFx.Impact(fx, fin, new Vector2(0f, 9f), 1.0f * H, p, 12);                       // V3 review: one compact final star in front
            WarriorGrandFx.Shock(fx, fin, 170f, Vector2.zero, 0.7f * H, r, p, 0.5f);                     // + a single quick ring 0.7 H -> the range
            WarriorGrandFx.Sector(fx, fin - 20f, 1f, 320f, 260f, Vector2.zero, 90f, 360f, r, p, 0.1f, 0.45f, false, 0.4f, 3);
            fx.At(fin, f => UltimateFx.Dust(f.Anchor, f.Own ? 10 : 3, r * 0.7f));
            WarriorLightSkills.Slash(fx, ArcRibbonFx.Arc(ring, r * 0.95f, sq, start, start - 300f), fin, 180f, 220f, 0.34f * H, p, 0.8f, RibbonSort.Split, 1, hero, Lift);
            WarriorLightSkills.SplitPulses(fx, "warrior.blade-cyclone", fin, true, ring + new Vector2(0f, 6f), p, 14f); // the finisher's extra damage numbers
        }

        /// <summary>One translucent arc orbiting the hero clockwise (seen from above) between `from` and `to` ms at `turnsPerSec` on a TILTED ring
        /// (`tilt` raises one side and lowers the other, so the three rings cross at different angles); the tail `tailMs` behind the head makes it a
        /// ~200 degree broken arc. Returns the end angle (deg) so a following ribbon continues from it.</summary>
        static float Orbit(ClericLightFx fx, HeroController hero, Vector2 centre, float rx, float tilt, float a0, float from, float to, float turnsPerSec, float tailMs, float width, ClericLightLook.Pal p, float alpha)
        {
            float span = turnsPerSec * 360f * (to - from) / 1000f, sq = ClericLightFx.Squash;
            ArcRibbonFx.PathFn path = u =>
            {
                float a = Mathf.Lerp(a0, a0 - span, u) * Mathf.Deg2Rad;
                return centre + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * rx * sq + Mathf.Cos(a) * tilt);
            };
            WarriorLightSkills.Slash(fx, path, from, to - from, tailMs, width, p, alpha, RibbonSort.Split, 0, hero, centre.y, true);
            return a0 - span;
        }
    }
}
