using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Mechanic area skills in light FX (see MechanicLightSkills). Ground rings lie on the ground ellipse (ClericLightFx.Squash) and the boundary ring
    /// sits exactly on the gameplay radius; the light gathers at the caster / targets (rule 3).
    ///  - Target Lock (status strike r 7 at 0 ms, 5 nearest marked): cyan seal under the caster, a cyan scan wave out to the radius, the boundary
    ///    ring, and a honey reticle locking onto each marked monster as the wave passes it;
    ///  - EMP Burst (strike r 3 at 0 ms, stun): cyan core flash, cyan seal, two cyan ring waves to the radius, boundary, crackle; cyan pops on the stunned;
    ///  - Deploy Turret (cast; the turret's own touch-down / shots: MechanicTurretLightFx): small cyan seal + hand flash;
    ///  - Orbital Cannon (6 shells 250 ms apart from 0 ms, scatter 4, blast 1.5): cyan target zone + uplink at the caster, a honey reticle shrinking
    ///    onto each landing spot until its shell, a short honey beam from the sky + soft fantasy explosion per shell, the last one bigger.
    /// </summary>
    public static partial class MechanicLightSkills
    {
        const float TargetLockMs = 760f, TargetScanMs = 320f, EmpMs = 820f, EmpWaveMs = 260f, TurretCastMs = 450f;

        static void TargetLock(SkillContext ctx)
        {
            SkillEffect e = First(ctx, EffectKind.Strike);
            if (e == null) return;
            float r = e.Radius * ctx.Units, dim = Dim(ctx);
            ClericLightLook.Pal tech = MechanicLightLook.Tech, honey = MechanicLightLook.Honey;
            var f = MechanicLightBursts.Host("TargetLock", ctx.Feet, TargetLockMs, dim);
            f.Ground(MechanicLightLook.Line(MechanicLightLook.Ring, tech), 0f, TargetLockMs, MechanicLightLook.RingQuad(12f), MechanicLightLook.RingQuad(14f), 0.85f, ClericLightFx.Squash, 50f, 6).Reveal(150f).Env(0f, 220f)
             .Ground(MechanicLightLook.M(MechanicLightLook.Band, tech, ClericLightLook.Role.Arc), 0f, TargetScanMs + 120f, 12f, MechanicLightLook.BandQuad(r), 1f, ClericLightFx.Squash, 0f, 5).Arc(9f)
             .Ground(MechanicLightLook.Line(MechanicLightLook.Ring, tech), 120f, TargetLockMs, MechanicLightLook.RingQuad(r) * 0.97f, MechanicLightLook.RingQuad(r), 0.6f, ClericLightFx.Squash, 0f, 4).Env(120f, 260f)
             .Ground(MechanicLightLook.Pool(tech), 0f, 480f, 12f, 30f, 0.9f).Kind(LayerKind.Glow)
             .Upright(MechanicLightLook.Flash(tech), ClericLightFx.Band.Over, false, 0f, 160f, 5f, 11f, 5f, 11f, 0.75f, Chest, 1);
            int limit = Mathf.Max(1, e.Count);
            List<Monster> marked = InCircle(ctx.Feet, r);
            for (int i = 0; i < marked.Count && i < limit; i++)
            {
                Monster m = marked[i];
                Vector2 spot = m.Feet;
                float when = Mathf.Clamp01((spot - ctx.Feet).magnitude / r) * TargetScanMs;
                f.At(when, h =>
                {
                    MechanicLightBursts.Reticle(spot, 32f, 200f, TargetLockMs - when + 120f, dim, honey);
                    MechanicLightBursts.Host("LockPing", spot, 220f, dim)
                        .Upright(MechanicLightLook.Flash(honey), ClericLightFx.Band.Over, false, 0f, 160f, 6f, 16f, 6f, 16f, 0.9f, new Vector2(0f, 10f), 1)
                        .At(0f, g => MechanicLightBursts.Sparks(g, spot + new Vector2(0f, 10f), 6, 70f, 90f, 360f, MechanicLightLook.Ember, 0.22f, 1.5f));
                });
            }
        }

        static void EmpBurst(SkillContext ctx)
        {
            SkillEffect e = First(ctx, EffectKind.Strike);
            if (e == null) return;
            float r = e.Radius * ctx.Units, dim = Dim(ctx);
            ClericLightLook.Pal tech = MechanicLightLook.Tech;
            Material wave = MechanicLightLook.Line(MechanicLightLook.Band, tech), ring = MechanicLightLook.Line(MechanicLightLook.Ring, tech);
            var f = MechanicLightBursts.Host("Emp", ctx.Feet, EmpMs, dim);
            f.Ground(MechanicLightLook.Pool(tech), 0f, 520f, r * 0.8f, r * 1.7f, 1f).Kind(LayerKind.Glow)
             .Ground(ring, 0f, 640f, MechanicLightLook.RingQuad(13f), MechanicLightLook.RingQuad(16f), 0.9f, ClericLightFx.Squash, 40f, 6).Reveal(90f).Env(0f, 260f)
             .Ground(wave, 0f, EmpWaveMs + 140f, 10f, MechanicLightLook.BandQuad(r), 0.95f, ClericLightFx.Squash, 0f, 7)
             .Ground(wave, 70f, EmpWaveMs + 260f, 8f, MechanicLightLook.BandQuad(r * 0.96f), 0.55f, ClericLightFx.Squash, 0f, 6).Kind(LayerKind.Detail)
             .Ground(ring, 140f, EmpMs, MechanicLightLook.RingQuad(r) * 0.97f, MechanicLightLook.RingQuad(r), 0.55f, ClericLightFx.Squash, -25f, 4).Env(80f, 300f)
             .Upright(MechanicLightLook.Flash(tech), ClericLightFx.Band.Over, false, 0f, 190f, 16f, 34f, 16f, 34f, 1f, Chest, 2)
             .Upright(ClericLightLook.Column(tech), ClericLightFx.Band.Over, true, 0f, 240f, 14f, 22f, 60f, 48f, 0.85f, Vector2.zero, 1)
             .Upright(MechanicLightLook.Glyph(MechanicLightLook.Spark, tech), ClericLightFx.Band.Over, false, 0f, 240f, 18f, 40f, 18f, 40f, 0.95f, Chest, 3)
             .At(0f, h => MechanicLightBursts.Sparks(h, h.Anchor + Chest, 26, 120f + r * 2.5f, 90f, 360f, MechanicLightLook.Crackle, 0.36f, 2f))
             .At(40f, h => h.Ring(true, h.Anchor + new Vector2(0f, 4f), 6, r * 2.2f, 1.6f, 2.4f, tech.Light, 0.24f));
            foreach (Monster m in InCircle(ctx.Feet, r))
            {
                Vector2 at = m.Center;
                float when = Mathf.Clamp01((m.Feet - ctx.Feet).magnitude / r) * EmpWaveMs;
                f.At(when, h => Pulses(ctx.Def.Id, at, 19f, dim, tech, MechanicLightLook.Crackle));
            }
            if (Local(ctx)) { GameClock.HitStop(60f); CameraFollow.SkillShake(0.4f); }
        }

        static void DeployTurret(SkillContext ctx)
        {
            float dim = Dim(ctx);
            ClericLightLook.Pal tech = MechanicLightLook.Tech;
            Vector2 hand = new Vector2(ctx.Dir.x * 7f, 11f);
            MechanicLightBursts.Host("TurretCast", ctx.Feet, TurretCastMs, dim)
                .Ground(MechanicLightLook.Line(MechanicLightLook.Ring, tech), 0f, TurretCastMs, MechanicLightLook.RingQuad(10f), MechanicLightLook.RingQuad(12f), 0.8f, ClericLightFx.Squash, 70f, 5).Reveal(200f).Env(0f, 160f)
                .Upright(MechanicLightLook.Flash(tech), ClericLightFx.Band.Over, false, 0f, 180f, 4f, 9f, 4f, 9f, 0.7f, hand, 1)
                .At(60f, h => MechanicLightBursts.Sparks(h, h.Anchor + hand, 4, 45f, 90f, 360f, MechanicLightLook.Crackle, 0.2f, 1.1f));
        }

        static void OrbitalCannon(SkillContext ctx)
        {
            SkillEffect e = First(ctx, EffectKind.Rain);
            if (e == null) return;
            float u = ctx.Units, dim = Dim(ctx), scatter = e.Radius * u, blast = e.ImpactRadius * u, zone = scatter + blast;
            int n = SkillSchedule.Repeats(e);
            float last = e.AtMs + (n - 1) * Mathf.Max(0, e.IntervalMs), end = last + 900f;
            Vector2 mid = SkillAim.RainCentre(ctx, e); // SkillEffects.Rain's scatter centre
            ClericLightLook.Pal tech = MechanicLightLook.Tech, honey = MechanicLightLook.Honey;
            // caster: uplink flash + a thin cyan streak to the sky + a small seal
            MechanicLightBursts.Host("OrbitalUplink", ctx.Feet, 520f, dim)
                .Ground(MechanicLightLook.Line(MechanicLightLook.Ring, tech), 0f, 500f, MechanicLightLook.RingQuad(10f), MechanicLightLook.RingQuad(13f), 0.8f, ClericLightFx.Squash, 60f, 6).Reveal(120f).Env(0f, 200f)
                .Upright(MechanicLightLook.Flash(tech), ClericLightFx.Band.Over, false, 0f, 200f, 5f, 12f, 5f, 12f, 0.8f, new Vector2(ctx.Dir.x * 6f, 14f), 1)
                .Upright(MechanicLightLook.Glyph(MechanicLightLook.Streak, tech), ClericLightFx.Band.Over, true, 40f, 380f, 3f, 2f, 20f, 46f, 0.75f, new Vector2(ctx.Dir.x * 6f, 14f), 2);
            var f = MechanicLightBursts.Host("Orbital", mid, end, dim);
            f.Ground(MechanicLightLook.Line(MechanicLightLook.Ring, tech), 0f, end - 300f, MechanicLightLook.RingQuad(zone) * 0.9f, MechanicLightLook.RingQuad(zone), 0.7f, ClericLightFx.Squash, 12f, 4).Reveal(220f).Env(0f, 320f)
             .Ground(MechanicLightLook.Pool(honey), 0f, end - 300f, zone * 1.2f, zone * 1.7f, 0.6f).Kind(LayerKind.Glow).Env(150f, 320f);
            for (int i = 0; i < n; i++)
            {
                float dx, dy, at = e.AtMs + i * Mathf.Max(0, e.IntervalMs);
                SkillScatter.InDisc(ctx.Seed, i, scatter, out dx, out dy);
                Vector2 spot = mid + new Vector2(dx, dy);
                bool final = i == n - 1;
                if (at >= 100f) MechanicLightBursts.Reticle(spot, blast * 1.4f, at, at + 60f, dim, honey, 9);
                f.At(at, h =>
                {
                    h.Upright(ClericLightLook.Column(honey), ClericLightFx.Band.Over, true, h.AgeMs, h.AgeMs + 240f, 14f, 24f, 96f, 80f, 0.95f, spot - h.Anchor, 3).Floor();
                    MechanicLightBursts.Explosion(spot, blast * (final ? 1.25f : 1f), dim, final);
                    MechanicLightBursts.SplitPulses(ctx.Def.Id, spot + new Vector2(0f, 6f), blast, dim, honey);
                    if (Local(ctx)) { CameraFollow.SkillShake(final ? 0.55f : 0.18f); if (final) { GameClock.HitStop(60f); CameraFollow.SkillPunch(0.5f); } }
                });
            }
        }
    }
}
