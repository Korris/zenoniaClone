using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The bag window, MU Online style: worn gear and the inventory on one screen, in the gold-and-navy frame of the quest windows.
    /// Left: the hero between two rows of worn slots, Stats behind a button. Centre: 5 x 4 paged grid. Right: the selected item, compared with what is worn, with Equip / Unequip and Enhance.
    /// Tapping a worn slot filters the grid to what fits it; the chip clears the filter. A second tap within 0.35 s activates; arrows move the selection and J / Space activates.
    /// B / I toggles (RpgSession), Esc closes; the game is paused while it is open.
    /// Partial: InventoryPanel.Loadout.cs (left), InventoryPanel.Bag.cs (centre), InventoryPanel.Detail.cs (right), InventoryPanel.Input.cs (keys and taps). Mock: ArtLibrary/custom/inventory-ui-ref/bag-layout-mock-1.png.
    /// What is listed / selected is BagModel, what an action does to the player is BagActions: both shared with the Armory Carousel (ArmoryWindow), which UiScreens opens instead when its switch is on.
    /// </summary>
    public partial class InventoryPanel : MonoBehaviour
    {
        const float W = 1052f, H = 556f, PanelY = 70f, PanelH = 460f;
        const float LeftW = 300f, CentreW = 402f, RightW = 290f, Gap = 6f;
        const float DoubleClickSeconds = 0.35f, MessageSeconds = 1.5f;
        const int WornClickBase = 1000; // keeps worn slots and grid cells apart in the double-tap check
        static InventoryPanel instance;

        readonly BagModel model = new BagModel(); // its defaults are this window: 20 cells a page, gold and empty stacks listed, all 50 slots
        RectTransform overlay, window;
        Text messageText;
        float messageUntil;
        float lastClickTime;
        int lastClickIndex = -1;
        bool confirmDiscard;

        public static bool IsOpen { get { return instance != null && instance.overlay.gameObject.activeSelf; } }

        /// <summary>Online: the server pushed a new state, redraw the open bag.</summary>
        public static void RefreshIfOpen() { if (IsOpen) instance.Refresh(); }

        public static void Toggle(Transform root) { if (IsOpen) Close(); else Open(root); }

        public static void Open(Transform root)
        {
            if (RpgSession.Player == null) return;
            if (instance == null)
            {
                var go = new GameObject("InventoryPanel");
                go.transform.SetParent(root, false);
                instance = go.AddComponent<InventoryPanel>();
                instance.Build(UiKit.Canvas(root).transform);
            }
            instance.overlay.gameObject.SetActive(true);
            instance.overlay.SetAsLastSibling();
            instance.messageUntil = 0f;
            instance.Refresh();
            GameClock.Paused = NetMode.UsesLocalTimeBending; // a shared zone cannot freeze for one player
        }

        public static void Close()
        {
            if (instance == null) return;
            instance.overlay.gameObject.SetActive(false);
            instance.confirmDiscard = false;
            instance.heroView.Stop();
            GameClock.Paused = false;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        void OnDestroy()
        {
            if (instance != this) return;
            instance = null;
            GameClock.Paused = false;
        }

        // ---- build ----

        void Build(Transform canvas)
        {
            overlay = UiKit.Fill(canvas, "BagOverlay");
            UiKit.Panel(overlay, new Color(0f, 0f, 0f, 0.5f), true); // also stops taps reaching the game
            RectTransform win = UiWinKit.Anchored(overlay, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, W, H);
            window = win;
            UiWinKit.Sliced(win, "panel", 0.9f, true);

            Image bagIcon = UiKit.Icon(win, "BagIcon", 25f, 7f, 52f);
            bagIcon.sprite = BagSprites.BagIcon();
            BagWidgets.Style(UiKit.Label(win, "Title", 82f, 10f, 80f, 46f, "Bag", 30, new Color(1f, 0.93f, 0.7f), TextAnchor.MiddleLeft, true), true);
            BuildTab(win, 150f, "tab-active", "Bag", null);
            BuildTab(win, 286f, "tab-inactive", "Skill", () => { Close(); UiScreens.Open(UiScreen.Skill); });
            BagWidgets.Divider(win, "HeaderRule", 432f, 36f, W - 510f);
            UiWinKit.CloseButton(win, W - 62f, 14f, 42f, Close);

            BuildLoadout(Section(win, "Left", 24f, LeftW));
            BuildBag(Section(win, "Centre", 24f + LeftW + Gap, CentreW));
            BuildDetail(Section(win, "Right", 24f + LeftW + Gap + CentreW + Gap, RightW));
            overlay.gameObject.SetActive(false);
        }

        void FitWindow()
        {
            float scale = Mathf.Min(1f, (overlay.rect.width - 16f) / W, (overlay.rect.height - 16f) / H);
            window.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }

        static void BuildTab(RectTransform win, float x, string piece, string text, System.Action onClick)
        {
            RectTransform rt = UiKit.Rect(win, "Tab" + text, x, 16f, 128f, 40f);
            Image img = UiWinKit.Sliced(rt, piece, 0f);
            BagWidgets.Style(UiKit.Label(UiKit.Fill(rt, "Label"), text, 21, UiWinKit.Cream, TextAnchor.MiddleCenter, true), true);
            if (onClick != null) UiWinKit.MakeButton(img, onClick);
        }

        static RectTransform Section(RectTransform win, string name, float x, float w)
        {
            RectTransform rt = UiKit.Rect(win, name, x, PanelY, w, PanelH);
            UiWinKit.Sliced(rt, "panel", 0.18f); // thin line and small corners: only the outer frame is heavy
            return rt;
        }

        // ---- actions (BagActions does the work; here only what this window shows afterwards) ----

        /// <summary>Drink a selected potion, equip a bag item, unequip a worn one.</summary>
        void Activate()
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            BagSupply kind;
            bool worn = model.Worn >= 0;
            if (!worn && model.SupplyAt(model.Cell, out kind))
            {
                if (BagModel.IsPotion(kind)) Show(BagActions.UsePotion(p, kind)); else Refresh();
                return;
            }
            Item item = model.Selected(p);
            if (item == null) return;
            BagResult r = worn ? BagActions.Unequip(p, item) : BagActions.Equip(p, item);
            if (r.Ok && !worn) model.Worn = Mathf.Max(0, Slots.Index(item.slot)); // online: the server's reply redraws
            Show(r);
        }

        void EnhanceSelected()
        {
            PlayerState p = RpgSession.Player;
            Show(BagActions.Enhance(p, p != null ? model.Selected(p) : null));
        }

        /// <summary>Tells the player the outcome and redraws, unless the server will (its reply redraws the open bag).</summary>
        void Show(BagResult r)
        {
            if (r.Message != null) Flash(r.Message);
            if (!r.Pending) Refresh();
        }

        void Flash(string text)
        {
            messageText.text = text;
            messageUntil = Time.unscaledTime + MessageSeconds;
        }

        // ---- refresh ----

        void Refresh()
        {
            PlayerState p = RpgSession.Player;
            HeroController hero = GameSession.Hero;
            if (p == null || hero == null) { Close(); return; }
            FitWindow();
            RefreshLoadout(p, hero);
            RefreshBag(p);
            RefreshDetail(p);
        }
    }
}
