using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Box model of the 4 x 4 board art (cards/board/board-4x4.png, 560 x 516 px), measured once from the art: where each cell and each label tab (1-4 on top, A-D on the left) sit.
    /// Cells, the column / row labels and the cards inside the cells ALL come from these functions, so a label is a box the size of its tab, a cell is a box the size of its cell, text is centred in its box
    /// and everything scales with one factor when the board is drawn bigger or smaller (like CSS: size, gap and padding come from one place, nothing is nudged by hand afterwards).
    /// All rects are in art pixels, top-left origin; multiply by Scale(boardHeight) for UI units.
    /// </summary>
    public static class CardDuelBoardLayout
    {
        public const float ArtW = 560f, ArtH = 516f;
        // cells: first cell's top-left, size, and the gap between cells (measured on the cell borders)
        const float CellX = 63f, CellY = 46f, CellW = 111f, CellH = 103f, GapX = 6f, GapY = 7f;
        // padding between a cell's border and the card drawn in it
        public const float CellPadding = 4f, CardAspect = 0.9f;
        // label tabs: column tabs are as wide as a cell and share the strip y .. y + h; row tabs are one strip x .. x + w, each centred on the tab drawn in the art
        const float ColTabY = 18f, ColTabW = 66f, ColTabH = 20f;
        const float RowTabX = 22f, RowTabW = 36f, RowTabH = 36f;
        static readonly float[] RowTabCentreY = { 102f, 202.5f, 310f, 418f };

        public static float Scale(float boardHeight) { return boardHeight / ArtH; }

        public static Rect Cell(int col, int row) { return new Rect(CellX + col * (CellW + GapX), CellY + row * (CellH + GapY), CellW, CellH); }

        /// <summary>The box a card occupies inside its cell: the cell minus its padding (like CSS padding), the card filling it as far as its 0.9 shape allows (the frame art is 0.82, a duel card is a little wider so the edge numbers fit) and centred.</summary>
        public static Rect CardIn(Rect cell)
        {
            float h = cell.height - 2f * CellPadding, w = Mathf.Min(cell.width - 2f * CellPadding, h * CardAspect);
            h = w / CardAspect;
            return new Rect(cell.x + (cell.width - w) * 0.5f, cell.y + (cell.height - h) * 0.5f, w, h);
        }

        public static Rect ColumnTab(int col)
        {
            Rect c = Cell(col, 0);
            return new Rect(c.center.x - ColTabW * 0.5f, ColTabY, ColTabW, ColTabH);
        }

        public static Rect RowTab(int row) { return new Rect(RowTabX, RowTabCentreY[row] - RowTabH * 0.5f, RowTabW, RowTabH); }

        public static Rect Scaled(Rect r, float s) { return new Rect(r.x * s, r.y * s, r.width * s, r.height * s); }
    }
}
