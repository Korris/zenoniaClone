namespace ZenoniaProto.Rpg
{
    public static partial class SkillTable
    {
        static SkillDef[] WizardSkills()
        {
            const string C = "wizard";
            return new[]
            {
                Def(C, 0, "Arcane Bolt", 1, 10, 3f, SkillTargeting.Facing, Fx(Proj(160f, 7f, 18f))),
                Def(C, 1, "Fireball", 8, 18, 6f, SkillTargeting.Facing,
                    Fx(Proj(230f, 7f, 12f, impact: 1.5f, status: new StatusSpec(StatusKind.Burn, 15f, 4000))), Req(0, 3)),
                // Kept signature: IceStormUltimate (6 waves x70% over 3 s, r3, slow 40%). MP/CD from the tree (today 20 MP / 7.7 s): rebalance in phase 12.
                Def(C, 2, "Ice Storm", 15, 28, 12f, SkillTargeting.Facing, NoFx(), Req(0, 5), lockMs: 650, legacy: LegacySkill.IceStorm),
                Def(C, 3, "Arcane Lance", 20, 24, 9f, SkillTargeting.Facing, Fx(Proj(320f, 8f, 30f, pierce: true)), Req(1, 3)),
                Def(C, 4, "Mana Shield", 26, 25, 22f, SkillTargeting.Self, Fx(BuffFx(BuffKind.Shield, 250f, 8000)), Req(2, 3)),
                Def(C, 5, "Frost Nova", 32, 30, 16f, SkillTargeting.AroundSelf,
                    Fx(Strike(AreaShape.Circle, 280f, radius: 3f, status: new StatusSpec(StatusKind.Freeze, 0f, 2000))), Req(2, 5)),
                Def(C, 6, "Elemental Burst", 40, 40, 20f, SkillTargeting.Facing,
                    Fx(new SkillEffect
                    {
                        Kind = EffectKind.Strike, Shape = AreaShape.Circle, Pct = 400f, Radius = 2.5f, Range = 4f,
                        BonusVsMask = SkillEffect.StatusBit(StatusKind.Burn) | SkillEffect.StatusBit(StatusKind.Freeze), BonusPct = 25f,
                    }), Req(3, 3, 5, 3), lockMs: 500),
                Def(C, 7, "Astral Cataclysm", 50, 90, 50f, SkillTargeting.Facing, Fx(Rain(120f, 8, 5f, 4f, 200, 1.5f)), Req(6, 5, 4, 3), lockMs: 900),
            };
        }
    }
}
