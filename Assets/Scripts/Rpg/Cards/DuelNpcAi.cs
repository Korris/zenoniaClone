using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Brain of Card Master Orin AND the tutorial's "Hint" button (one level, easy): it only looks at a board and one hand and picks the move that flips the most cards.
    /// Orin (careful = false): a random one among the moves that flip the most. Hint (careful = true): among those, the one leaving the fewest weak edges (under 6) facing an empty cell,
    /// so the advice is stable and teaches defence. Works on a board + hand (not a match) so the client can call it on a DuelView. Pure, deterministic from the rng.
    /// </summary>
    public static class DuelNpcAi
    {
        struct Option { public int Hand, Cell, Flips, Risk; }

        /// <summary>The (hand index, cell) to play. False when no legal move (no empty cell / empty hand). `rng` may be null (ties go to the first).</summary>
        public static bool Best(DuelBoard board, IList<int> hand, int side, DuelRng rng, bool careful, out int handIndex, out int cell)
        {
            handIndex = -1; cell = -1;
            var options = new List<Option>(hand.Count * DuelBoard.Cells);
            for (int h = 0; h < hand.Count; h++)
                for (int c = 0; c < DuelBoard.Cells; c++)
                {
                    if (!board.IsEmpty(c)) continue;
                    options.Add(new Option { Hand = h, Cell = c, Flips = DuelBoard.PopCount(board.PreviewFlips(side, hand[h], c)), Risk = careful ? Risk(board, hand[h], c) : 0 });
                }
            if (options.Count == 0) return false;
            options.Sort((a, b) =>
            {
                if (a.Flips != b.Flips) return b.Flips.CompareTo(a.Flips);
                if (a.Risk != b.Risk) return a.Risk.CompareTo(b.Risk);
                return a.Cell != b.Cell ? a.Cell.CompareTo(b.Cell) : a.Hand.CompareTo(b.Hand);
            });
            int tied = 1; // options that tie with the first on flips (and risk when careful)
            while (tied < options.Count && options[tied].Flips == options[0].Flips && options[tied].Risk == options[0].Risk) tied++;
            int pick = rng != null && !careful ? rng.Next(tied) : 0;
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
    }
}
