using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Online fight bot: an online client only knows the monsters inside its area of interest, so unlike the offline bot it must be able to walk to
    /// the fight. It follows a tile-grid distance field (TileNav: 4-neighbour BFS around cliffs and the river, over the bridge) to the boss / the nearest known
    /// monster / a patrol spot, and hands over to the precise aim-and-swing Think() once a target is close and in line of sight.
    /// </summary>
    public partial class DebugCapture
    {
        readonly TileNav nav = new TileNav(); // tile-grid BFS shared with AutoBattle
        int patrolIndex;
        float patrolSince;

        static Vector2Int TileOf(Vector2 p) { return TileNav.TileOf(p); }
        static Vector2 TileCenter(Vector2Int t) { return TileNav.TileCenter(t); }
        Vector2 StepToward(Vector2 from, Vector2 goalWorld) { return nav.StepToward(from, goalWorld); }

        /// <summary>Walk spot of the current patrol zone (cycles every 12 s without a target).</summary>
        Vector2 PatrolSpot(int botSlot)
        {
            if (Time.time - patrolSince > 12f) { patrolSince = Time.time; patrolIndex++; }
            FrostHollowMap.Zone z = FrostHollowMap.Zones[(botSlot + patrolIndex) % FrostHollowMap.Zones.Length];
            return TileCenter(new Vector2Int(z.Tx + z.W / 2, z.Ty + z.H / 2));
        }

        /// <summary>
        /// Portal soak: walks to a portal of whatever map the server put it on, takes the next one along on each new map, and fights nothing. The server does the moving,
        /// so the stats (zoneChanges here, zoneTransfers on the server) count real transfers. Works for any map a new portal leads to.
        /// </summary>
        /// <summary>`preferTarget`: when the current map has a portal into that map id, always take it (the persist test must reach Frost Hollow, not wander east); otherwise the slot picks.</summary>
        void EnablePortalBot(string preferTarget = null)
        {
            int slot = 0;
            foreach (char c in NetLaunchArgs.Name ?? "") slot = slot * 7 + c;
            slot = Mathf.Abs(slot);
            IGameMap lastMap = null;
            MapPortal goal = default(MapPortal);
            Vector2 move = Vector2.zero;
            int decidedFrame = -1;
            InputAdapter.BotHeld = k =>
            {
                if (decidedFrame != Time.frameCount)
                {
                    decidedFrame = Time.frameCount;
                    move = Vector2.zero;
                    HeroController hero = GameSession.Hero;
                    MapPortal[] portals = MapRegistry.Current.Portals;
                    if (hero != null && !hero.IsDead && portals.Length > 0)
                    {
                        if (lastMap != MapRegistry.Current) { lastMap = MapRegistry.Current; slot++; goal = portals[slot % portals.Length]; foreach (MapPortal p in portals) if (preferTarget != null && p.TargetMapId == preferTarget) { goal = p; break; } }
                        Vector2 center = TileCenter(new Vector2Int(goal.Tx, goal.Ty));
                        move = StepToward(hero.Position, center);
                        // On the goal tile by the feet, but the trigger samples a little above them (PortalTile.OfFeet): walking from above it would stop just short. On to the middle.
                        if (move == Vector2.zero) move = new Vector2(Mathf.Abs(center.x - hero.Position.x) > 1f ? Mathf.Sign(center.x - hero.Position.x) : 0f, Mathf.Abs(center.y - hero.Position.y) > 1f ? Mathf.Sign(center.y - hero.Position.y) : 0f);
                    }
                }
                return (k == GameKey.Left && move.x < 0f) || (k == GameKey.Right && move.x > 0f) || (k == GameKey.Down && move.y < 0f) || (k == GameKey.Up && move.y > 0f);
            };
            InputAdapter.BotPressed = k => false;
        }

        /// <summary>`crowd`: fight the monster nearest to the hero with the most others around it (crowd readability captures put the camera hero inside the pack).</summary>
        void EnableNavBot(bool boss, Vector2 offset, bool cycleSkills = false, bool crowd = false, bool wantedBoss = false)
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
                HeroController pack = crowd ? DensestHero(hero) : null;
                Vector2 from = pack != null ? pack.Position : hero.Position; // crowd: the monster the pack is fighting
                Monster bossTarget = wantedBoss ? Monster.Wanted : Monster.Boss; // wantedBoss: the daily event boss instead of the map boss
                Monster target = boss && bossTarget != null && !bossTarget.IsDying ? bossTarget : null;
                float best = target != null ? 0f : float.MaxValue;
                if (target == null)
                    foreach (Monster m in Monster.All)
                    {
                        if (m.IsDying || m.MaxHp > 300f) continue;
                        float d = Vector2.Distance(m.Center, from);
                        if (d < best) { best = d; target = m; }
                    }
                if (target == null) { move = StepToward(hero.Position, PatrolSpot(slot)); return; }
                float dist2 = Vector2.Distance(target.Center, hero.Position);
                if (dist2 < 90f && (dist2 < 30f || HasLineOfSight(hero.Position, target.Center))) Think(hero, offset, ref move, ref attack, boss, wantedBoss);
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

        /// <summary>The other living hero with the most living heroes within 60 units (null when alone).</summary>
        static HeroController DensestHero(HeroController self)
        {
            Zone zone = Zone.Main;
            if (zone == null) return null;
            var all = zone.Heroes.All;
            HeroController best = null;
            int bestN = 0;
            foreach (HeroController a in all)
            {
                if (a == null || a == self || a.IsDead) continue;
                int n = 1;
                foreach (HeroController b in all)
                    if (b != null && b != a && b != self && !b.IsDead && Vector2.Distance(a.Position, b.Position) < 60f) n++;
                if (n > bestN) { bestN = n; best = a; }
            }
            return best;
        }
    }
}
