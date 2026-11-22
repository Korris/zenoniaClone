using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Building blocks of the Warrior's skill FX in the "RO trailer style" (user 21/11: Warrior = Knight of Light and Steel, BIG + TRANSLUCENT +
    /// PALE + clear shapes; guide docs/reference/ro-trailer-skill-style/prompt-templates.md). All shader quads / mesh ribbons / particles on a
    /// ClericLightFx host (its clock, anchor and crowd LOD), sizes in world units (H = hero height ~20.5):
    ///  - Sector: the pale Bash fan lying flat on the ground (RoSoftSector), opening as a sweep, bright ivory rim on the real hit range;
    ///  - Slash: 3-layer sword ribbon: soft Outer veil (2.2x, ~0.12), translucent Body (~0.34), thin Bright / Core edge line (0.22x, additive);
    ///  - Impact: short ivory flash (2-3 frames), a translucent star, a few ivory sparkles (no embers);
    ///  - Shock: thin ground ring (war-shock) with a bright edge and a transparent body, running out to a radius;
    ///  - Column: a translucent golden light column (Earth Break, Berserker) instead of the old fire pillars;
    ///  - Lick / HoneyBurst / Embers: the small honey-fire touches of the two real Fire skills (Flame Cleave, Inferno Crash) only;
    ///  - Charge: anticipation glow gathering on the blade before a big hit.
    /// No dark rims, scorch decals, smoke or red anywhere (red = boss warnings).
    /// </summary>
    public static class WarriorGrandFx
    {
        const float H = WarriorLightLook.H, Sq = ClericLightFx.Squash;
        static readonly Dictionary<string, ClericLightLook.Pal> Mono = new Dictionary<string, ClericLightLook.Pal>();

        /// <summary>Ivory-white highlight palette (impact cores, the edge line of a slash).</summary>
        public static readonly ClericLightLook.Pal Hot = new ClericLightLook.Pal("WarIvory", 0xF5EA7C, 0xFEFDF8, 0xFFFFFF, 0xD5C378, new Color(0.5f, 0.47f, 0.32f));

        static ClericLightLook.Pal One(ClericLightLook.Pal p, string tag, Color c)
        {
            string key = p.Name + "|" + tag;
            ClericLightLook.Pal m;
            if (Mono.TryGetValue(key, out m)) return m;
            m = new ClericLightLook.Pal(key, 0, 0, 0, 0, p.Glow) { Main = c, Light = c, Core = c, Rim = c };
            Mono[key] = m;
            return m;
        }

        static Color K(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, 1f); }

        /// <summary>Sword ribbon in 3 translucent layers (see the class summary); `width` = the body. Returns the body ribbon.</summary>
        public static ArcRibbonFx Slash(ClericLightFx fx, ArcRibbonFx.PathFn path, float t0, float sweepMs, float tailMs, float width, ClericLightLook.Pal p, float alpha,
            RibbonSort sort = RibbonSort.Over, int order = 0, HeroController around = null, float splitY = 6f, bool linear = false)
        {
            var parts = new List<ArcRibbonFx>(3);
            parts.Add(ArcRibbonFx.Add(fx, path, t0, sweepMs, tailMs * 1.15f, width * 1.6f, One(p, "veil", p.Rim), alpha * 0.85f, sort, order - 1, around, splitY,
                WarriorLightLook.TrailLayer(p.Name + "|veil", ClericLightLook.A(p.Rim, 0.3f), K(p.Glow, 0.25f), 0f))); // r2: same-family Outer edge, what keeps the ribbon on snow
            ArcRibbonFx body = ArcRibbonFx.Add(fx, path, t0, sweepMs, tailMs, width, p, alpha, sort, order, around, splitY, WarriorLightLook.TrailBright(p));
            parts.Add(body);
            parts.Add(ArcRibbonFx.Add(fx, path, t0, sweepMs, tailMs * 0.8f, width * 0.26f, One(p, "edge", p.Light), alpha, sort, order + 1, around, splitY,
                WarriorLightLook.TrailLayer(p.Name + "|edge", ClericLightLook.A(p.Light, 0.75f), K(p.Glow, 1.1f), 0.9f)));
            if (linear) foreach (ArcRibbonFx r in parts) if (r != null) r.Linear();
            return body;
        }

        /// <summary>
        /// Pale ground fan (the Bash sector) centred `at` (offset from the host anchor), pointing `aimDeg`, `angleDeg` wide (>= 360 = full disc),
        /// its bright rim at `radius` (the real hit range). Opens as a sweep over `sweepMs` from `t`, holds, fades over `outMs` before `t + life`.
        /// `mirror` opens it the other way round. Returns the host. When the shader is missing: a ground ribbon arc on the rim.
        /// </summary>
        public static ClericLightFx Sector(ClericLightFx fx, float t, float sweepMs, float life, float outMs, Vector2 at, float aimDeg, float angleDeg, float radius,
            ClericLightLook.Pal p, float bodyA = 0.26f, float rimA = 0.6f, bool mirror = false, float inner = 0.12f, int order = 2)
        {
            Material m = WarriorLightLook.Sector(p, angleDeg, bodyA, rimA, inner);
            float d = 2f * radius / WarriorLightLook.SectorRim;
            if (m == null)
            {
                float gm = WarriorLightLook.ParamDeg(aimDeg, Sq), half = Mathf.Min(180f, angleDeg * 0.5f);
                Slash(fx, ArcRibbonFx.Arc(at, radius, Sq, gm + half, gm - half), t, sweepMs, life, 0.3f * H, p, 0.8f, RibbonSort.Ground, order);
                return fx;
            }
            return fx.Ground(m, t, t + life, d * 0.97f, d, 1f, 1f, 0f, order, at).Aim(aimDeg, Sq, mirror).Reveal(Mathf.Max(1f, sweepMs)).Env(0f, outMs);
        }

        /// <summary>Impact at `at` (offset from the host anchor) at `t`, `size` units across (RO: 0.8-1.8 H, ultimate 1.5-2.5 H): a short ivory flash,
        /// a translucent star, `sparks` ivory sparkles. `band` Behind keeps the hero visible for a hit on himself. `floorH` >= 0: `at` is a real ground
        /// point (from a world direction) plus (0, floorH) of height (ClericLightFx.Floor), else a picture offset round the anchor.</summary>
        public static void Impact(ClericLightFx fx, float t, Vector2 at, float size, ClericLightLook.Pal p, int sparks, ClericLightFx.Band band = ClericLightFx.Band.Over, float floorH = -1f)
        {
            fx.Upright(WarriorLightLook.Spark(p), band, false, t, t + 200f, size * 0.45f, size * 1.0f, size * 0.45f, size * 1.0f, 0.75f, at, 6).Turn(Random.value * 30f);
            if (floorH >= 0f) fx.Floor(floorH);
            fx.Upright(WarriorLightLook.Flash(Hot), band, false, t, t + 60f, size * 0.16f, size * 0.26f, size * 0.16f, size * 0.26f, 0.65f, at, 7).Env(0f, 35f); // V3 review: core -25-30 %
            if (floorH >= 0f) fx.Floor(floorH);
            if (sparks <= 0) return;
            fx.At(t, f =>
            {
                MechanicLightBursts.Sparks(f, f.Anchor + at, sparks, 100f + size * 2f, 90f, 360f, p.Light, 0.26f, 1.7f);
                f.Ring(true, f.Anchor + at, Mathf.Max(2, sparks / 4), 40f, 1.4f, 2.2f, Hot.Core, 0.22f);
            });
        }

        /// <summary>Thin ground shockwave from radius r0 to r1 over `ms` (bright edge lands exactly on r1, body transparent), centred `at`.</summary>
        public static ClericLightFx Shock(ClericLightFx fx, float t, float ms, Vector2 at, float r0, float r1, ClericLightLook.Pal p, float alpha = 0.8f, int order = 7)
        {
            return fx.Ground(WarriorLightLook.Wave(p), t, t + ms, 2f * r0 / WarriorLightLook.ShockBand, 2f * r1 / WarriorLightLook.ShockBand, alpha, Sq, 0f, order, at);
        }

        /// <summary>Translucent golden light column standing at `at`: `w` x `h`, pops up, streaks scrolling, dissolves out (replaces the fire pillars).</summary>
        public static void Column(ClericLightFx fx, float t, float life, Vector2 at, float w, float h, ClericLightLook.Pal p, ClericLightFx.Band band = ClericLightFx.Band.Over,
            int order = 3, LayerKind kind = LayerKind.Main, float alpha = 0.75f)
        {
            fx.Upright(ClericLightLook.Column(p), band, true, t, t + life, w * 0.6f, w, h * 0.35f, h, alpha, at, order).Floor().Pop(140f).Env(0f, life * 0.45f).Kind(kind)
              .Upright(WarriorLightLook.Glyph(ClericLightLook.Streak, Hot), band, true, t, t + life * 0.55f, w * 0.18f, w * 0.28f, h * 0.3f, h * 0.85f, 0.7f, at, order + 1).Floor().Pop(120f).Dissolve().Kind(LayerKind.Detail);
        }

        /// <summary>Small honey flame tongue (Fire skills only): `w` x `h`, quick pop, gone in `life` (&lt;= ~250 ms for Flame Cleave).</summary>
        public static void Lick(ClericLightFx fx, float t, float life, Vector2 at, float w, float h, ClericLightFx.Band band = ClericLightFx.Band.Over, int order = 3, LayerKind kind = LayerKind.Detail, float alpha = 0.78f)
        {
            fx.Upright(WarriorLightLook.Glyph(WarriorLightLook.FlameTongue, WarriorLightLook.Flame), band, true, t, t + life, w * 0.5f, w * 0.85f, h * 0.3f, h, alpha, at, order).Floor().Pop(110f).Dissolve().Kind(kind);
        }

        /// <summary>Honey fire burst at `at` (radius r, Fire skills only): 3 translucent honey puffs rising + an ivory core, no smoke.</summary>
        public static void HoneyBurst(ClericLightFx fx, float t, Vector2 at, float r)
        {
            Material fire = WarriorLightLook.Glyph(MechanicLightLook.Puff, WarriorLightLook.Flame), hot = WarriorLightLook.Flash(Hot);
            for (int i = 0; i < 3; i++)
            {
                float a = (i * 120f + Random.Range(-30f, 30f)) * Mathf.Deg2Rad, s = r * Random.Range(0.9f, 1.1f);
                Vector2 off = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f) * r * 0.25f;
                fx.Upright(fire, ClericLightFx.Band.Over, false, t + i * 20f, t + 320f + i * 30f, s * 0.4f, s * 1.1f, s * 0.4f, s * 1.05f, 0.6f, off, 3 + i).Rise(r * 0.3f).Dissolve();
            }
            fx.Upright(hot, ClericLightFx.Band.Over, false, t, t + 150f, r * 0.3f, r * 0.6f, r * 0.3f, r * 0.6f, 0.9f, at, 7).Env(0f, 90f);
        }

        /// <summary>Anticipation on the blade from t0 to t1: a soft ivory glow gathering at `at` (grows to `size`), a small star, motes drawn in.</summary>
        public static void Charge(ClericLightFx fx, float t0, float t1, Vector2 at, float size, ClericLightLook.Pal p)
        {
            float span = Mathf.Max(60f, t1 - t0);
            fx.Upright(WarriorLightLook.Flash(p), ClericLightFx.Band.Over, false, t0, t1 + 50f, size * 0.25f, size * 0.8f, size * 0.25f, size * 0.8f, 0.7f, at, 4).Env(span * 0.85f, 50f)
              .Upright(WarriorLightLook.Spark(Hot), ClericLightFx.Band.Over, false, t0 + span * 0.4f, t1 + 50f, size * 0.3f, size * 0.7f, size * 0.3f, size * 0.7f, 0.8f, at, 5).Env(span * 0.4f, 40f);
            for (int i = 0; i < 3; i++)
                fx.At(t0 + i * span / 3f, f =>
                {
                    int n = f.Count(5);
                    for (int k = 0; k < n; k++)
                    {
                        Vector2 d = WarriorLightSkills.Dir(Random.value * 360f) * size * 1.6f;
                        f.Emit(false, f.Anchor + at + d, -d * 3.5f, Random.Range(1.4f, 2.2f), p.Light, 0.28f);
                    }
                });
        }

        /// <summary>`n` honey embers rising from `at` within `spread` (Fire skills only; keep n small: RO solo 12-20 at most).</summary>
        public static void Embers(ClericLightFx f, Vector2 at, int n, float spread, ClericLightLook.Pal p)
        {
            f.Puff(false, at, n, spread, 34f, 1.3f, 2.2f, p.Light, 0.6f);
        }

        /// <summary>A point on the ground ellipse at `deg`, `dist` units from the centre (2.5D / 3D maps: on the real circle, the camera foreshortens it).</summary>
        public static Vector2 OnGround(float deg, float dist)
        {
            Vector2 d = WarriorLightSkills.Dir(deg) * dist;
            if (!ClericLightFx.Deep) d.y *= Sq;
            return d;
        }
    }
}
