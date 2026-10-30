using System;
using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Adapter from a resolved SkillContext to SkillChoreo.Play: works out the caster centre, the aim point (area centre / projectile reach) and,
    /// for projectiles, the monsters on the flight line, so the integration point is one call at cast start (see report). View side only.
    /// </summary>
    public static class SkillChoreoCastHook
    {
        public static SkillChoreoPlayer Play(SkillContext ctx)
        {
            if (ctx == null || !ctx.View || !SkillChoreo.Has(ctx.Def.Class, ctx.Def.Id)) return null;
            SkillEffect e = ctx.Def.Effects.Length > 0 ? ctx.Def.Effects[0] : null;
            Vector2 centre = ctx.Feet + new Vector2(0f, 6f), aim = centre;
            List<Vector2> hits = null;
            float u = ctx.Units;
            if (e != null && e.Kind == EffectKind.Projectile)
            {
                float reach = e.Range * u;
                aim = centre + ctx.Dir * reach;
                hits = SkillChoreo.PierceHits(centre, ctx.Dir, reach, Mathf.Max(0.5f, e.Width) * u * 0.5f + 5f);
                if (!e.Pierce && hits.Count > 1) hits.RemoveRange(1, hits.Count - 1); // a plain bolt stops at the first monster
            }
            else if (e != null && (e.Kind == EffectKind.Strike || e.Kind == EffectKind.Rain) && e.Range > 0f)
                aim = SkillAim.ClampToWalls(ctx.CastFrom, ctx.CastFrom + ctx.Dir * e.Range * u) + new Vector2(0f, 5f); // sprite centre one drop above the ground point
            else if (e != null && e.Kind == EffectKind.Strike) aim = ctx.CastFrom + new Vector2(0f, 5f);
            Action<string> sfx = cue => { if (cue == "break") SkillSfx.Play(ctx, SkillSfx.Impact); }; // cast / impact / loop sounds stay with the existing SkillSfx calls
            return SkillChoreo.Play(ctx.Def.Class, ctx.Def.Id, centre, aim, ctx.Body.Hero.transform, null, hits, sfx);
        }
    }
}
