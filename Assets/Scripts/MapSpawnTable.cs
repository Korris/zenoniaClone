using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Which monsters a map spawns, by map id: Amber Dunes and Emberfen Marsh have their own zones and no boss, Frostfang Den and Sunken Reliquary their own zones and boss; every
    /// other hostile map keeps Frost Hollow's zones and the Frostfang King (the online zone server, the headless bench and the Editor tests depend on those). Keyed by string so the
    /// data needs no map class. A boss map's kill is saved per map (RpgSession.BossDefeatedOn); only a map with a boss gate hides its forward portal until then.
    /// </summary>
    public static class MapSpawnTable
    {
        public const string AmberDunesId = "amberDunes", FrostfangDenId = "frostfangDen", EmberfenMarshId = "emberfenMarsh", SunkenReliquaryId = "sunkenReliquary";
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

        /// <summary>A few guards on the way to the boss: goblins in the north cave, wolves in the south cave. The arena itself only holds the boss.</summary>
        static readonly FrostHollowMap.Zone[] FrostfangDen =
        {
            new FrostHollowMap.Zone("iceGoblin", 4, 8, 3, 10, 6, 10000f),
            new FrostHollowMap.Zone("spectralWolf", 3, 4, 12, 11, 6, 11000f),
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

        public static FrostHollowMap.Zone[] ZonesFor(string mapId)
        {
            switch (mapId)
            {
                case AmberDunesId: return AmberDunes;
                case FrostfangDenId: return FrostfangDen;
                case EmberfenMarshId: return EmberfenMarsh;
                case SunkenReliquaryId: return SunkenReliquary;
                default: return FrostHollowMap.Zones;
            }
        }

        public static bool HasBoss(string mapId) { return BossFor(mapId) != null; }

        /// <summary>The boss a map places once, or null.</summary>
        public static MonsterDef BossFor(string mapId)
        {
            switch (mapId)
            {
                case AmberDunesId: case EmberfenMarshId: return null;
                case FrostfangDenId: return MonsterDef.GlacierWarden;
                case SunkenReliquaryId: return MonsterDef.MirefangHydra;
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
                default: return FrostHollowMap.BossTile;
            }
        }

        /// <summary>True for a map with a boss of its own, whose kill is saved in SaveData.defeatedBossMaps; the king's kill stays in SaveData.bossDefeated.</summary>
        public static bool HasOwnBoss(string mapId) { return mapId == FrostfangDenId || mapId == SunkenReliquaryId; }

        /// <summary>True when the map's forward portal stays closed until its boss is dead. Add Sunken Reliquary here when it gets an exit.</summary>
        public static bool HasBossGate(string mapId) { return mapId == FrostfangDenId; }

        /// <summary>The map whose gate a save positioned on `mapId` has already passed, or null. Saves from before the Den existed reached these maps without a kill.</summary>
        public static string GatePassedOn(string mapId)
        {
            return mapId == SunspireOasisMap.MapId || mapId == AmberDunesId || mapId == EmberfenMarshId || mapId == SunkenReliquaryId ? FrostfangDenId : null;
        }

        /// <summary>
        /// True when a spot of a table-driven map is too close to where heroes arrive (its portals, its start tile). Monsters are placed before the
        /// hero moves in, and these rects are not hand-fitted to the entrance like Frost Hollow's, whose spots are never rejected here.
        /// </summary>
        public static bool NearEntrance(IGameMap map, Vector2 feet)
        {
            if (map.Id != AmberDunesId && map.Id != FrostfangDenId && map.Id != EmberfenMarshId && map.Id != SunkenReliquaryId) return false;
            if (Vector2.Distance(feet, map.PlayerStart) < EntranceClearance) return true;
            foreach (MapPortal p in map.Portals)
                if (Vector2.Distance(feet, FrostHollowMap.TileFeet(p.Tx, p.Ty, 2f)) < EntranceClearance) return true;
            return false;
        }
    }
}
