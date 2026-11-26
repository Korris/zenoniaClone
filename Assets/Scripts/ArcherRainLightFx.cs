using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Arrow Rain and Starfall Volley (Archer AoE / ultimate), RO3 Soft Fantasy V1 (2026-11-26, user: "skill của archer sao ko thấy hiệu ứng gì vậy").
    /// SkillTable Rain from 0 ms: Arrow Rain 8 drops every 120 ms, 6 tiles ahead, 2.5-tile scatter, 1.2-tile blasts; Starfall Volley 10 drops every
    /// 150 ms, 7 tiles ahead, 3.5-tile scatter, 1.5-tile blasts. Drop spots are the sim's own (SkillScatter.InDisc with the cast seed, centre as
    /// SkillEffects.Rain), so every arrow lands on its blast at its step's time (the first is due on the cast frame: it lands at once).
    ///  - cast: Arrow Rain = three light arrows shot up from the bow + gold flash; Starfall = a gold light column from the bow + seal under the feet;
    ///  - ground: the target circle (rune ring exactly on the farthest blast edge, soft pool; Starfall adds a gold star sigil turning) draws itself
    ///    in, peaks, rests at ~40 % through the volley and fades after the last drop (rule 4 / 10);
    ///  - each drop: the painted arrow falling steeply with a soft trail (Starfall: star arrows, gold star glow on the head, gold trail), landing on
    ///    its blast: ring on the blast radius, star pop, flash, dust puff dissolving, sparks + dust motes;
    ///  - Starfall finale after the last drop: a starburst over the centre with a flash, a wave over the circle, a gold shockwave past it, motes,
    ///    own screen punch (user 21/11: skills stay grand without sprites).
    /// Crowd: another player's arrows have no trail, their bursts fewer particles (ClericLightFx LOD).
    /// </summary>
    public sealed class ArcherRainLightFx : MonoBehaviour
    {
        const float EndPadMs = 520f;
        SkillContext ctx;
        Vector2[] spots;
        int[] landMs;
        ArcherFallingArrow[] arrows;
        bool[] spawned, landed;
        float age, dim, impactR, endMs, fallMs;
        bool star;
        Vector2 sky;

        public static void Cast(SkillContext ctx)
        {
            SkillEffect rain = ArcherLightSkills.Effect(ctx.Def, EffectKind.Rain);
            if (rain == null) return;
            new GameObject("ArcherRainLight").AddComponent<ArcherRainLightFx>().Init(ctx, rain);
        }

        void Init(SkillContext c, SkillEffect rain)
        {
            ctx = c;
            star = c.Def.Index == ArcherLightSkills.Starfall;
            fallMs = star ? 340f : 260f;
            sky = star ? new Vector2(-30f, 120f) : new Vector2(c.Dir.x >= 0f ? -18f : 18f, 100f); // rain arrows come in steep from behind the shot
            dim = ArcherLightLook.Dim(c.Body.Hero);
            float u = c.Units, scatter = rain.Radius * u;
            impactR = rain.ImpactRadius * u;
            Vector2 mid = SkillAim.RainCentre(c, rain); // SkillEffects.Rain
            int n = Mathf.Max(1, rain.Count);
            spots = new Vector2[n]; landMs = new int[n]; arrows = new ArcherFallingArrow[n]; spawned = new bool[n]; landed = new bool[n];
            for (int i = 0; i < n; i++)
            {
                float dx, dy;
                SkillScatter.InDisc(c.Seed, i, scatter, out dx, out dy);
                spots[i] = mid + new Vector2(dx, dy);
                landMs[i] = rain.AtMs + i * Mathf.Max(0, rain.IntervalMs);
            }
            endMs = landMs[n - 1] + EndPadMs;
            // Lives under the circle's ClericLightFx: a map change (ClericLightFx.ClearAll) or the circle's end takes the falling arrows with it
            transform.SetParent(Circle(mid, scatter + impactR).transform, false);
            CasterCue();
            Tick(0f);
        }

        /// <summary>The target circle on the ground at the rain centre, boundary on the farthest blast edge.</summary>
        ClericLightFx Circle(Vector2 mid, float reach)
        {
            ClericLightLook.Pal leaf = ArcherLightLook.Leaf, gold = ArcherLightLook.Gold;
            float d = 2f * reach, ring = d / 0.955f, last = landMs[landMs.Length - 1];
            var fx = ClericLightFx.Create(star ? "ArcherStarfallCircle" : "ArcherRainCircle", mid, null, endMs, dim);
            fx.Ground(ArcherLightLook.Arc(ArcherLightLook.RuneRing, leaf), 0f, endMs, ring * 0.9f, ring, 0.9f, ClericLightFx.Squash, 8f, 6).Reveal(200f).Arc(2.5f, 0f, 3).Env(30f, 400f).Phases(0f, 300f, 1f, 0.42f)
              .Ground(ArcherLightLook.Pool(leaf), 0f, endMs, d * 0.6f, d * 0.95f, 1f).Env(160f, 420f).Kind(LayerKind.Glow)
              .Ground(ArcherLightLook.Line(ArcherLightLook.Reticle, star ? gold : leaf), 60f, endMs - 120f, d * 0.22f, d * 0.3f, 0.7f, ClericLightFx.Squash, -20f, 7).Pop(200f).Env(60f, 360f).Phases(60f, 340f, 1f, 0.45f).Kind(LayerKind.Detail);
            if (star)
            {
                fx.Ground(ArcherLightLook.Line(ArcherLightLook.SigilStar, gold), 100f, endMs - 80f, d * 0.42f, d * 0.56f, 0.8f, ClericLightFx.Squash, 10f, 8).Reveal(260f).Env(100f, 420f).Phases(last, last + 260f, 0.55f, 0.35f)
                  // finale after the last drop: starburst over the centre, a wave over the whole circle and a gold shockwave past it, motes rising
                  .Ground(ArcherLightLook.Line(ArcherLightLook.Band, leaf), last + 60f, last + 480f, d * 0.2f, ArcherLightLook.BandQuad(reach), 0.85f, ClericLightFx.Squash, 0f, 9)
                  .Ground(ArcherLightLook.Line(ArcherLightLook.Band, gold), last + 40f, last + 360f, d * 0.3f, ArcherLightLook.BandQuad(reach) * 1.12f, 0.5f, ClericLightFx.Squash, 0f, 10).Kind(LayerKind.Detail)
                  .Upright(ArcherLightLook.Glyph(ArcherLightLook.Star, gold), ClericLightFx.Band.Over, false, last + 40f, last + 560f, 22f, 62f, 22f, 62f, 0.9f, new Vector2(0f, 16f), 9).Pop(220f).Env(20f, 260f)
                  .Upright(ArcherLightLook.Flash(leaf), ClericLightFx.Band.Over, false, last + 40f, last + 320f, 16f, 46f, 16f, 46f, 0.85f, new Vector2(0f, 16f), 10);
                Color mote = ArcherLightLook.Mote, glint = ArcherLightLook.Glint;
                fx.At(last + 50f, f => f.Ring(true, mid + new Vector2(0f, 16f), 20, reach * 1.2f, 2f, 3.2f, glint, 0.55f))
                  .At(last + 80f, f => f.Puff(false, mid, 18, reach * 0.7f, 18f, 1.8f, 2.8f, mote, 0.9f));
                if (ctx.Body.IsLocal) fx.At(last + 40f, f => { CameraFollow.SkillShake(0.35f); GameClock.HitStop(60f); });
            }
            return fx;
        }

        /// <summary>At the caster: light arrows shot up (Arrow Rain) / a gold light column (Starfall), flash at the bow, seal under the feet.</summary>
        void CasterCue()
        {
            HeroController caster = ctx.Body.Hero;
            ClericLightLook.Pal leaf = ArcherLightLook.Leaf, gold = ArcherLightLook.Gold;
            Vector2 bow = ArcherLightSkills.Bow(ctx.Dir) + new Vector2(0f, 4f);
            var fx = ClericLightFx.Create("ArcherRainCast", caster.Position, null, 700f, dim);
            fx.Ground(ArcherLightLook.Line(ArcherLightLook.Compass, star ? gold : leaf), 0f, 700f, 16f, 28f, 0.85f, ClericLightFx.Squash, 40f, 5).Reveal(160f).Env(40f, 280f)
              .Upright(ArcherLightLook.Flash(gold), ClericLightFx.Band.Over, false, 0f, 200f, 5f, 15f, 5f, 15f, 0.8f, bow)
              .Upright(ArcherLightLook.Line(ArcherLightLook.Band, gold), ClericLightFx.Band.Over, false, 0f, 280f, 16f, 30f, 6f, 10f, 0.8f, bow + new Vector2(0f, 6f), 1);
            if (star)
                fx.Upright(ArcherLightLook.Glyph(ArcherLightLook.Streak, gold), ClericLightFx.Band.Over, false, 0f, 420f, 10f, 6f, 30f, 70f, 0.85f, bow + new Vector2(0f, 18f), 2).Rise(26f)
                  .Upright(ArcherLightLook.Glyph(ArcherLightLook.Star, gold), ClericLightFx.Band.Over, false, 60f, 520f, 6f, 14f, 6f, 14f, 0.9f, bow + new Vector2(0f, 30f), 3).Rise(40f).Pop(140f);
            for (int i = 0; i < 3; i++)                                     // light arrows shot high (the volley going up)
            {
                float t0 = i * 45f, x = (i - 1) * 4f;
                fx.Upright(ArcherLightLook.Glyph(ArcherLightLook.Streak, star ? gold : leaf), ClericLightFx.Band.Over, false, t0, t0 + 260f, 4f, 3f, 16f, 22f, 0.85f, bow + new Vector2(x, 8f), 4 + i).Rise(70f).Kind(i == 1 ? LayerKind.Main : LayerKind.Detail);
            }
            Color spark = star ? ArcherLightLook.Glint : ArcherLightLook.Spark;
            Vector2 at = caster.Position + bow;
            fx.At(0f, f => { int m = f.Count(8); for (int i = 0; i < m; i++) f.Emit(true, at, new Vector2(Random.Range(-14f, 14f), Random.Range(50f, 90f)), Random.Range(1.4f, 2.2f), spark, 0.35f); });
        }

        void Update()
        {
            float dt = GameClock.Dt;
            age += dt * 1000f;
            Tick(dt);
            if (age > endMs + 300f) Destroy(gameObject);
        }

        void Tick(float dt)
        {
            for (int i = 0; i < spots.Length; i++)
            {
                float start = landMs[i] - fallMs;
                if (!spawned[i] && age >= start)
                {
                    spawned[i] = true;
                    Vector2 from = spots[i] + sky * (i % 2 == 0 ? 1f : 0.88f) + new Vector2((i % 3 - 1) * 6f, 0f);
                    float tw = dim >= 0.99f ? (star ? 5f : 3.5f) : dim >= 0.6f ? 2.5f : 0f;
                    arrows[i] = new ArcherFallingArrow(transform, from, spots[i] + new Vector2(0f, 2f), star ? 1.3f : 1.15f, star ? ArcherLightLook.Gold : ArcherLightLook.Leaf, tw,
                        star ? ArcherLightLook.Gold : null, dim >= 0.99f ? 1f : 0.7f);
                }
                if (arrows[i] != null)
                {
                    arrows[i].Tick((age - start) / fallMs, dt);
                    if (arrows[i].Done) arrows[i] = null;
                }
                if (!landed[i] && age >= landMs[i]) { landed[i] = true; Land(spots[i]); }
            }
        }

        void Land(Vector2 at)
        {
            ClericLightLook.Pal p = star ? ArcherLightLook.Gold : ArcherLightLook.Leaf, leaf = ArcherLightLook.Leaf;
            float d = 2f * impactR, k = star ? 1.2f : 1f;
            Vector2 lift = new Vector2(0f, 5f);
            var fx = ClericLightFx.Create(star ? "ArcherStarHit" : "ArcherRainHit", at, null, 560f, dim);
            fx.Ground(ArcherLightLook.Line(ArcherLightLook.Band, leaf), 0f, 340f, 8f, ArcherLightLook.BandQuad(impactR), 0.85f, ClericLightFx.Squash, 0f, 9)
              .Ground(ArcherLightLook.Pool(leaf), 0f, 400f, d * 0.5f, d, 1f).Kind(LayerKind.Glow)
              .Upright(ArcherLightLook.Glyph(ArcherLightLook.Star, p), ClericLightFx.Band.Over, false, 0f, 300f, 10f * k, 24f * k, 10f * k, 24f * k, 0.92f, lift, 1).Pop(120f)
              .Upright(ArcherLightLook.Flash(p), ClericLightFx.Band.Over, false, 0f, 170f, 8f * k, 22f * k, 8f * k, 22f * k, 0.8f, lift, 2)
              .Upright(ArcherLightLook.DustMat, ClericLightFx.Band.Over, false, 20f, 520f, 10f, 22f * k, 7f, 13f * k, 0.75f, new Vector2(0f, 3f), -2).Rise(5f).Dissolve();
            if (star) fx.Ground(ArcherLightLook.Line(ArcherLightLook.Band, ArcherLightLook.Gold), 0f, 260f, 10f, ArcherLightLook.BandQuad(impactR) * 1.2f, 0.5f, ClericLightFx.Squash, 0f, 10).Kind(LayerKind.Detail);
            Color spark = star ? ArcherLightLook.Glint : ArcherLightLook.Spark, dust = ArcherLightLook.A(ArcherLightLook.Dust.Main, 0.85f);
            fx.At(0f, f => f.Ring(true, at + lift, star ? 9 : 7, impactR * 2.2f, 1.6f, 2.6f, spark, 0.38f))
              .At(10f, f => f.Ring(false, at + new Vector2(0f, 2f), 6, impactR * 1.4f, 1.8f, 2.8f, dust, 0.45f));
        }
    }
}
