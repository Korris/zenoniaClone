using System;
using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Adapter from a resolved SkillContext to SkillChoreo.Play: works out the caster centre, the aim point (area centre, projectile reach, landing spot, locked target) and,
    /// for projectiles, the monsters on the flight line, so the integration point is one call at cast start (see report). View side only.
    /// </summary>
    public static class SkillChoreoCastHook
    {
        const float GroundLift = 5f; // sprite centre one drop above the ground point

        public static SkillChoreoPlayer Play(SkillContext ctx)
        {
            if (ctx == null || !ctx.View || !SkillChoreo.Has(ctx.Def.Class, ctx.Def.Id)) return null;
            SkillEffect e = FirstNot(ctx.Def, EffectKind.Motion), move = ctx.Def.Effects.Length > 0 && ctx.Def.Effects[0].Kind == EffectKind.Motion ? ctx.Def.Effects[0] : null;
            Vector2 centre = ctx.Feet + new Vector2(0f, 6f), aim = Aim(ctx, e, move, centre);
            List<Vector2> hits = null;
            if (e != null && e.Kind == EffectKind.Projectile)
            {
                float reach = e.Range * ctx.Units;
                aim = centre + ctx.Dir * reach;
                hits = SkillChoreo.PierceHits(centre, ctx.Dir, reach, Mathf.Max(0.5f, e.Width) * ctx.Units * 0.5f + 5f);
                if (!e.Pierce && hits.Count > 1) hits.RemoveRange(1, hits.Count - 1); // a plain bolt stops at the first monster
            }
            Action<string> sfx = cue => { if (cue == "break") SkillSfx.Play(ctx, SkillSfx.Impact); }; // cast / impact / loop sounds stay with the existing SkillSfx calls
            SkillChoreoPlayer player = SkillChoreo.Play(ctx.Def.Class, ctx.Def.Id, centre, aim, ctx.Body.Hero.transform, null, hits, sfx, ctx.Dir, ctx.Body.IsLocal);
            if (player == null || e == null) return player;
            bool shield = ctx.Authority && e.Kind == EffectKind.Buff && e.Buff.Kind == BuffKind.Shield;
            if (shield || e.Kind == EffectKind.Buff || (e.Kind == EffectKind.Zone && e.Attached)) SkillChoreoShieldWatch.Attach(player, ctx.Body.Hero, shield);
            return player;
        }

        static SkillEffect FirstNot(SkillDef def, EffectKind kind)
        {
            foreach (SkillEffect x in def.Effects) if (x.Kind != kind) return x;
            return null;
        }

        /// <summary>Where the Target anchor sits. Mirrors SkillEffects.Center and StrikeArt, which the choreography replaces.</summary>
        static Vector2 Aim(SkillContext ctx, SkillEffect e, SkillEffect move, Vector2 centre)
        {
            float u = ctx.Units;
            Vector2 lift = new Vector2(0f, GroundLift);
            if (ctx.Target != null) return ctx.Target.Center;
            if (ctx.Ally != null) return ctx.Ally.Position + lift;
            if (move != null && move.Motion != MotionKind.Blink) return SkillAim.ClampToWalls(ctx.CastFrom, ctx.CastFrom + ctx.Dir * move.MoveTiles * u) + lift; // the landing spot
            if (e == null) return centre;
            if (e.Kind == EffectKind.Rain) return SkillAim.ClampToWalls(ctx.CastFrom, ctx.CastFrom + ctx.Dir * e.Range * u) + lift;
            if (e.Kind != EffectKind.Strike) return ctx.CastFrom + lift;
            if (e.Range > 0f) return SkillAim.ClampToWalls(ctx.CastFrom, ctx.CastFrom + ctx.Dir * e.Range * u) + lift;
            switch (e.Shape)
            {
                case AreaShape.Arc:
                case AreaShape.Cone: return ctx.CastFrom + ctx.Dir * e.Radius * u * 0.5f + lift;
                case AreaShape.Line: return ctx.CastFrom + ctx.Dir * e.Length * u * 0.5f + lift;
                default: return ctx.CastFrom + lift;
            }
        }
    }
}
