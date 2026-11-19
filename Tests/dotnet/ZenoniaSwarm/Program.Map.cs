using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ZenoniaProto;

/// <summary>
/// Map knowledge for the crowd test, from the real rows in Assets/Scripts/Maps (compiled in, like ZenoniaNet.Tests; props block their footprint tiles):
///   --map ID              with --goal: a breadth-first distance field from the goal tile, so a fighter walking back to the goal follows the corridors
///                         instead of pressing straight into a wall (GoalStep).
///   --free-ring ID --center TX,TY --radius MIN,MAX --count N [--skip TX,TY;TX,TY]
///                         prints N walkable tiles (reachable from the centre, MIN to MAX tiles away, the skipped ones left out) spread evenly by angle
///                         around the centre, one "x y tx ty" line each (x / y = world feet at the tile centre), then exits. Tools/wanted-boss-crowd-playtest.ps1
///                         places the bots there with dev-boost-character.mjs --x --y.
/// Tiles: tx from the west edge, ty from the top row (as in the rows); world y grows upward.
/// </summary>
static partial class Program
{
    static TileMapBase flowMap;
    static int[] flowDist; // tiles to the goal tile, -1 = solid or cut off

    static TileMapBase LoadMap(string id)
    {
        var map = MapRegistry.Get(id) as TileMapBase;
        if (map == null) throw new ArgumentException("unknown map " + id);
        return map;
    }

    static int TileX(TileMapBase m, float x) { return (int)Math.Floor(x / TileMapBase.Tile); }
    static int TileY(TileMapBase m, float y) { return (int)Math.Floor((m.Height * TileMapBase.Tile - y) / TileMapBase.Tile); }
    static float FeetX(int tx) { return (tx + 0.5f) * TileMapBase.Tile; }
    static float FeetY(TileMapBase m, int ty) { return m.Height * TileMapBase.Tile - (ty + 0.5f) * TileMapBase.Tile; }

    /// <summary>4-neighbour breadth-first distances from (tx, ty) over walkable tiles.</summary>
    static int[] Distances(TileMapBase m, int tx, int ty)
    {
        var d = Enumerable.Repeat(-1, m.Width * m.Height).ToArray();
        if (m.IsSolidTile(tx, ty)) return d;
        var q = new Queue<int>();
        d[ty * m.Width + tx] = 0; q.Enqueue(ty * m.Width + tx);
        int[] ox = { 1, -1, 0, 0 }, oy = { 0, 0, 1, -1 };
        while (q.Count > 0)
        {
            int i = q.Dequeue(), x = i % m.Width, y = i / m.Width;
            for (int k = 0; k < 4; k++)
            {
                int nx = x + ox[k], ny = y + oy[k];
                if (m.IsSolidTile(nx, ny) || d[ny * m.Width + nx] >= 0) continue;
                d[ny * m.Width + nx] = d[i] + 1; q.Enqueue(ny * m.Width + nx);
            }
        }
        return d;
    }

    static void ReadMapArgs(string[] args)
    {
        string id = Arg(args, "--map", null);
        if (id == null || !hasGoal) return;
        flowMap = LoadMap(id);
        flowDist = Distances(flowMap, TileX(flowMap, goalX), TileY(flowMap, goalY));
    }

    /// <summary>Direction bits toward the goal: down the distance field when --map is known (next tile's centre), else straight at it.</summary>
    static ushort GoalStep(Bot b)
    {
        if (flowDist == null) return StepTo(goalX - b.X, goalY - b.Y);
        TileMapBase m = flowMap;
        int tx = TileX(m, b.X), ty = TileY(m, b.Y);
        if (tx < 0 || ty < 0 || tx >= m.Width || ty >= m.Height) return StepTo(goalX - b.X, goalY - b.Y);
        int here = flowDist[ty * m.Width + tx];
        if (here <= 1) return StepTo(goalX - b.X, goalY - b.Y); // on (or next to) the goal tile, or off the field: straight on
        int[] ox = { 1, -1, 0, 0 }, oy = { 0, 0, 1, -1 };
        int bestX = tx, bestY = ty, best = here;
        for (int k = 0; k < 4; k++)
        {
            int nx = tx + ox[k], ny = ty + oy[k];
            if (nx < 0 || ny < 0 || nx >= m.Width || ny >= m.Height) continue;
            int d = flowDist[ny * m.Width + nx];
            if (d >= 0 && d < best) { best = d; bestX = nx; bestY = ny; }
        }
        return StepTo(FeetX(bestX) - b.X, FeetY(m, bestY) - b.Y);
    }

    /// <summary>--free-ring: prints the spawn tiles and returns true (Main exits).</summary>
    static bool FreeRing(string[] args)
    {
        string id = Arg(args, "--free-ring", null);
        if (id == null) return false;
        TileMapBase m = LoadMap(id);
        int[] c = Arg(args, "--center", "0,0").Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        double[] r = Arg(args, "--radius", "3,6").Split(',').Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        int count = int.Parse(Arg(args, "--count", "39"), CultureInfo.InvariantCulture);
        var skip = new HashSet<(int, int)>();
        foreach (string s in Arg(args, "--skip", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        { string[] p = s.Split(','); skip.Add((int.Parse(p[0], CultureInfo.InvariantCulture), int.Parse(p[1], CultureInfo.InvariantCulture))); }
        int[] reach = Distances(m, c[0], c[1]);
        var cand = new List<(int tx, int ty, double angle)>();
        for (int ty = 0; ty < m.Height; ty++)
            for (int tx = 0; tx < m.Width; tx++)
            {
                double dx = tx - c[0], dy = ty - c[1], dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < r[0] || dist > r[1] || reach[ty * m.Width + tx] < 0 || skip.Contains((tx, ty))) continue;
                cand.Add((tx, ty, Math.Atan2(dy, dx)));
            }
        cand.Sort((a, b) => a.angle.CompareTo(b.angle));
        if (cand.Count < count) Console.Error.WriteLine("only " + cand.Count + " free tiles in the ring, wanted " + count);
        int n = Math.Min(count, cand.Count);
        for (int i = 0; i < n; i++)
        {
            var t = cand[(int)((long)i * cand.Count / n)]; // evenly by angle, never the same tile twice
            Console.WriteLine(F(FeetX(t.tx), 0) + " " + F(FeetY(m, t.ty), 0) + " " + t.tx + " " + t.ty);
        }
        return true;
    }
}
