using UnityEngine;

/// <summary>Walkability from the 2D map's tile rows (row 0 = north): snow 's' and bridge 'B' are walkable, cliffs 'C' and water 'w' block. 1 tile = Tile metres.</summary>
public static class TileWalk
{
    public static string[] Rows;
    public static float Tile = 0.8f;

    public static bool Walkable(Vector3 p)
    {
        if (Rows == null) return true;
        int tx = Mathf.FloorToInt(p.x / Tile), tyFromBottom = Mathf.FloorToInt(p.z / Tile);
        int ty = Rows.Length - 1 - tyFromBottom;
        if (ty < 0 || ty >= Rows.Length || tx < 0 || tx >= Rows[ty].Length) return false;
        char c = Rows[ty][tx];
        return c == 's' || c == 'B';
    }

    /// <summary>Moves from `from` by `delta`, sliding along blocked axes (like the 2D game's tile movement).</summary>
    public static Vector3 Move(Vector3 from, Vector3 delta)
    {
        Vector3 to = from + delta;
        if (Walkable(to)) return to;
        Vector3 x = from + new Vector3(delta.x, 0f, 0f);
        if (Walkable(x)) return x;
        Vector3 z = from + new Vector3(0f, 0f, delta.z);
        return Walkable(z) ? z : from;
    }
}
