using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Tile-grid walking for bots: a 4-neighbour BFS distance field to a goal tile around cliffs, water and props (IGameMap.IsSolidTile), refreshed
    /// when the goal tile or the map changes (or every 3 s), and the one-tile step toward it. Used by the online capture bots (DebugCapture.NetNav)
    /// and by AutoBattle, which both drive the hero through InputAdapter keys, and by click-to-move (ClickMove), which steers along Waypoint.
    /// </summary>
    public sealed class TileNav
    {
        static int TW { get { return GameConfig.TilesW; } } // size of the current map; `dist` is allocated for the largest one
        static int TH { get { return GameConfig.TilesH; } }
        readonly int[] dist = new int[GameConfig.MaxTilesW * GameConfig.MaxTilesH];
        readonly Queue<int> bfs = new Queue<int>(GameConfig.MaxTilesW * GameConfig.MaxTilesH);
        Vector2Int distGoal = new Vector2Int(-1, -1);
        IGameMap distMap;
        float distStamp;

        public static Vector2Int TileOf(Vector2 p)
        {
            return new Vector2Int(Mathf.Clamp(Mathf.FloorToInt(p.x / GameConfig.TileUnits), 0, TW - 1), Mathf.Clamp(TH - 1 - Mathf.FloorToInt(p.y / GameConfig.TileUnits), 0, TH - 1));
        }

        public static Vector2 TileCenter(Vector2Int t) { return new Vector2((t.x + 0.5f) * GameConfig.TileUnits, GameConfig.MapH - (t.y + 0.5f) * GameConfig.TileUnits); }

        /// <summary>Fills `dist` with the walking steps from every tile to `goal` (-1 = unreachable).</summary>
        void ComputeDistanceField(Vector2Int goal)
        {
            for (int i = 0; i < TW * TH; i++) dist[i] = -1;
            bfs.Clear();
            if (MapRegistry.Current.IsSolidTile(goal.x, goal.y)) { goal = NearestOpenTile(goal); }
            dist[goal.y * TW + goal.x] = 0;
            bfs.Enqueue(goal.y * TW + goal.x);
            int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
            while (bfs.Count > 0)
            {
                int cur = bfs.Dequeue(), cx = cur % TW, cy = cur / TW;
                for (int k = 0; k < 4; k++)
                {
                    int nx = cx + dx[k], ny = cy + dy[k];
                    if (nx < 0 || ny < 0 || nx >= TW || ny >= TH || MapRegistry.Current.IsSolidTile(nx, ny) || dist[ny * TW + nx] >= 0) continue;
                    dist[ny * TW + nx] = dist[cur] + 1;
                    bfs.Enqueue(ny * TW + nx);
                }
            }
            distGoal = goal;
        }

        public static Vector2Int NearestOpenTile(Vector2Int t)
        {
            for (int r = 1; r < 6; r++)
                for (int y = -r; y <= r; y++)
                    for (int x = -r; x <= r; x++)
                        if (!MapRegistry.Current.IsSolidTile(t.x + x, t.y + y) && t.x + x >= 0 && t.y + y >= 0 && t.x + x < TW && t.y + y < TH) return new Vector2Int(t.x + x, t.y + y);
            return t;
        }

        /// <summary>Walking steps (tiles) from `from` to `goalWorld`; -1 when walls cut it off.</summary>
        public int Steps(Vector2 goalWorld, Vector2 from)
        {
            Ensure(goalWorld);
            Vector2Int t = TileOf(from);
            return dist[t.y * TW + t.x];
        }

        void Ensure(Vector2 goalWorld)
        {
            Vector2Int goal = TileOf(goalWorld);
            if (goal != distGoal || distMap != MapRegistry.Current || Time.time - distStamp > 3f) { ComputeDistanceField(goal); distStamp = Time.time; distMap = MapRegistry.Current; }
        }

        /// <summary>The hero's feet box (10 x 8, as FrostHollowMap.Move) can slide straight from a to b: no post, fence or wall in between.</summary>
        public static bool ClearPath(Vector2 a, Vector2 b)
        {
            IGameMap map = MapRegistry.Current;
            if (map == null) return true;
            int n = Mathf.CeilToInt(Vector2.Distance(a, b) / 4f);
            for (int i = 1; i < n; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)n);
                if (map.RectBlocked(p.x - 5f, p.y, 10f, 8f)) return false;
            }
            return true;
        }

        /// <summary>
        /// Where to steer toward `goalWorld` from `from` (click-to-move): the goal itself when the straight line is clear, else the furthest tile centre down
        /// the BFS path (up to `look` tiles ahead) that can be reached in a straight line, so the walk cuts corners smoothly instead of zig-zagging tile by tile.
        /// Unreachable goal: the goal itself (the caller stops when the hero stops making progress).
        /// </summary>
        public Vector2 Waypoint(Vector2 from, Vector2 goalWorld, int look = 8)
        {
            if (ClearPath(from, goalWorld)) return goalWorld;
            Ensure(goalWorld);
            Vector2Int here = TileOf(from);
            int d = dist[here.y * TW + here.x];
            if (d < 0) return goalWorld;
            int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
            Vector2 best = Vector2.zero;
            bool found = false;
            Vector2Int cur = here;
            for (int step = 0; step < look && d > 0; step++)
            {
                Vector2Int next = cur;
                for (int k = 0; k < 4; k++)
                {
                    int nx = cur.x + dx[k], ny = cur.y + dy[k];
                    if (nx < 0 || ny < 0 || nx >= TW || ny >= TH) continue;
                    int nd = dist[ny * TW + nx];
                    if (nd >= 0 && nd < d) { d = nd; next = new Vector2Int(nx, ny); }
                }
                if (next == cur) break;
                cur = next;
                Vector2 c = TileCenter(cur);
                if (d == 0) c = goalWorld; // the goal's own tile: the goal point
                if (ClearPath(from, c)) { best = c; found = true; }
                else if (found) break; // the line is cut beyond the last clear one: steer there first
            }
            if (found) return best;
            Vector2 axis = StepToward(from, goalWorld); // hugging a corner: the old one-tile axis step gets it free
            return axis == Vector2.zero ? goalWorld : from + axis * GameConfig.TileUnits;
        }

        /// <summary>Direction (-1 / 0 / 1 per axis) toward the next tile on the way to `goalWorld`; zero when there or unreachable.</summary>
        public Vector2 StepToward(Vector2 from, Vector2 goalWorld)
        {
            Ensure(goalWorld);
            Vector2Int here = TileOf(from);
            int best = dist[here.y * TW + here.x];
            Vector2Int next = here;
            int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
            for (int k = 0; k < 4; k++)
            {
                int nx = here.x + dx[k], ny = here.y + dy[k];
                if (nx < 0 || ny < 0 || nx >= TW || ny >= TH) continue;
                int d = dist[ny * TW + nx];
                if (d >= 0 && (best < 0 || d < best)) { best = d; next = new Vector2Int(nx, ny); }
            }
            if (next == here) return Vector2.zero;
            Vector2 c = TileCenter(next);
            // move on the dominant axis only: tile-to-tile steps stay clear of corners
            Vector2 d2 = c - from;
            return Mathf.Abs(d2.x) > Mathf.Abs(d2.y) ? new Vector2(Mathf.Sign(d2.x), Mathf.Abs(d2.y) > 4f ? Mathf.Sign(d2.y) : 0f) : new Vector2(Mathf.Abs(d2.x) > 4f ? Mathf.Sign(d2.x) : 0f, Mathf.Sign(d2.y));
        }
    }
}
