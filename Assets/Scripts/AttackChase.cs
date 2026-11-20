using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Walk-to-target and auto-attack for the lock-target slash (Warrior; user OK 2026-11-20, RO style): one attack press engages a monster (the
    /// one in reach, else the locked one if it is near, else the nearest within 8 tiles that can be walked to), walks the hero to it and keeps
    /// pressing the slash while it is in reach, until it dies. It drives the hero through the same keys a player presses (InputAdapter asks Held / Pressed, like AutoBattle), so online only move
    /// and attack input goes to the server, which still moves the hero and decides the hit. Moving by hand, the target dying, getting too far
    /// (LockTargetRules.ChaseLeash), a long walk or being stuck ends it. The way around cliffs comes from TileNav (as AutoBattle).
    /// </summary>
    public static class AttackChase
    {
        const float Near = 48f;      // within 3 tiles with a clear line: walk straight at it instead of tile by tile
        const float DeadZone = 3f;   // an axis this close to the target is not pressed (no zig-zag)
        const int MaxSteps = 12;     // a target more walking tiles away than this (around cliffs) is not chased

        static int thoughtFrame = -1;
        static Monster target;
        static float startedAt, movedAt;
        static Vector2 lastPos;
        static int sx, sy;           // move keys this frame
        static bool attackNow;       // press the slash this frame (arrived)
        static bool owns;            // this frame the attack key is ours (chasing, or the arrival press)
        static readonly TileNav walk = new TileNav(), reach = new TileNav();

        /// <summary>The monster being walked to, or null (TargetMarker draws its ring under it).</summary>
        public static Monster Target { get { Think(); return target; } }

        /// <summary>Move key the walk holds this frame.</summary>
        public static bool Held(GameKey k)
        {
            Think();
            if (target == null) return false;
            switch (k)
            {
                case GameKey.Left: return sx < 0;
                case GameKey.Right: return sx > 0;
                case GameKey.Down: return sy < 0;
                case GameKey.Up: return sy > 0;
                default: return false;
            }
        }

        /// <summary>True when the attack key is decided here this frame (the player's own presses are swallowed while walking); then AttackPressed is the answer.</summary>
        public static bool OwnsAttack { get { Think(); return owns; } }
        public static bool AttackPressed { get { Think(); return attackNow; } }

        static void Stop() { target = null; sx = sy = 0; }

        /// <summary>Decided once per frame, so every reader of the frame (hero, online input sampler) sees the same keys.</summary>
        static void Think()
        {
            if (thoughtFrame == Time.frameCount) return;
            thoughtFrame = Time.frameCount;
            sx = sy = 0; attackNow = false; owns = false;
            HeroController hero = LocalPlayer.Hero;
            if (hero == null || hero.IsDead || !hero.LocksTarget || AutoBattle.On || InputAdapter.ManualMoveHeld()) { Stop(); return; }
            float now = Time.time;
            Vector2 at = hero.SimPosition;
            if (target == null)
            {
                if (!InputAdapter.PressedNoChase(GameKey.Attack)) return;
                // One press engages (RO style, user 2026-11-20 "ấn attack 1 cái nó đánh 2 cái rồi dừng"): the monster in reach, else the nearest one to walk to.
                target = hero.LockCandidateInReach(0f) ?? PickTarget(hero, hero.ChestPoint);
                if (target == null) return; // nobody near: the slash whiffs where the hero stands
                startedAt = movedAt = now; lastPos = at;
            }
            owns = true; // swallow the player's presses (a held attack key would whiff every frame on the way)
            if (target == null || target.IsDying || target.Hp <= 0f) { Stop(); return; } // killed (by anyone) or despawned
            if ((at - lastPos).sqrMagnitude > 0.25f || hero.IsBasicAttacking) { lastPos = at; movedAt = now; } // a swing in progress is not being stuck
            Vector2 chest = hero.ChestPoint, edge = HeroController.ContactPoint(target, chest); // nearest point of its body: big bosses are reached at their edge
            if (!LockTargetRules.KeepChasing((edge - chest).magnitude, now - startedAt, now - movedAt)) { Stop(); return; }
            if (hero.InLockReach(target, LockTargetRules.ChaseReachMargin))
            {
                // In reach: keep swinging at it until it dies, the player moves, Auto turns on or it runs off (then the walk resumes).
                hero.SetLockTarget(target);
                attackNow = !hero.IsBasicAttacking; // the next slash as soon as the last one ends; the windup keeps this lock and turns the hero to it
                startedAt = now; // the walk time limit only counts walking, not fighting
                return;
            }
            Vector2 d = edge - chest; // walk the chest onto the body edge (the sword then touches it)
            if (d.sqrMagnitude < Near * Near && ClearPath(at, HeroController.ContactPoint(target, at))) LockTargetRules.ChaseDir(d.x, d.y, DeadZone, out sx, out sy);
            else
            {
                Vector2 step = walk.StepToward(at, HeroController.ContactPoint(target, at)); // the body edge on the ground, not the centre (a boss's feet can sit on a blocked tile)
                if (step == Vector2.zero) LockTargetRules.ChaseDir(d.x, d.y, DeadZone, out sx, out sy);
                else { sx = step.x > 0.1f ? 1 : step.x < -0.1f ? -1 : 0; sy = step.y > 0.1f ? 1 : step.y < -0.1f ? -1 : 0; }
            }
        }

        /// <summary>The locked monster while it is near, else the nearest live monster (bosses and event bosses too, as in other RPGs) within ChaseStartRange
        /// of the chest, measured to its body edge, that can be walked to.</summary>
        static Monster PickTarget(HeroController hero, Vector2 chest)
        {
            float r2 = LockTargetRules.ChaseStartRange * LockTargetRules.ChaseStartRange;
            Vector2 at = hero.SimPosition;
            Monster locked = hero.LockTarget;
            if (locked != null && !locked.IsDying && locked.Hp > 0f && EdgeDistSq(locked, chest) <= r2 && Walkable(at, locked)) return locked;
            Monster best = null;
            float bestD = r2;
            foreach (Monster m in Monster.All)
            {
                if (m == null || m.IsDying || m.Hp <= 0f) continue;
                float d = EdgeDistSq(m, chest);
                if (d > bestD || !Walkable(at, m)) continue;
                bestD = d; best = m;
            }
            return best;
        }

        static float EdgeDistSq(Monster m, Vector2 chest) { return (HeroController.ContactPoint(m, chest) - chest).sqrMagnitude; }

        static bool Walkable(Vector2 at, Monster m)
        {
            int steps = reach.Steps(at, HeroController.ContactPoint(m, at)); // field around the hero: one BFS answers every monster; to its nearest body edge
            return steps >= 0 && steps <= MaxSteps;
        }

        /// <summary>The hero's feet box (10 x 8, as FrostHollowMap.Move) can slide straight from a to b: no post, fence or wall in between (else TileNav walks around).</summary>
        static bool ClearPath(Vector2 a, Vector2 b)
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
    }
}
