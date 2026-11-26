using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// ClickMove input: the left button on the world (the HUD, a window or a right-button camera drag keep their clicks), and what lies under the cursor,
    /// picked on SCREEN against each monster / villager / chest's drawn body (feet to head through View25D.Above, so a tilted or turned camera still hits
    /// the figure, not the ground behind it), else the ground point where the camera ray meets the walking plane z = 0.
    /// </summary>
    public static partial class ClickMove
    {
        const float MinHitPx = 44f; // a small slime is still easy to click (at 720p, scaled with the screen)

        static bool Desktop { get { return !Application.isMobilePlatform && Application.isFocused; } }

        static void ReadMouse(HeroController hero)
        {
            if (!Desktop || Input.touchCount > 0) { holding = false; return; }
            bool down = Input.GetMouseButtonDown(0), held = Input.GetMouseButton(0);
            if (!held) holding = false;
            Camera cam = Camera.main;
            if (cam == null) return;
            if (down && !Input.GetMouseButton(1) && !CameraWheelZoom.WindowOwnsPointer() && !GroundAimInput.OwnsMouse) Press(hero, cam, Input.mousePosition); // a Ground skill aim takes the click
            if (holding && mode == Mode.Ground)
            {
                Vector2 p;
                if (GroundAt(cam, Input.mousePosition, out p)) goal = OpenGoal(p);
            }
        }

        static void Press(HeroController hero, Camera cam, Vector2 screen)
        {
            if (PartyPlayerMenu.HeroAt(screen) != null) return; // another player: the party / trade box takes that click
            Monster m = MonsterAt(cam, screen);
            if (m != null)
            {
                Stop();
                if (hero.LocksTarget) { AttackChase.Engage(m); return; } // Warrior: the lock walk + slash (AttackChase) does the rest
                foe = m;
                Begin(Mode.Attack, hero);
                return;
            }
            TownNpc n = NpcAt(cam, screen);
            DungeonPointView v = n == null ? PointAt(cam, screen) : null;
            if (n != null || v != null)
            {
                Stop();
                npc = n; point = v;
                goal = OpenGoal(n != null ? n.Feet : v.Foot);
                Begin(Mode.Interact, hero);
                return;
            }
            Vector2 p;
            if (!GroundAt(cam, screen, out p)) return;
            Stop();
            goal = OpenGoal(p);
            holding = true;
            Begin(Mode.Ground, hero);
            ClickMoveMarker.Show();
        }

        /// <summary>Where the camera ray through `screen` meets the walking plane (z = 0); false when it looks away from it.</summary>
        static bool GroundAt(Camera cam, Vector2 screen, out Vector2 p)
        {
            p = Vector2.zero;
            Ray r = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
            if (Mathf.Abs(r.direction.z) < 1e-5f) return false;
            float t = -r.origin.z / r.direction.z;
            if (t < 0f) return false;
            Vector3 hit = r.origin + r.direction * t;
            p = new Vector2(Mathf.Clamp(hit.x, 0f, GameConfig.MapW), Mathf.Clamp(hit.y, 0f, GameConfig.MapH));
            return true;
        }

        /// <summary>Squared screen distance from `screen` to the middle of a figure standing at `feet`, `height` units tall; &lt; 0 when it misses.</summary>
        static float BodyHit(Camera cam, Vector2 screen, Vector2 feet, float height, float minHalfWidthPx)
        {
            Vector3 f3 = cam.WorldToScreenPoint(View25D.Above(feet.x, feet.y, 0f)), h3 = cam.WorldToScreenPoint(View25D.Above(feet.x, feet.y, height));
            if (f3.z <= 0f || h3.z <= 0f) return -1f; // behind the camera
            Vector2 f = f3, h = h3;
            float s = Screen.height / 720f, tall = Mathf.Abs(h.y - f.y);
            float halfW = Mathf.Max(MinHitPx * 0.5f * s, minHalfWidthPx, tall * 0.35f);
            float bottom = Mathf.Min(f.y, h.y) - 6f * s, top = Mathf.Max(f.y, h.y) + 4f * s;
            if (tall < MinHitPx * s) { float mid = (f.y + h.y) * 0.5f; bottom = Mathf.Min(bottom, mid - MinHitPx * 0.5f * s); top = Mathf.Max(top, mid + MinHitPx * 0.5f * s); }
            float cx = (f.x + h.x) * 0.5f;
            if (Mathf.Abs(screen.x - cx) > halfW || screen.y < bottom || screen.y > top) return -1f;
            return (screen - (f + h) * 0.5f).sqrMagnitude;
        }

        static Monster MonsterAt(Camera cam, Vector2 screen)
        {
            Monster best = null;
            float bestSq = float.MaxValue;
            foreach (Monster m in Monster.All)
            {
                if (m == null || m.IsDying || m.Hp <= 0f) continue;
                Vector3 edge = cam.WorldToScreenPoint(View25D.Above(m.Feet.x + m.FootRadius, m.Feet.y, 0f)), mid = cam.WorldToScreenPoint(View25D.Above(m.Feet.x, m.Feet.y, 0f));
                float sq = BodyHit(cam, screen, m.Feet, m.DrawnHeight * 0.9f, Mathf.Abs(edge.x - mid.x));
                if (sq >= 0f && sq < bestSq) { bestSq = sq; best = m; }
            }
            return best;
        }

        static TownNpc NpcAt(Camera cam, Vector2 screen)
        {
            TownNpc best = null;
            float bestSq = float.MaxValue;
            foreach (TownNpc n in TownNpc.Villagers)
            {
                if (n == null) continue;
                float sq = BodyHit(cam, screen, n.Feet, 26f, 0f);
                if (sq >= 0f && sq < bestSq) { bestSq = sq; best = n; }
            }
            return best;
        }

        /// <summary>A chest / crystal that has something to offer (a full chest, an attuned crystal), as DungeonInteract.Nearest counts them.</summary>
        static DungeonPointView PointAt(Camera cam, Vector2 screen)
        {
            DungeonPointView best = null;
            float bestSq = float.MaxValue;
            foreach (DungeonPointView v in DungeonPointView.Views)
            {
                if (v == null || (v.Chest != null ? v.StateOn : !v.StateOn)) continue;
                float sq = BodyHit(cam, screen, v.Foot, 20f, 0f);
                if (sq >= 0f && sq < bestSq) { bestSq = sq; best = v; }
            }
            return best;
        }
    }
}
