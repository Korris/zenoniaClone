namespace ZenoniaProto.Rpg
{
    public static partial class SkillTable
    {
        /// <summary>
        /// Fighter tree (2026-11-04, first cut): every skill is built from the generic effects (Strike / Projectile / Motion / Buff), so the executor, the
        /// server and the skill pane need nothing new. No painted art yet: SkillVfx draws its tinted ring fallback, orange like the basic attacks.
        /// Dash Punch is the signature skill on L (magazine slot 1 as the first learned skill).
        /// </summary>
        static SkillDef[] FighterSkills()
        {
            const string C = "fighter";
            var stun = new StatusSpec(StatusKind.Stun, 0f, 1500);
            return new[]
            {
                // L: a short dash into a straight punch.
                Def(C, 0, "Dash Punch", 1, 8, 3f, SkillTargeting.Facing,
                    Fx(Move(MotionKind.Dash, 3f, 200), Strike(AreaShape.Line, 180f, length: 3f, width: 1.5f, atMs: 200)), lockMs: 320),
                Def(C, 1, "Iron Fist", 8, 16, 8f, SkillTargeting.Self, Fx(BuffFx(BuffKind.AtkPct, 20f, 10000)), Req(0, 3), lockMs: 300),
                // Both fists into the ground: everything around is launched (bosses are immune, StatusRules).
                Def(C, 2, "Ground Slam", 15, 24, 10f, SkillTargeting.AroundSelf,
                    Fx(Strike(AreaShape.Circle, 220f, radius: 2.5f, status: new StatusSpec(StatusKind.Launch, 0f, 1500))), Req(0, 5), lockMs: 450),
                Def(C, 3, "Rapid Jabs", 20, 22, 9f, SkillTargeting.Facing,
                    Fx(Strike(AreaShape.Cone, 70f, radius: 2f, angle: 60f, hits: 5, intervalMs: 90)), Req(1, 3), lockMs: 520),
                // A punch so hard its shockwave flies on through the line.
                Def(C, 4, "Shockwave", 26, 26, 12f, SkillTargeting.Facing,
                    Fx(Proj(220f, 6f, 14f, width: 1.5f, pierce: true, status: new StatusSpec(StatusKind.Knockback, 1f, 0))), Req(2, 3)),
                // Three short dashes, a hook after each (the Assassin's Phantom Rush shape).
                Def(C, 5, "Tiger Rush", 32, 32, 14f, SkillTargeting.Facing,
                    Fx(Move(MotionKind.Dash, 2f, 160, 120), Strike(AreaShape.Line, 130f, length: 2f, width: 1.5f, atMs: 280),
                       Move(MotionKind.Dash, 2f, 160, 280), Strike(AreaShape.Line, 130f, length: 2f, width: 1.5f, atMs: 440),
                       Move(MotionKind.Dash, 2f, 160, 440), Strike(AreaShape.Line, 130f, length: 2f, width: 1.5f, atMs: 600)), Req(3, 3), lockMs: 660),
                Def(C, 6, "Iron Body", 40, 35, 20f, SkillTargeting.Self, Fx(BuffFx(BuffKind.Shield, 60f, 8000), BuffFx(BuffKind.DefPct, 30f, 8000)), Req(1, 5), lockMs: 400),
                // Ultimate: leap and drive the fist down, stunning everything in the crater.
                Def(C, 7, "Dragon Fist", 50, 75, 45f, SkillTargeting.Facing,
                    Fx(Move(MotionKind.Leap, 4f, 400), Strike(AreaShape.Circle, 600f, radius: 3f, atMs: 400, status: stun)), Req(5, 5, 6, 5), lockMs: 800),
            };
        }
    }
}
