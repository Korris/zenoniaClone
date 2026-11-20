using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Walk-to-target for the lock-target slash (Warrior; user OK 2026-11-20, RO style): an attack press with no monster in reach picks one (the
    /// locked one if it is near, else the nearest within 8 tiles that can be walked to), walks the hero to it and presses the slash once it is in
    /// reach. It drives the hero through the same keys a player presses (InputAdapter asks Held / Pressed, like AutoBattle), so online only move
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
                if (!InputAdapter.PressedNoChase(GameKey.Attack) || hero.LockCandidateInReach(0f) != null) return; // a monster in reach: the plain slash
                target = PickTarget(hero, at);
                if (target == null) return; // nobody near: the slash whiffs where the hero stands
                startedAt = movedAt = now; lastPos = at;
            }
            owns = true; // swallow the player's presses (a held attack key would whiff every frame on the way)
            if (target == null || target.IsDying || target.Hp <= 0f) { Stop(); return; } // killed (by anyone) or despawned
            if ((at - lastPos).sqrMagnitude > 0.25f || hero.IsBasicAttacking) { lastPos = at; movedAt = now; } // a swing in progress is not being stuck
            if (!LockTargetRules.KeepChasing((target.Feet - at).magnitude, now - startedAt, now - movedAt)) { Stop(); return; }
            if (hero.InLockReach(target, LockTargetRules.ChaseReachMargin))
            {
                hero.SetLockTarget(target);
                attackNow = true; // arrived: one slash press; the windup keeps this lock and turns the hero to it
                Stop();
                return;
            }
            Vector2 d = target.Feet - at;
            if (d.sqrMagnitude < Near * Near && LineOfSight(at, target.Feet)) LockTargetRules.ChaseDir(d.x, d.y, DeadZone, out sx, out sy);
            else
            {
                Vector2 step = walk.StepToward(at, target.Feet);
                if (step == Vector2.zero) LockTargetRules.ChaseDir(d.x, d.y, DeadZone, out sx, out sy);
                else { sx = step.x > 0.1f ? 1 : step.x < -0.1f ? -1 : 0; sy = step.y > 0.1f ? 1 : step.y < -0.1f ? -1 : 0; }
            }
        }

        /// <summary>The locked monster while it is near, else the nearest live monster within ChaseStartRange that can be walked to.</summary>
        static Monster PickTarget(HeroController hero, Vector2 at)
        {
            float r2 = LockTargetRules.ChaseStartRange * LockTargetRules.ChaseStartRange;
            Monster locked = hero.LockTarget;
            if (locked != null && !locked.IsDying && locked.Hp > 0f && (locked.Feet - at).sqrMagnitude <= r2 && Walkable(at, locked)) return locked;
            Monster best = null;
            float bestD = r2;
            foreach (Monster m in Monster.All)
            {
                if (m == null || m.IsDying || m.Hp <= 0f) continue;
                float d = (m.Feet - at).sqrMagnitude;
                if (d > bestD || !Walkable(at, m)) continue;
                bestD = d; best = m;
            }
            return best;
        }

        static bool Walkable(Vector2 at, Monster m)
        {
            int steps = reach.Steps(at, m.Feet); // field around the hero: one BFS answers every monster
            return steps >= 0 && steps <= MaxSteps;
        }

        static bool LineOfSight(Vector2 a, Vector2 b)
        {
            IGameMap map = MapRegistry.Current;
            if (map == null) return true;
            int n = Mathf.CeilToInt(Vector2.Distance(a, b) / 6f);
            for (int i = 1; i < n; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)n);
                if (map.RectBlocked(p.x - 3f, p.y, 6f, 6f)) return false;
            }
            return true;
        }
    }
}
