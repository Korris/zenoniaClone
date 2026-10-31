using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// One skill projectile (bolt, lance, rocket, plasma shard, punch). Flies `Range` tiles, sweeps monster boxes in steps of at most 4 units, pierces or
    /// stops, optionally homes in and explodes in a circle (ImpactRadius). Damage runs on the authority; a client replay draws the same flight and
    /// stops at the first contact with the monster proxies (damage numbers arrive as MonsterHit events).
    /// </summary>
    public sealed class SkillBolt : MonoBehaviour
    {
        const float SubStep = 4f, TurnDegPerSec = 260f, AcquireTiles = 6f;
        static readonly List<Monster> Pool = new List<Monster>(16);
        SkillContext ctx;
        SkillEffect fx;
        Vector2 pos, dir;
        float left, speed, radius;
        Monster seek;
        readonly List<Monster> pierced = new List<Monster>(8);
        SkillFxAnim body;
        SpriteRenderer dot;

        public static void Launch(SkillContext ctx, SkillEffect e)
        {
            Vector2 centre = ctx.Feet + new Vector2(0f, 6f);
            int n = Mathf.Max(1, e.Count);
            List<Monster> seekers = e.Homing ? Acquire(ctx.Feet) : null;
            for (int i = 0; i < n; i++)
            {
                float a = SkillScatter.FanAngle(i, n, e.SpreadDeg) * Mathf.Deg2Rad;
                Vector2 d = Rotate(ctx.Dir, a);
                var go = new GameObject("SkillBolt");
                go.transform.SetParent(ctx.Body.Hero.transform.parent, false);
                SkillBolt b = go.AddComponent<SkillBolt>();
                b.ctx = ctx; b.fx = e; b.dir = d; b.pos = centre + d * 8f;
                b.left = e.Range * ctx.Units; b.speed = e.Speed * ctx.Units; b.radius = Mathf.Max(0.5f, e.Width) * ctx.Units * 0.5f;
                if (seekers != null && seekers.Count > 0) b.seek = seekers[i % seekers.Count];
                b.StartArt();
            }
            if (ctx.View) { SkillVfx.Play(ctx, SkillVfxSet.Cast, centre + ctx.Dir * 8f, Mathf.Atan2(ctx.Dir.y, ctx.Dir.x) * Mathf.Rad2Deg, 1.6f * ctx.Units); SkillSfx.Play(ctx, SkillSfx.Cast); }
        }

        static Vector2 Rotate(Vector2 v, float rad) { float c = Mathf.Cos(rad), s = Mathf.Sin(rad); return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c); }

        /// <summary>Live monsters near the caster, nearest first: the homing targets (rocket i takes the i-th nearest).</summary>
        static List<Monster> Acquire(Vector2 from)
        {
            Pool.Clear();
            float r = AcquireTiles * GameConfig.TileUnits;
            foreach (Monster m in Monster.All) if (!m.IsDying && (m.Center - from).sqrMagnitude <= r * r) Pool.Add(m);
            Pool.Sort((a, c) => (a.Center - from).sqrMagnitude.CompareTo((c.Center - from).sqrMagnitude));
            return Pool;
        }

        void StartArt()
        {
            if (!ctx.View || SkillChoreo.DrawsBody(ctx.Def.Class, ctx.Def.Id)) return; // the choreography draws the flight (no tinted dot fallback)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, width = Mathf.Max(1.2f * ctx.Units, radius * 2.4f);
            body = SkillVfx.Loop(ctx, pos, angle, width, left / Mathf.Max(1f, speed) + 0.2f);
            if (body != null) return;
            dot = ProceduralSprites.Child(transform, "Dot", FxSprites.Dot()); // no painted loop yet: tinted glow orb
            dot.color = SkillVfx.Tint(ctx);
            dot.sortingOrder = FeelConfig.FxOrder;
            dot.transform.localScale = Vector3.one * Mathf.Max(10f, radius * 2.4f); // sprite is 1 unit wide
            dot.transform.position = pos;
        }

        void Update()
        {
            float dt = GameClock.Dt;
            if (dt <= 0f) return;
            if (seek != null && !seek.IsDying)
            {
                Vector2 want = (seek.Center - pos).normalized;
                float maxRad = TurnDegPerSec * Mathf.Deg2Rad * dt, ang = Vector2.SignedAngle(dir, want) * Mathf.Deg2Rad;
                dir = Rotate(dir, Mathf.Clamp(ang, -maxRad, maxRad));
            }
            float dist = Mathf.Min(speed * dt, left);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist / SubStep));
            for (int i = 0; i < steps; i++)
            {
                pos += dir * (dist / steps);
                if (TryHit()) { End(true); return; }
            }
            left -= dist;
            if (left <= 0.01f) { End(false); return; }
            if (body != null) body.MoveTo(pos, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            if (dot != null) dot.transform.position = pos;
        }

        /// <summary>Checks the monsters under the bolt; true when the bolt ends here (non-piercing hit, or a blast).</summary>
        bool TryHit()
        {
            IReadOnlyList<Monster> all = Monster.All;
            for (int i = 0; i < all.Count; i++)
            {
                Monster m = all[i];
                if (m.IsDying || pierced.Contains(m)) continue;
                Rect b = m.Box;
                if (!SkillGeometry.InCircle(pos.x, pos.y, radius, b.xMin, b.yMin, b.xMax, b.yMax)) continue;
                if (fx.ImpactRadius > 0f) return true; // the blast does the damage
                if (ctx.Authority) SkillDamage.Single(ctx.Dmg, fx, m, pos - dir * 8f, dir);
                if (ctx.View) SkillVfx.Play(ctx, SkillVfxSet.Impact, new Vector2(m.Center.x, pos.y), 0f, 1.8f * ctx.Units);
                if (!fx.Pierce) return true;
                pierced.Add(m);
            }
            return false;
        }

        /// <summary>Ends the flight: a blast projectile explodes wherever it ends, others fade out softly when they ran out of range without a hit.</summary>
        void End(bool hit)
        {
            if (fx.ImpactRadius > 0f)
            {
                SkillEffect blast = SkillEffects.Derive(fx, AreaShape.Circle, fx.ImpactRadius);
                if (ctx.Authority) SkillDamage.Area(ctx.Dmg, blast, pos, dir);
                if (ctx.View)
                {
                    SkillVfx.Play(ctx, SkillVfxSet.Impact, pos, 0f, 2f * fx.ImpactRadius * ctx.Units);
                    SkillSfx.Play(ctx, SkillSfx.Impact, 0.7f);
                    if (ctx.Body.IsLocal && !SkillChoreo.DrawsBody(ctx.Def.Class, ctx.Def.Id)) CameraFollow.AddTrauma(0.08f);
                }
            }
            else if (ctx.View && !hit && !fx.Pierce) SkillVfx.Play(ctx, SkillVfxSet.Impact, pos, 0f, 1.4f * ctx.Units);
            if (body != null) body.Stop();
            Destroy(gameObject);
        }
    }
}
