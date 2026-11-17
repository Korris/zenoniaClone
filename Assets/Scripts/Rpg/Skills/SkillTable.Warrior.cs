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
                // Multi-Hit Barrage trial (ChatGPT V3, docs/reference/warrior-skill-redesign-v2-chatgpt.md): 8 hits at 90-440 ms, 6 x 55 + 100 + 180 = 610 %.
                // The first hit gathers; DEF is split over the chain (SkillDef.DefShare). Effect 0 stays the arc the choreography and tests read.
                Def(C, 0, "Power Slash", 1, 8, 3f, SkillTargeting.Facing,
                    Fx(new SkillEffect { Kind = EffectKind.Strike, Shape = AreaShape.Arc, Pct = 55f, Radius = 3f, AngleDeg = 160f, AtMs = 90, PullTiles = 0.75f, PullFocusTiles = 1.25f },
                       SlashArc(55f, 135, hits: 5, intervalMs: 45), SlashArc(100f, 365), SlashArc(180f, 440, finisher: true))),
                Def(C, 1, "Whirlwind", 8, 18, 8f, SkillTargeting.AroundSelf, Fx(Strike(AreaShape.Circle, 100f, radius: 2.5f, hits: 3, intervalMs: 250)), Req(0, 3), lockMs: 750),
                // Hand-made timeline: EarthBreakUltimate (Multi-Hit Barrage: 8 sweep hits + 100 % + the 300 % slam = 880 %, then 4 burns of 20 %; LegacySkillNumbers).
                Def(C, 2, "Earth Break", 15, 26, 12f, SkillTargeting.Facing, NoFx(), Req(0, 5), lockMs: 1450, legacy: LegacySkill.EarthBreak),
                Def(C, 3, "Flame Cleave", 20, 22, 10f, SkillTargeting.Facing, Fx(Strike(AreaShape.Arc, 300f, radius: 3f, angle: 140f, status: burn)), Req(1, 3), lockMs: 450),
                Def(C, 4, "Charging Strike", 26, 24, 12f, SkillTargeting.Facing,
                    Fx(Move(MotionKind.Dash, 5f, 250), Strike(AreaShape.Line, 300f, length: 5f, width: 2f, atMs: 250)), Req(2, 3)),
                // Hand-made timeline: BladeCycloneUltimate (Multi-Hit Barrage: 18 hits within 2.5 tiles, 1080 %, the last knocks back; LegacySkillNumbers). Hit count is the same at every rank.
                Def(C, 5, "Blade Cyclone", 32, 40, 12f, SkillTargeting.AroundSelf, NoFx(), Req(1, 5), lockMs: 450, legacy: LegacySkill.BladeCyclone),
                // Multi-Hit Barrage (ChatGPT V3): lands at 400, 13 x 50 % every 35 ms from 450, 140 % at 940, the 350 % finisher at 1100 = 1140 %.
                // Effect 0 stays the leap (InfernoCrashUltimate reads its MoveTiles); burn rides the small hits (re-applying only refreshes it).
                Def(C, 6, "Inferno Crash", 40, 40, 22f, SkillTargeting.Facing,
                    Fx(Move(MotionKind.Leap, 4f, 400), Strike(AreaShape.Circle, 50f, radius: 3f, hits: 13, intervalMs: 35, atMs: 450, status: burn),
                       CraterBlast(140f, 940), CraterBlast(350f, 1100, finisher: true)), Req(3, 3, 4, 3), lockMs: 700),
                Def(C, 7, "Berserker Awakening", 50, 75, 50f, SkillTargeting.Self,
                    Fx(BuffFx(BuffKind.AtkPct, 25f, 12000), BuffFx(BuffKind.AtkSpeedPct, 15f, 12000)), Req(5, 5, 6, 5), lockMs: 600),
            };
        }

        /// <summary>One more blast of Inferno Crash's 3-tile crater.</summary>
        static SkillEffect CraterBlast(float pct, int atMs, bool finisher = false)
        {
            SkillEffect e = Strike(AreaShape.Circle, pct, radius: 3f, atMs: atMs);
            e.Finisher = finisher;
            return e;
        }

        /// <summary>One more sweep of Power Slash's 160-degree, 3-tile arc (no gather).</summary>
        static SkillEffect SlashArc(float pct, int atMs, int hits = 1, int intervalMs = 0, bool finisher = false)
        {
            SkillEffect e = Strike(AreaShape.Arc, pct, radius: 3f, angle: 160f, hits: hits, intervalMs: intervalMs, atMs: atMs);
            e.Finisher = finisher;
            return e;
        }
    }
}
