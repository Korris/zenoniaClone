namespace ZenoniaProto.Rpg
{
    /// <summary>One scripted placement of the tutorial duel. Side 0 = the player, 1 = Orin. Title / Body are what the lesson banner says (English UI text).</summary>
    public sealed class DuelScriptStep
    {
        public readonly int Side, Cell;
        public readonly string CardId, Title, Body;
        public DuelScriptStep(int side, string cardId, int cell, string title, string body) { Side = side; CardId = cardId; Cell = cell; Title = title; Body = body; }
    }

    /// <summary>
    /// The first card duel with Card Master Orin (Taurus V4): both decks are fixed 8 Common cards and nothing is shuffled. The player's deck = the 5 starter cards (TaurusQuests.StarterCards) + 3 cards BORROWED from Orin
    /// (they exist only in this duel: the deck is built here, never from the collection, so nothing borrowed can reach My Cards, PvP or a trade, and a dropped connection cannot leave or duplicate a card).
    /// Orin plays the same 8 cards, and the player moves first. The first 5 turns are scripted (3 lessons for the player, 2 replies by Orin): place a card, capture, an edge facing an empty cell is a risk. From turn 6 the duel is
    /// free and Orin uses DuelNpcAi. DuelTutorialScriptTests plays the script on a real DuelMatch and checks every flip below.
    /// </summary>
    public static class DuelTutorialScript
    {
        public const string OrinName = "Card Master Orin";

        /// <summary>The three cards Orin lends for the duel, on top of the 5 starter cards.</summary>
        public static readonly string[] Borrowed = { "tauBrambleBoar", "tauRootling", "tauAmberSporeling" };

        public static readonly string[] PlayerDeck = { "tauMossMushroom", "tauHornRabbit", "tauLeafMantis", "tauCloverSlime", "tauReedFrog", "tauBrambleBoar", "tauRootling", "tauAmberSporeling" };
        /// <summary>Orin's deck is also the list the winner picks the reward from (CardDuelQuest.RewardPickCards): 8 Common cards, 3 of them (Borrowed) not in the starter deck.</summary>
        public static readonly string[] OrinDeck = { "tauCloverSlime", "tauBrambleBoar", "tauHornRabbit", "tauReedFrog", "tauMossMushroom", "tauLeafMantis", "tauRootling", "tauAmberSporeling" };

        // cells: A1 = 0, A2 = 1, B1 = 4, B2 = 5, C1 = 8 (letter = row, number = column)
        public static readonly DuelScriptStep[] Steps =
        {
            new DuelScriptStep(0, "tauMossMushroom", 0, "Place a card", "Pick Moss Mushroom and tap A1. Each card has 4 numbers, one per side. A corner keeps two of its sides safe against the edge of the board."),
            new DuelScriptStep(1, "tauCloverSlime", 5, "Orin's turn", "Orin places Clover Slime at B2. Its Left edge is only 3: a weak side facing an empty cell is a target."),
            new DuelScriptStep(0, "tauHornRabbit", 4, "Capture", "Horn Rabbit at B1: its Right edge (7) beats the Slime's Left edge (3), so the Slime flips to your side."),
            new DuelScriptStep(1, "tauBrambleBoar", 8, "Orin strikes back", "Bramble Boar at C1: its Top edge (6) beats your Rabbit's Bottom edge (2). Placing next to a card does not always win."),
            new DuelScriptStep(0, "tauLeafMantis", 1, "Defend", "Leaf Mantis at A2: its open side is the strong Right edge (6), and the weak sides touch your own cards or the board edge."),
        };

        /// <summary>Number of lessons the player plays (steps of side 0).</summary>
        public static int Lessons { get { int n = 0; foreach (DuelScriptStep s in Steps) if (s.Side == 0) n++; return n; } }

        /// <summary>The lesson (1 ..) a script position belongs to: how many player steps come before `pos`, + 1. 0 when the script is over.</summary>
        public static int LessonAt(int pos)
        {
            if (pos < 0 || pos >= Steps.Length) return 0;
            int n = 1;
            for (int i = 0; i < pos; i++) if (Steps[i].Side == 0) n++;
            return n;
        }
    }
}
