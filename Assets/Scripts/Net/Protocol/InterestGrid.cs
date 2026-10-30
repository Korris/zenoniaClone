using System;
using System.Collections.Generic;

namespace ZenoniaProto.Net
{
    /// <summary>
    /// Uniform spatial grid over the map (cell = 64 units: a camera view of ~240 x 160 spans about 4 x 3 cells) holding entity ids by cell.
    /// Rebuilt or moved every tick by the server; area-of-interest queries read the cells around a client's hero.
    /// </summary>
    public sealed class InterestGrid
    {
        public const float CellSize = 64f;
        readonly int cols, rows;
        readonly List<ushort>[] cells;
        readonly Dictionary<ushort, int> cellOf = new Dictionary<ushort, int>(512);

        public InterestGrid(float mapWidth, float mapHeight)
        {
            cols = Math.Max(1, (int)Math.Ceiling(mapWidth / CellSize));
            rows = Math.Max(1, (int)Math.Ceiling(mapHeight / CellSize));
            cells = new List<ushort>[cols * rows];
            for (int i = 0; i < cells.Length; i++) cells[i] = new List<ushort>(8);
        }

        public int Columns { get { return cols; } }
        public int Rows { get { return rows; } }
        public int Count { get { return cellOf.Count; } }

        public int CellX(float x) { return Math.Max(0, Math.Min(cols - 1, (int)Math.Floor(x / CellSize))); }
        public int CellY(float y) { return Math.Max(0, Math.Min(rows - 1, (int)Math.Floor(y / CellSize))); }

        public void Clear()
        {
            for (int i = 0; i < cells.Length; i++) cells[i].Clear();
            cellOf.Clear();
        }

        /// <summary>Inserts the entity, or moves it when it changed cell. Out-of-map positions clamp to the border cells.</summary>
        public void Set(ushort id, float x, float y)
        {
            int c = CellY(y) * cols + CellX(x);
            int old;
            if (cellOf.TryGetValue(id, out old))
            {
                if (old == c) return;
                cells[old].Remove(id);
            }
            cells[c].Add(id);
            cellOf[id] = c;
        }

        public void Remove(ushort id)
        {
            int old;
            if (!cellOf.TryGetValue(id, out old)) return;
            cells[old].Remove(id);
            cellOf.Remove(id);
        }

        public bool TryCellOf(ushort id, out int cx, out int cy)
        {
            int c;
            if (!cellOf.TryGetValue(id, out c)) { cx = cy = -1; return false; }
            cx = c % cols; cy = c / cols;
            return true;
        }

        /// <summary>Appends every id within `radius` cells (square window, Chebyshev) of cell (cx, cy).</summary>
        public void Query(int cx, int cy, int radius, List<ushort> into)
        {
            int x0 = Math.Max(0, cx - radius), x1 = Math.Min(cols - 1, cx + radius), y0 = Math.Max(0, cy - radius), y1 = Math.Min(rows - 1, cy + radius);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++) into.AddRange(cells[y * cols + x]);
        }
    }

    /// <summary>
    /// What one client currently knows about: entities enter when they come within SeeRadius cells of the client's cell and only leave once
    /// farther than DropRadius cells (hysteresis, so walking along a cell border never flickers spawn / despawn pairs) or when they stop existing.
    /// </summary>
    public sealed class InterestSet
    {
        public const int SeeRadius = 2, DropRadius = 3;
        readonly HashSet<ushort> visible = new HashSet<ushort>();
        readonly List<ushort> scratch = new List<ushort>(64);

        public IEnumerable<ushort> Visible { get { return visible; } }
        public int Count { get { return visible.Count; } }
        public bool Contains(ushort id) { return visible.Contains(id); }

        /// <summary>Recomputes the set around cell (cx, cy); fills `entered` (to Spawn) and `left` (to Despawn, `gone` = no longer in the grid).</summary>
        public void Update(InterestGrid grid, int cx, int cy, List<ushort> entered, List<ushort> left, List<ushort> gone)
        {
            entered.Clear(); left.Clear(); gone.Clear();
            scratch.Clear();
            grid.Query(cx, cy, SeeRadius, scratch);
            for (int i = 0; i < scratch.Count; i++) if (visible.Add(scratch[i])) entered.Add(scratch[i]);
            foreach (ushort id in visible)
            {
                int ex, ey;
                if (!grid.TryCellOf(id, out ex, out ey)) { gone.Add(id); continue; }
                if (Math.Max(Math.Abs(ex - cx), Math.Abs(ey - cy)) > DropRadius) left.Add(id);
            }
            for (int i = 0; i < gone.Count; i++) visible.Remove(gone[i]);
            for (int i = 0; i < left.Count; i++) visible.Remove(left[i]);
        }

        public void Clear() { visible.Clear(); }
    }
}
