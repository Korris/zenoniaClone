using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// The 40 monster cards (35 normal monsters + 5 bosses), order = album index (append only, saves store the index in the album bitmask).
    /// Pure data: ids are MonsterDef type ids, checked against MonsterDef.All by CardIds debug check on the Unity side and by CardIdsExportTests against server-api card-ids.json.
    /// Rarity follows the monster's itemLevel: up to 10 N, 11-20 R, 21+ SR, bosses SSR. Regions are map ids (Rootburrow covers its three floors).
    /// </summary>
    public static partial class CardTable
    {
        public const int Count = 40;
        public const int MaxStack = 999; // copies of one card (and of one gem kind) a character keeps

        /// <summary>Edge totals per rarity (N, R, SR, SSR), inclusive.</summary>
        public static readonly int[] EdgeSumMin = { 14, 18, 22, 26 }, EdgeSumMax = { 18, 22, 26, 30 };

        /// <summary>Map ids that hold cards, in tab order.</summary>
        public static readonly string[] Regions =
        {
            "willowbrookMeadows", "bramblewoodForest", "rootburrow", "frostHollow", "frostfangDen", "amberDunes", "emberfenMarsh", "sunkenReliquary", "sunbreakFoothills", "scorpionGorge",
        };

        static readonly Dictionary<string, int> IndexById = new Dictionary<string, int>(Count);

        // A static constructor runs after every field initializer of this partial class, whatever order the files compile in (All lives in CardTableData.cs).
        static CardTable() { for (int i = 0; i < All.Length; i++) IndexById[All[i].Id] = i; }

        public static CardDef Get(string id) { int i; return id != null && IndexById.TryGetValue(id, out i) ? All[i] : null; }
        public static int Index(string id) { int i; return id != null && IndexById.TryGetValue(id, out i) ? i : -1; }
        public static CardDef ByIndex(int i) { return i >= 0 && i < All.Length ? All[i] : null; }
        public static bool IsCardId(string id) { return Index(id) >= 0; }
    }
}
