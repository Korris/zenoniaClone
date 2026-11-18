using UnityEngine;

/// <summary>Walkability from the 2D map's tile rows (row 0 = north): snow 's', grass 'g' and bridge 'B' are walkable; cliffs 'C', trees 'T' and water 'w' block. 1 tile = Tile metres.</summary>
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
        return c == 's' || c == 'g' || c == 'B';
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

    /// <summary>A body of radius `r` fits at `p` (centre and the four points at r around it are walkable).</summary>
    public static bool Walkable(Vector3 p, float r)
    {
        return Walkable(p) && Walkable(p + new Vector3(r, 0f, 0f)) && Walkable(p - new Vector3(r, 0f, 0f)) && Walkable(p + new Vector3(0f, 0f, r)) && Walkable(p - new Vector3(0f, 0f, r));
    }

    /// <summary>Hero movement: like Move but with a body radius, sliding along the free axis (larger component first) and, when pushing
    /// straight into the edge of a blocked tile, stepping sideways round the corner instead of stopping dead (corner nudge, up to 0.6 tile).</summary>
    public static Vector3 Slide(Vector3 from, Vector3 delta, float r)
    {
        if (!Walkable(from, r)) return Move(from, delta);   // spawned or pushed half into a wall: point rules until free
        if (Walkable(from + delta, r)) return from + delta;
        Vector3 x = new Vector3(delta.x, 0f, 0f), z = new Vector3(0f, 0f, delta.z);
        Vector3 a = Mathf.Abs(delta.x) >= Mathf.Abs(delta.z) ? x : z, b = a == x ? z : x;
        if (a.sqrMagnitude > 1e-8f && Walkable(from + a, r)) return from + a;
        if (b.sqrMagnitude > 1e-8f && Walkable(from + b, r)) return from + b;
        float len = delta.magnitude;
        if (len < 1e-5f) return from;
        Vector3 dir = delta / len, side = new Vector3(dir.z, 0f, -dir.x);
        for (float off = 0.1f; off <= Tile * 0.6f; off += 0.1f)
            foreach (float sg in new[] { 1f, -1f })
                if (Walkable(from + side * (sg * off) + delta, r))
                {
                    Vector3 step = from + side * (sg * len);
                    return Walkable(step, r) ? step : from;
                }
        return from;
    }
}
