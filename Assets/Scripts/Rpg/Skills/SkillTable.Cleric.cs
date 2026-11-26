namespace ZenoniaProto.Rpg
{
    public static partial class SkillTable
    {
        static SkillDef[] ClericSkills()
        {
            const string C = "cleric";
            return new[]
            {
                // Redone Heal: 220% ATK on the lowest-HP ally within 6 tiles, and always on the caster too (LowestAllyHealRule, 2026-11-20).
                Def(C, 0, "Heal", 1, 12, 5f, SkillTargeting.LowestAlly, Fx(HealFx(220f)), targetRange: 6f),
                Def(C, 1, "Spirit Blessing", 8, 18, 18f, SkillTargeting.Self, Fx(BuffFx(BuffKind.AtkPct, 15f, 60000, ally: true, radius: 4f)), Req(0, 3)),
                Def(C, 2, "Divine Aegis", 15, 20, 18f, SkillTargeting.Self, Fx(BuffFx(BuffKind.DefPct, 15f, 60000, ally: true, radius: 4f)), Req(0, 5)),
                Def(C, 3, "Divine Light", 20, 24, 12f, SkillTargeting.Ground, Fx(Strike(AreaShape.Circle, 300f, radius: 2f, range: 3f), HealFx(100f, radius: 2f, range: 3f)), Req(1, 3), castRange: 5f), // Ground (ChatGPT 26/11)
                Def(C, 4, "Sanctuary", 26, 30, 20f, SkillTargeting.Ground, // Ground (ChatGPT 26/11): the healing zone sits on the chosen point
                    Fx(new SkillEffect { Kind = EffectKind.Zone, Shape = AreaShape.Circle, Radius = 3f, Pct = 35f, Ally = true, Hits = 8, IntervalMs = 1000, DurationMs = 8000 }), Req(0, 5), castRange: 5f),
                Def(C, 5, "Guardian Spirit", 32, 32, 22f, SkillTargeting.Self, Fx(BuffFx(BuffKind.Shield, 200f, 8000, ally: true, radius: 4f)), Req(1, 5)),
                // Cleanse is a no-op until heroes can carry debuffs.
                Def(C, 6, "Purification", 40, 38, 20f, SkillTargeting.AroundSelf, Fx(Strike(AreaShape.Circle, 350f, radius: 3f)), Req(3, 3, 4, 3), lockMs: 450),
                Def(C, 7, "Divine Ascension", 50, 85, 50f, SkillTargeting.AroundSelf,
                    Fx(HealFx(450f, radius: 5f), Strike(AreaShape.Circle, 550f, radius: 5f, atMs: 100)), Req(5, 5, 6, 5), lockMs: 900),
            };
        }
    }
}
