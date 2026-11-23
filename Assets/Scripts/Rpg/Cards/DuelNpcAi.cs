using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// The practice opponent's brain. It only ever looks at its own hand and the board (the player's hand is hidden from it, as from a human) and only picks moves the match accepts.
    /// Easy: a random one of its 3 best moves. Normal / Hard: the move that flips the most; on a tie the one that leaves the fewest weak edges facing an empty cell; still tied, a random one.
    /// </summary>
    public static class DuelNpcAi
    {
        struct Option { public int Hand, Cell, Flips, Risk; }

        /// <summary>The (hand index, cell) to play. False when there is no legal move (never for a running match).</summary>
        public static bool Pick(DuelMatch match, int side, DuelNpcLevel level, DuelRng rng, out int handIndex, out int cell)
        {
            handIndex = -1; cell = -1;
            IList<int> hand = match.Hand(side);
            var options = new List<Option>(hand.Count * DuelBoard.Cells);
            for (int h = 0; h < hand.Count; h++)
                for (int c = 0; c < DuelBoard.Cells; c++)
                {
                    if (!match.Board.IsEmpty(c)) continue;
                    options.Add(new Option { Hand = h, Cell = c, Flips = PopCount(match.Board.PreviewFlips(side, hand[h], c)), Risk = Risk(match.Board, hand[h], c) });
                }
            if (options.Count == 0) return false;
            options.Sort((a, b) =>
            {
                if (a.Flips != b.Flips) return b.Flips.CompareTo(a.Flips);
                if (a.Risk != b.Risk) return a.Risk.CompareTo(b.Risk);
                return a.Cell != b.Cell ? a.Cell.CompareTo(b.Cell) : a.Hand.CompareTo(b.Hand);
            });
            int pick;
            if (level == DuelNpcLevel.Easy) pick = rng.Next(System.Math.Min(3, options.Count));
            else
            {
                int best = 1; // how many options tie with the first on both flips and risk
                while (best < options.Count && options[best].Flips == options[0].Flips && options[best].Risk == options[0].Risk) best++;
                pick = rng.Next(best);
            }
            handIndex = options[pick].Hand; cell = options[pick].Cell;
            return true;
        }

        /// <summary>Weak edges (value under 6) that would face an empty cell: the enemy can still answer there.</summary>
        static int Risk(DuelBoard b, int cardIndex, int cell)
        {
            CardDef c = CardTable.ByIndex(cardIndex);
            int row = cell / DuelBoard.Size, col = cell % DuelBoard.Size, risk = 0;
            if (row > 0 && b.IsEmpty(cell - DuelBoard.Size)) risk += Weak(c.Top);
            if (col < DuelBoard.Size - 1 && b.IsEmpty(cell + 1)) risk += Weak(c.Right);
            if (row < DuelBoard.Size - 1 && b.IsEmpty(cell + DuelBoard.Size)) risk += Weak(c.Bottom);
            if (col > 0 && b.IsEmpty(cell - 1)) risk += Weak(c.Left);
            return risk;
        }

        static int Weak(int edge) { return edge >= 6 ? 0 : 6 - edge; }

        public static int PopCount(int mask) { int n = 0; while (mask != 0) { mask &= mask - 1; n++; } return n; }
    }
}
