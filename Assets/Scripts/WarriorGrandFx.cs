using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Building blocks of the Warrior's GRAND skill FX (21/11; user after the first shader pass: "chơi warrior vẫn thấy chán, không có hiệu ứng gì
    /// mạnh mẽ"; review loop + ChatGPT concepts in docs/reference/warrior-fx-grand/). All shader quads / mesh ribbons / particles on a ClericLightFx
    /// host (its clock, anchor and crowd LOD), sizes in world units (H = hero height ~20):
    ///  - Slash: 4-layer sword ribbon: wide soft glow, DARK rim underlay (1.5x), opaque saturated body, white-hot core line (0.38x);
    ///  - Impact: white-hot flash for 2-3 frames, spiky burst star, orange flash halo, streaking sparks + embers;
    ///  - Shock: thick opaque ground shockwave (war-shock: sharp front + solid trailing band) running out to a radius;
    ///  - Scorch: dark burnt decal + glowing cracks lingering ~0.6-1 s;
    ///  - Pillar: solid flame pillar (dark red rim, white-yellow core tongue inside), pops in, dissolves out, embers off the top;
    ///  - Fireball: solid fire puffs rising + dark smoke; Charge: anticipation glow gathering on the blade before a big hit.
    /// </summary>
    public static class WarriorGrandFx
    {
        const float H = WarriorLightLook.H, Sq = ClericLightFx.Squash;
        static readonly Dictionary<string, ClericLightLook.Pal> Mono = new Dictionary<string, ClericLightLook.Pal>();

        /// <summary>White-yellow inner fire (core tongue of a pillar, fireball centre).</summary>
        public static readonly ClericLightLook.Pal Hot = new ClericLightLook.Pal("WarHot", 0xFFC45B, 0xFFEC90, 0xFFFFF2, 0xFF8B35, new Color(1f, 0.6f, 0.15f));
        /// <summary>Flame pillar body (ChatGPT round 1: outer 35-45 % red, middle orange, inner yellow, white <= 8 %).</summary>
        public static readonly ClericLightLook.Pal Pyre = new ClericLightLook.Pal("WarPyre", 0xE72A25, 0xFF8B35, 0xFFC45B, 0x541B1E, new Color(0.85f, 0.25f, 0.04f));
        static readonly ClericLightLook.Pal Soot = new ClericLightLook.Pal("WarSoot", 0x3A2A22, 0x5E4A3E, 0x7A6656, 0x1A120E, Color.black);

        static ClericLightLook.Pal One(ClericLightLook.Pal p, string tag, Color c)
        {
            string key = p.Name + "|" + tag;
            ClericLightLook.Pal m;
            if (Mono.TryGetValue(key, out m)) return m;
            m = new ClericLightLook.Pal(key, 0, 0, 0, 0, p.Glow) { Main = c, Light = c, Core = c, Rim = c };
            Mono[key] = m;
            return m;
        }

        /// <summary>Sword ribbon in 4 layers (see the class summary); `width` = the opaque body (0.3-0.45 H for skills). Returns the body ribbon.</summary>
        public static ArcRibbonFx Slash(ClericLightFx fx, ArcRibbonFx.PathFn path, float t0, float sweepMs, float tailMs, float width, ClericLightLook.Pal p, float alpha,
            RibbonSort sort = RibbonSort.Over, int order = 0, HeroController around = null, float splitY = 6f, bool linear = false)
        {
            var parts = new List<ArcRibbonFx>(4);
            parts.Add(ArcRibbonFx.Add(fx, path, t0, sweepMs, tailMs * 1.2f, width * 2.3f, One(p, "glow", p.Main), alpha * 0.5f, sort, order - 2, around, splitY,
                WarriorLightLook.TrailLayer(p.Name + "|glow", ClericLightLook.A(p.Main, 0.3f), new Color(p.Glow.r * 0.9f, p.Glow.g * 0.9f, p.Glow.b * 0.9f, 1f), 0f)));
            parts.Add(ArcRibbonFx.Add(fx, path, t0, sweepMs, tailMs * 1.08f, width * 1.5f, One(p, "rim", p.Rim), alpha * 0.9f, sort, order - 1, around, splitY,
                WarriorLightLook.TrailLayer(p.Name + "|rim", ClericLightLook.A(p.Rim, 0.92f), new Color(0.12f, 0.03f, 0.01f, 1f), 0f)));
            ArcRibbonFx body = ArcRibbonFx.Add(fx, path, t0, sweepMs, tailMs, width, p, alpha, sort, order, around, splitY, WarriorLightLook.TrailBright(p));
            parts.Add(body);
            parts.Add(ArcRibbonFx.Add(fx, path, t0, sweepMs, tailMs * 0.85f, width * 0.38f, One(p, "core", p.Core), alpha, sort, order + 1, around, splitY,
                WarriorLightLook.TrailLayer(p.Name + "|core", ClericLightLook.A(p.Core, 1f), new Color(1.5f, 1.25f, 0.9f, 1f), 1.6f)));
            if (linear) foreach (ArcRibbonFx r in parts) if (r != null) r.Linear();
            return body;
        }

        /// <summary>Impact at `at` (offset from the host anchor) at `t`, `size` units across (1.5-2.5 H for a big hit): a white-hot disc for ~50 ms,
        /// a spiky burst star, an orange flash halo, `sparks` streaking sparks + embers. `band` Behind keeps the hero visible for a hit on himself.</summary>
        public static void Impact(ClericLightFx fx, float t, Vector2 at, float size, ClericLightLook.Pal p, int sparks, ClericLightFx.Band band = ClericLightFx.Band.Over)
        {
            fx.Upright(WarriorLightLook.Flash(p), band, false, t, t + 260f, size * 0.4f, size * 0.95f, size * 0.36f, size * 0.85f, 0.85f, at, 5)
              .Upright(WarriorLightLook.Spark(p), band, false, t, t + 200f, size * 0.45f, size * 1.1f, size * 0.45f, size * 1.1f, 1f, at, 6).Turn(Random.value * 30f)
              .Upright(WarriorLightLook.Flash(Hot), band, false, t, t + 60f, size * 0.3f, size * 0.45f, size * 0.3f, size * 0.45f, 1f, at, 7).Env(0f, 25f);
            fx.At(t, f =>
            {
                MechanicLightBursts.Sparks(f, f.Anchor + at, sparks, 110f + size * 2.5f, 90f, 360f, p.Light, 0.32f, 1.9f);
                MechanicLightBursts.Sparks(f, f.Anchor + at, sparks / 2, 70f + size * 2f, 90f, 360f, Hot.Core, 0.24f, 1.6f);
                Embers(f, f.Anchor + at, sparks / 2, size * 0.3f, p);
            });
        }

        /// <summary>Thick ground shockwave from radius r0 to r1 over `ms` (front lands exactly on r1), centred `at`.</summary>
        public static ClericLightFx Shock(ClericLightFx fx, float t, float ms, Vector2 at, float r0, float r1, ClericLightLook.Pal p, float alpha = 1f, int order = 7)
        {
            return fx.Ground(WarriorLightLook.Wave(p), t, t + ms, 2f * r0 / WarriorLightLook.ShockBand, 2f * r1 / WarriorLightLook.ShockBand, alpha, Sq, 0f, order, at);
        }

        /// <summary>Burnt decal of radius `r` at `at` that lingers `ms` (60 ms in, dissolves over the last 40 %), glowing `fire` cracks over it.</summary>
        public static void Scorch(ClericLightFx fx, float t, float ms, Vector2 at, float r, ClericLightLook.Pal fire)
        {
            fx.Ground(WarriorLightLook.ScorchMat, t, t + ms, r * 1.7f, r * 2f, 1f, Sq, Random.value * 360f, 1, at).Env(60f, ms * 0.4f).Dissolve()
              .Ground(WarriorLightLook.Line(WarriorLightLook.Crack, fire), t, t + ms * 0.75f, r * 1.8f, r * 1.9f, 1f, Sq, 0f, 3, at).Env(30f, ms * 0.35f).Dissolve();
        }

        /// <summary>Flame pillar standing at `at`: solid tongue `w` x `h` (dark red rim, gold body) + white-yellow inner tongue, embers off the top.</summary>
        public static void Pillar(ClericLightFx fx, float t, float life, Vector2 at, float w, float h, ClericLightLook.Pal p, ClericLightFx.Band band = ClericLightFx.Band.Over, int order = 3, LayerKind kind = LayerKind.Main)
        {
            fx.Upright(WarriorLightLook.Glyph(WarriorLightLook.FlameTongue, Pyre), band, true, t, t + life, w * 0.55f, w, h * 0.25f, h, 1f, at, order).Pop(150f).Dissolve().Kind(kind)
              .Upright(WarriorLightLook.Glyph(WarriorLightLook.FlameTongue, Hot), band, true, t + 20f, t + life * 0.7f, w * 0.25f, w * 0.45f, h * 0.12f, h * 0.5f, 1f, at, order + 1).Pop(130f).Dissolve().Kind(LayerKind.Detail);
            fx.At(t + 90f, f => Embers(f, f.Anchor + at + new Vector2(0f, h * 0.6f), 3, w * 0.3f, p));
        }

        /// <summary>Solid fireball at `at` (radius r): 3 fire puffs growing and rising, a white-yellow core, then dark smoke rising and dissolving.</summary>
        public static void Fireball(ClericLightFx fx, float t, Vector2 at, float r, ClericLightLook.Pal p)
        {
            Material fire = WarriorLightLook.Glyph(MechanicLightLook.Puff, p), hot = WarriorLightLook.Glyph(MechanicLightLook.Puff, Hot);
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + Random.Range(-30f, 30f)) * Mathf.Deg2Rad, s = r * Random.Range(1f, 1.25f);
                Vector2 off = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f) * r * 0.25f;
                fx.Upright(fire, ClericLightFx.Band.Over, false, t + i * 20f, t + 380f + i * 40f, s * 0.4f, s * 1.2f, s * 0.4f, s * 1.15f, 1f, off, 3 + i).Rise(r * 0.35f).Dissolve();
            }
            fx.Upright(hot, ClericLightFx.Band.Over, false, t, t + 240f, r * 0.4f, r * 0.9f, r * 0.4f, r * 0.9f, 1f, at, 7).Dissolve();
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + Random.Range(-25f, 25f)) * Mathf.Deg2Rad, s = r * Random.Range(0.8f, 1.05f), t0 = t + 140f + i * 35f;
                Vector2 off = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * Sq) * r * 0.45f;
                fx.Upright(SkillLightMaterials.Smoke(MechanicLightLook.Puff, Soot, 0.8f), ClericLightFx.Band.Over, false, t0, t0 + 760f, s * 0.5f, s * 1.3f, s * 0.5f, s * 1.3f, 0.85f, off, i % 2 == 0 ? 1 : 2)
                  .Rise(r * 0.8f).Dissolve().Kind(LayerKind.Detail);
            }
        }

        /// <summary>Anticipation on the blade from t0 to t1: a hot glow gathering at `at` (grows to `size`), a spinning star, motes drawn in.</summary>
        public static void Charge(ClericLightFx fx, float t0, float t1, Vector2 at, float size, ClericLightLook.Pal p)
        {
            float span = Mathf.Max(60f, t1 - t0);
            fx.Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, t0, t1 + 60f, size * 0.25f, size, size * 0.25f, size, 0.95f, at, 4).Env(span * 0.85f, 60f)
              .Upright(WarriorLightLook.Spark(Hot), ClericLightFx.Band.Over, false, t0 + span * 0.4f, t1 + 60f, size * 0.3f, size * 0.8f, size * 0.3f, size * 0.8f, 1f, at, 5).Env(span * 0.4f, 50f);
            for (int i = 0; i < 3; i++)
                fx.At(t0 + i * span / 3f, f =>
                {
                    int n = f.Count(6);
                    for (int k = 0; k < n; k++)
                    {
                        Vector2 d = WarriorLightSkills.Dir(Random.value * 360f) * size * 1.6f;
                        f.Emit(false, f.Anchor + at + d, -d * 3.5f, Random.Range(1.6f, 2.6f), p.Light, 0.28f);
                    }
                });
        }

        /// <summary>`n` embers (soft hot dots) rising and drifting from `at` within `spread`.</summary>
        public static void Embers(ClericLightFx f, Vector2 at, int n, float spread, ClericLightLook.Pal p)
        {
            f.Puff(false, at, n, spread, 38f, 1.6f, 2.8f, p.Light, 0.7f);
            f.Puff(false, at, Mathf.Max(1, n / 2), spread, 55f, 1.2f, 2f, Hot.Core, 0.55f);
        }
    }
}
