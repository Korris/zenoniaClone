using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// RO3 Soft Fantasy V1 pictures of the Mechanic skills (2026-11-21, user: "làm lại hết skill của warrior, wizard, mechanic đổi qua kiểu mới"),
    /// rules at the top of docs/sprite-rules.md: Particle System + mesh trail + shader, no sprite frames; honey-gold / brass + cyan tech (MechanicLightLook).
    /// Render only: damage, status, the turret itself stay in the skill data (SkillRun / SkillBolt / DeployedTurret); every piece is timed on the
    /// SkillTable step it shows. Bolts fly with the sim's own speed / range / width (SkillBolt geometry) and pop where they meet a monster.
    ///  - Power Shot / Plasma Barrage / Rocket Punch: MechanicLightSkills.Shots.cs;
    ///  - Target Lock / EMP Burst / Deploy Turret (cast) / Orbital Cannon: MechanicLightSkills.Areas.cs;
    ///  - Rocket Barrage keeps its hand-made timeline (RocketBarrageUltimate), redrawn with the same pieces; turret shots: MechanicTurretLightFx.
    /// The painted choreographies (SkillChoreoDefsMechanic) and frames are gone.
    /// </summary>
    public static partial class MechanicLightSkills
    {
        public const string PowerShotId = "mechanic.power-shot", BarrageId = "mechanic.plasma-barrage", TargetLockId = "mechanic.target-lock", PunchId = "mechanic.rocket-punch",
            TurretId = "mechanic.deploy-turret", EmpId = "mechanic.emp-burst", OrbitalId = "mechanic.orbital-cannon";
        static readonly Vector2 Chest = new Vector2(0f, 10f);

        /// <summary>True when this skill draws with the light FX (renders FX, shaders ready).</summary>
        public static bool Handles(string skillId)
        {
            switch (skillId)
            {
                case PowerShotId: case BarrageId: case TargetLockId: case PunchId: case TurretId: case EmpId: case OrbitalId: return MechanicLightLook.Active;
                default: return false;
            }
        }

        public static void Cast(SkillContext ctx)
        {
            if (!ctx.View || !Handles(ctx.Def.Id)) return;
            try
            {
                switch (ctx.Def.Id)
                {
                    case PowerShotId: PowerShot(ctx); break;
                    case BarrageId: PlasmaBarrage(ctx); break;
                    case PunchId: RocketPunch(ctx); break;
                    case TargetLockId: TargetLock(ctx); break;
                    case EmpId: EmpBurst(ctx); break;
                    case TurretId: DeployTurret(ctx); break;
                    case OrbitalId: OrbitalCannon(ctx); break;
                }
            }
            catch (System.Exception e) { Debug.LogError("[ZenoniaProto] mechanic light FX " + ctx.Def.Id + " failed: " + e); }
        }

        static float Dim(SkillContext ctx) { return MechanicLightLook.Dim(ctx.Body.Hero); }
        static bool Local(SkillContext ctx) { return ctx.Body.IsLocal; }

        static SkillEffect First(SkillContext ctx, EffectKind kind)
        {
            foreach (SkillEffect e in ctx.Def.Effects) if (e.Kind == kind) return e;
            return null;
        }

        static float Angle(Vector2 d) { return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg; }

        static Vector2 Rotate(Vector2 v, float deg) { float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r); return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c); }

        /// <summary>Live (or just killed: SkillRun fires step 0 before this) monsters whose box touches the circle, nearest first.</summary>
        static List<Monster> InCircle(Vector2 c, float r)
        {
            var list = new List<Monster>();
            foreach (Monster m in Monster.All)
            {
                if (m == null) continue;
                Rect b = m.Box;
                if (SkillGeometry.InCircle(c.x, c.y, r, b.xMin, b.yMin, b.xMax, b.yMax)) list.Add(m);
            }
            list.Sort((a, b) => (a.Center - c).sqrMagnitude.CompareTo((b.Center - c).sqrMagnitude));
            return list;
        }

        /// <summary>
        /// One monster hit at `at`: the full pop now, then (own casts only: the split numbers are the caster's) a lighter pulse for every further
        /// damage number of SkillHitSplit.For(skillId), StartDelayMs + k * IntervalMs later, so the flashes land with the numbers (21/11 multi-hit).
        /// </summary>
        static void Pulses(string skillId, Vector2 at, float size, float dim, ClericLightLook.Pal p, Color spark)
        {
            MechanicLightBursts.Pop(at, size, dim, p, spark);
            MechanicLightBursts.SplitPulses(skillId, at, size, dim, p);
        }
    }
}
