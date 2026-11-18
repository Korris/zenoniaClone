using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// View-only 8-way facing used by the RO Soft hero sheets. Index = sheet row: 0 down, 1 down-right, 2 right, 3 up-right, 4 up,
    /// 5 up-left, 6 left, 7 down-left. The sim's 4-dir facing (HeroRenderer.DirNames: 0 down, 1 up, 2 left, 3 right) is untouched.
    /// </summary>
    public static class Dir8Util
    {
        public static readonly string[] Names = { "down", "down-right", "right", "up-right", "up", "up-left", "left", "down-left" };
        static readonly int[] CardinalRow = { 0, 4, 6, 2 };           // 4-dir facing -> Dir8 row
        static readonly int[] AxisA = { 0, 0, 3, 1, 1, 1, 2, 0 };     // the 4-dir facings a Dir8 row agrees with (one for a cardinal, two for a diagonal)
        static readonly int[] AxisB = { 0, 3, 3, 3, 1, 2, 2, 2 };

        public static int FromCardinal(int dir4) { return CardinalRow[dir4 & 3]; }

        /// <summary>True when the Dir8 row points along the 4-dir facing (a diagonal matches both of its axes).</summary>
        public static bool Matches(int dir8, int dir4) { return dir8 >= 0 && dir8 < 8 && (AxisA[dir8] == dir4 || AxisB[dir8] == dir4); }

        /// <summary>Nearest of the 8 directions for a world vector (y up); -1 for a zero vector.</summary>
        public static int FromVector(Vector2 v)
        {
            if (v.sqrMagnitude < 1e-8f) return -1;
            float deg = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg; // 0 = right, 90 = up
            int i = Mathf.RoundToInt((deg + 90f) / 45f);       // -90 (down) -> row 0, counter-clockwise
            return ((i % 8) + 8) % 8;
        }

        /// <summary>Row index of a name in Names (case-insensitive); -1 when unknown.</summary>
        public static int Parse(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;
            for (int i = 0; i < Names.Length; i++) if (string.Equals(Names[i], name.Trim(), System.StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }
    }
}
