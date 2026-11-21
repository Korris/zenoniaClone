using UnityEngine;
using ZenoniaProto.Rpg;
using L = ZenoniaProto.ClericLightLook;

namespace ZenoniaProto
{
    /// <summary>
    /// The four Aetherglyph Owl skill timelines (Rootburrow's Wanted boss, 21/11/2026; ChatGPT spec "FX RO3 Soft Fantasy V1 - quy mô raid",
    /// docs/reference/rootburrow-dark-25d/rootburrow-dark-dungeon-art-direction-chatgpt.md): shaders / meshes / particles only (ClericLightFx toolkit),
    /// rune cyan + crystal violet, geometry 60 / glow 25 / particles 15, and GRAND (user 21/11): big rune circles, falling light columns, crystal spikes,
    /// double shockwaves, the cave lit by every strike (DungeonLights.Flash) and a camera kick for the local hero standing in it.
    /// `strike` = local ms from now to the server's strike instant (CaveBossLightFx).
    /// </summary>
    static class AetherglyphOwlLightCasts
    {
        const float Round = 1f;

        public static void Play(WantedSkillDef skill, Vector2 at, float dir, float strike)
        {
            float u = WantedSkillArea.TileUnits;
            switch (skill.Id)
            {
                case 0: Starfall(at, skill.RadiusTiles * u, strike); break;
                case 1: Wingstorm(at, skill.RadiusTiles * u, dir, skill.AngleDeg, strike); break;
                case 2: Judgment(at, skill.LengthTiles * u, skill.WidthTiles * u, dir, strike); break;
                default: Eclipse(at, skill.RadiusTiles * u, dir, skill.SafeArcDeg, strike); break;
            }
        }

        static void Impact(Vector2 at, float reach, Color light, float lightRadius, float intensity, float shake)
        {
            DungeonLights.Flash(at, light, lightRadius, intensity, 560f);
            HeroController me = LocalPlayer.Hero;
            if (me != null && !me.IsDead && (me.Position - at).sqrMagnitude <= reach * reach) CameraFollow.SkillShake(shake);
        }

        /// <summary>Rune Starfall (one per targeted hunter): a hexagon seal draws itself and turns; at the strike a violet column of light crashes down
        /// into it, a shockwave ring runs out and sparks fly.</summary>
        static void Starfall(Vector2 at, float r, float s)
        {
            L.Pal rune = CaveBossLightFx.OwlRune, cry = CaveBossLightFx.OwlCrystal;
            ClericLightFx fx = ClericLightFx.Create("OwlStarfall", at, null, s + 800f, 1f);
            fx.Ground(L.M(L.RuneRing, rune, L.Role.Arc), 0f, s + 120f, r * 0.6f, r * 2.05f, 0.85f, Round, 40f).Reveal(450f).Arc(2.6f, 0f, 2).Env(120f, 140f);
            fx.Ground(L.M(L.Hexagon, rune, L.Role.Line), s * 0.3f, s + 100f, r * 0.8f, r * 1.3f, 0.75f, Round, -70f).Env(200f, 100f);
            fx.Upright(L.Column(cry), ClericLightFx.Band.Over, true, s - 140f, s + 260f, 12f, 16f, 70f, 60f, 0.95f, Vector2.zero).Phases(140f, 300f, 0.5f, 0.4f);
            fx.Upright(L.M(L.Core, cry, L.Role.Flash), ClericLightFx.Band.Over, false, s, s + 180f, 12f, 34f, 12f, 34f, 0.9f, new Vector2(0f, 8f));
            fx.Ground(L.M(L.Band, cry, L.Role.Line), s, s + 380f, r * 0.4f, r * 2.4f, 1f, Round).Phases(0f, 150f, 1f, 0.5f);
            fx.Ground(L.M(L.Band, rune, L.Role.Fill), s, s + 400f, r * 0.8f, r * 2.2f, 0.55f, Round).Kind(LayerKind.Glow);
            fx.At(s, f => { Impact(at, r * 1.2f, new Color(0.66f, 0.52f, 0.92f), r * 3f, 1.1f, 0.16f); f.Ring(true, at, 12, 60f, 2.4f, 4f, L.A(rune.Light, 0.95f), 0.5f); });
        }

        /// <summary>Astral Wingstorm: six violet rune arcs fan out over the cone; at the strike two huge cyan wings of light sweep out of the boss to the
        /// rim with a wash of light and 24 rune motes.</summary>
        static void Wingstorm(Vector2 at, float r, float dir, float angle, float s)
        {
            L.Pal rune = CaveBossLightFx.OwlRune, cry = CaveBossLightFx.OwlCrystal;
            ClericLightFx fx = ClericLightFx.Create("OwlWingstorm", at, null, s + 900f, 1f);
            Vector2 fwd = new Vector2(Mathf.Cos(dir * Mathf.Deg2Rad), Mathf.Sin(dir * Mathf.Deg2Rad));
            for (int i = 0; i < 6; i++)
            {
                float a = dir + (i - 2.5f) * angle / 6f;
                Vector2 d = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                fx.Ground(L.M(L.Streak, cry, L.Role.Line), s * 0.35f + Mathf.Abs(i - 2.5f) * 90f, s + 60f, r * 0.12f, r * 0.16f, 0.75f, 7f, 0f, 3, d * r * 0.5f).Turn(a - 90f).Env(150f, 60f);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float a = dir + side * angle * 0.22f;
                Vector2 d = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                fx.Ground(L.M(L.Wings, rune, L.Role.Glyph), s, s + 340f, r * 0.5f, r * 1.1f, 1f, 0.5f, 0f, 6, d * r * 0.2f).Turn(a - 90f).Move(d * r * 0.55f).Phases(0f, 130f, 1f, 0.45f);
            }
            fx.Ground(L.M(L.Band, rune, L.Role.Fill), s - 200f, s + 420f, r * 0.8f, r * 1.7f, 0.5f, Round, 0f, 2, fwd * r * 0.5f).Env(200f, 160f).Kind(LayerKind.Glow);
            fx.Upright(L.M(L.Core, rune, L.Role.Flash), ClericLightFx.Band.Over, false, s, s + 200f, 16f, 46f, 16f, 46f, 0.9f, fwd * 12f + new Vector2(0f, 14f));
            fx.At(s, f => Impact(at + fwd * r * 0.5f, r * 1.1f, new Color(0.45f, 0.86f, 0.96f), r * 1.8f, 1.2f, 0.22f));
            fx.At(s + 60f, f =>
            {
                for (int i = 0; i < 24; i++)
                {
                    float a = (dir + Random.Range(-angle * 0.5f, angle * 0.5f)) * Mathf.Deg2Rad;
                    Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    f.Emit(true, at + d * r * 0.2f, d * Random.Range(55f, 110f), Random.Range(2.8f, 4.6f), L.A(Random.value < 0.5f ? rune.Light : cry.Light, 0.95f), Random.Range(0.4f, 0.6f));
                }
            });
        }

        /// <summary>Crystal Judgment: a line of rune squares lights up away from the boss; at the strike seven violet crystal spikes burst up one after
        /// another along it under a white-cyan beam, with debris at each.</summary>
        static void Judgment(Vector2 at, float len, float wid, float dir, float s)
        {
            L.Pal rune = CaveBossLightFx.OwlRune, cry = CaveBossLightFx.OwlCrystal;
            ClericLightFx fx = ClericLightFx.Create("OwlJudgment", at, null, s + 1000f, 1f);
            Vector2 d = new Vector2(Mathf.Cos(dir * Mathf.Deg2Rad), Mathf.Sin(dir * Mathf.Deg2Rad));
            for (int i = 0; i < 8; i++)
            {
                float k = (i + 0.5f) / 8f;
                fx.Ground(L.M(L.Diamond, rune, L.Role.Line), s * 0.3f + k * s * 0.6f, s + 100f, wid * 0.55f, wid * 0.8f, 0.85f, Round, 0f, 3, d * len * k).Turn(dir).Env(120f, 60f);
            }
            fx.Ground(L.M(L.Streak, rune, L.Role.Line), s * 0.3f, s + 200f, wid * 0.45f, wid * 0.45f, 0.7f, len / (wid * 0.45f), 0f, 2, d * len * 0.5f).Turn(dir - 90f).Reveal(s * 0.6f).Env(100f, 150f);
            fx.Ground(L.M(L.Streak, rune, L.Role.Flash), s, s + 200f, wid * 0.7f, wid * 0.9f, 0.9f, len / (wid * 0.8f), 0f, 6, d * len * 0.5f).Turn(dir - 90f).Phases(0f, 180f, 1f, 0.4f);
            for (int i = 0; i < 7; i++)
            {
                float k = 0.12f + i * 0.13f, t0 = s + i * 40f;
                Vector2 o = d * len * k;
                fx.Upright(L.M(L.Diamond, cry, L.Role.Glyph), ClericLightFx.Band.Over, true, t0, t0 + 520f, 5f, 11f, 4f, 20f, 1f, o).Pop(140f).Phases(0f, 180f, 1f, 0.6f);
                fx.At(t0 + 20f, f => f.Ring(false, at + o, 7, 38f, 2.2f, 3.8f, L.A(cry.Light, 0.95f), 0.4f));
            }
            fx.At(s, f => Impact(at + d * len * 0.5f, len * 0.6f, new Color(0.7f, 0.92f, 1f), len * 0.8f, 1.3f, 0.24f));
        }

        /// <summary>Eclipse of Runes (enrage): three concentric rune rings and a star sigil fill the arena while crystal shards circle; at the strike a
        /// ring of light columns bursts up everywhere outside the safe lane, a double shockwave runs past the rim, the whole cave flashes, the camera kicks.</summary>
        static void Eclipse(Vector2 at, float r, float dir, float safeDeg, float s)
        {
            L.Pal rune = CaveBossLightFx.OwlRune, cry = CaveBossLightFx.OwlCrystal;
            ClericLightFx fx = ClericLightFx.Create("OwlEclipse", at, null, s + 1200f, 1f);
            fx.Ground(L.M(L.RuneRing, rune, L.Role.Arc), 0f, s + 200f, r * 1.6f, r * 2.05f, 0.85f, Round, 20f).Reveal(700f).Arc(1.8f, 0f, 3).Env(150f, 160f);
            fx.Ground(L.M(L.RuneRing, cry, L.Role.Line), 120f, s + 200f, r * 1.0f, r * 1.4f, 0.75f, Round, -32f).Reveal(600f).Env(150f, 160f);
            fx.Ground(L.M(L.Compass, rune, L.Role.Line), 240f, s + 200f, r * 0.5f, r * 0.72f, 0.75f, Round, 50f).Reveal(500f).Env(150f, 160f);
            fx.Ground(L.M(L.SigilStar, cry, L.Role.Line), s * 0.45f, s + 240f, r * 0.2f, r * 0.42f, 0.9f, Round, -60f).Env(250f, 150f);
            for (int i = 0; i < 12; i++) // crystal shards circling over the rings while it charges
            {
                float a = i * 30f;
                Vector2 o = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * r * 0.55f;
                fx.Upright(L.M(L.Diamond, cry, L.Role.Glyph), ClericLightFx.Band.Over, true, s * 0.35f + i * 25f, s + 60f, 4f, 5f, 9f, 12f, 0.85f, o).Pop(160f).Env(160f, 60f).Kind(LayerKind.Detail);
            }
            for (int i = 0; i < 14; i++) // light columns outside the safe lane at the strike
            {
                float a = dir + 180f + (i - 6.5f) * (360f - safeDeg) / 14f;
                if (Mathf.Abs(Mathf.DeltaAngle(a, dir)) < safeDeg * 0.5f + 6f) continue;
                float rr = r * (i % 2 == 0 ? 0.45f : 0.78f);
                Vector2 o = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * rr;
                fx.Upright(L.Column(i % 2 == 0 ? rune : cry), ClericLightFx.Band.Over, true, s + i * 10f, s + 380f + i * 10f, 9f, 13f, 40f, 34f, 0.95f, o).Phases(0f, 180f, 1f, 0.4f);
            }
            fx.Ground(L.M(L.Band, rune, L.Role.Line), s, s + 480f, r * 0.4f, r * 2.1f, 1f, Round).Phases(0f, 180f, 1f, 0.5f);
            fx.Ground(L.M(L.Band, cry, L.Role.Line), s + 120f, s + 650f, r * 0.6f, r * 2.5f, 0.65f, Round).Phases(0f, 120f, 1f, 0.4f);
            fx.Upright(L.M(L.Core, rune, L.Role.Flash), ClericLightFx.Band.Over, false, s, s + 280f, 20f, 70f, 20f, 70f, 0.95f, new Vector2(0f, 20f));
            fx.At(s, f => { Impact(at, r * 1.1f, new Color(0.62f, 0.86f, 1f), r * 2.6f, 1.6f, 0.38f); f.Ring(true, at, 32, 95f, 3f, 5f, L.A(rune.Light, 0.95f), 0.7f); });
            fx.At(s + 90f, f => f.Ring(false, at, 22, 60f, 2.4f, 4f, L.A(cry.Light, 0.9f), 0.65f));
        }
    }
}
