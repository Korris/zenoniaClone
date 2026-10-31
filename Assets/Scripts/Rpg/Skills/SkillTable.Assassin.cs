namespace ZenoniaProto.Rpg
{
    public static partial class SkillTable
    {
        static SkillDef[] AssassinSkills()
        {
            const string C = "assassin";
            return new[]
            {
                Def(C, 0, "Shadow Strike", 1, 8, 3f, SkillTargeting.Facing, Fx(Strike(AreaShape.Arc, 95f, radius: 1.8f, angle: 120f, hits: 2, intervalMs: 120)), lockMs: 300),
                // Redone Shadow Step: blink behind the nearest enemy within 6 tiles, then a 260% hit (+30% from behind).
                Def(C, 1, "Shadow Step", 8, 18, 8f, SkillTargeting.NearestEnemy,
                    Fx(Move(MotionKind.Blink, 6f, 0, 180), new SkillEffect { Kind = EffectKind.Strike, Shape = AreaShape.Single, Pct = 260f, Radius = 1.5f, AtMs = 300, BehindBonusPct = 30f }),
                    Req(0, 3), targetRange: 6f, lockMs: 480),
                Def(C, 2, "Blade Dance", 15, 22, 10f, SkillTargeting.AroundSelf, Fx(Strike(AreaShape.Circle, 85f, radius: 2f, hits: 4, intervalMs: 150)), Req(0, 5), lockMs: 600),
                Def(C, 3, "Phantom Rush", 20, 24, 12f, SkillTargeting.Facing,
                    Fx(Move(MotionKind.Dash, 2f, 180, 140), Strike(AreaShape.Line, 120f, length: 2f, width: 1.5f, atMs: 320),
                       Move(MotionKind.Dash, 2f, 180, 320), Strike(AreaShape.Line, 120f, length: 2f, width: 1.5f, atMs: 500),
                       Move(MotionKind.Dash, 2f, 180, 500), Strike(AreaShape.Line, 120f, length: 2f, width: 1.5f, atMs: 680)), Req(1, 3), lockMs: 740),
                Def(C, 4, "Venom Daggers", 26, 20, 18f, SkillTargeting.Facing,
                    Fx(Strike(AreaShape.Cone, 120f, radius: 2.5f, angle: 60f, status: new StatusSpec(StatusKind.Poison, 25f, 10000))), Req(2, 3)),
                Def(C, 5, "Shadow Clone", 32, 30, 20f, SkillTargeting.Self,
                    Fx(new SkillEffect { Kind = EffectKind.Summon, DurationMs = 8000, Pct = 50f, Radius = 1.8f, IntervalMs = 600 }), Req(1, 5), lockMs: 400),
                Def(C, 6, "Death Mark", 40, 35, 22f, SkillTargeting.NearestEnemy,
                    Fx(new SkillEffect { Kind = EffectKind.Strike, Shape = AreaShape.Single, Radius = 1.5f, Status = new StatusSpec(StatusKind.Mark, 50f, 10000, 3) }),
                    Req(3, 3), targetRange: 6f),
                Def(C, 7, "Nightfall Execution", 50, 75, 45f, SkillTargeting.AroundSelf, Fx(Strike(AreaShape.Circle, 125f, radius: 3f, hits: 8, intervalMs: 120)), Req(5, 5, 6, 5), lockMs: 1000),
            };
        }
    }
}
