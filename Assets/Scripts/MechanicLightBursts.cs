using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Reusable pieces of the Mechanic light FX (RO3 Soft Fantasy V1, 2026-11-21), all shader quads + particles on ClericLightFx hosts:
    ///  - Muzzle: flare cone along the aim + small core flash + 2-4 brass sparks (shots, cannon, turret);
    ///  - Explosion: soft fantasy blast = warm honey core flash, ground ring stopping on the blast radius, faint pool, 3-5 smoke puffs rising and
    ///    dissolving, brass sparks streaking out (no fireball sprite, no debris rocks);
    ///  - Pop: small hit = 4-point sparkle + thin ring + 3-4 sparks in one palette (honey bullets, cyan plasma / EMP stun);
    ///  - Reticle: lock-on reticle that shrinks onto its spot and spins in (Target Lock, Orbital Cannon, Rocket Barrage).
    /// Sizes in world units (H ~ 20). Crowd LOD comes from the host (ClericLightFxLod): strangers get the main shapes only, few particles.
    /// </summary>
    public static partial class MechanicLightBursts
    {
        static readonly Vector2 Lift = new Vector2(0f, 6f);

        /// <summary>A fixed-point host for `ms` (+ particle tail), dim = crowd dim of the caster.</summary>
        public static ClericLightFx Host(string name, Vector2 at, float ms, float dim) { return ClericLightFx.Create("Mech" + name, at, null, ms, dim); }

        /// <summary>`n` sparks from `at` streaking out (stretched particles), spread over `arcDeg` around `angleDeg` (360 = all round).</summary>
        public static void Sparks(ClericLightFx f, Vector2 at, int n, float speed, float angleDeg, float arcDeg, Color c, float life, float size = 1.4f)
        {
            n = f.Count(n);
            if (n <= 0 || !FeelConfig.FxOn) return;
            Transform t = f.transform.Find("Sparks");
            ParticleSystem ps = t != null ? t.GetComponent<ParticleSystem>() : SkillLightMaterials.Sparks(f.transform, FeelConfig.FxOrder - 17, 64, 6f);
            float alpha = f.Lod == FxTier.Own ? 1f : f.Lod == FxTier.Near ? 0.8f : 0.55f;
            for (int i = 0; i < n; i++)
            {
                float a = (angleDeg + (arcDeg >= 360f ? i * 360f / n + Random.Range(-15f, 15f) : Random.Range(-arcDeg, arcDeg) * 0.5f)) * Mathf.Deg2Rad;
                Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f);
                WantedFxParticles.Emit(ps, f.ParticlePos(at + d * 1.5f), f.ParticleVel(d * speed * Random.Range(0.6f, 1.15f)), size * Random.Range(0.8f, 1.2f), new Color(c.r, c.g, c.b, c.a * alpha), life * Random.Range(0.75f, 1.15f));
            }
        }

        /// <summary>Muzzle discharge at `at` along `angleDeg`: flare `len` units long (out of the muzzle), core flash, sparks. `order` 0 = FX band.</summary>
        public static ClericLightFx Muzzle(Vector2 at, float angleDeg, float len, float dim, ClericLightLook.Pal p, int sparks = 3, int order = 0)
        {
            var f = Host("Muzzle", at, 180f, dim);
            int o = order != 0 ? order : FeelConfig.FxOrder - 8;
            MechanicLightQuad.Add(f, MechanicLightLook.Glyph(MechanicLightLook.Flare, p), at, angleDeg, 0f, 140f, new Vector2(len * 0.5f, len * 0.35f), new Vector2(len * 1.1f, len * 0.6f), 1f, o, false, true);
            MechanicLightQuad.Add(f, MechanicLightLook.Glyph(MechanicLightLook.Flare, MechanicLightLook.Fire), at, angleDeg, 0f, 110f, new Vector2(len * 0.4f, len * 0.22f), new Vector2(len * 0.8f, len * 0.38f), 0.9f, o + 1, false, true);
            MechanicLightQuad.Add(f, MechanicLightLook.Flash(MechanicLightLook.Fire), at, 0f, 0f, 110f, new Vector2(len * 0.45f, len * 0.45f), new Vector2(len * 0.8f, len * 0.8f), 0.95f, o + 2);
            if (sparks > 0) Sparks(f, at, sparks, 80f + len * 3f, angleDeg, 55f, MechanicLightLook.Ember, 0.2f, 1.5f);
            return f;
        }

        /// <summary>Small hit at `at` (centre of the monster) in palette `p`, `size` units (~0.6-1 H).</summary>
        public static ClericLightFx Pop(Vector2 at, float size, float dim, ClericLightLook.Pal p, Color spark)
        {
            var f = Host("Pop", at, 280f, dim);
            f.Upright(MechanicLightLook.Flash(p), ClericLightFx.Band.Over, false, 0f, 130f, size * 0.5f, size * 0.95f, size * 0.5f, size * 0.95f, 0.95f, Vector2.zero, 1)
             .Upright(MechanicLightLook.Glyph(MechanicLightLook.Spark, p), ClericLightFx.Band.Over, false, 0f, 220f, size * 0.6f, size * 1.3f, size * 0.6f, size * 1.3f, 1f, Vector2.zero, 2)
             .Ground(MechanicLightLook.Line(MechanicLightLook.Band, p), 0f, 240f, size * 0.4f, size * 2.1f, 0.9f, ClericLightFx.Squash, 0f, 7, new Vector2(0f, -6f))
             .At(0f, h => Sparks(h, at, 7, 85f, 90f, 360f, spark, 0.24f, 1.6f));
            return f;
        }

        /// <summary>
        /// Lighter pulses (flash + thin ring, no sparks) for the 2nd..Nth damage number of a split hit (Rpg.SkillHitSplit.For(skillId)), timed
        /// StartDelayMs + k * IntervalMs after the hit. Own casts only (dim 1): only the caster's numbers are split.
        /// </summary>
        public static void SplitPulses(string skillId, Vector2 at, float size, float dim, ClericLightLook.Pal p, bool finisher = false)
        {
            if (dim < 0.99f) return;
            Rpg.HitSplit s = Rpg.SkillHitSplit.For(skillId);
            int parts = s.PartsFor(finisher);
            if (parts <= 1) return;
            float step = Mathf.Max(30f, s.IntervalMs), end = s.StartDelayMs + (parts - 1) * step + 200f;
            var f = Host("Pulses", at, end, dim);
            for (int k = 1; k < parts; k++)
            {
                float t0 = s.StartDelayMs + k * step, sz = size * (k == parts - 1 ? 0.85f : 0.7f);
                f.Upright(MechanicLightLook.Flash(p), ClericLightFx.Band.Over, false, t0, t0 + 110f, sz * 0.3f, sz * 0.6f, sz * 0.3f, sz * 0.6f, 0.7f, Vector2.zero, 1)
                 .Ground(MechanicLightLook.Line(MechanicLightLook.Band, p), t0, t0 + 180f, sz * 0.3f, sz * 1.3f, 0.6f, ClericLightFx.Squash, 0f, 7, new Vector2(0f, -6f)).Kind(LayerKind.Detail);
            }
        }

        /// <summary>Lock-on reticle on `at` (ground point), diameter `d`: starts 1.8x and spins in to its size by `lockMs`, held to `endMs`.</summary>
        public static ClericLightFx Reticle(Vector2 at, float d, float lockMs, float endMs, float dim, ClericLightLook.Pal p, int order = 10)
        {
            var f = Host("Reticle", at, endMs, dim);
            f.Ground(MechanicLightLook.Glyph(MechanicLightLook.Reticle, p), 0f, endMs, d * 1.8f, d, 0.9f, 0.85f, 45f, order).Env(80f, 160f).Pop(Mathf.Max(60f, lockMs))
             .Ground(MechanicLightLook.Pool(p), 0f, endMs, d * 0.4f, d * 0.8f, 0.7f, 0.85f).Kind(LayerKind.Glow).Env(80f, 160f);
            return f;
        }
    }
}
