namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Damage and area numbers of the hand-written ultimates, shared by IceStormUltimate / EarthBreakUltimate / RocketBarrageUltimate / BladeCycloneUltimate and the skill tree text so the two cannot
    /// drift apart. Multipliers are x ATK, distances are world units as the ultimates use them.
    /// </summary>
    public static class LegacySkillNumbers
    {
        public const float TileUnits = 16f;

        public const float IceStormRadius = 44f, IceStormMult = 1.4f, IceStormRange = 26f, IceStormChillMs = 2500f;
        public const int IceStormWaves = 4;

        // Multi-Hit Barrage (ChatGPT V3): 8 small sweep hits, a stronger one as the sword falls, then the 300 % slam = 880 %; then 4 fissure burns of 20 % ATK.
        public const float EarthBreakRadius = 54f, EarthBreakRange = 36f, EarthBreakSmallMult = 0.6f, EarthBreakPenultMult = 1.0f, EarthBreakFinishMult = 3.0f, EarthBreakBurn = 0.2f;
        public const int EarthBreakSmallHits = 8, EarthBreakHits = EarthBreakSmallHits + 2, EarthBreakBurns = 4;
        public const float EarthBreakTotalMult = EarthBreakSmallHits * EarthBreakSmallMult + EarthBreakPenultMult + EarthBreakFinishMult;

        // Rockets land at Range x (Near + (i % 3) x Step) in a forward fan, so the farthest one is at RocketBarrageReach.
        public const float RocketBarrageRadius = 18f, RocketBarrageMult = 4f, RocketBarrageRange = 70f, RocketBarrageFanDeg = 60f, RocketBarrageNear = 0.65f, RocketBarrageStep = 0.16f, RocketBarrageKnockback = 35f;
        public const float RocketBarrageReach = RocketBarrageRange * (RocketBarrageNear + 2f * RocketBarrageStep);
        public const int RocketBarrageRockets = 10;

        // Multi-Hit Barrage (ChatGPT V3, docs/reference/warrior-skill-redesign-v2-chatgpt.md): 16 small hits, a stronger one, then the knock-back finisher.
        // 16 x 45 + 100 + 260 = 1080 % per cast; DEF is subtracted once per cast, split by each hit's share.
        public const float BladeCycloneRadius = 40f, BladeCycloneSmallMult = 0.45f, BladeCyclonePenultMult = 1.0f, BladeCycloneFinishMult = 2.6f, BladeCycloneKnockback = 45f;
        public const int BladeCycloneSmallHits = 16, BladeCycloneHits = BladeCycloneSmallHits + 2;
        public const float BladeCycloneTotalMult = BladeCycloneSmallHits * BladeCycloneSmallMult + BladeCyclonePenultMult + BladeCycloneFinishMult;
    }
}
