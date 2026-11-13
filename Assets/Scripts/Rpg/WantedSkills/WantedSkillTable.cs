namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Skill tables of the daily Wanted Bosses (docs/wanted-boss-skills-design.md, ChatGPT numbers, not yet balanced). Each boss: 3 plain skills
    /// (Id 0-2) + 1 enrage skill (Id 3, Enrage = true, only once the boss is below WantedSkillClock.EnrageHpFraction). Rimeclaw Tyrant has its own
    /// art (telegraph cast rows + "rimeclawTyrant-skillN.png" impact pictures, WantedImpactArt); Glacial Colossus and Mire Hydra Queen got their cast rows on 2026-11-12; a boss without them reuses the
    /// shared telegraph pictures only (CastRow -1, no boss-specific impact picture yet) until their own art lands.
    /// </summary>
    public static class WantedSkillTable
    {
        public static readonly WantedSkillDef[] RimeclawTyrant =
        {
            new WantedSkillDef { Id = 0, Name = "Frostclaw Sweep", Shape = WantedAreaShape.Cone, RadiusTiles = 4f, AngleDeg = 120f,
                TelegraphMs = 1200, CooldownMs = 9000, DamageMult = 1.5f, KnockbackTiles = 1f, CastRow = -1 },
            new WantedSkillDef { Id = 1, Name = "Broken Chain Slam", Shape = WantedAreaShape.Circle, OnPlayers = true, RadiusTiles = 2f, Count = 1,
                TelegraphMs = 1500, CooldownMs = 13000, DamageMult = 1.2f, StatusKind = StatusKind.Slow, StatusValue = 30f, StatusMs = 2000, CastRow = 0 },
            // 2026-11-12: Freeze instead of a slow (docs/wanted-boss-skills-design.md note) -- HeroController.WantedStatus.cs locks out every
            // action (move / attack / skill / dash) for StatusMs, non-stacking (max of remaining and new), no knockback while it holds.
            new WantedSkillDef { Id = 2, Name = "Frozen Ground Stomp", Shape = WantedAreaShape.Circle, RadiusTiles = 4f,
                TelegraphMs = 1600, CooldownMs = 17000, DamageMult = 1.8f, StatusKind = StatusKind.Freeze, StatusMs = 3000, CastRow = 1 },
            new WantedSkillDef { Id = 3, Name = "Tyrant's Blizzard", Shape = WantedAreaShape.Circle, RadiusTiles = 6f, SafeArcDeg = 90f, SafeArcCount = 1,
                TelegraphMs = 2200, CooldownMs = 28000, DamageMult = 2.2f, StatusKind = StatusKind.Slow, StatusValue = 40f, StatusMs = 3000, CastRow = 2, Enrage = true },
        };

        public static readonly WantedSkillDef[] GlacialColossus =
        {
            new WantedSkillDef { Id = 0, Name = "Crystal Fissure", Shape = WantedAreaShape.Line, LengthTiles = 9f, WidthTiles = 2f,
                TelegraphMs = 1400, CooldownMs = 10000, DamageMult = 1.8f, StatusKind = StatusKind.Slow, StatusValue = 30f, StatusMs = 2000, CastRow = 0 },
            new WantedSkillDef { Id = 1, Name = "Rune Impact", Shape = WantedAreaShape.Cone, RadiusTiles = 4f, AngleDeg = 100f,
                TelegraphMs = 1300, CooldownMs = 12000, DamageMult = 2.0f, KnockbackTiles = 2f, CastRow = 0 }, // same two-handed slam as the fissure
            new WantedSkillDef { Id = 2, Name = "Crystal Fall", Shape = WantedAreaShape.Circle, OnPlayers = true, RadiusTiles = 1.5f, Count = 3,
                TelegraphMs = 1800, CooldownMs = 18000, DamageMult = 1.4f, StatusKind = StatusKind.Freeze, StatusMs = 2000, CastRow = 1 }, // 2026-11-12: freeze, not slow (the set lacked a real threat)
            new WantedSkillDef { Id = 3, Name = "Core Overload", Shape = WantedAreaShape.Circle, RadiusTiles = 7f, SafeArcDeg = 60f, SafeArcCount = 2,
                TelegraphMs = 2400, CooldownMs = 30000, DamageMult = 2.5f, KnockbackTiles = 2f, CastRow = 2, Enrage = true },
        };

        public static readonly WantedSkillDef[] MireHydraQueen =
        {
            new WantedSkillDef { Id = 0, Name = "Triple Venom Breath", Shape = WantedAreaShape.Cone, RadiusTiles = 6f, AngleDeg = 120f,
                TelegraphMs = 1600, CooldownMs = 11000, DamageMult = 1.2f, StatusKind = StatusKind.Poison, StatusValue = 15f, StatusMs = 4000, CastRow = 0 },
            new WantedSkillDef { Id = 1, Name = "Poison Bloom", Shape = WantedAreaShape.Circle, OnPlayers = true, RadiusTiles = 2f, Count = 3,
                TelegraphMs = 1900, CooldownMs = 15000, DamageMult = 0.8f, StatusKind = StatusKind.Poison, StatusValue = 15f, StatusMs = 4000, CastRow = 0 }, // same venom breath pose
            new WantedSkillDef { Id = 2, Name = "Serpent Tail Crush", Shape = WantedAreaShape.Circle, RadiusTiles = 4f,
                TelegraphMs = 1500, CooldownMs = 16000, DamageMult = 1.7f, KnockbackTiles = 2f, CastRow = 1 },
            new WantedSkillDef { Id = 3, Name = "Queen's Toxic Reign", Shape = WantedAreaShape.Circle, OnPlayers = true, RadiusTiles = 2f, Count = 5,
                TelegraphMs = 2200, CooldownMs = 30000, DamageMult = 1.5f, StatusKind = StatusKind.Poison, StatusValue = 20f, StatusMs = 5000, CastRow = 2, Enrage = true },
        };

        /// <summary>The 4-skill table of a wanted boss type, or null when it has none (every other monster).</summary>
        public static WantedSkillDef[] For(string bossType)
        {
            switch (bossType)
            {
                case "rimeclawTyrant": return RimeclawTyrant;
                case "glacialColossus": return GlacialColossus;
                case "mireHydraQueen": return MireHydraQueen;
                default: return null;
            }
        }
    }
}
