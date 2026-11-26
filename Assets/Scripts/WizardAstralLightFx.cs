using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Astral Cataclysm (Wizard ultimate), RO3 Soft Fantasy V1 (2026-11-21; icon: crystal star-meteors with warm tails under a crescent moon).
    /// SkillTable Rain: 8 drops every 200 ms from 0 ms, 5 tiles ahead, scattered in a 4-tile disc, each a 1.5-tile blast (120 %); cast lock 900 ms.
    /// Ultimate = class colour + 1 accent (rule 8): starlight blue (ElementLightLook.Astral) + gold stars. Drop spots are the sim's own
    /// (SkillScatter.InDisc with the cast seed), so every star lands on its blast at its step's time. ~2000 ms:
    ///  - ground: the astral circle (rune ring exactly on the outer reach of the blasts, scatter + impact radius; gold star inside, soft pool) draws
    ///    itself in over 220 ms, peaks, then rests at ~40 % through the shower and fades after the last drop (rule 4 / 10);
    ///  - a small seal + staff flash at the caster;
    ///  - 8 falling stars (ElementMeteor: gold star head, blue core, short blue-white trail) from the upper left, each landing exactly at i x 200 ms
    ///    (the first drop is due on the cast frame: it lands at once, a visible fall would trail its numbers);
    ///  - each landing: blue ring on the 1.5-tile blast radius, star 1.2 H, flash, one pulse per damage number, 5 sparks;
    ///  - a gold star seal turning over the whole shower; after the last drop the finale: a starburst 3 H over the centre with a flash, a wave
    ///    over the circle and a gold shockwave past it, 20 sparks + motes, own screen punch (user 21/11: skills must stay grand without sprites).
    /// Crowd: another player's stars have no trail, their landings fewer particles (ClericLightFx LOD).
    /// </summary>
    public sealed class WizardAstralLightFx : MonoBehaviour
    {
        const float FallMs = 380f, EndPadMs = 600f;
        static readonly Vector2 SkyOffset = new Vector2(-46f, 118f);

        SkillContext ctx;
        Vector2[] spots;
        int[] landMs;
        ElementMeteor[] stars;
        bool[] spawned, landed;
        float age, dim, impactR, endMs;
        ClericLightLook.Pal pal, gold;

        public static void Cast(SkillContext ctx)
        {
            SkillEffect rain = WizardLightSkills.Effect(ctx.Def, EffectKind.Rain);
            if (rain == null) return;
            new GameObject("WizardAstralLight").AddComponent<WizardAstralLightFx>().Init(ctx, rain);
        }

        void Init(SkillContext c, SkillEffect rain)
        {
            ctx = c;
            pal = ElementLightLook.Astral; gold = ElementLightLook.Gold;
            dim = ClericLightFx.DimFor(c.Body.Hero, null);
            float u = c.Units, scatter = rain.Radius * u;
            impactR = rain.ImpactRadius * u;
            Vector2 mid = SkillAim.RainCentre(c, rain); // SkillEffects.Rain
            int n = Mathf.Max(1, rain.Count);
            spots = new Vector2[n]; landMs = new int[n]; stars = new ElementMeteor[n]; spawned = new bool[n]; landed = new bool[n];
            for (int i = 0; i < n; i++)
            {
                float dx, dy;
                SkillScatter.InDisc(c.Seed, i, scatter, out dx, out dy);
                spots[i] = mid + new Vector2(dx, dy);
                landMs[i] = rain.AtMs + i * Mathf.Max(0, rain.IntervalMs);
            }
            endMs = landMs[n - 1] + EndPadMs;
            // Lives under the circle's ClericLightFx: a map change (ClericLightFx.ClearAll) or the circle's end takes the falling stars with it
            transform.SetParent(Circle(mid, scatter + impactR).transform, false);
            CasterSeal();
            Tick();
        }

        /// <summary>The astral circle on the ground at the rain centre, boundary on the farthest blast edge.</summary>
        ClericLightFx Circle(Vector2 mid, float reach)
        {
            float d = 2f * reach, ring = d / 0.955f, last = landMs[landMs.Length - 1];
            var fx = ClericLightFx.Create("AstralCircle", mid, null, endMs, dim);
            fx.Ground(ElementLightLook.M(ClericLightLook.RuneRing, pal, ClericLightLook.Role.Arc), 0f, endMs, ring * 0.9f, ring, 0.9f, ClericLightFx.Squash, 6f, 6).Reveal(220f).Arc(2.5f, 0f, 3).Env(30f, 450f).Phases(0f, 320f, 1f, 0.42f)
              .Ground(ElementLightLook.M(ClericLightLook.RuneRing, pal, ClericLightLook.Role.Line), 120f, endMs - 100f, ring * 0.5f, ring * 0.6f, 0.7f, ClericLightFx.Squash, -14f, 5).Env(150f, 400f).Phases(120f, 380f, 1f, 0.4f).Kind(LayerKind.Detail)
              .Ground(ElementLightLook.M(ClericLightLook.Star, gold, ClericLightLook.Role.Glyph), 60f, endMs - 150f, d * 0.2f, d * 0.28f, 0.75f, ClericLightFx.Squash, 10f, 7).Pop(220f).Env(60f, 400f).Phases(60f, 360f, 1f, 0.45f)
              .Ground(ElementLightLook.M(ClericLightLook.Band, pal, ClericLightLook.Role.Fill), 0f, endMs, d * 0.6f, d * 0.95f, 1f).Env(200f, 500f).Kind(LayerKind.Glow)
              // the gold star seal turning over the whole shower (the ultimate's emblem), brightest at the finale
              .Ground(ElementLightLook.M(ClericLightLook.SigilStar, gold, ClericLightLook.Role.Line), 100f, endMs - 80f, d * 0.42f, d * 0.55f, 0.8f, ClericLightFx.Squash, 8f, 6).Reveal(260f).Env(100f, 420f).Phases(last, last + 260f, 0.55f, 0.35f)
              // finale after the last drop: a starburst over the centre, a wave over the whole circle and a gold shockwave past it, motes drifting up
              .Ground(ElementLightLook.M(ClericLightLook.Band, pal, ClericLightLook.Role.Line), last + 60f, last + 480f, d * 0.2f, d / 0.78f, 0.85f, ClericLightFx.Squash, 0f, 8)
              .Ground(ElementLightLook.M(ClericLightLook.Band, gold, ClericLightLook.Role.Line), last + 40f, last + 360f, d * 0.3f, d / 0.78f * 1.12f, 0.5f, ClericLightFx.Squash, 0f, 9).Kind(LayerKind.Detail)
              .Upright(ElementLightLook.M(ClericLightLook.Star, gold, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, last + 40f, last + 560f, 22f, 62f, 22f, 62f, 0.9f, new Vector2(0f, 16f), 9).Pop(220f).Env(20f, 260f)
              .Upright(ElementLightLook.M(ClericLightLook.Core, pal, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, last + 40f, last + 320f, 16f, 46f, 16f, 46f, 0.85f, new Vector2(0f, 16f), 10);
            Color mote = ElementLightLook.A(pal.Light, 0.9f), star = gold.Light;
            fx.At(last + 50f, f => f.Ring(true, mid + new Vector2(0f, 16f), 20, reach * 1.2f, 2f, 3.2f, star, 0.55f))
              .At(last + 80f, f => f.Puff(false, mid, 18, reach * 0.7f, 18f, 1.8f, 2.8f, mote, 0.9f))
              .At(last + 120f, f => f.Puff(true, mid, 8, reach * 0.5f, 14f, 1.6f, 2.4f, star, 0.6f));
            if (ctx.Body.IsLocal) fx.At(last + 40f, f => { CameraFollow.SkillShake(0.35f); GameClock.HitStop(60f); });
            return fx;
        }

        void CasterSeal()
        {
            HeroController caster = ctx.Body.Hero;
            Vector2 hand = new Vector2(ctx.Dir.x * 7f, 16f);
            ClericLightFx.Create("AstralSeal", caster.Position, caster, 900f, dim)
                .Ground(ElementLightLook.M(ClericLightLook.Compass, pal, ClericLightLook.Role.Line), 0f, 900f, 16f, 28f, 0.85f, ClericLightFx.Squash, 40f, 5).Reveal(160f).Env(40f, 300f)
                .Upright(ElementLightLook.M(ClericLightLook.Core, pal, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 0f, 200f, 4f, 12f, 4f, 12f, 0.75f, hand)
                .Upright(ElementLightLook.M(ClericLightLook.Star, gold, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, 0f, 320f, 5f, 11f, 5f, 11f, 0.9f, hand, 1);
        }

        void Update() { age += GameClock.Dt * 1000f; Tick(); if (age > endMs + 300f) Destroy(gameObject); }

        void Tick()
        {
            for (int i = 0; i < spots.Length; i++)
            {
                float start = landMs[i] - FallMs;
                if (!spawned[i] && age >= start)
                {
                    spawned[i] = true;
                    bool trail = dim >= 0.6f;
                    stars[i] = new ElementMeteor(transform, spots[i] + SkyOffset * (i % 2 == 0 ? 1f : 0.85f) + new Vector2(0f, 8f), spots[i] + new Vector2(0f, 6f),
                        ElementLightLook.M(ClericLightLook.Star, gold, ClericLightLook.Role.Glyph), 17f, 17f, ElementLightLook.M(ClericLightLook.Core, pal, ClericLightLook.Role.Flash), 13f,
                        pal, trail ? 5.5f : 0f, 0.24f, dim >= 0.99f ? 1f : 0.7f, false);
                }
                if (stars[i] != null)
                {
                    stars[i].Tick((age - start) / FallMs, age / 1000f, age * 0.4f + i * 37f);
                    if (stars[i].Done(age / 1000f)) { stars[i].Destroy(); stars[i] = null; }
                }
                if (!landed[i] && age >= landMs[i]) { landed[i] = true; Land(spots[i]); }
            }
        }

        void Land(Vector2 at)
        {
            float d = 2f * impactR;
            Vector2 lift = new Vector2(0f, 6f);
            var fx = ClericLightFx.Create("AstralHit", at, null, 520f, dim);
            fx.Ground(ElementLightLook.M(ClericLightLook.Band, pal, ClericLightLook.Role.Line), 0f, 340f, 8f, d / 0.78f, 0.85f, ClericLightFx.Squash, 0f, 9)
              .Ground(ElementLightLook.M(ClericLightLook.Band, pal, ClericLightLook.Role.Fill), 0f, 420f, d * 0.5f, d, 1f).Kind(LayerKind.Glow)
              .Upright(ElementLightLook.M(ClericLightLook.Star, gold, ClericLightLook.Role.Glyph), ClericLightFx.Band.Over, false, 0f, 340f, 14f, 32f, 14f, 32f, 0.92f, lift, 1).Pop(140f)
              .Upright(ElementLightLook.M(ClericLightLook.Core, pal, ClericLightLook.Role.Flash), ClericLightFx.Band.Over, false, 0f, 190f, 10f, 28f, 10f, 28f, 0.85f, lift, 2);
            WizardLightSkills.Pulses(fx, ctx.Def.Id, 30f, ElementLightLook.M(ClericLightLook.Core, gold, ClericLightLook.Role.Flash), 8f, 18f, lift, 0.6f);
            Color spark = pal.Light;
            fx.At(0f, f => f.Ring(true, at + lift, 8, impactR * 2f, 1.8f, 2.8f, spark, 0.4f));
        }
    }
}
