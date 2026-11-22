using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PC left-click to move, Ragnarok style (user 22/11/2026: "trên bản PC cho chuột trái ấn để di chuyển"). A press on the world (not on the HUD):
    /// on the ground the hero walks there round cliffs and props (TileNav.Waypoint), and while the button stays down it keeps steering toward the cursor;
    /// a small ring marks the goal (ClickMoveMarker). On a monster: a lock-target class (Warrior) hands it to AttackChase (walk + slash until it dies),
    /// the others walk into range and keep attacking it. On a villager / a usable chest or crystal: walk up and press Interact once on arrival.
    /// It drives the hero through the same 4 map-axis move keys a player presses (InputAdapter.Held), picked every frame as the 8-way direction nearest
    /// to the true line to the goal (with hysteresis), so online only the usual input bits go to the server, which still moves the hero.
    /// The ground point is a ray from the camera to the walking plane z = 0, so it is right in 2D, on the tilted / turned 2.5D camera and on --map3d.
    /// Any move key, a window taking the screen, death or a map change cancel it. Mobile keeps the joystick (not active there).
    /// </summary>
    public static partial class ClickMove
    {
        enum Mode { None, Ground, Attack, Interact }

        const float ArriveUnits = 1.5f, NearUnits = 10f, StuckSeconds = 0.7f, SectorHysteresisDeg = 10f;
        const float MeleeRange = 24f, RangedRange = 80f, TalkUnits = 24f; // as AutoBattle; villagers talk within 30

        static int thoughtFrame = -1;
        static Mode mode;
        static bool holding;           // the left button went down on the ground and is still held: the goal follows the cursor
        static Vector2 goal;
        static Monster foe;
        static TownNpc npc;
        static DungeonPointView point;
        static string mapId;
        static int sx, sy, sector = -1;
        static bool attackNow, interactNow;
        static float movedAt;
        static Vector2 lastPos;
        static readonly TileNav nav = new TileNav();

        /// <summary>Goal of a ground walk for the marker; false when there is none.</summary>
        public static bool GroundGoal(out Vector2 at) { Think(); at = goal; return mode == Mode.Ground; }

        /// <summary>The monster a non-lock class walks to / attacks by click (TargetMarker rings it).</summary>
        public static Monster Target { get { Think(); return mode == Mode.Attack ? foe : null; } }

        /// <summary>The hero walks by click this frame: counts as a manual move (Auto-battle and the lock walk give way).</summary>
        public static bool Walking { get { Think(); return mode != Mode.None; } }

        public static bool Held(GameKey k)
        {
            Think();
            switch (k)
            {
                case GameKey.Left: return sx < 0;
                case GameKey.Right: return sx > 0;
                case GameKey.Down: return sy < 0;
                case GameKey.Up: return sy > 0;
                default: return false;
            }
        }

        public static bool Pressed(GameKey k)
        {
            Think();
            return (k == GameKey.Attack && attackNow) || (k == GameKey.Interact && interactNow);
        }

        static void Stop() { mode = Mode.None; foe = null; npc = null; point = null; holding = false; sector = -1; }

        static void Think()
        {
            if (thoughtFrame == Time.frameCount) return;
            thoughtFrame = Time.frameCount;
            sx = sy = 0; attackNow = interactNow = false;
            HeroController hero = LocalPlayer.Hero;
            string map = MapRegistry.Current != null ? MapRegistry.Current.Id : null;
            if (hero == null || hero.IsDead || map != mapId || UiWindows.BlocksGameplay || InputAdapter.KeyMoveHeld()) { Stop(); mapId = map; return; }
            ReadMouse(hero);
            if (mode == Mode.None) return;
            Vector2 at = hero.SimPosition;
            float now = Time.time;
            if ((at - lastPos).sqrMagnitude > 0.25f || hero.IsBasicAttacking) { lastPos = at; movedAt = now; }
            switch (mode)
            {
                case Mode.Ground:
                    if ((goal - at).magnitude <= ArriveUnits) { if (!holding) Stop(); sector = -1; return; }
                    break;
                case Mode.Attack:
                    if (foe == null || foe.IsDying || foe.Hp <= 0f) { Stop(); return; }
                    goal = foe.Feet;
                    float range = hero.Class.Ranged ? RangedRange : MeleeRange;
                    if ((foe.Center - hero.Position - new Vector2(0f, 4f)).magnitude <= range && TileNav.ClearPath(at, foe.Feet))
                    {
                        Steer(foe.Center - hero.Position); // keep facing it between swings
                        attackNow = true;
                        movedAt = now;
                        return;
                    }
                    break;
                case Mode.Interact:
                    Vector2 spot = npc != null ? npc.Feet : point != null ? point.Foot : goal;
                    float reach = npc != null ? TalkUnits : DungeonPoints.ReachUnits - 4f;
                    if ((npc == null && point == null) || (spot - hero.Position).magnitude <= reach) { interactNow = npc != null || point != null; Stop(); return; }
                    break;
            }
            if (!holding && now - movedAt > StuckSeconds) { Stop(); return; } // walled off / pushed against a monster: give up like RO
            Vector2 wp = nav.Waypoint(at, goal);
            Vector2 d = wp - at;
            if ((goal - at).magnitude < NearUnits) { sector = -1; LockTargetRules.ChaseDir(d.x, d.y, ArriveUnits * 0.5f, out sx, out sy); } // last steps: per axis, lands on the point
            else Steer(d);
        }

        /// <summary>The 8-way keys nearest to `d` (sector hysteresis: no flicker on a sector edge).</summary>
        static void Steer(Vector2 d)
        {
            if (d.sqrMagnitude < 0.01f) return;
            float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            if (sector < 0 || Mathf.Abs(Mathf.DeltaAngle(a, sector * 45f)) >= 22.5f + SectorHysteresisDeg) sector = ((int)Mathf.Round(a / 45f) + 8) % 8;
            sx = sector == 0 || sector == 1 || sector == 7 ? 1 : sector == 3 || sector == 4 || sector == 5 ? -1 : 0;
            sy = sector == 1 || sector == 2 || sector == 3 ? 1 : sector == 5 || sector == 6 || sector == 7 ? -1 : 0;
        }

        static void Begin(Mode m, HeroController hero)
        {
            mode = m;
            movedAt = Time.time;
            lastPos = hero.SimPosition;
            sector = -1;
        }

        /// <summary>A goal on a blocked tile (a tree, a rock) moves to the nearest open tile, so the walk ends next to it instead of stuck against it.</summary>
        static Vector2 OpenGoal(Vector2 p)
        {
            IGameMap map = MapRegistry.Current;
            if (map == null) return p;
            Vector2Int t = TileNav.TileOf(p);
            if (!map.IsSolidTile(t.x, t.y) && !map.RectBlocked(p.x - 5f, p.y, 10f, 8f)) return p;
            return TileNav.TileCenter(TileNav.NearestOpenTile(t));
        }
    }
}
