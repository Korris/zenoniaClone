namespace ZenoniaProto.Rpg
{
    public enum DuelNpcLevel { Easy = 0, Normal = 1, Hard = 2 }

    public sealed class DuelNpcDef
    {
        public readonly string Id, Name, Portrait;
        public readonly DuelNpcLevel Level;
        public readonly string[] Deck;
        public readonly int FirstWinGold;

        public DuelNpcDef(string id, string name, DuelNpcLevel level, string[] deck, int firstWinGold, string portrait)
        {
            Id = id; Name = name; Level = level; Deck = deck; FirstWinGold = firstWinGold; Portrait = portrait;
        }
    }

    /// <summary>
    /// The three practice opponents (plan phase 05). The index is the wire value and the bit of CardCollection.NpcFirstWins, so the order is append-only.
    /// Hard plays the same greedy AI as Normal but with a stronger deck (a boss and two SR cards). Every deck follows DeckRules (checked by a test).
    /// </summary>
    public static class DuelNpcTable
    {
        public const int DailyWinsNeeded = 3, DailyBonusGold = 500;

        public static readonly DuelNpcDef[] All =
        {
            new DuelNpcDef("meadowApprentice", "Meadow Apprentice", DuelNpcLevel.Easy,
                new[] { "meadowSlime", "hornRabbit", "thornBoar", "meadowMantis", "forestWolf", "brambleTreant", "thornSprite", "caveMantis" }, 1000, "cards/board/npc-meadow-apprentice"),
            new DuelNpcDef("frostScoutNia", "Frost Scout Nia", DuelNpcLevel.Normal,
                new[] { "gloomWolf", "snowFox", "frostBear", "frostSlime", "iceGoblin", "spectralWolf", "hollowTreant", "sandRaider" }, 2500, "cards/board/npc-frost-scout-nia"),
            new DuelNpcDef("emberVeteran", "Ember Veteran", DuelNpcLevel.Hard,
                new[] { "sandRaider", "amberWisp", "thornfangAlpha", "gloomWolf", "spectralWolf", "iceGoblin", "frostbiteYeti", "stonebackGuardian" }, 5000, "cards/board/npc-ember-veteran"),
        };

        public static int Count { get { return All.Length; } }
        public static DuelNpcDef Get(int i) { return i >= 0 && i < All.Length ? All[i] : null; }
        public static int FirstWinBit(int npc) { return 1 << npc; }
    }
}
