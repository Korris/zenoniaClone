using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Every Archer skill in RO3 Soft Fantasy V1 (2026-11-26, user testing the exe: "skill của archer sao ko thấy hiệu ứng gì vậy": Arrow Rain,
    /// Starfall Volley and the buffs fell back to the generic tinted rings, nearly invisible). Particle System + mesh trail + shader quads on the
    /// Cleric light toolkit (ArcherLightLook palettes), no FX frames; the arrow itself stays the painted arrow sprite (a solid object).
    /// Render only: damage, statuses and buffs stay in the skill data (SkillTable.Archer.cs); every picture is timed on the step it shows.
    ///  - Piercing Arrow / Multi Shot / Binding Shot: the real SkillBolt flight carries the picture (ArcherSkillArrowArt via SkillBoltArt); the bow
    ///    flash at release is drawn here (once per cast, not once per Multi Shot arrow); Binding Shot's vine bind: ArcherBindingLightFx;
    ///  - Eagle Eye / Hunter's Focus: ArcherBuffLightFx (cast burst + a ring round the body while the buff runs);
    ///  - Arrow Rain / Starfall Volley: ArcherRainLightFx (arrows land exactly on the sim's drop spots at each drop's time);
    ///  - Wind Step: ArcherWindStepLightFx (wind burst, dash trail, landing gust, wind ring at the feet while the next-hit bonus waits).
    /// Plays on every rendering side (SkillExecutor runs for remote casters too, NetEventPlayer -> PlaySkillFx), crowd LOD from ClericLightFx.
    /// </summary>
    public static class ArcherLightSkills
    {
        public const string ClassId = "archer";
        public const int PiercingArrow = 0, MultiShot = 1, EagleEye = 2, ArrowRain = 3, BindingShot = 4, WindStep = 5, HuntersFocus = 6, Starfall = 7;

        /// <summary>True when this Archer skill draws with the light FX (renders FX, shaders and masks ready).</summary>
        public static bool Handles(string skillId)
        {
            return skillId != null && skillId.StartsWith(ClassId + ".") && ArcherLightLook.Active;
        }

        /// <summary>Cast start (SkillExecutor, every rendering side). Bolts draw from SkillBolt; the rest start here.</summary>
        public static void Cast(SkillContext ctx)
        {
            if (!ctx.View || ctx.Def.Class != ClassId || !ArcherLightLook.Active || ctx.Body.Hero == null) return;
            switch (ctx.Def.Index)
            {
                case PiercingArrow: BowFlash(ctx, ArcherLightLook.Leaf, 1.25f, 0f, 1); break;
                case MultiShot: { SkillEffect p = Effect(ctx.Def, EffectKind.Projectile); BowFlash(ctx, ArcherLightLook.Leaf, 1f, p != null ? p.SpreadDeg : 50f, p != null ? p.Count : 5); break; }
                case BindingShot: BowFlash(ctx, ArcherLightLook.Vine, 1.1f, 0f, 1); break;
                case EagleEye: case HuntersFocus: ArcherBuffLightFx.Cast(ctx); break;
                case ArrowRain: case Starfall: ArcherRainLightFx.Cast(ctx); break;
                case WindStep: ArcherWindStepLightFx.Cast(ctx); break;
            }
        }

        /// <summary>First effect of `kind` in the skill (null if none).</summary>
        public static SkillEffect Effect(SkillDef def, EffectKind kind)
        {
            foreach (SkillEffect e in def.Effects) if (e.Kind == kind) return e;
            return null;
        }

        /// <summary>Picture offset of the bow (left hand, held toward the aim) from the feet.</summary>
        public static Vector2 Bow(Vector2 dir) { return new Vector2(dir.x * 8f, 11f + dir.y * 3f); }

        /// <summary>
        /// Release at the bow: a gold flash + wind ring the arrow leaves through, and one wind gust per arrow along its line (`count` arrows over
        /// `spreadDeg`), 6 lime sparks forward. `scale` 1.25 for the strong single shots.
        /// </summary>
        static void BowFlash(SkillContext ctx, ClericLightLook.Pal p, float scale, float spreadDeg, int count)
        {
            HeroController h = ctx.Body.Hero;
            Vector2 bow = Bow(ctx.Dir);
            float angle = Mathf.Atan2(ctx.Dir.y, ctx.Dir.x) * Mathf.Rad2Deg, k = scale;
            var fx = ClericLightFx.Create("ArcherBow", h.Position, null, 420f, ArcherLightLook.Dim(h));
            fx.Upright(ArcherLightLook.Flash(ArcherLightLook.Gold), ClericLightFx.Band.Over, false, 0f, 160f, 5f * k, 15f * k, 5f * k, 15f * k, 0.8f, bow)
              .Upright(ArcherLightLook.Line(ArcherLightLook.Band, ArcherLightLook.Gold), ClericLightFx.Band.Over, false, 0f, 260f, 4f * k, 9f * k, 10f * k, 26f * k, 0.85f, bow + ctx.Dir * 4f, 1).Turn(angle);
            int n = Mathf.Clamp(count, 1, 7);
            for (int i = 0; i < n; i++)
            {
                float a = n == 1 ? angle : angle - spreadDeg * 0.5f + spreadDeg * i / (n - 1), rad = a * Mathf.Deg2Rad;
                var d = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                fx.Upright(ArcherLightLook.Line(ArcherLightLook.WindArc, p), ClericLightFx.Band.Over, false, 20f, 300f, 7f * k, 13f * k, 10f * k, 18f * k, 0.8f, bow + d * 6f, 2).Turn(a).Move(d * 16f * k);
            }
            Vector2 at = h.Position + bow;
            Color spark = ArcherLightLook.Spark;
            Vector2 dir = ctx.Dir;
            fx.At(0f, f =>
            {
                int m = f.Count(6);
                for (int i = 0; i < m; i++)
                    f.Emit(true, at, (dir * Random.Range(40f, 70f) + Random.insideUnitCircle * 14f), Random.Range(1.4f, 2.2f), spark, 0.28f);
            });
        }

        /// <summary>Own screen punch of a big hit (local player only).</summary>
        public static void Punch(SkillContext ctx, float shake, float hitStopMs)
        {
            if (!ctx.Body.IsLocal) return;
            if (shake > 0f) CameraFollow.SkillShake(shake);
            if (hitStopMs > 0f) GameClock.HitStop(hitStopMs);
        }

        /// <summary>The live (or just killed) monster nearest `at` within `r`, null if none.</summary>
        public static Monster NearestMonster(Vector2 at, float r)
        {
            Monster best = null;
            float bd = r * r;
            foreach (Monster m in Monster.All)
            {
                if (m == null) continue;
                float d = (m.Center - at).sqrMagnitude;
                if (d < bd) { bd = d; best = m; }
            }
            return best;
        }
    }
}
