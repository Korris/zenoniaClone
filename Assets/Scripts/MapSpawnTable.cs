using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Which monsters a map spawns, by map id: Amber Dunes, Emberfen Marsh and Willowbrook Meadows have their own zones and no boss, Frostfang Den and Sunken Reliquary their own zones and boss; every
    /// other hostile map keeps Frost Hollow's zones (the five old ones in its north-west valley plus the ten outer clusters) and the Frostfang King (the online zone server, the headless bench and the Editor tests depend on those). Keyed by string so the
    /// data needs no map class. A boss map's kill is saved per map (RpgSession.BossDefeatedOn) for the boss's own rewards; World Map 2.0 step 1 (13/11/2026) dropped the last portal gated on a kill
    /// (Frostfang Den -> Sunspire), so no map hides a portal any more (see HasOwnBoss / BossFor; there used to be HasBossGate and GatePassedOn here, both removed).
    /// </summary>
    public static class MapSpawnTable
    {
        public const string AmberDunesId = "amberDunes", FrostfangDenId = "frostfangDen", EmberfenMarshId = "emberfenMarsh", SunkenReliquaryId = "sunkenReliquary", WillowbrookMeadowsId = "willowbrookMeadows";
        public const string BramblewoodForestId = "bramblewoodForest", RootburrowId = "rootburrow", ThornheartChamberId = "thornheartChamber";
        static readonly Vector2Int BramblewoodForestBossTile = new Vector2Int(24, 18), ThornheartChamberBossTile = new Vector2Int(12, 5);
        const float EntranceClearance = 80f; // units; just over the default aggro range, so nothing wakes up as the hero walks in
        static readonly Vector2Int FrostfangDenBossTile = new Vector2Int(26, 11), SunkenReliquaryBossTile = new Vector2Int(30, 14);

        /// <summary>Tile rects are wide on purpose: TryZoneSpot skips solid tiles. Scarabs in the first field and the south of the middle one, raiders north of it, wisps in the last field.</summary>
        static readonly FrostHollowMap.Zone[] AmberDunes =
        {
            new FrostHollowMap.Zone("duneScarab", 5, 3, 8, 7, 11, 8000f),
            new FrostHollowMap.Zone("duneScarab", 3, 16, 16, 6, 5, 9000f),
            new FrostHollowMap.Zone("sandRaider", 4, 16, 2, 6, 10, 10000f),
            new FrostHollowMap.Zone("sandRaider", 2, 28, 2, 5, 6, 10000f),
            new FrostHollowMap.Zone("amberWisp", 4, 28, 8, 5, 8, 11000f),
            new FrostHollowMap.Zone("amberWisp", 2, 27, 18, 6, 3, 11000f),
        };

        /// <summary>Public-dungeon rules until instances exist (design answer 13/11/2026): dungeon guards come back after 25 s (fields: 6-11 s), a dungeon's end boss after 5 min online
        /// (field bosses: BossRespawnTimer.DefaultRespawnMs, 30 min). Elites (90 s) are not in the game yet.</summary>
        // The zone tables below write 25000f as a literal: the map tests and Tools/ read the rects with a regex.
        public const float DungeonRespawnMs = 25000f, DungeonBossRespawnMs = 5f * 60f * 1000f;

        /// <summary>How long after its death the map's boss returns online.</summary>
        public static float BossRespawnMs(string mapId) { return mapId == FrostfangDenId || mapId == ThornheartChamberId ? DungeonBossRespawnMs : BossRespawnTimer.DefaultRespawnMs; }

        /// <summary>A few guards on the way to the boss: Lv16 goblins in the north cave, Lv18 wolves in the south cave (World Map 2.0 step 1, 13/11/2026 moved both from Lv2 to fill the Lv16-20
        /// band; Glacier Warden stays Lv20). The arena itself only holds the boss.</summary>
        static readonly FrostHollowMap.Zone[] FrostfangDen =
        {
            new FrostHollowMap.Zone("iceGoblin", 4, 8, 3, 10, 6, 25000f),
            new FrostHollowMap.Zone("spectralWolf", 3, 4, 12, 11, 6, 25000f),
        };

        /// <summary>Spread along the path: slimes in the south field, toads in the middle one, stalkers on its far side and in the north field.</summary>
        static readonly FrostHollowMap.Zone[] EmberfenMarsh =
        {
            new FrostHollowMap.Zone("bogSlime", 4, 2, 16, 15, 4, 8000f),
            new FrostHollowMap.Zone("bogSlime", 3, 23, 16, 10, 4, 8000f),
            new FrostHollowMap.Zone("mireToad", 4, 2, 8, 9, 4, 10000f),
            new FrostHollowMap.Zone("mireToad", 3, 15, 8, 7, 4, 10000f),
            new FrostHollowMap.Zone("marshStalker", 3, 25, 8, 8, 2, 11000f),
            new FrostHollowMap.Zone("marshStalker", 3, 14, 1, 16, 4, 11000f),
        };

        /// <summary>Wisps in every room along the causeway; the arena itself only holds the boss.</summary>
        static readonly FrostHollowMap.Zone[] SunkenReliquary =
        {
            new FrostHollowMap.Zone("ruinWisp", 3, 4, 2, 9, 7, 11000f),
            new FrostHollowMap.Zone("ruinWisp", 3, 18, 2, 8, 6, 11000f),
            new FrostHollowMap.Zone("ruinWisp", 2, 30, 2, 4, 3, 11000f),
            new FrostHollowMap.Zone("ruinWisp", 4, 4, 13, 17, 6, 11000f),
        };

        /// <summary>
        /// The 72x48 meadow hunting field, 70 monsters in 11 clusters (plan layout/willowbrook-72x48-clusters.json, adjusted to the painting), the level climbing with the walking distance from the west gate:
        /// meadowSlime next to the gate, hornRabbit across the first stream, in the north-west and the east meadow, thornBoar in the middle and the far east (World Map 2.1, 13/11/2026: the band is
        /// Lv1-5; meadowMantis and brambleTreant, which used to hold the three far clusters, now live in Bramblewood Forest). Respawn 6-10 s (slower far from the gate). Rects are wide on purpose
        /// (TryZoneSpot skips solid tiles; each holds at least 7x its count of free tiles). The west gate and start tile stay clear (NearEntrance).
        /// </summary>
        static readonly FrostHollowMap.Zone[] WillowbrookMeadows =
        {
            new FrostHollowMap.Zone("meadowSlime", 6, 4, 14, 9, 5, 6000f),     // 1  north of the gate, old section
            new FrostHollowMap.Zone("meadowSlime", 6, 12, 27, 12, 5, 6500f),   // 2  south-west meadow
            new FrostHollowMap.Zone("hornRabbit", 5, 25, 15, 8, 6, 7000f),     // 3  across the stream, old section
            new FrostHollowMap.Zone("hornRabbit", 6, 4, 3, 12, 6, 7500f),      // 4  north-west plain
            new FrostHollowMap.Zone("hornRabbit", 5, 28, 3, 7, 6, 6000f),      // 5  north, east of the stream
            new FrostHollowMap.Zone("thornBoar", 7, 40, 4, 12, 7, 8000f),      // 6  middle north
            new FrostHollowMap.Zone("thornBoar", 6, 39, 14, 14, 6, 8500f),     // 7  middle, north of the road
            new FrostHollowMap.Zone("thornBoar", 6, 45, 30, 9, 7, 9000f),      // 8  above the pond
            // World Map 2.1 (13/11/2026): mantis and treant moved to Bramblewood Forest, so Willowbrook is Lv1-5 only; the three far clusters keep their counts (70 in all).
            new FrostHollowMap.Zone("thornBoar", 8, 57, 3, 11, 8, 9500f),      // 9  ruins, north-east
            new FrostHollowMap.Zone("hornRabbit", 8, 57, 15, 11, 8, 9000f),    // A  east meadow
            new FrostHollowMap.Zone("thornBoar", 7, 60, 30, 10, 9, 10000f),    // B  south-east
        };

        /// <summary>
        /// Bramblewood Forest (Lv6-10, World Map 2.1): 30 monsters in its three clearings, the level climbing with the walk from the top road (Willowbrook): north-west mantis Lv6 + wolves Lv7,
        /// north-east (over the bridge) wolves Lv7 + treants Lv8, the big south clearing treants Lv8 + sprites Lv9 around Thornfang Alpha (map boss, BossFor). Respawn 6-9 s. Every rect is all
        /// open ground (at least 3x its monsters) and 5+ tiles from the top road, the cave mouth and the start tile.
        /// </summary>
        static readonly FrostHollowMap.Zone[] BramblewoodForest =
        {
            new FrostHollowMap.Zone("meadowMantis", 5, 7, 4, 6, 5, 6000f),     // 1  north-west clearing
            new FrostHollowMap.Zone("forestWolf", 5, 7, 4, 6, 5, 6500f),
            new FrostHollowMap.Zone("forestWolf", 4, 24, 6, 9, 6, 7000f),      // 2  north-east clearing
            new FrostHollowMap.Zone("brambleTreant", 6, 24, 6, 9, 6, 8000f),
            new FrostHollowMap.Zone("brambleTreant", 4, 16, 16, 11, 5, 8500f), // 3  south clearing (boss at 24,18)
            new FrostHollowMap.Zone("thornSprite", 6, 16, 16, 11, 5, 9000f),
        };

        /// <summary>
        /// Rootburrow (Lv10 dungeon): 16 guards from the hard end of Bramblewood's list, respawning every DungeonRespawnMs like Frostfang Den's (no instances yet): wolves and treants in the east hall behind the
        /// entrance crack, sprites in the north hall and in the west hall in front of the boss arch. The boss stands in Thornheart Chamber, not here.
        /// </summary>
        static readonly FrostHollowMap.Zone[] Rootburrow =
        {
            new FrostHollowMap.Zone("forestWolf", 2, 18, 12, 8, 5, 25000f),    // 1  east hall
            new FrostHollowMap.Zone("brambleTreant", 3, 18, 12, 8, 5, 25000f),
            new FrostHollowMap.Zone("thornSprite", 5, 15, 3, 9, 4, 25000f),    // 2  north hall
            new FrostHollowMap.Zone("thornSprite", 3, 6, 12, 7, 5, 25000f),    // 3  west hall
            new FrostHollowMap.Zone("brambleTreant", 3, 6, 12, 7, 5, 25000f),
        };

        public const string SunbreakFoothillsId = "sunbreakFoothills";
        static readonly Vector2Int SunbreakFoothillsBossTile = new Vector2Int(38, 4);

        /// <summary>
        /// Sunbreak Foothills (48x32, Lv16-20): 50 monsters in 8 rects over its fields, the level climbing with the walk from the south road (Frost Hollow): ibex Lv16 in the two southern fields,
        /// lizards Lv17 west of the stream and in the south of the central field (over either bridge), harpies Lv18 in its north and in the north-west field, guardians Lv20 in the east field
        /// on the way to Sunspire. Ridgehorn Behemoth (map boss, BossFor) stands alone on the north-east plateau. Respawn 7-10 s. Every rect holds at least 4x its count in free tiles and is
        /// 5+ tiles from the three arrival tiles and their portals; no prop stands inside one (Tools/preview-sunbreak-foothills-composite.py checks all of it).
        /// </summary>
        static readonly FrostHollowMap.Zone[] SunbreakFoothills =
        {
            new FrostHollowMap.Zone("highlandIbex", 8, 4, 20, 11, 6, 7000f),       // 1  south-west field (snow side)
            new FrostHollowMap.Zone("highlandIbex", 5, 27, 24, 6, 5, 7000f),       // 2  south field, between the road and the stream
            new FrostHollowMap.Zone("dustscaleLizard", 5, 4, 11, 7, 4, 8000f),     // 3  field west of the stream
            new FrostHollowMap.Zone("dustscaleLizard", 7, 18, 13, 8, 5, 8000f),    // 4  central field, south half
            new FrostHollowMap.Zone("cliffHarpy", 5, 26, 10, 6, 4, 9000f),         // 5  central field, north-east
            new FrostHollowMap.Zone("cliffHarpy", 8, 10, 4, 9, 5, 9000f),          // 6  north-west field
            new FrostHollowMap.Zone("stonebackGuardian", 6, 32, 19, 6, 5, 10000f), // 7  east field, west half
            new FrostHollowMap.Zone("stonebackGuardian", 6, 38, 20, 6, 5, 10000f), // 8  east field, east half
        };

        static readonly FrostHollowMap.Zone[] NoZones = new FrostHollowMap.Zone[0];

        /// <summary>
        /// Frost Hollow's 66 monsters outside the original 36x24 section, in 12 clusters on the painted 72x48 rows (plan layout/frost-hollow-72x48-clusters.md, rects fitted to the walkable tiles).
        /// FrostHollowMap.Zones stays the five old ones (the bots and the bench use them directly); ZonesFor("frostHollow") is both. World Map 2.0 step 1 (13/11/2026) moved Frost Hollow to a Lv11-15
        /// band: Lv11 frost slime in the old valley and the south-west, Lv12 snow foxes / imps, Lv13 bears, Lv13 wraiths, Lv14 yetis and Lv15 golems by the ice cave and on the watchtower plateau
        /// (stairs at (49-50,5-6) stay free). Zone 2 (south-west plaza) is ice goblin, which moved to Frostfang Den's Lv16 band instead (shared MonsterDef, so this pocket is a level spike by design
        /// for now; same for the old valley's ice goblin / spectral wolf, FrostHollowMap.Zones). Respawn 7-10.5 s. Every rect is at least 5 tiles from the start, the gate and the cave.
        /// </summary>
        public static readonly FrostHollowMap.Zone[] FrostHollowOuter =
        {
            new FrostHollowMap.Zone("frostSlime", 6, 2, 27, 10, 5, 7000f),       // 1  south of the village gate
            new FrostHollowMap.Zone("iceGoblin", 6, 3, 37, 12, 6, 8000f),        // 2  south-west plaza
            new FrostHollowMap.Zone("snowFox", 6, 26, 28, 10, 6, 7500f),         // 3  south bank, east of the river (bridge 2)
            new FrostHollowMap.Zone("snowFox", 5, 38, 8, 10, 7, 7500f),          // 4  north-middle field, east of the old valley
            new FrostHollowMap.Zone("iceImp", 6, 40, 19, 12, 5, 9000f),          // 5  north shore of the lake
            new FrostHollowMap.Zone("frostBear", 5, 48, 8, 9, 5, 9500f),         // 6  north field, under the plateau
            new FrostHollowMap.Zone("iceWraith", 6, 57, 7, 9, 7, 9500f),         // 7  north-east field
            new FrostHollowMap.Zone("frostbiteYeti", 6, 40, 37, 14, 6, 10000f),  // 8  south of the lake
            new FrostHollowMap.Zone("crystalGolem", 5, 56, 15, 6, 9, 10500f),    // 9  approach to the ice cave
            new FrostHollowMap.Zone("frostbiteYeti", 5, 58, 38, 10, 6, 10000f),  // A  south-east corner
            new FrostHollowMap.Zone("crystalGolem", 5, 43, 1, 13, 3, 10500f),    // B  watchtower plateau (stairs below at 49-50,5-6)
            new FrostHollowMap.Zone("frostBear", 5, 25, 38, 10, 6, 9500f),       // C  south-middle, west of the lake
        };

        static readonly FrostHollowMap.Zone[] FrostHollowAll = Join(FrostHollowMap.Zones, FrostHollowOuter);

        static FrostHollowMap.Zone[] Join(FrostHollowMap.Zone[] a, FrostHollowMap.Zone[] b)
        {
            var all = new FrostHollowMap.Zone[a.Length + b.Length];
            a.CopyTo(all, 0); b.CopyTo(all, a.Length);
            return all;
        }

        public static FrostHollowMap.Zone[] ZonesFor(string mapId)
        {
            switch (mapId)
            {
                case AmberDunesId: return AmberDunes;
                case FrostfangDenId: return FrostfangDen;
                case EmberfenMarshId: return EmberfenMarsh;
                case SunkenReliquaryId: return SunkenReliquary;
                case WillowbrookMeadowsId: return WillowbrookMeadows;
                case BramblewoodForestId: return BramblewoodForest;
                case RootburrowId: return Rootburrow;
                case ThornheartChamberId: return NoZones; // only the boss
                case SunbreakFoothillsId: return SunbreakFoothills;
                default: return FrostHollowAll;
            }
        }

        public static bool HasBoss(string mapId) { return BossFor(mapId) != null; }

        /// <summary>The boss a map places once, or null.</summary>
        public static MonsterDef BossFor(string mapId)
        {
            switch (mapId)
            {
                case AmberDunesId: case EmberfenMarshId: case WillowbrookMeadowsId: return null;
                case FrostfangDenId: return MonsterDef.GlacierWarden;
                case SunkenReliquaryId: return MonsterDef.MirefangHydra;
                case RootburrowId: return null;
                case BramblewoodForestId: return MonsterDef.ThornfangAlpha;
                case ThornheartChamberId: return MonsterDef.ThornheartTreant;
                case SunbreakFoothillsId: return MonsterDef.RidgehornBehemoth;
                default: return MonsterDef.FrostfangKing;
            }
        }

        /// <summary>Home tile of the map's boss (see BossFor).</summary>
        public static Vector2Int BossTileFor(string mapId)
        {
            switch (mapId)
            {
                case FrostfangDenId: return FrostfangDenBossTile;
                case SunkenReliquaryId: return SunkenReliquaryBossTile;
                case BramblewoodForestId: return BramblewoodForestBossTile;
                case ThornheartChamberId: return ThornheartChamberBossTile;
                case SunbreakFoothillsId: return SunbreakFoothillsBossTile;
                default: return FrostHollowMap.BossTile;
            }
        }

        /// <summary>True for a map with a boss of its own, whose kill is saved in SaveData.defeatedBossMaps; the king's kill stays in SaveData.bossDefeated.</summary>
        public static bool HasOwnBoss(string mapId) { return mapId == FrostfangDenId || mapId == SunkenReliquaryId || mapId == BramblewoodForestId || mapId == ThornheartChamberId || mapId == SunbreakFoothillsId; }

        /// <summary>
        /// True when a spot of a table-driven map is too close to where heroes arrive (its portals, its start tile). Monsters are placed before the
        /// hero moves in, and these rects are not hand-fitted to the entrance like Frost Hollow's, whose spots are never rejected here.
        /// </summary>
        public static bool NearEntrance(IGameMap map, Vector2 feet)
        {
            if (Vector2.Distance(feet, map.PlayerStart) < EntranceClearance) return true;
            foreach (MapPortal p in map.Portals)
                if (Vector2.Distance(feet, FrostHollowMap.TileFeet(p.Tx, p.Ty, 2f)) < EntranceClearance) return true;
            return false;
        }
    }
}
