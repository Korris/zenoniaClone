using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Summoned helper (Shadow Clone, Robot Assault drone). Lives DurationMs, shadows its caster, and every IntervalMs strikes: the clone walks to the nearest
    /// monster and hits the one in reach (Single), the mechanic drone hovers beside the caster and pulses a circle of Radius tiles. At most 2 per caster.
    /// </summary>
    public sealed class SkillMinion : MonoBehaviour
    {
        const float FollowSpeed = 110f, ChaseSpeed = 90f, ChaseTiles = 5f, Reach = 14f;
        SkillContext ctx;
        SkillEffect fx;
        Vector2 pos;
        float lifeMs, strikeInMs;
        bool pulse;
        SkillFxAnim body;
        SpriteRenderer dot;

        public static void Spawn(SkillContext ctx, SkillEffect e)
        {
            var go = new GameObject("SkillMinion");
            go.transform.SetParent(ctx.Body.Hero.transform.parent, false);
            SkillMinion m = go.AddComponent<SkillMinion>();
            m.ctx = ctx; m.fx = e; m.pos = ctx.Feet + new Vector2(-ctx.Dir.x, -ctx.Dir.y) * 12f;
            m.lifeMs = e.DurationMs; m.strikeInMs = 250f; m.pulse = ctx.Def.Class == "mechanic";
            SkillCaps.Add(ctx.Body.Hero, SkillCaps.MaxMinions, m);
            if (!ctx.View) return;
            SkillVfx.Play(ctx, SkillVfxSet.Cast, m.pos, 0f, 2.6f * ctx.Units);
            SkillSfx.Play(ctx, SkillSfx.Cast);
            m.body = SkillVfx.Loop(ctx, m.pos, 0f, 1.6f * ctx.Units, e.DurationMs / 1000f);
            if (m.body != null) return;
            m.dot = ProceduralSprites.Child(m.transform, "Body", FxSprites.Dot());
            m.dot.color = m.pulse ? new Color(1f, 0.7f, 0.25f, 0.95f) : new Color(0.45f, 0.2f, 0.8f, 0.8f);
            m.dot.sortingOrder = FeelConfig.FxOrder - 6;
            m.dot.transform.localScale = Vector3.one * 12f; // sprite is 1 unit wide
        }

        void Update()
        {
            float dt = GameClock.Dt, dtMs = dt * 1000f;
            if (dt <= 0f) return;
            lifeMs -= dtMs;
            if (lifeMs <= 0f || ctx.Body.IsDead) { End(); return; }
            Move(dt);
            strikeInMs -= dtMs;
            if (strikeInMs <= 0f) { strikeInMs += Mathf.Max(100, fx.IntervalMs); Strike(); }
            if (body != null) body.MoveTo(pos, 0f);
            if (dot != null) dot.transform.position = pos + new Vector2(0f, 6f + Mathf.Sin(Time.time * 5f) * 1.5f);
        }

        /// <summary>Clone: chase the nearest monster within 5 tiles, else stay near the caster. Drone: hover beside the caster.</summary>
        void Move(float dt)
        {
            Vector2 home = ctx.Body.Feet + new Vector2(-ctx.Dir.x * 14f + ctx.Dir.y * 10f, -ctx.Dir.y * 14f - ctx.Dir.x * 10f);
            Monster foe = pulse ? null : SkillAim.NearestMonster(pos, ChaseTiles * ctx.Units);
            Vector2 goal = foe != null ? foe.Feet : home;
            float speed = foe != null ? ChaseSpeed : FollowSpeed, stop = foe != null ? Reach : 2f;
            Vector2 to = goal - pos;
            if (to.magnitude > stop) pos += to.normalized * Mathf.Min(speed * dt, to.magnitude - stop);
        }

        void Strike()
        {
            SkillEffect e = SkillEffects.Derive(fx, pulse ? AreaShape.Circle : AreaShape.Single, fx.Radius);
            bool hit = false;
            if (ctx.Authority) hit = SkillDamage.Area(ctx.Dmg, e, pos, ctx.Dir).Count > 0;
            if (!ctx.View) return;
            float w = pulse ? 2f * fx.Radius * ctx.Units : 2f * ctx.Units;
            if (pulse || hit) SkillVfx.Play(ctx, SkillVfxSet.Impact, pos, 0f, w);
            if (pulse || hit) SkillSfx.Play(ctx, SkillSfx.Impact, 0.5f);
        }

        void End()
        {
            if (body != null) body.Stop();
            if (ctx.View) SkillVfx.Play(ctx, SkillVfxSet.Cast, pos, 0f, 1.8f * ctx.Units);
            Destroy(gameObject);
        }
    }
}
