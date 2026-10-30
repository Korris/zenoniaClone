using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// One running cast: walks the skill's step list by elapsed time and fires each due effect (SkillSchedule is the pure part). Same object on
    /// offline, server, host and as a visual replay on clients. Ends with the last step or when the caster dies. At most 3 per caster.
    /// </summary>
    public sealed class SkillRun : MonoBehaviour
    {
        SkillContext ctx;
        SkillStep[] steps;
        int cursor;
        float ageMs;

        public static SkillRun Begin(SkillContext ctx)
        {
            var go = new GameObject("Skill_" + ctx.Def.Id);
            go.transform.SetParent(ctx.Body.Hero.transform.parent, false);
            SkillRun run = go.AddComponent<SkillRun>();
            run.ctx = ctx;
            run.steps = SkillSchedule.Build(ctx.Def.Effects);
            SkillCaps.Add(ctx.Body.Hero, SkillCaps.MaxRuns, run);
            if (ctx.View) CastArt(ctx);
            run.Fire(); // step 0 lands on the cast frame, not one frame later
            return run;
        }

        /// <summary>Cast flash at the caster: a ring-sized burst for self / around-self skills, a small muzzle glow otherwise (projectiles add their own).</summary>
        static void CastArt(SkillContext ctx)
        {
            bool self = ctx.Def.Targeting == SkillTargeting.Self || ctx.Def.Targeting == SkillTargeting.AroundSelf;
            SkillEffect first = ctx.Def.Effects.Length > 0 ? ctx.Def.Effects[0] : null;
            if (first != null && first.Kind == EffectKind.Projectile) return; // SkillBolt plays the muzzle art
            float w = self && first != null && first.Radius > 0f ? Mathf.Max(1.6f, first.Radius * 1.6f) * ctx.Units : 1.8f * ctx.Units;
            SkillVfx.Play(ctx, SkillVfxSet.Cast, ctx.Feet, 0f, w);
            SkillSfx.Play(ctx, SkillSfx.Cast);
        }

        void Update()
        {
            ageMs += GameClock.Dt * 1000f;
            if (ctx.Body.IsDead) { Destroy(gameObject); return; } // a fallen caster ends the timeline
            Fire();
        }

        void Fire()
        {
            int due = SkillSchedule.Advance(steps, cursor, Mathf.FloorToInt(ageMs));
            while (cursor < due)
            {
                SkillStep s = steps[cursor++];
                try { SkillEffects.Fire(ctx, ctx.Def.Effects[s.EffectIndex], s.HitIndex); }
                catch (System.Exception ex) { Debug.LogError("[ZenoniaProto] skill " + ctx.Def.Id + " effect " + s.EffectIndex + " failed: " + ex); }
            }
            if (cursor >= steps.Length) Destroy(gameObject);
        }
    }
}
