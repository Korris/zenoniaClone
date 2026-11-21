using UnityEngine;
using ZenoniaProto.Rpg;
using L = ZenoniaProto.ClericLightLook;

namespace ZenoniaProto
{
    /// <summary>
    /// The four Ambercap Warden skill timelines (CaveBossLightFx; ChatGPT spec section E). `strike` = local ms from now to the server's strike
    /// instant: the cast pieces run up to it, the impact peaks right after it (RO3 rule 4: peak 100-180 ms, then a short fade).
    /// Proportions follow RO3 rule 2 (rune rings / glyphs / streaks carry the shape, glow and particles stay lighter), and the user's 21/11 rule that a
    /// boss skill must still be GRAND without sprites: every impact also lights the dark cave for a moment (DungeonLights.Flash), throws a second
    /// shockwave and debris, and shakes the camera a little for the local hero standing in it.
    /// </summary>
    static class AmbercapLightCasts
    {
        const float Round = 1f; // squash: the 2.5D camera foreshortens the ground itself

        public static void Play(WantedSkillDef skill, Vector2 at, float dir, float strike)
        {
            float r = (skill.Shape == WantedAreaShape.Line ? skill.LengthTiles : skill.RadiusTiles) * WantedSkillArea.TileUnits;
            switch (skill.Id)
            {
                case 0: Pulse(at, r, strike); break;
                case 1: Fan(at, r, dir, skill.AngleDeg, strike); break;
                case 2: Rootline(at, r, skill.WidthTiles * WantedSkillArea.TileUnits, dir, strike); break;
                default: BloomOf(at, r, dir, skill.SafeArcDeg, strike); break;
            }
        }

        /// <summary>Impact light + a camera kick for the local hero when it stands within `reach` of `at`.</summary>
        static void Impact(ClericLightFx f, Vector2 at, float reach, Color light, float lightRadius, float intensity, float shake)
        {
            DungeonLights.Flash(at, light, lightRadius, intensity, 520f);
            HeroController me = LocalPlayer.Hero;
            if (me != null && !me.IsDead && (me.Position - at).sqrMagnitude <= reach * reach) CameraFollow.SkillShake(shake);
        }

        /// <summary>Amber Pulse: a 12-glyph rune ring draws itself under the boss and turns 35 deg/s while a compass sigil gathers light; at the strike a
        /// thick shockwave ring and a second, wider echo run out past the rim, the cave flashes amber, sparks fly up.</summary>
        static void Pulse(Vector2 at, float r, float s)
        {
            L.Pal p = CaveBossLightFx.PalOf(0);
            ClericLightFx fx = ClericLightFx.Create("WardenPulse", at, null, s + 900f, 1f);
            fx.Ground(L.M(L.RuneRing, p, L.Role.Arc), 0f, s + 120f, r * 0.5f, r * 2.05f, 0.85f, Round, 35f).Reveal(500f).Arc(2.4f, 0f, 2).Env(120f, 160f);
            fx.Ground(L.M(L.Compass, p, L.Role.Line), s * 0.3f, s + 80f, r * 0.6f, r * 1.0f, 0.7f, Round, -50f).Env(200f, 120f);
            fx.Ground(L.M(L.Band, p, L.Role.Fill), s * 0.5f, s + 60f, r * 0.4f, r * 1.2f, 0.45f, Round).Env(300f, 60f).Kind(LayerKind.Glow);
            fx.Ground(L.M(L.Band, p, L.Role.Line), s, s + 420f, r * 0.3f, r * 2.2f, 1f, Round).Phases(0f, 160f, 1f, 0.5f);
            fx.Ground(L.M(L.Band, p, L.Role.Line), s + 90f, s + 560f, r * 0.5f, r * 2.7f, 0.65f, Round).Phases(0f, 120f, 1f, 0.4f);
            fx.Ground(L.M(L.Band, p, L.Role.Fill), s, s + 420f, r * 1.0f, r * 2.3f, 0.6f, Round).Kind(LayerKind.Glow);
            fx.Upright(L.M(L.Core, p, L.Role.Flash), ClericLightFx.Band.Over, false, s, s + 200f, 16f, 44f, 16f, 44f, 0.9f, new Vector2(0f, 14f));
            fx.At(s, f => { Impact(f, at, r * 1.15f, new Color(1f, 0.78f, 0.45f), r * 2.6f, 1.3f, 0.22f); f.Ring(true, at, 18, 70f, 2.6f, 4.4f, L.A(p.Light, 0.95f), 0.6f); });
            fx.At(s + 60f, f => f.Puff(true, at, 16, r * 0.9f, 22f, 2.4f, 4f, L.A(p.Main, 0.9f), 0.8f));
        }

        /// <summary>Spore Fan: five rune streaks fan out across the cone and light up inside-out; at the strike five wide petal streaks sweep to the rim,
        /// a soft pool washes the cone, the cave flashes spring-green and a cloud of round spores drifts out low.</summary>
        static void Fan(Vector2 at, float r, float dir, float angle, float s)
        {
            L.Pal p = CaveBossLightFx.PalOf(1);
            ClericLightFx fx = ClericLightFx.Create("WardenFan", at, null, s + 900f, 1f);
            Vector2 fwd = new Vector2(Mathf.Cos(dir * Mathf.Deg2Rad), Mathf.Sin(dir * Mathf.Deg2Rad));
            for (int i = 0; i < 5; i++)
            {
                float a = dir + (i - 2) * angle / 5f, rad = a * Mathf.Deg2Rad;
                Vector2 d = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                float lit = s * 0.4f + Mathf.Abs(i - 2) * 110f;
                fx.Ground(L.M(L.Streak, p, L.Role.Line), lit, s + 60f, r * 0.13f, r * 0.17f, 0.7f, 6.4f, 0f, 3, d * r * 0.48f).Turn(a - 90f).Env(160f, 60f);
                fx.Ground(L.M(L.Streak, p, L.Role.Glyph), s, s + 340f, r * 0.14f, r * 0.3f, 1f, 6.5f, 0f, 5, d * r * 0.2f).Turn(a - 90f).Move(d * r * 0.65f).Phases(0f, 130f, 1f, 0.45f);
            }
            fx.Ground(L.M(L.Band, p, L.Role.Fill), s - 200f, s + 380f, r * 0.7f, r * 1.6f, 0.5f, Round, 0f, 2, fwd * r * 0.5f).Env(200f, 150f).Kind(LayerKind.Glow);
            fx.Upright(L.M(L.Core, p, L.Role.Flash), ClericLightFx.Band.Over, false, s, s + 180f, 12f, 34f, 12f, 34f, 0.85f, fwd * 10f + new Vector2(0f, 12f));
            fx.At(s, f => Impact(f, at + fwd * r * 0.5f, r * 1.1f, new Color(0.72f, 0.9f, 0.55f), r * 1.8f, 1.1f, 0.18f));
            fx.At(s + 60f, f =>
            {
                for (int i = 0; i < 26; i++)
                {
                    float a = (dir + Random.Range(-angle * 0.5f, angle * 0.5f)) * Mathf.Deg2Rad;
                    Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    f.Emit(false, at + d * r * 0.2f, d * Random.Range(45f, 95f), Random.Range(2.8f, 4.6f), L.A(Random.value < 0.6f ? p.Main : p.Light, 0.9f), Random.Range(0.4f, 0.65f));
                }
            });
        }

        /// <summary>Rootline Burst: square rune glyphs light up from the boss along the line and a glowing root vein runs out; at the strike five root
        /// spikes pop up one after another, each with a burst of debris, the whole line flashes teal.</summary>
        static void Rootline(Vector2 at, float len, float wid, float dir, float s)
        {
            L.Pal p = CaveBossLightFx.PalOf(2);
            ClericLightFx fx = ClericLightFx.Create("WardenRootline", at, null, s + 900f, 1f);
            float rad = dir * Mathf.Deg2Rad;
            Vector2 d = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            const int Glyphs = 7;
            for (int i = 0; i < Glyphs; i++)
            {
                float k = (i + 0.5f) / Glyphs;
                fx.Ground(L.M(L.Diamond, p, L.Role.Line), s * 0.35f + k * s * 0.55f, s + 100f, wid * 0.55f, wid * 0.8f, 0.8f, Round, 0f, 3, d * len * k).Turn(dir).Env(120f, 60f);
            }
            fx.Ground(L.M(L.Streak, p, L.Role.Line), s * 0.35f, s + 160f, wid * 0.4f, wid * 0.4f, 0.65f, len / (wid * 0.4f), 0f, 2, d * len * 0.5f).Turn(dir - 90f).Reveal(s * 0.6f).Env(100f, 120f);
            fx.Ground(L.M(L.Band, p, L.Role.Fill), s, s + 360f, wid * 1.6f, wid * 2.2f, 0.45f, len / (wid * 1.8f), 0f, 2, d * len * 0.5f).Turn(dir - 90f).Kind(LayerKind.Glow);
            for (int i = 0; i < 5; i++)
            {
                float k = 0.15f + i * 0.19f, t0 = s + i * 45f;
                Vector2 o = d * len * k;
                fx.Upright(L.M(L.Diamond, p, L.Role.Glyph), ClericLightFx.Band.Over, true, t0, t0 + 460f, 4f, 9f, 3f, 13f, 1f, o).Pop(130f).Phases(0f, 170f, 1f, 0.6f);
                fx.At(t0 + 20f, f => f.Ring(false, at + o, 8, 34f, 2.2f, 3.6f, L.A(p.Main, 0.95f), 0.4f));
            }
            fx.At(s, f => { Impact(f, at + d * len * 0.5f, len * 0.6f, new Color(0.47f, 0.86f, 0.82f), len * 0.75f, 1.1f, 0.2f); f.Puff(true, at + d * len * 0.5f, 12, len * 0.45f, 14f, 2.2f, 3.6f, L.A(p.Light, 0.9f), 0.7f); });
        }

        /// <summary>Bloom of the Hollow (enrage): two concentric rune rings + an amber star sigil gather light over the whole hall; at the strike 7 wide
        /// petal arcs burst out (never into the safe lane), a double shockwave runs past the rim, the cave flashes bright and the camera kicks.</summary>
        static void BloomOf(Vector2 at, float r, float dir, float safeDeg, float s)
        {
            L.Pal p = CaveBossLightFx.PalOf(3), amber = CaveBossLightFx.PalOf(0);
            ClericLightFx fx = ClericLightFx.Create("WardenBloom", at, null, s + 1100f, 1f);
            fx.Ground(L.M(L.RuneRing, p, L.Role.Arc), 0f, s + 200f, r * 1.6f, r * 2.05f, 0.85f, Round, 25f).Reveal(600f).Arc(2f, 0f, 3).Env(150f, 150f);
            fx.Ground(L.M(L.RuneRing, p, L.Role.Line), 100f, s + 200f, r * 0.9f, r * 1.25f, 0.7f, Round, -40f).Reveal(500f).Env(150f, 150f);
            fx.Ground(L.M(L.SigilStar, amber, L.Role.Line), s * 0.4f, s + 220f, r * 0.35f, r * 0.7f, 0.85f, Round, 45f).Env(250f, 150f);
            fx.Ground(L.M(L.Band, amber, L.Role.Fill), s * 0.6f, s + 80f, r * 0.3f, r * 0.9f, 0.5f, Round).Env(300f, 60f).Kind(LayerKind.Glow);
            for (int i = 0; i < 8; i++)
            {
                float a = dir + 180f + (i - 3.5f) * (360f - safeDeg) / 7f;
                if (Mathf.Abs(Mathf.DeltaAngle(a, dir)) < safeDeg * 0.5f + 8f) continue;   // never a petal in the safe lane
                Vector2 d = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                fx.Ground(L.M(L.Streak, p, L.Role.Glyph), s + i * 12f, s + 420f, r * 0.2f, r * 0.42f, 1f, 5.5f, 0f, 5, d * r * 0.25f).Turn(a - 90f).Move(d * r * 0.5f).Phases(0f, 180f, 1f, 0.45f);
            }
            fx.Ground(L.M(L.Band, p, L.Role.Line), s, s + 460f, r * 0.4f, r * 2.1f, 1f, Round).Phases(0f, 180f, 1f, 0.5f);
            fx.Ground(L.M(L.Band, amber, L.Role.Line), s + 110f, s + 620f, r * 0.6f, r * 2.5f, 0.6f, Round).Phases(0f, 120f, 1f, 0.4f);
            fx.Upright(L.M(L.Core, amber, L.Role.Flash), ClericLightFx.Band.Over, false, s, s + 260f, 18f, 60f, 18f, 60f, 0.95f, new Vector2(0f, 16f));
            fx.At(s, f => { Impact(f, at, r * 1.1f, new Color(0.7f, 0.95f, 0.95f), r * 2.4f, 1.5f, 0.35f); f.Ring(true, at, 28, 85f, 2.8f, 4.8f, L.A(p.Light, 0.95f), 0.65f); });
            fx.At(s + 80f, f => f.Ring(false, at, 20, 55f, 2.4f, 4f, L.A(amber.Light, 0.9f), 0.6f));
        }
    }
}
