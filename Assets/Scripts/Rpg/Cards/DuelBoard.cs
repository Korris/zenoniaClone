using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// The 4 x 4 board of the card duel. Cell = row * 4 + column, rows A-D (top to bottom), columns 1-4, so cell 0 is A1 and cell 15 is D4. Each cell holds a card index (CardTable.ByIndex) and the side that owns it.
    /// Rule: when a card is placed, each ENEMY card next to it (4 directions) is flipped if the placed card's edge facing it is higher than that card's edge facing back. No chains, no randomness.
    /// </summary>
    public sealed class DuelBoard
    {
        public const int Size = 4, Cells = 16, Empty = -1;

        readonly int[] card = new int[Cells], owner = new int[Cells];

        public DuelBoard() { for (int i = 0; i < Cells; i++) { card[i] = Empty; owner[i] = Empty; } }

        public static string Coord(int cell) { return cell < 0 || cell >= Cells ? "??" : ((char)('A' + cell / Size)).ToString() + (cell % Size + 1); }

        /// <summary>"B3" -> 6; -1 for anything else.</summary>
        public static int ParseCoord(string text)
        {
            if (text == null || text.Length != 2) return -1;
            int row = char.ToUpperInvariant(text[0]) - 'A', col = text[1] - '1';
            return row >= 0 && row < Size && col >= 0 && col < Size ? row * Size + col : -1;
        }

        public bool IsEmpty(int cell) { return cell >= 0 && cell < Cells && card[cell] == Empty; }
        public int CardAt(int cell) { return cell >= 0 && cell < Cells ? card[cell] : Empty; }
        /// <summary>Side owning the cell (0 or 1), -1 when empty.</summary>
        public int OwnerAt(int cell) { return cell >= 0 && cell < Cells ? owner[cell] : Empty; }
        public int Placed { get { int n = 0; for (int i = 0; i < Cells; i++) if (card[i] != Empty) n++; return n; } }
        public bool Full { get { return Placed == Cells; } }

        public int Score(int side) { int n = 0; for (int i = 0; i < Cells; i++) if (owner[i] == side) n++; return n; }

        /// <summary>The cells that placing `cardIndex` for `side` at `cell` WOULD flip (bit i = cell i). Nothing changes. 0 when the cell is not empty or the card is unknown.</summary>
        public int PreviewFlips(int side, int cardIndex, int cell)
        {
            CardDef c = CardTable.ByIndex(cardIndex);
            if (c == null || !IsEmpty(cell)) return 0;
            int row = cell / Size, col = cell % Size, mask = 0;
            if (row > 0) mask |= FlipBit(side, cell - Size, c.Top, Bottom);
            if (col < Size - 1) mask |= FlipBit(side, cell + 1, c.Right, Left);
            if (row < Size - 1) mask |= FlipBit(side, cell + Size, c.Bottom, Top);
            if (col > 0) mask |= FlipBit(side, cell - 1, c.Left, Right);
            return mask;
        }

        // the edge of the NEIGHBOUR that faces the placed card
        const int Top = 0, Right = 1, Bottom = 2, Left = 3;

        int FlipBit(int side, int neighbour, int mine, int theirEdge)
        {
            if (card[neighbour] == Empty || owner[neighbour] == side) return 0;
            CardDef t = CardTable.ByIndex(card[neighbour]);
            int theirs = theirEdge == Top ? t.Top : theirEdge == Right ? t.Right : theirEdge == Bottom ? t.Bottom : t.Left;
            return mine > theirs ? 1 << neighbour : 0;
        }

        /// <summary>Puts the card down and flips what the rule says. Returns the flip mask. Throws when the cell is taken or the card unknown (DuelMatch checks first).</summary>
        public int Place(int side, int cardIndex, int cell)
        {
            if (side != 0 && side != 1) throw new ArgumentOutOfRangeException("side");
            if (CardTable.ByIndex(cardIndex) == null) throw new ArgumentException("unknown card " + cardIndex);
            if (!IsEmpty(cell)) throw new ArgumentException("cell " + Coord(cell) + " is not empty");
            int mask = PreviewFlips(side, cardIndex, cell);
            card[cell] = cardIndex; owner[cell] = side;
            for (int i = 0; i < Cells; i++) if ((mask & (1 << i)) != 0) owner[i] = side;
            return mask;
        }

        /// <summary>Puts a card on a cell with no rule (rebuilding a board from a move list whose flips are already known).</summary>
        public void Set(int cell, int cardIndex, int side) { card[cell] = cardIndex; owner[cell] = side; }

        public void SetOwner(int cell, int side) { if (card[cell] != Empty) owner[cell] = side; }
    }
}
