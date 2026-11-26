namespace ZenoniaProto.Rpg
{
    public static partial class SkillTable
    {
        /// <summary>
        /// Archer tree (Asteria Ranger, 2026-11-26, first cut): like the Fighter, every skill is built from the generic effects (Projectile / Rain /
        /// Buff / Motion), so the executor, the server and the skill pane need nothing new. Piercing Arrow is the signature skill on L (slot 1 as the
        /// first learned skill). No painted skill art: FX are particles / shaders (CLAUDE.md), green like the class palette.
        /// Role split 2026-11-26 (ChatGPT "Ragnarok Và Đạo Chúa", user approved): Archer = mobile precision DPS, outranges the Mechanic (arrow skills
        /// 7-8 tiles vs his 6-6.5), wins on one target / a boss, loses on packs. Arrow Rain 90 -> 140 % (his pack tool), Hunter's Focus ATK 25 -> 22 %.
        /// Ratios: Tests/dotnet/ZenoniaSkills.Tests/ArcherMechanicRoleBalanceTests.cs. Shared with the server: rebuild it.
        /// </summary>
        static SkillDef[] ArcherSkills()
        {
            const string C = "archer";
            return new[]
            {
                // L: one strong arrow that flies through the whole line.
                Def(C, 0, "Piercing Arrow", 1, 8, 3f, SkillTargeting.Facing, Fx(Proj(170f, 8f, 26f, width: 0.8f, pierce: true)), lockMs: 320),
                // Five arrows in a 50-degree fan.
                Def(C, 1, "Multi Shot", 8, 18, 7f, SkillTargeting.Facing, Fx(Proj(70f, 7f, 24f, width: 0.6f, count: 5, spread: 50f)), Req(0, 3), lockMs: 380),
                Def(C, 2, "Eagle Eye", 15, 16, 10f, SkillTargeting.Self, Fx(BuffFx(BuffKind.AtkSpeedPct, 20f, 10000), BuffFx(BuffKind.AtkPct, 10f, 10000)), Req(0, 5), lockMs: 300),
                // Arrows shot high come down over the area ahead.
                Def(C, 3, "Arrow Rain", 20, 26, 10f, SkillTargeting.Ground, Fx(Rain(140f, 8, 6f, 2.5f, 120, 1.2f)), Req(1, 3), lockMs: 420, castRange: 7f), // Ground (ChatGPT 26/11)
                // A weighted arrow that pins the first foe it hits.
                Def(C, 4, "Binding Shot", 26, 22, 11f, SkillTargeting.Facing,
                    Fx(Proj(220f, 7.5f, 22f, width: 1f, status: new StatusSpec(StatusKind.Stun, 0f, 1200))), Req(2, 3), lockMs: 350),
                // A quick wind-step dash, then the next arrows hit harder.
                Def(C, 5, "Wind Step", 32, 14, 8f, SkillTargeting.Facing,
                    Fx(Move(MotionKind.Dash, 3f, 220), BuffFx(BuffKind.NextHitsPct, 40f, 5000)), Req(3, 3), lockMs: 300),
                Def(C, 6, "Hunter's Focus", 40, 30, 20f, SkillTargeting.Self, Fx(BuffFx(BuffKind.AtkPct, 22f, 12000), BuffFx(BuffKind.AtkSpeedPct, 15f, 12000)), Req(4, 3), lockMs: 400),
                // Ultimate: a storm of starlit arrows over a wide area.
                Def(C, 7, "Starfall Volley", 50, 80, 45f, SkillTargeting.Ground, Fx(Rain(160f, 10, 7f, 3.5f, 150, 1.5f)), Req(5, 5, 6, 5), lockMs: 800, castRange: 7.5f),
            };
        }
    }
}
