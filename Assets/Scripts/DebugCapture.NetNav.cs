using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Online fight bot: an online client only knows the monsters inside its area of interest, so unlike the offline bot it must be able to walk to
    /// the fight. It follows a tile-grid distance field (4-neighbour BFS around cliffs and the river, over the bridge) to the boss / the nearest known
    /// monster / a patrol spot, and hands over to the precise aim-and-swing Think() once a target is close and in line of sight.
    /// </summary>
    public partial class DebugCapture
    {
        const int TW = FrostHollowMap.TilesW, TH = FrostHollowMap.TilesH;
        readonly int[] dist = new int[TW * TH];
        readonly Queue<int> bfs = new Queue<int>(TW * TH);
        Vector2Int distGoal = new Vector2Int(-1, -1);
        float distStamp;
        int patrolIndex;
        float patrolSince;

        static Vector2Int TileOf(Vector2 p)
        {
            return new Vector2Int(Mathf.Clamp(Mathf.FloorToInt(p.x / GameConfig.TileUnits), 0, TW - 1), Mathf.Clamp(TH - 1 - Mathf.FloorToInt(p.y / GameConfig.TileUnits), 0, TH - 1));
        }

        static Vector2 TileCenter(Vector2Int t) { return new Vector2((t.x + 0.5f) * GameConfig.TileUnits, GameConfig.MapH - (t.y + 0.5f) * GameConfig.TileUnits); }

        /// <summary>Fills `dist` with the walking steps from every tile to `goal` (-1 = unreachable).</summary>
        void ComputeDistanceField(Vector2Int goal)
        {
            for (int i = 0; i < dist.Length; i++) dist[i] = -1;
            bfs.Clear();
            if (FrostHollowMap.IsSolidTile(goal.x, goal.y)) { goal = NearestOpenTile(goal); }
            dist[goal.y * TW + goal.x] = 0;
            bfs.Enqueue(goal.y * TW + goal.x);
            int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
            while (bfs.Count > 0)
            {
                int cur = bfs.Dequeue(), cx = cur % TW, cy = cur / TW;
                for (int k = 0; k < 4; k++)
                {
                    int nx = cx + dx[k], ny = cy + dy[k];
                    if (nx < 0 || ny < 0 || nx >= TW || ny >= TH || FrostHollowMap.IsSolidTile(nx, ny) || dist[ny * TW + nx] >= 0) continue;
                    dist[ny * TW + nx] = dist[cur] + 1;
                    bfs.Enqueue(ny * TW + nx);
                }
            }
            distGoal = goal;
        }

        static Vector2Int NearestOpenTile(Vector2Int t)
        {
            for (int r = 1; r < 6; r++)
                for (int y = -r; y <= r; y++)
                    for (int x = -r; x <= r; x++)
                        if (!FrostHollowMap.IsSolidTile(t.x + x, t.y + y) && t.x + x >= 0 && t.y + y >= 0 && t.x + x < TW && t.y + y < TH) return new Vector2Int(t.x + x, t.y + y);
            return t;
        }

        /// <summary>Direction (-1 / 0 / 1 per axis) toward the next tile on the way to `goalWorld`.</summary>
        Vector2 StepToward(Vector2 from, Vector2 goalWorld)
        {
            Vector2Int goal = TileOf(goalWorld);
            if (goal != distGoal || Time.time - distStamp > 3f) { ComputeDistanceField(goal); distStamp = Time.time; }
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

        /// <summary>Walk spot of the current patrol zone (cycles every 12 s without a target).</summary>
        Vector2 PatrolSpot(int botSlot)
        {
            if (Time.time - patrolSince > 12f) { patrolSince = Time.time; patrolIndex++; }
            FrostHollowMap.Zone z = FrostHollowMap.Zones[(botSlot + patrolIndex) % FrostHollowMap.Zones.Length];
            return TileCenter(new Vector2Int(z.Tx + z.W / 2, z.Ty + z.H / 2));
        }

        void EnableNavBot(bool boss, Vector2 offset, bool cycleSkills = false)
        {
            int slot = 0;
            foreach (char c in NetLaunchArgs.Name ?? "") slot = slot * 7 + c;
            slot = Mathf.Abs(slot);
            Vector2 move = Vector2.zero;
            bool attack = false;
            int decidedFrame = -1;
            float nextSkill = Time.time + 3f + Random.value * 3f, nextPotion = 0f;
            System.Action decide = () =>
            {
                if (decidedFrame == Time.frameCount) return;
                decidedFrame = Time.frameCount;
                move = Vector2.zero; attack = false;
                HeroController hero = GameSession.Hero;
                if (hero == null || hero.IsDead) return;
                Monster target = boss && Monster.Boss != null && !Monster.Boss.IsDying ? Monster.Boss : null;
                float best = target != null ? 0f : float.MaxValue;
                if (target == null)
                    foreach (Monster m in Monster.All)
                    {
                        if (m.IsDying || m.MaxHp > 300f) continue;
                        float d = Vector2.Distance(m.Center, hero.Position);
                        if (d < best) { best = d; target = m; }
                    }
                if (target == null) { move = StepToward(hero.Position, PatrolSpot(slot)); return; }
                float dist2 = Vector2.Distance(target.Center, hero.Position);
                if (dist2 < 90f && (dist2 < 30f || HasLineOfSight(hero.Position, target.Center))) Think(hero, offset, ref move, ref attack, boss);
                else move = StepToward(hero.Position, target.Feet);
                if (move == Vector2.zero && !attack) move = StepToward(hero.Position, PatrolSpot(slot)); // stuck: shake loose toward a patrol spot
            };
            InputAdapter.BotHeld = k =>
            {
                decide();
                return (k == GameKey.Left && move.x < 0f) || (k == GameKey.Right && move.x > 0f) || (k == GameKey.Down && move.y < 0f) || (k == GameKey.Up && move.y > 0f);
            };
            InputAdapter.BotPressed = k =>
            {
                decide();
                if (k == GameKey.Attack) return attack;
                HeroController h = GameSession.Hero;
                if (cycleSkills && h != null && k >= GameKey.Skill1 && k <= GameKey.Skill8)
                {
                    int next = NextBotSkill(h.Rpg);
                    if (!attack || Time.time < nextSkill || next != k - GameKey.Skill1) return false;
                    botSkillCursor = next; nextSkill = Time.time + 1.2f;
                    return true;
                }
                if (!cycleSkills && k == GameKey.Skill && attack && Time.time >= nextSkill) { nextSkill = Time.time + 4.5f + Random.value; return true; }
                if (k == GameKey.HpPotion && h != null && h.Hp < h.MaxHp * 0.4f && Time.time >= nextPotion) { nextPotion = Time.time + 2f; return true; }
                return false;
            };
        }
    }
}
