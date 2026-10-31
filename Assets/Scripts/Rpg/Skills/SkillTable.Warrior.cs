namespace ZenoniaProto.Rpg
{
    public static partial class SkillTable
    {
        static SkillDef[] WarriorSkills()
        {
            const string C = "warrior";
            var burn = new StatusSpec(StatusKind.Burn, 15f, 3000);
            return new[]
            {
                Def(C, 0, "Power Slash", 1, 8, 3f, SkillTargeting.Facing, Fx(Strike(AreaShape.Arc, 170f, radius: 2.5f, angle: 120f))),
                Def(C, 1, "Whirlwind", 8, 18, 8f, SkillTargeting.AroundSelf, Fx(Strike(AreaShape.Circle, 100f, radius: 2.5f, hits: 3, intervalMs: 250)), Req(0, 3), lockMs: 750),
                // Kept signature: EarthBreakUltimate (380% cone 5 long, cracks + spikes). MP/CD from the tree (today 29 MP / 8 s): rebalance in phase 12.
                Def(C, 2, "Earth Break", 15, 26, 12f, SkillTargeting.Facing, NoFx(), Req(0, 5), lockMs: 1450, legacy: LegacySkill.EarthBreak),
                Def(C, 3, "Flame Cleave", 20, 22, 10f, SkillTargeting.Facing, Fx(Strike(AreaShape.Arc, 300f, radius: 3f, angle: 140f, status: burn)), Req(1, 3), lockMs: 450),
                Def(C, 4, "Charging Strike", 26, 24, 12f, SkillTargeting.Facing,
                    Fx(Move(MotionKind.Dash, 5f, 250), Strike(AreaShape.Line, 300f, length: 5f, width: 2f, atMs: 250)), Req(2, 3)),
                // Hand-made timeline: BladeCycloneUltimate (6 waves x150% within 2.5 tiles, the last knocks back). Hit count stays 6 at every rank: the table has no per-rank hit count.
                Def(C, 5, "Blade Cyclone", 32, 40, 12f, SkillTargeting.AroundSelf, NoFx(), Req(1, 5), lockMs: 450, legacy: LegacySkill.BladeCyclone),
                Def(C, 6, "Inferno Crash", 40, 40, 22f, SkillTargeting.Facing,
                    Fx(Move(MotionKind.Leap, 4f, 400), Strike(AreaShape.Circle, 520f, radius: 3f, atMs: 400, status: burn)), Req(3, 3, 4, 3), lockMs: 700),
                Def(C, 7, "Berserker Awakening", 50, 75, 50f, SkillTargeting.Self,
                    Fx(BuffFx(BuffKind.AtkPct, 25f, 12000), BuffFx(BuffKind.AtkSpeedPct, 15f, 12000)), Req(5, 5, 6, 5), lockMs: 600),
            };
        }
    }
}
