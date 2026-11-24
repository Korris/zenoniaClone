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
    /// The first card duel with Card Master Orin: both decks are fixed (8 common cards each, the player's is BORROWED from Orin so nothing in the collection is touched), nothing is shuffled and the player moves first.
    /// The first 5 turns are scripted (3 lessons for the player, 2 replies by Orin): place a card, capture, defend. From turn 6 the duel is free and Orin uses DuelNpcAi.
    /// DuelTutorialScriptTests plays the script on a real DuelMatch and checks every flip below.
    /// </summary>
    public static class DuelTutorialScript
    {
        public const string OrinName = "Card Master Orin";

        public static readonly string[] PlayerDeck = { "thornBoar", "hornRabbit", "brambleTreant", "meadowMantis", "forestWolf", "meadowSlime", "thornSprite", "caveMantis" };
        public static readonly string[] OrinDeck = { "meadowSlime", "forestWolf", "caveMantis", "thornSprite", "hornRabbit", "thornBoar", "meadowMantis", "brambleTreant" };

        // cells: A1 = 0, A2 = 1, B1 = 4, B2 = 5, C1 = 8
        public static readonly DuelScriptStep[] Steps =
        {
            new DuelScriptStep(0, "thornBoar", 0, "Place a card", "Pick Thorn Boar and tap A1. Each card has 4 numbers, one per side. A corner keeps two of its sides safe against the edge of the board."),
            new DuelScriptStep(1, "meadowSlime", 5, "Orin's turn", "Orin places Meadow Slime at B2."),
            new DuelScriptStep(0, "hornRabbit", 4, "Capture", "Horn Rabbit at B1: its Right edge (7) beats the Slime's Left edge (4), so the Slime flips to your side."),
            new DuelScriptStep(1, "forestWolf", 8, "Orin strikes back", "Forest Wolf at C1: its Top edge (7) beats your Rabbit's Bottom edge (2). A weak edge facing an empty cell is a risk."),
            new DuelScriptStep(0, "meadowMantis", 1, "Defend", "Meadow Mantis at A2: its open side is the strong Right edge (8), and the weak sides touch your own cards or the board edge."),
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
