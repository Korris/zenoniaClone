using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// The monster cards: 40 pre-Taurus (35 normal monsters + 5 bosses, hidden from the Album) then the 12 Taurus cards (8 N, 3 R, 1 SR boss) then the Matriarch Field Boss card (R, outside the Album), order = album index (append only, saves store the index in the album bitmask).
    /// Pure data: ids are MonsterDef type ids, checked against MonsterDef.All by CardIds debug check on the Unity side and by CardIdsExportTests against server-api card-ids.json.
    /// Rarity follows the monster's itemLevel: up to 10 N, 11-20 R, 21+ SR, bosses SSR. Regions are map ids (Rootburrow covers its three floors).
    /// </summary>
    public static partial class CardTable
    {
        public const int Count = 53, LiveCount = 12, LiveStart = 40; // Count = every row (saves index them), LiveCount = the Taurus album rows (indexes LiveStart..LiveStart+11); row 52 = the Field Boss card, outside the album
        public const int MaxStack = 999; // copies of one card (and of one gem kind) a character keeps

        /// <summary>Edge totals per rarity (N, R, SR, SSR), inclusive.</summary>
        public static readonly int[] EdgeSumMin = { 14, 18, 22, 26 }, EdgeSumMax = { 18, 22, 26, 30 };

        /// <summary>Regions of the live game (Taurus V4): the Album and its tabs list only these.</summary>
        public static readonly string[] Regions = { "tauCloverMeadows", "tauAncientRootShrine" };

        /// <summary>Pre-Taurus map ids, in their old tab order: their 40 cards stay in the table (append-only indexes) but are hidden from the Album.</summary>
        public static readonly string[] LegacyRegions =
        {
            "willowbrookMeadows", "bramblewoodForest", "rootburrow", "frostHollow", "frostfangDen", "amberDunes", "emberfenMarsh", "sunkenReliquary", "sunbreakFoothills", "scorpionGorge",
        };

        /// <summary>Region of the Field Boss cards (Brambleback Matriarch): droppable and playable, but not in the Album, so AlbumCount stays out of 12 (phase 03b).</summary>
        public const string FieldBossRegion = "tauFieldBoss";

        /// <summary>True for the 12 Taurus album cards (Album, milestones); the Field Boss card is not live.</summary>
        public static bool IsLive(CardDef c) { return c != null && System.Array.IndexOf(Regions, c.Region) >= 0; }

        static readonly Dictionary<string, int> IndexById = new Dictionary<string, int>(Count);

        // A static constructor runs after every field initializer of this partial class, whatever order the files compile in (All lives in CardTableData.cs).
        static CardTable() { for (int i = 0; i < All.Length; i++) IndexById[All[i].Id] = i; }

        public static CardDef Get(string id) { int i; return id != null && IndexById.TryGetValue(id, out i) ? All[i] : null; }
        public static int Index(string id) { int i; return id != null && IndexById.TryGetValue(id, out i) ? i : -1; }
        public static CardDef ByIndex(int i) { return i >= 0 && i < All.Length ? All[i] : null; }
        public static bool IsCardId(string id) { return Index(id) >= 0; }
    }
}
