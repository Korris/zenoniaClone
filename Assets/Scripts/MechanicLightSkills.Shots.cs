using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Mechanic projectile skills in light FX (see MechanicLightSkills). Each shot uses SkillBolt's own geometry: starts 8 units out of the caster
    /// centre (feet + 6) along its direction, flies Range tiles at Speed tiles / s, hit radius = max(0.5, Width) tiles / 2, so the pops land with
    /// the damage numbers.
    ///  - Power Shot (pierce): honey flare + cyan ring at the feet, honey bolt 0.65 H with a cyan trail (rule 9), a honey pop on every monster pierced;
    ///  - Plasma Barrage (6 bolts, 60 deg fan): three cyan flares, cyan plasma orbs with short trails, cyan pops;
    ///  - Rocket Punch (pierce, knockback): big honey burst at the hand, a heavy honey shot (0.8 H) with exhaust puffs, punchy honey impacts.
    /// </summary>
    public static partial class MechanicLightSkills
    {
        struct Flight { public Vector2 Start, Dir; public float Dist, Ms, Radius; }

        static Flight FlightOf(SkillContext ctx, SkillEffect e, Vector2 dir)
        {
            float u = ctx.Units;
            return new Flight { Dir = dir, Start = ctx.Feet + new Vector2(0f, 6f) + dir * 8f, Dist = e.Range * u, Ms = e.Range / Mathf.Max(0.1f, e.Speed) * 1000f, Radius = Mathf.Max(0.5f, e.Width) * u * 0.5f };
        }

        /// <summary>A shot that ran out of range without a hit: a small flash and two sparks.</summary>
        static void Fizzle(Vector2 at, float dim, ClericLightLook.Pal p)
        {
            MechanicLightBursts.Host("Fizzle", at, 180f, dim)
                .Upright(MechanicLightLook.Flash(p), ClericLightFx.Band.Over, false, 0f, 150f, 6f, 3f, 6f, 3f, 0.6f, Vector2.zero, 1)
                .At(0f, h => MechanicLightBursts.Sparks(h, at, 2, 30f, 90f, 360f, p == MechanicLightLook.Tech ? MechanicLightLook.Crackle : MechanicLightLook.Ember, 0.15f, 1f));
        }

        static void PowerShot(SkillContext ctx)
        {
            SkillEffect e = First(ctx, EffectKind.Projectile);
            if (e == null) return;
            float dim = Dim(ctx);
            Flight f = FlightOf(ctx, e, ctx.Dir);
            ClericLightLook.Pal honey = MechanicLightLook.Honey, tech = MechanicLightLook.Tech;
            MechanicLightBursts.Muzzle(f.Start, Angle(f.Dir), 30f, dim, honey, 9);
            MechanicLightBursts.Host("PowerShotCast", ctx.Feet, 420f, dim)
                .Ground(MechanicLightLook.Line(MechanicLightLook.Ring, tech), 0f, 380f, MechanicLightLook.RingQuad(10f), MechanicLightLook.RingQuad(17f), 0.9f, ClericLightFx.Squash, 60f, 6).Env(30f, 200f)
                .Ground(MechanicLightLook.Line(MechanicLightLook.Band, honey), 0f, 260f, 6f, MechanicLightLook.BandQuad(20f), 0.85f, ClericLightFx.Squash, 0f, 5, f.Dir * 6f);
            if (Local(ctx)) CameraFollow.SkillShake(0.2f);
            var host = MechanicLightBursts.Host("PowerShot", f.Start, f.Ms + 80f, dim);
            MechanicLightBolt.Fire(host, t => f.Start + f.Dir * f.Dist * t, f.Ms, MechanicLightLook.Glyph(MechanicLightLook.Core, honey), new Vector2(17f, 6.5f), tech, 4.6f, 0.17f)
                .Grow(0.5f, 10f).Halo(MechanicLightLook.Flash(honey), 14f)
                .Hits(f.Radius, e.Pierce, (at, m) => Pulses(ctx.Def.Id, at, 24f, dim, honey, MechanicLightLook.Crackle))
                .OnEnd((at, hit) => { if (!hit) Fizzle(at, dim, honey); });
        }

        static void PlasmaBarrage(SkillContext ctx)
        {
            SkillEffect e = First(ctx, EffectKind.Projectile);
            if (e == null) return;
            float dim = Dim(ctx);
            ClericLightLook.Pal tech = MechanicLightLook.Tech;
            int n = Mathf.Max(1, e.Count);
            Flight mid = FlightOf(ctx, e, ctx.Dir);
            MechanicLightBursts.Muzzle(mid.Start, Angle(ctx.Dir), 24f, dim, tech, 8);
            MechanicLightBursts.Muzzle(mid.Start, Angle(ctx.Dir) + 24f, 17f, dim, tech, 3);
            MechanicLightBursts.Muzzle(mid.Start, Angle(ctx.Dir) - 24f, 17f, dim, tech, 3);
            MechanicLightBursts.Host("BarrageCast", ctx.Feet, 360f, dim)
                .Ground(MechanicLightLook.Line(MechanicLightLook.Ring, tech), 0f, 340f, MechanicLightLook.RingQuad(12f), MechanicLightLook.RingQuad(18f), 0.9f, ClericLightFx.Squash, -80f, 6).Reveal(90f).Env(0f, 180f)
                .Ground(MechanicLightLook.Pool(tech), 0f, 300f, 12f, 26f, 0.8f).Kind(LayerKind.Glow);
            if (Local(ctx)) CameraFollow.SkillShake(0.16f);
            for (int i = 0; i < n; i++)
            {
                Flight f = FlightOf(ctx, e, Rotate(ctx.Dir, SkillScatter.FanAngle(i, n, e.SpreadDeg)));
                var host = MechanicLightBursts.Host("Plasma", f.Start, f.Ms + 80f, dim);
                MechanicLightBolt.Fire(host, t => f.Start + f.Dir * f.Dist * t, f.Ms, MechanicLightLook.Glyph(MechanicLightLook.Core, tech), new Vector2(11f, 8f), tech, 3.4f, 0.12f)
                    .Grow(0.55f, 8f).Halo(MechanicLightLook.Flash(tech), 12f)
                    .Hits(f.Radius, e.Pierce, (at, m) => Pulses(ctx.Def.Id, at, 17f, dim, tech, MechanicLightLook.Crackle))
                    .OnEnd((at, hit) => { if (!hit) Fizzle(at, dim, tech); });
            }
        }

        static void RocketPunch(SkillContext ctx)
        {
            SkillEffect e = First(ctx, EffectKind.Projectile);
            if (e == null) return;
            float dim = Dim(ctx);
            Flight f = FlightOf(ctx, e, ctx.Dir);
            ClericLightLook.Pal honey = MechanicLightLook.Honey;
            bool first = true;
            MechanicLightBursts.Muzzle(f.Start, Angle(f.Dir), 34f, dim, honey, 12);
            Vector2 back = -f.Dir * 7f + new Vector2(0f, 2f);
            MechanicLightBursts.Host("PunchCast", ctx.Feet, 520f, dim)
                .Ground(MechanicLightLook.Line(MechanicLightLook.Band, honey), 0f, 280f, 8f, MechanicLightLook.BandQuad(22f), 0.9f, ClericLightFx.Squash, 0f, 6)
                .Upright(MechanicLightLook.SmokeMat, ClericLightFx.Band.Over, false, 0f, 460f, 5f, 12f, 5f, 12f, 0.6f, back + new Vector2(-3f, 0f), -3).Rise(5f).Dissolve()
                .Upright(MechanicLightLook.SmokeMat, ClericLightFx.Band.Over, false, 30f, 500f, 4f, 10f, 4f, 10f, 0.55f, back + new Vector2(4f, 1f), -3).Rise(5f).Dissolve();
            if (Local(ctx)) CameraFollow.SkillShake(0.24f);
            var host = MechanicLightBursts.Host("Punch", f.Start, f.Ms + 120f, dim);
            MechanicLightBolt.Fire(host, t => f.Start + f.Dir * f.Dist * t, f.Ms, MechanicLightLook.Glyph(MechanicLightLook.Core, honey), new Vector2(24f, 13f), MechanicLightLook.Fire, 7f, 0.2f)
                .Grow(0.55f, 12f).Halo(MechanicLightLook.Flash(MechanicLightLook.Fire), 24f).Exhaust(5f, 5f)
                .Hits(f.Radius, e.Pierce, (at, m) =>
                {
                    Pulses(ctx.Def.Id, at, 28f, dim, honey, MechanicLightLook.Ember);
                    MechanicLightBursts.Explosion(at + new Vector2(0f, -6f), 13f, dim, false);
                    MechanicLightBursts.Host("PunchHit", at, 300f, dim)
                        .Ground(MechanicLightLook.Line(MechanicLightLook.Band, honey), 0f, 260f, 6f, MechanicLightLook.BandQuad(13f), 0.8f, ClericLightFx.Squash, 0f, 7, new Vector2(0f, -6f))
                        .Upright(MechanicLightLook.SmokeMat, ClericLightFx.Band.Over, false, 20f, 420f, 4f, 10f, 4f, 10f, 0.5f, -f.Dir * 4f, -2).Rise(4f).Dissolve();
                    if (first && Local(ctx)) { first = false; CameraFollow.SkillShake(0.3f); GameClock.HitStop(45f); }
                })
                .OnEnd((at, hit) => { if (!hit) Fizzle(at, dim, honey); });
        }
    }
}
