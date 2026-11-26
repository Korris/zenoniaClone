using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Picture of one flying SkillBolt drawn by a class's light FX instead of the painted loop (RO3 Soft Fantasy V1, 2026-11-21). The bolt keeps its
    /// flight and hits (sim, same on every side); the art only follows it, so a projectile picture can never arrive before or after its damage.
    /// SkillBolt calls Begin once (view side), then Tick every frame, Hit for each monster a piercing bolt passes, End when the flight ends.
    /// </summary>
    public interface ISkillBoltArt
    {
        void Tick(Vector2 pos, Vector2 dir);
        /// <summary>A monster struck at `at` (piercing bolts: every one; plain bolts: the one that stopped it, then End follows).</summary>
        void Hit(Vector2 at);
        /// <summary>Flight over at `pos`: `hit` = stopped by a monster, `blast` = an ImpactRadius projectile exploding here.</summary>
        void End(Vector2 pos, bool hit, bool blast);
    }

    /// <summary>Which class light FX draws a skill bolt (null = the painted / tinted-dot path of SkillBolt). One line per class.</summary>
    public static class SkillBoltArt
    {
        public static ISkillBoltArt Begin(SkillContext ctx, SkillEffect e, Vector2 pos, Vector2 dir, Transform parent)
        {
            if (ctx == null || !ctx.View) return null;
            if (WizardLightSkills.Handles(ctx.Def.Id)) return WizardBoltLightFx.Begin(ctx, e, pos, dir, parent);
            if (ctx.Def.Class == "archer") return new ArcherSkillArrowArt(ctx, e, pos, dir, parent);
            return null;
        }
    }
}
