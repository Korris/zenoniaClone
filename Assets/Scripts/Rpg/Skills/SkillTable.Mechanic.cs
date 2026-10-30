namespace ZenoniaProto.Rpg
{
    public static partial class SkillTable
    {
        static SkillDef[] MechanicSkills()
        {
            const string C = "mechanic";
            var stun = new StatusSpec(StatusKind.Stun, 0f, 1500);
            return new[]
            {
                Def(C, 0, "Power Shot", 1, 8, 3f, SkillTargeting.Facing, Fx(Proj(150f, 7f, 24f, pierce: true))),
                Def(C, 1, "Homing Rockets", 8, 16, 7f, SkillTargeting.Facing, Fx(Proj(75f, 6f, 10f, count: 3, spread: 30f, homing: true, impact: 1f)), Req(0, 3)),
                Def(C, 2, "Plasma Barrage", 15, 22, 9f, SkillTargeting.Facing, Fx(Proj(55f, 6f, 16f, width: 0.6f, count: 6, spread: 60f)), Req(0, 5)),
                Def(C, 3, "Target Lock", 20, 18, 15f, SkillTargeting.AroundSelf,
                    Fx(new SkillEffect { Kind = EffectKind.Strike, Shape = AreaShape.Circle, Radius = 7f, Count = 5, Status = new StatusSpec(StatusKind.Mark, 15f, 8000) }), Req(1, 3)),
                Def(C, 4, "Rocket Punch", 26, 24, 11f, SkillTargeting.Facing,
                    Fx(Proj(280f, 4f, 14f, width: 2f, pierce: true, status: new StatusSpec(StatusKind.Knockback, 2f, 0))), Req(2, 3)),
                Def(C, 5, "Robot Assault", 32, 28, 18f, SkillTargeting.Self,
                    Fx(new SkillEffect { Kind = EffectKind.Summon, DurationMs = 8000, Pct = 65f, Radius = 6f, IntervalMs = 1000 }), Req(3, 3), lockMs: 450),
                Def(C, 6, "EMP Burst", 40, 35, 20f, SkillTargeting.AroundSelf, Fx(Strike(AreaShape.Circle, 350f, radius: 3f, status: stun)), Req(4, 3), lockMs: 450),
                Def(C, 7, "Orbital Cannon", 50, 80, 45f, SkillTargeting.Facing, Fx(Rain(140f, 6, 7f, 4f, 250, 1.5f)), Req(5, 5, 6, 5), lockMs: 900),
            };
        }
    }
}
