namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Damage and area numbers of the hand-written ultimates, shared by IceStormUltimate / EarthBreakUltimate / RocketBarrageUltimate and the skill tree text so the two cannot
    /// drift apart. Multipliers are x ATK, distances are world units as the ultimates use them.
    /// </summary>
    public static class LegacySkillNumbers
    {
        public const float TileUnits = 16f;

        public const float IceStormRadius = 44f, IceStormMult = 1.4f, IceStormRange = 26f, IceStormChillMs = 2500f;
        public const int IceStormWaves = 4;

        public const float EarthBreakRadius = 54f, EarthBreakMult = 3.8f, EarthBreakRange = 36f, EarthBreakSweep = 0.2f, EarthBreakBurn = 0.12f;
        public const int EarthBreakBurns = 4;

        // Rockets land at Range x (Near + (i % 3) x Step) in a forward fan, so the farthest one is at RocketBarrageReach.
        public const float RocketBarrageRadius = 18f, RocketBarrageMult = 4f, RocketBarrageRange = 70f, RocketBarrageFanDeg = 60f, RocketBarrageNear = 0.65f, RocketBarrageStep = 0.16f, RocketBarrageKnockback = 35f;
        public const float RocketBarrageReach = RocketBarrageRange * (RocketBarrageNear + 2f * RocketBarrageStep);
        public const int RocketBarrageRockets = 10;
    }
}
