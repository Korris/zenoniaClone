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
            new WantedSkillDef { Id = 0, Name = "Frostclaw Sweep", Hint = "A wide claw sweep. Get behind the boss.", Shape = WantedAreaShape.Cone, RadiusTiles = 4f, AngleDeg = 120f,
                TelegraphMs = 1200, CooldownMs = 9000, DamageMult = 1.5f, KnockbackTiles = 1f, CastRow = -1 },
            new WantedSkillDef { Id = 1, Name = "Broken Chain Slam", Hint = "A chain slams one hunter. Leave the red circle.", Shape = WantedAreaShape.Circle, OnPlayers = true, RadiusTiles = 2f, Count = 1,
                TelegraphMs = 1500, CooldownMs = 13000, DamageMult = 1.2f, StatusKind = StatusKind.Slow, StatusValue = 30f, StatusMs = 2000, CastRow = 0 },
            // 2026-11-12: Freeze instead of a slow (docs/wanted-boss-skills-design.md note) -- HeroController.WantedStatus.cs locks out every
            // action (move / attack / skill / dash) for StatusMs, non-stacking (max of remaining and new), no knockback while it holds.
            new WantedSkillDef { Id = 2, Name = "Frozen Ground Stomp", Hint = "The ground freezes around the boss. Back away.", Shape = WantedAreaShape.Circle, RadiusTiles = 4f,
                TelegraphMs = 1600, CooldownMs = 17000, DamageMult = 1.8f, StatusKind = StatusKind.Freeze, StatusMs = 3000, CastRow = 1 },
            new WantedSkillDef { Id = 3, Name = "Tyrant's Blizzard", Hint = "A huge blizzard. Stay in the safe lane.", Shape = WantedAreaShape.Circle, RadiusTiles = 6f, SafeArcDeg = 90f, SafeArcCount = 1,
                TelegraphMs = 2200, CooldownMs = 28000, DamageMult = 2.2f, StatusKind = StatusKind.Slow, StatusValue = 40f, StatusMs = 3000, CastRow = 2, Enrage = true },
        };

        public static readonly WantedSkillDef[] GlacialColossus =
        {
            new WantedSkillDef { Id = 0, Name = "Crystal Fissure", Hint = "A straight ice burst. Move to the side.", Shape = WantedAreaShape.Line, LengthTiles = 9f, WidthTiles = 2f,
                TelegraphMs = 1400, CooldownMs = 10000, DamageMult = 1.8f, StatusKind = StatusKind.Slow, StatusValue = 30f, StatusMs = 2000, CastRow = 0 },
            new WantedSkillDef { Id = 1, Name = "Rune Impact", Hint = "A wide frontal slam. Move out of the red area.", Shape = WantedAreaShape.Cone, RadiusTiles = 4f, AngleDeg = 100f,
                TelegraphMs = 1300, CooldownMs = 12000, DamageMult = 2.0f, KnockbackTiles = 2f, CastRow = 0 }, // same two-handed slam as the fissure
            new WantedSkillDef { Id = 2, Name = "Crystal Fall", Hint = "Multiple ice crashes. Avoid the red circles.", Shape = WantedAreaShape.Circle, OnPlayers = true, RadiusTiles = 1.5f, Count = 3,
                TelegraphMs = 1800, CooldownMs = 18000, DamageMult = 1.4f, StatusKind = StatusKind.Freeze, StatusMs = 2000, CastRow = 1 }, // 2026-11-12: freeze, not slow (the set lacked a real threat)
            new WantedSkillDef { Id = 3, Name = "Core Overload", Hint = "Massive explosion. Stay in a safe lane.", Shape = WantedAreaShape.Circle, RadiusTiles = 7f, SafeArcDeg = 60f, SafeArcCount = 2,
                TelegraphMs = 2400, CooldownMs = 30000, DamageMult = 2.5f, KnockbackTiles = 2f, CastRow = 2, Enrage = true },
        };

        public static readonly WantedSkillDef[] MireHydraQueen =
        {
            new WantedSkillDef { Id = 0, Name = "Triple Venom Breath", Hint = "A wide poison breath. Get behind the boss.", Shape = WantedAreaShape.Cone, RadiusTiles = 6f, AngleDeg = 120f,
                TelegraphMs = 1600, CooldownMs = 11000, DamageMult = 1.2f, StatusKind = StatusKind.Poison, StatusValue = 15f, StatusMs = 4000, CastRow = 0 },
            new WantedSkillDef { Id = 1, Name = "Poison Bloom", Hint = "Poison blooms under hunters. Avoid the red circles.", Shape = WantedAreaShape.Circle, OnPlayers = true, RadiusTiles = 2f, Count = 3,
                TelegraphMs = 1900, CooldownMs = 15000, DamageMult = 0.8f, StatusKind = StatusKind.Poison, StatusValue = 15f, StatusMs = 4000, CastRow = 0 }, // same venom breath pose
            new WantedSkillDef { Id = 2, Name = "Serpent Tail Crush", Hint = "The tail crushes all around. Back away.", Shape = WantedAreaShape.Circle, RadiusTiles = 4f,
                TelegraphMs = 1500, CooldownMs = 16000, DamageMult = 1.7f, KnockbackTiles = 2f, CastRow = 1 },
            new WantedSkillDef { Id = 3, Name = "Queen's Toxic Reign", Hint = "Poison rains on five hunters. Avoid the red circles.", Shape = WantedAreaShape.Circle, OnPlayers = true, RadiusTiles = 2f, Count = 5,
                TelegraphMs = 2200, CooldownMs = 30000, DamageMult = 1.5f, StatusKind = StatusKind.Poison, StatusValue = 20f, StatusMs = 5000, CastRow = 2, Enrage = true },
            // 2026-11-19 user: "cho nó 1 skill khi xài sẽ rút độc máu của người chơi". A 5th plain skill (Id 4, after the enrage one so ids stay
            // put), numbers from ChatGPT (Tay, chat 6aff091a, not balanced yet): circle 6 tiles round the boss, 2.0 s, only poisoned heroes,
            // 0.8x, poison removed, boss heals 50 % of the HP taken, capped at 2 % of its max HP per cast, 22 s cooldown.
            new WantedSkillDef { Id = 4, Name = "Venom Drain", Hint = "The queen drains poisoned hunters. Leave the circle.", Shape = WantedAreaShape.Circle, RadiusTiles = 6f,
                TelegraphMs = 2000, CooldownMs = 22000, DamageMult = 0.8f, CastRow = 0, DrainPoisoned = true, DrainHealShare = 0.5f, DrainHealCap = 0.02f },
        };

        /// <summary>
        /// Ambercap Warden, Rootburrow's dungeon boss (21/11/2026, MonsterDef.SkillBoss, not a Wanted boss): ChatGPT's four skills
        /// (docs/reference/rootburrow-dark-25d/rootburrow-dark-dungeon-art-direction-chatgpt.md section D) with the reach scaled to the boss's own body
        /// (1.8 heroes: ChatGPT's 1.8 m pulse would sit under its cap), the hall (12 x 5 tiles + corridors) and the user's 21/11 rule that boss skills stay GRAND
        /// (sizes at the top of their band, docs/sprite-rules.md rule 3). Rootline Burst is one line toward the
        /// target (the line shape is anchored at the boss here, not on players). Light FX: CaveBossLightFx + AmbercapLightCasts (particles / meshes / shaders, no sprites).
        /// Cast rows of ambercapWarden-cast.png: 0 spore puff (fan), 1 root slam (pulse + rootline), 2 cap bloom (enrage), 3 defeat.
        /// </summary>
        public static readonly WantedSkillDef[] AmbercapWarden =
        {
            new WantedSkillDef { Id = 0, Name = "Amber Pulse", Hint = "An amber ring bursts round the boss. Step out of it.", Shape = WantedAreaShape.Circle, RadiusTiles = 3.5f,
                TelegraphMs = 1300, CooldownMs = 7000, DamageMult = 1.25f, StatusKind = StatusKind.Slow, StatusValue = 25f, StatusMs = 2000, CastRow = 1 },
            new WantedSkillDef { Id = 1, Name = "Spore Fan", Hint = "A fan of spores in front. Get to the side.", Shape = WantedAreaShape.Cone, RadiusTiles = 5.5f, AngleDeg = 90f,
                TelegraphMs = 1500, CooldownMs = 10000, DamageMult = 1.4f, StatusKind = StatusKind.Poison, StatusValue = 12f, StatusMs = 3000, CastRow = 0 },
            new WantedSkillDef { Id = 2, Name = "Rootline Burst", Hint = "Roots burst along a line. Move out of it.", Shape = WantedAreaShape.Line, LengthTiles = 8f, WidthTiles = 1.8f,
                TelegraphMs = 1700, CooldownMs = 13000, DamageMult = 1.65f, KnockbackTiles = 1f, CastRow = 1 },
            new WantedSkillDef { Id = 3, Name = "Bloom of the Hollow", Hint = "The hall blooms with light. Stand in the safe lane.", Shape = WantedAreaShape.Circle, RadiusTiles = 6.5f, SafeArcDeg = 90f, SafeArcCount = 1,
                TelegraphMs = 2300, CooldownMs = 22000, DamageMult = 2.2f, StatusKind = StatusKind.Freeze, StatusMs = 1200, CastRow = 2, Enrage = true },
        };

        /// <summary>
        /// Aetherglyph Owl, Rootburrow's Wanted boss (21/11/2026, ChatGPT section B, docs/reference/rootburrow-dark-25d/): the reach at the top of its band
        /// (user: grand) and the 17 x 17 arena. Crystal Judgment is one line toward the target (lines are anchored at the boss here). Light FX:
        /// CaveBossLightFx + AetherglyphOwlLightCasts. Cast rows of aetherglyphOwl-cast.png: 0 wings folded focus (starfall), 1 wings spread (wingstorm,
        /// judgment), 2 wings arched + crown blazing (eclipse), 3 defeat.
        /// </summary>
        public static readonly WantedSkillDef[] AetherglyphOwl =
        {
            new WantedSkillDef { Id = 0, Name = "Rune Starfall", Hint = "Light falls on five hunters. Leave the red circles.", Shape = WantedAreaShape.Circle, OnPlayers = true, RadiusTiles = 2f, Count = 5,
                TelegraphMs = 1600, CooldownMs = 9000, DamageMult = 1.35f, StatusKind = StatusKind.Slow, StatusValue = 25f, StatusMs = 2000, CastRow = 0 },
            new WantedSkillDef { Id = 1, Name = "Astral Wingstorm", Hint = "A storm of wings in front. Get behind the owl.", Shape = WantedAreaShape.Cone, RadiusTiles = 7f, AngleDeg = 110f,
                TelegraphMs = 1800, CooldownMs = 13000, DamageMult = 1.65f, KnockbackTiles = 1.2f, CastRow = 1 },
            new WantedSkillDef { Id = 2, Name = "Crystal Judgment", Hint = "Crystals burst along a line. Step out of it.", Shape = WantedAreaShape.Line, LengthTiles = 10f, WidthTiles = 1.8f,
                TelegraphMs = 2000, CooldownMs = 16000, DamageMult = 1.85f, StatusKind = StatusKind.Freeze, StatusMs = 1000, CastRow = 1 },
            new WantedSkillDef { Id = 3, Name = "Eclipse of Runes", Hint = "The whole arena erupts. Stand in the safe lane.", Shape = WantedAreaShape.Circle, RadiusTiles = 8f, SafeArcDeg = 150f, SafeArcCount = 1,
                TelegraphMs = 2800, CooldownMs = 28000, DamageMult = 2.5f, StatusKind = StatusKind.Slow, StatusValue = 40f, StatusMs = 3000, CastRow = 2, Enrage = true },
        };

        /// <summary>The skill table (4 skills; Mire Hydra Queen 5) of a wanted boss type, or null when it has none (every other monster).</summary>
        public static WantedSkillDef[] For(string bossType)
        {
            switch (bossType)
            {
                case "rimeclawTyrant": return RimeclawTyrant;
                case "glacialColossus": return GlacialColossus;
                case "mireHydraQueen": return MireHydraQueen;
                case "ambercapWarden": return AmbercapWarden;
                case "aetherglyphOwl": return AetherglyphOwl;
                default: return null;
            }
        }
    }
}
