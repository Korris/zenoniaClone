using UnityEngine;
using UnityEngine.EventSystems;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Aiming of Ground skills (SkillTargeting.Ground; UX by ChatGPT 26/11/2026, docs/reference/skill-targeting/skill-targeting-design-chatgpt.md):
    ///  PC: the skill key / hotbar click opens the preview at the smart spot (locked target, best monster group, else ahead); it follows the mouse once
    ///      the mouse moves; left click casts there, right click / Esc / the same key again cancels (nothing spent), another skill key switches.
    ///      Quick Cast (PlayerPrefs "skillQuickCast" = 1 or `--quick-cast`): the key casts at once on the mouse point.
    ///  Mobile: a tap casts at once on the smart spot (smart cast); holding a ground skill button 150 ms opens the preview, the drag moves it
    ///      (relative, x DragGain), releasing casts, releasing back on the button cancels (SwipeDetector hooks from RoHudControls).
    ///  Auto Battle / bots never aim: their press goes through and the point is auto-picked (SkillGroundAim).
    /// A confirmed cast becomes a one-frame press of Skill1..8 (like a TouchControls tap) plus the point: offline HeroController takes it (Take), online
    /// the input sampler puts it in the frame (PointForFrame). The look is GroundAimIndicator.
    /// </summary>
    public sealed class GroundAimInput : MonoBehaviour
    {
        const float FollowMousePx = 4f, DragGain = 1.2f, HoldSeconds = 0.15f, TimeoutSeconds = 8f, CancelRadiusPx = 48f;
        static GroundAimInput instance;
        static readonly bool[] pending = new bool[8], pressed = new bool[8];
        static int pendingIndex = -1;          // the confirmed point waits here for the hero / the input sampler
        static Vector2 pendingPoint;
        static int quick = -1;

        public static bool Aiming { get; private set; }
        public static int AimSkill { get; private set; }
        public static SkillDef AimDef { get; private set; }
        /// <summary>Current centre (range clamped), whether the raw wish was past the range (gold) or unreachable (red), and the aim's age.</summary>
        public static Vector2 Point { get; private set; }
        public static bool AtEdge { get; private set; }
        public static bool Invalid { get; private set; }
        public static float StartedAt { get; private set; }
        /// <summary>Unscaled time of the last confirm / cancel (the indicator plays its shrink / fade out).</summary>
        public static float EndedAt { get; private set; } = -10f;
        public static bool EndedConfirmed { get; private set; }

        static bool touchDrag;                // aiming by a held skill button (mobile)
        static Vector2 startScreen, startMouse, dragStartPoint;
        static bool mouseMoved, draggedOut, testHold; // testHold: a capture run placed the preview, the real mouse must not move it
        static int endFrame = -10, pressFrame = -10;

        public static bool QuickCast
        {
            get
            {
                if (quick < 0) { quick = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--quick-cast") >= 0 ? 1 : 0; try { if (PlayerPrefs.GetInt("skillQuickCast", 0) == 1) quick = 1; } catch (System.Exception) { } }
                return quick == 1;
            }
        }

        /// <summary>The left mouse button belongs to the aim this frame (ClickMove must not walk / attack with it).</summary>
        public static bool OwnsMouse { get { return Aiming && !touchDrag || Time.frameCount - endFrame <= 1; } }

        static void Ensure()
        {
            if (instance != null) return;
            instance = new GameObject("GroundAimInput").AddComponent<GroundAimInput>();
            instance.gameObject.AddComponent<GroundAimIndicator>();
        }

        /// <summary>Tree skill `index` of the local hero is a Ground skill the player aims by hand.</summary>
        public static bool IsAimed(int index)
        {
            HeroController h = LocalPlayer.Hero;
            return h != null && h.Rpg != null && GroundTargetRules.IsGround(SkillTable.Get(h.Rpg.ClassId, index));
        }

        /// <summary>
        /// InputAdapter: the player pressed Ground skill `index` (key, hotbar click or mobile tap). Returns true when the aim took the press (the
        /// skill is not pressed now); false = let it through (mobile tap = smart cast, a skill that cannot be cast shows its refusal as before).
        /// </summary>
        public static bool HumanPress(int index)
        {
            if (Time.frameCount == pressFrame) return true; // InputAdapter.Pressed is asked several times per frame
            HeroController hero = LocalPlayer.Hero;
            if (hero == null || hero.IsDead || Input.touchCount > 0 || Application.isMobilePlatform) return false;
            SkillDef def = SkillTable.Get(hero.Rpg.ClassId, index);
            int rank = hero.Rpg.Skills[index];
            if (def == null || rank <= 0 || hero.SkillCdLeftMs(index) > 0f || (!SkillCooldowns.DevFreeMp && hero.Mp < def.MpAt(rank))) return false;
            pressFrame = Time.frameCount;
            Ensure();
            if (Aiming && AimSkill == index) { Cancel(); return true; } // the same key again: cancel
            Begin(hero, def, false, Vector2.zero);
            if (QuickCast) { Vector2 g; if (ScreenToGround(Input.mousePosition, out g)) Point = Clamp(hero, g); Confirm(); }
            return true;
        }

        /// <summary>Mobile: a Ground skill button was held HoldSeconds (SwipeDetector.onHoldStart). False = not a ground skill / not castable.</summary>
        public static bool TouchBegin(int index, Vector2 screen)
        {
            HeroController hero = LocalPlayer.Hero;
            if (hero == null || hero.IsDead || !IsAimed(index)) return false;
            SkillDef def = SkillTable.Get(hero.Rpg.ClassId, index);
            int rank = hero.Rpg.Skills[index];
            if (rank <= 0 || hero.SkillCdLeftMs(index) > 0f) return false;
            Ensure();
            Begin(hero, def, true, screen);
            return true;
        }

        public static void TouchDrag(Vector2 screen)
        {
            HeroController hero = LocalPlayer.Hero;
            Camera cam = Camera.main;
            if (!Aiming || !touchDrag || hero == null || cam == null) return;
            Vector3 s0 = cam.WorldToScreenPoint(new Vector3(dragStartPoint.x, dragStartPoint.y, 0f));
            Vector2 g;
            if ((screen - startScreen).magnitude > CancelRadiusPx * 1.5f) draggedOut = true;
            if (ScreenToGround((Vector2)s0 + (screen - startScreen) * DragGain, out g)) Point = Clamp(hero, g);
        }

        /// <summary>Mobile: the finger lifted. Dragged out and back onto the button = cancel (ChatGPT: "kéo vào nút huỷ rồi thả"), anywhere else = cast.</summary>
        public static void TouchEnd(Vector2 screen)
        {
            if (!Aiming || !touchDrag) return;
            if (draggedOut && (screen - startScreen).magnitude < CancelRadiusPx) { Cancel(); return; }
            Confirm();
        }

        /// <summary>Capture runs (DebugCapture `aim:I:DX:DY`): opens the preview of skill `index` on `point` (clamped like the mouse). False = not a Ground skill.</summary>
        public static bool TestAim(int index, Vector2 point)
        {
            HeroController hero = LocalPlayer.Hero;
            if (hero == null || !IsAimed(index)) return false;
            Ensure();
            Begin(hero, SkillTable.Get(hero.Rpg.ClassId, index), false, Vector2.zero);
            Point = Clamp(hero, point);
            testHold = true; // the preview stays where the test put it, wherever the window's mouse is
            return true;
        }

        public static void TestConfirm() { Confirm(); }

        /// <summary>Confirmed this frame (one-frame press, the TouchControls rhythm).</summary>
        public static bool Pressed(GameKey k) { int i = k - GameKey.Skill1; return i >= 0 && i < 8 && pressed[i]; }

        /// <summary>Offline hero, at the cast: the point confirmed for `index` (consumed), null = none (auto pick).</summary>
        public static Vector2? Take(int index)
        {
            if (pendingIndex != index) return null;
            pendingIndex = -1;
            return pendingPoint;
        }

        /// <summary>Online input sampler: the point of a Ground skill press for the frame (aimed, else the smart pick). False = not a Ground skill.</summary>
        public static bool PointForFrame(HeroController hero, int index, out Vector2 p)
        {
            p = Vector2.zero;
            SkillDef def = hero != null && hero.Rpg != null ? SkillTable.Get(hero.Rpg.ClassId, index) : null;
            if (!GroundTargetRules.IsGround(def)) return false;
            Vector2? aimed = Take(index);
            p = aimed ?? SkillGroundAim.Point(def, new HeroSkillBody(hero), null, true);
            return true;
        }

        static void Begin(HeroController hero, SkillDef def, bool touch, Vector2 screen)
        {
            AimSkill = def.Index; AimDef = def; Aiming = true; touchDrag = touch; mouseMoved = false; draggedOut = false;
            StartedAt = Time.unscaledTime; startScreen = screen; startMouse = Input.mousePosition;
            Point = Clamp(hero, SkillGroundAim.Auto(def, hero.Position, HeroSkillBody.DirOf(hero.Facing), SkillGroundAim.PreferredTarget(hero)));
            dragStartPoint = Point;
        }

        static void Confirm()
        {
            if (!Aiming) return;
            pendingIndex = AimSkill; pendingPoint = Point;
            pending[AimSkill] = true;
            End(true);
        }

        public static void Cancel() { if (Aiming) End(false); }

        static void End(bool confirmed)
        {
            Aiming = false; touchDrag = false; testHold = false;
            EndedAt = Time.unscaledTime; EndedConfirmed = confirmed; endFrame = Time.frameCount;
        }

        /// <summary>Range-clamped point, plus the gold (past the range) / red (a wall stops a leap or a turret) states.</summary>
        static Vector2 Clamp(HeroController hero, Vector2 wish)
        {
            Vector2 feet = hero.Position;
            float range = AimDef.CastRange * GameConfig.TileUnits, x, y;
            AtEdge = !GroundTargetRules.InRange(feet.x, feet.y, wish.x, wish.y, range);
            GroundTargetRules.ClampToRange(feet.x, feet.y, wish.x, wish.y, range, out x, out y);
            var p = new Vector2(x, y);
            Invalid = SkillGroundAim.Blocked(AimDef, feet, p);
            return p;
        }

        /// <summary>Where the camera ray through `screen` meets the walking plane z = 0 (ClickMove's rule).</summary>
        static bool ScreenToGround(Vector2 screen, out Vector2 p)
        {
            p = Vector2.zero;
            Camera cam = Camera.main;
            if (cam == null) return false;
            Ray r = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
            if (Mathf.Abs(r.direction.z) < 1e-5f) return false;
            float t = -r.origin.z / r.direction.z;
            if (t < 0f) return false;
            Vector3 hit = r.origin + r.direction * t;
            p = new Vector2(hit.x, hit.y);
            return true;
        }

        void Update()
        {
            for (int i = 0; i < 8; i++) { pressed[i] = pending[i]; pending[i] = false; }
            if (pendingIndex >= 0 && Time.frameCount - endFrame > 30) pendingIndex = -1; // a confirm the hero never used (busy, out of MP): drop it
            if (!Aiming) return;
            HeroController hero = LocalPlayer.Hero;
            if (hero == null || hero.IsDead || UiWindows.BlocksGameplay || InventoryPanel.IsOpen || Time.unscaledTime - StartedAt > TimeoutSeconds || hero.Rpg.ClassId != AimDef.Class) { Cancel(); return; }
            if (touchDrag) { Point = Clamp(hero, Point); return; } // the finger moves it (TouchDrag); keep the range / wall state fresh as the hero moves
            if (!mouseMoved && !testHold && ((Vector2)Input.mousePosition - startMouse).magnitude > FollowMousePx) mouseMoved = true;
            Vector2 g;
            if (mouseMoved && ScreenToGround(Input.mousePosition, out g)) Point = Clamp(hero, g);
            else Point = Clamp(hero, Point);
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) { Cancel(); return; }
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(0) && !overUi) Confirm();
        }

        void OnDestroy() { if (instance == this) { instance = null; Aiming = false; } }
    }
}
