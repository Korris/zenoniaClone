using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// How a player picks someone to invite: a tap (or click) on another player, or on their name above them, opens a small box with their name and Invite. A quick tap only, so a
    /// drag on the joystick never counts. The joystick's touch zone is the one piece of UI a tap passes through, because it covers the lower left of the world around the hero; every
    /// other control keeps its taps. Tapping elsewhere closes the box, and so does the target going away. Any finger counts, so this works with the other thumb on the stick.
    /// Reads the input module's BaseInput, the same source the buttons use.
    /// </summary>
    public sealed class PartyPlayerMenu : MonoBehaviour
    {
        const float AboveHead = 38f, W = 130f, H = 60f, TapSeconds = 0.35f, TapMovePixels = 24f, OpenSeconds = 6f, MinHitPixels = 48f;
        const int MouseFinger = -100, NoFinger = -1;
        static PartyPlayerMenu instance;

        readonly List<RaycastResult> uiHits = new List<RaycastResult>(8);
        RectTransform layer, box;
        Text nameLabel;
        HeroController target;
        float closeAt, downAt;
        Vector2 downPos;
        int finger = NoFinger;
        bool downOnUi;

        public static void Create(Transform canvas)
        {
            RectTransform fill = UiKit.Fill(canvas, "PartyPlayerMenu");
            instance = fill.gameObject.AddComponent<PartyPlayerMenu>(); // full-screen layer: the box goes where the tap was
            instance.layer = fill;
            instance.Build();
        }

        void OnDestroy() { if (instance == this) instance = null; }

        /// <summary>Whose box is open (null when none).</summary>
        public static string OpenName { get { return instance != null && instance.target != null ? instance.target.PlayerName : null; } }

        void Build()
        {
            box = UiWinKit.Anchored(transform, "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), 0f, 0f, W, H);
            nameLabel = UiKit.Label(box, "Name", 0f, 0f, W, 22f, "", 15, Color.white, TextAnchor.MiddleCenter, true);
            PartyArt.ArtButton(box, "Invite", "btn-invite", (W - 110f) * 0.5f, 24f, 110f, 36f, "Invite", 15, UiWinKit.Cream, Invite);
            box.gameObject.SetActive(false);
        }

        void Invite()
        {
            if (target != null) NetClient.SendParty(PartyOp.Invite, target.PlayerId);
            Close();
        }

        void Close() { target = null; box.gameObject.SetActive(false); }

        /// <summary>Opens the box for `hero` at a screen point. Unlike a real tap it does not look at what is under the point (scripted captures use this).</summary>
        public static void OpenFor(HeroController hero, Vector2 screen)
        {
            if (instance == null || hero == null) return;
            instance.target = hero;
            instance.closeAt = Time.unscaledTime + OpenSeconds;
            instance.nameLabel.text = hero.PlayerName;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(instance.layer, screen, null, out local);
            Vector2 half = instance.layer.rect.size * 0.5f;
            local.x = Mathf.Clamp(local.x, -half.x + W * 0.5f + 4f, half.x - W * 0.5f - 4f);
            local.y = Mathf.Clamp(local.y, -half.y + 4f, half.y - H - 4f);
            instance.box.anchoredPosition = local;
            instance.box.gameObject.SetActive(true);
            instance.transform.SetAsLastSibling();
        }

        void Update()
        {
            bool usable = PartyPanel.Live && !InventoryPanel.IsOpen && !HeroChangeScreen.IsOpen && !UiWindows.BlocksGameplay;
            if (target != null || box.gameObject.activeSelf)
                if (!usable || target == null || target.IsDead || Time.unscaledTime >= closeAt) Close();
            Vector2 tap;
            if (!usable) { finger = NoFinger; return; }
            if (!TapEnded(out tap)) return;
            HeroController hit = HeroAt(tap);
            if (hit != null) OpenFor(hit, Camera.main.WorldToScreenPoint(new Vector3(hit.Position.x, hit.Position.y + AboveHead, 0f)));
            else Close();
        }

        /// <summary>True on the frame a quick tap ended. Touches rule while there are any: a phone also reports the first finger as the mouse.</summary>
        bool TapEnded(out Vector2 pos)
        {
            pos = Vector2.zero;
            BaseInput input = EventSystem.current != null && EventSystem.current.currentInputModule != null ? EventSystem.current.currentInputModule.input : null;
            if (input == null) return false;
            for (int i = 0; i < input.touchCount; i++)
            {
                Touch t = input.GetTouch(i);
                if (t.phase == TouchPhase.Began && finger == NoFinger) { finger = t.fingerId; PressedAt(t.position); }
                else if ((t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) && t.fingerId == finger)
                {
                    finger = NoFinger;
                    pos = t.position;
                    return t.phase == TouchPhase.Ended && IsQuick(pos);
                }
            }
            if (input.touchCount > 0) return false;
            if (input.GetMouseButtonDown(0)) { finger = MouseFinger; PressedAt(input.mousePosition); }
            else if (input.GetMouseButtonUp(0) && finger == MouseFinger)
            {
                finger = NoFinger;
                pos = input.mousePosition;
                return IsQuick(pos);
            }
            return false;
        }

        /// <summary>What lies under a press is settled here, not at the release: Invite has closed its box by then, and the tap would fall through to the hero behind it.</summary>
        void PressedAt(Vector2 pos) { downAt = Time.unscaledTime; downPos = pos; downOnUi = OverUi(pos); }

        bool IsQuick(Vector2 pos) { return !downOnUi && Time.unscaledTime - downAt <= TapSeconds && (pos - downPos).sqrMagnitude <= TapMovePixels * TapMovePixels; }

        /// <summary>A control took this tap. The joystick zone does not count: it lies over the world and has no use for a tap.</summary>
        bool OverUi(Vector2 screen)
        {
            if (EventSystem.current == null) return false;
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, uiHits);
            return uiHits.Count > 0 && uiHits[0].gameObject.GetComponent<TouchJoystick>() == null;
        }

        /// <summary>The player under a screen point, by body or name tag; generous on small screens, the nearest body or tag wins an overlap. Your own party is not offered.</summary>
        static HeroController HeroAt(Vector2 screen)
        {
            Camera cam = Camera.main;
            Zone zone = Zone.Main;
            if (cam == null || zone == null) return null;
            float scale = Screen.height / 640f;
            HeroController best = null;
            float bestSq = float.MaxValue;
            IReadOnlyList<HeroController> heroes = zone.Heroes.All;
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroController h = heroes[i];
                if (h == null || h == LocalPlayer.Hero || h.IsDead || h.PlayerId == 0 || NetClient.Party.Contains(h.PlayerId)) continue;
                Vector2 feet = cam.WorldToScreenPoint(new Vector3(h.Position.x, h.Position.y, 0f));
                Vector2 head = cam.WorldToScreenPoint(new Vector3(h.Position.x, h.Position.y + 26f, 0f));
                Vector2 tag = cam.WorldToScreenPoint(new Vector3(h.Position.x, h.Position.y + 30f, 0f));
                Vector2 middle = (feet + head) * 0.5f;
                float halfW = Mathf.Max(MinHitPixels * 0.5f * scale, (head.y - feet.y) * 0.5f), halfH = Mathf.Max(MinHitPixels * 0.5f * scale, (head.y - feet.y) * 0.6f);
                bool onBody = Mathf.Abs(screen.x - middle.x) <= halfW && Mathf.Abs(screen.y - middle.y) <= halfH;
                bool onTag = Mathf.Abs(screen.x - tag.x) <= 60f * scale && Mathf.Abs(screen.y - tag.y) <= 12f * scale;
                if (!onBody && !onTag) continue;
                float sq = Mathf.Min((screen - middle).sqrMagnitude, onTag ? (screen - tag).sqrMagnitude : float.MaxValue);
                if (sq < bestSq) { bestSq = sq; best = h; }
            }
            return best;
        }
    }
}
