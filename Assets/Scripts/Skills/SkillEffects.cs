using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Fires one step of a skill timeline. Damage / status / heal / motion run only on the authority, art + sound only where RendersFx, so the same
    /// code serves offline, host, server and the visual replay on clients. Strike / Rain / Motion live here, the rest in SkillSupport / SkillBolt /
    /// SkillMinion / SkillGroundZone.
    /// </summary>
    public static class SkillEffects
    {
        static readonly Dictionary<KeyValuePair<SkillEffect, int>, SkillEffect> Derived = new Dictionary<KeyValuePair<SkillEffect, int>, SkillEffect>();

        public static void Fire(SkillContext ctx, SkillEffect e, int hit)
        {
            switch (e.Kind)
            {
                case EffectKind.Strike: Strike(ctx, e); break;
                case EffectKind.Rain: Rain(ctx, e, hit); break;
                case EffectKind.Motion: Motion(ctx, e); break;
                case EffectKind.Projectile: SkillBolt.Launch(ctx, e); break;
                case EffectKind.Buff: SkillSupport.Buff(ctx, e); break;
                case EffectKind.Heal: SkillSupport.Heal(ctx, e); break;
                case EffectKind.Summon: SkillMinion.Spawn(ctx, e); break;
                case EffectKind.Zone: SkillGroundZone.Spawn(ctx, e); break;
            }
        }

        /// <summary>A derived strike (projectile blast, minion pulse) cached per source effect so the hot path allocates nothing.</summary>
        public static SkillEffect Derive(SkillEffect src, AreaShape shape, float radius)
        {
            var key = new KeyValuePair<SkillEffect, int>(src, (int)shape);
            SkillEffect d;
            if (Derived.TryGetValue(key, out d)) return d;
            d = new SkillEffect
            {
                Kind = EffectKind.Strike, Shape = shape, Radius = radius, Pct = src.Pct, Status = src.Status,
                BonusVsMask = src.BonusVsMask, BonusPct = src.BonusPct, BehindBonusPct = src.BehindBonusPct, Count = src.Count,
            };
            Derived[key] = d;
            return d;
        }

        static Vector2 Center(SkillContext ctx, SkillEffect e)
        {
            Vector2 from = e.Shape == AreaShape.Line && ctx.LineFrom.HasValue ? ctx.LineFrom.Value : ctx.Feet; // a line right after a dash sweeps the path
            if (e.Shape == AreaShape.Single && ctx.Target != null && !ctx.Target.IsDying) return ctx.Target.Center;
            return e.Range > 0f ? SkillAim.ClampToWalls(from, from + ctx.Dir * e.Range * ctx.Units) : from;
        }

        static void Strike(SkillContext ctx, SkillEffect e)
        {
            Vector2 center = Center(ctx, e);
            int hits = 0;
            if (ctx.Authority) hits = SkillDamage.Area(ctx.Dmg, e, center, ctx.Dir).Count;
            if (!ctx.View) return;
            StrikeArt(ctx, e, center);
            SkillSfx.Play(ctx, SkillSfx.Impact);
            if (hits > 0 && ctx.Body.IsLocal && e.Pct >= 300f) { CameraFollow.AddTrauma(0.22f); GameClock.HitStop(FeelConfig.HitStopFinisherMs); }
        }

        /// <summary>Impact art laid over the shape: centred on a circle, at the middle of an arc / cone, repeated along a line.</summary>
        static void StrikeArt(SkillContext ctx, SkillEffect e, Vector2 center)
        {
            float u = ctx.Units, angle = Mathf.Atan2(ctx.Dir.y, ctx.Dir.x) * Mathf.Rad2Deg;
            switch (e.Shape)
            {
                case AreaShape.Circle: SkillVfx.Play(ctx, SkillVfxSet.Impact, center, 0f, 2f * e.Radius * u); break;
                case AreaShape.Single: SkillVfx.Play(ctx, SkillVfxSet.Impact, center, 0f, 2.2f * u); break;
                case AreaShape.Arc:
                case AreaShape.Cone: SkillVfx.Play(ctx, SkillVfxSet.Impact, center + ctx.Dir * e.Radius * u * 0.5f, angle, e.Radius * u * 1.4f); break;
                case AreaShape.Line:
                    int n = Mathf.Clamp(Mathf.CeilToInt(e.Length / Mathf.Max(1f, e.Width * 1.5f)), 1, 5);
                    for (int i = 0; i < n; i++)
                        SkillVfx.Play(ctx, SkillVfxSet.Impact, center + ctx.Dir * e.Length * u * (i + 0.5f) / n, angle, Mathf.Max(e.Width * u * 1.4f, e.Length * u / n));
                    break;
            }
        }

        /// <summary>One rain drop: lands inside the scatter disc (same point for server and clients) and explodes.</summary>
        static void Rain(SkillContext ctx, SkillEffect e, int drop)
        {
            float u = ctx.Units, dx, dy;
            Vector2 mid = SkillAim.ClampToWalls(ctx.CastFrom, ctx.CastFrom + ctx.Dir * e.Range * u);
            SkillScatter.InDisc(ctx.Seed, drop, e.Radius * u, out dx, out dy);
            Vector2 at = mid + new Vector2(dx, dy);
            SkillEffect blast = Derive(e, AreaShape.Circle, e.ImpactRadius);
            if (ctx.Authority) SkillDamage.Area(ctx.Dmg, blast, at, ctx.Dir);
            if (!ctx.View) return;
            SkillVfx.Play(ctx, SkillVfxSet.Impact, at, 0f, 2f * e.ImpactRadius * u);
            SkillSfx.Play(ctx, SkillSfx.Impact, 0.6f);
            if (ctx.Body.IsLocal) CameraFollow.AddTrauma(0.06f);
        }

        /// <summary>Dash / Leap push the caster along the aim; Blink jumps (behind the locked target when there is one). Authority only.</summary>
        static void Motion(SkillContext ctx, SkillEffect e)
        {
            float u = ctx.Units;
            Vector2 from = ctx.Feet;
            if (e.Motion == MotionKind.Blink)
            {
                Vector2 dest;
                if (!BlinkDestination(ctx, e, out dest)) return;
                if (ctx.Authority) { if (!ctx.Body.TryBlink(dest)) return; ctx.Body.AddInvuln(300f); }
                if (ctx.Target != null) ctx.Dir = (ctx.Target.Center - dest).normalized;
                if (ctx.Authority) ctx.Body.Face(ctx.Dir);
                if (ctx.View) HeroFx.ShadowTrail(from, dest);
                return;
            }
            ctx.LineFrom = from;
            if (ctx.Authority) ctx.Body.BeginForcedMove(ctx.Dir, e.MoveTiles * u, e.DurationMs);
            if (ctx.View) ShockRing.Spawn(from, 10f, 0.25f, SkillVfx.Tint(ctx), FeelConfig.FxOrder - 8, 0.7f, 0.3f, true);
            if (ctx.View) SkillSfx.Play(ctx, SkillSfx.Cast, 0.7f);
        }

        /// <summary>Behind the target (12 units past it), else in front of it, else nowhere; with no target: along the aim, stopped by walls.</summary>
        static bool BlinkDestination(SkillContext ctx, SkillEffect e, out Vector2 dest)
        {
            Vector2 from = ctx.Feet;
            if (ctx.Target == null || ctx.Target.IsDying) { dest = SkillAim.ClampToWalls(from, from + ctx.Dir * e.MoveTiles * ctx.Units); return true; }
            Vector2 feet = ctx.Target.Feet, dir = feet - from;
            dir = dir.sqrMagnitude > 0.01f ? dir.normalized : ctx.Dir;
            dest = feet + dir * 12f;
            if (Blocked(dest)) dest = feet - dir * 12f;
            return !Blocked(dest);
        }

        static bool Blocked(Vector2 p) { return MapRegistry.Current.RectBlocked(p.x - HeroSkillBody.BoxW * 0.5f, p.y, HeroSkillBody.BoxW, HeroSkillBody.BoxH); }
    }
}
