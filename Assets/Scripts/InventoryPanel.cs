using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using W = ZenoniaProto.InventoryRoWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// "Bag &amp; Equipment", the RO Soft Fantasy bag, built to the approved mockup docs/reference/ui-style-ro-soft-fantasy/03-bag.png
    /// (every piece painted by ChatGPT, see InventoryRoWidgets). Header: mascot, title, Gold and Pet Crystal counters, close.
    /// Left: the hero in his real gear on the castle stage between the 8 worn slots, Lv, Power, Stats and Enhance. Centre: 6 category tabs,
    /// Bag (n/50), Sort dropdown, an 8 x 8 grid (supplies first, then the 50 bag slots, "+" = free). Right: the selected item, Equipped vs
    /// Selected stats, Effect, Equip / Enhance / Discard.
    /// A second tap within 0.35 s activates; arrows move the selection and J / Space activates. B / I toggles (RpgSession), Esc closes.
    /// Partial: .Loadout (left), .Bag (centre), .Detail (right), .Input (keys, taps). What is listed / selected is BagModel, what an action
    /// does is BagActions (online: the server decides and its reply redraws): both shared with the Armory Carousel (ArmoryWindow).
    /// </summary>
    public partial class InventoryPanel : MonoBehaviour
    {
        const float WinW = 1124f, WinH = 616f, PanelY = 70f, PanelH = 536f;
        const float LeftX = 8f, LeftW = 326f, CentreX = 342f, CentreW = 426f, RightX = 776f, RightW = 340f;
        const float DoubleClickSeconds = 0.35f, MessageSeconds = 1.8f;
        const int WornClickBase = 1000; // keeps worn slots and grid cells apart in the double-tap check
        static InventoryPanel instance;

        readonly BagModel model = new BagModel { PageSize = 64, ListCoin = false, ListEmptySupplies = false }; // gold is in the header; one page holds everything
        RectTransform overlay, window;
        Text messageText, goldText, gemText;
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
            instance.CloseSortList();
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
            UiKit.Panel(overlay, new Color(0.16f, 0.22f, 0.3f, 0.22f), true); // also stops taps reaching the game
            window = UiWinKit.Anchored(overlay, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, WinW, WinH);
            W.Img(window, "Frame", 0f, 0f, WinW, WinH, "bag-window", true);
            BuildHeader(window);
            BuildLoadout(W.Img(window, "Left", LeftX, PanelY, LeftW, PanelH, "bag-section").rectTransform);
            BuildBag(W.Img(window, "Centre", CentreX, PanelY - 4f, CentreW, PanelH + 4f, "bag-section").rectTransform);
            BuildDetail(W.Img(window, "Right", RightX, PanelY, RightW, PanelH, "bag-section").rectTransform);
            BuildStatsPopup(window);
            overlay.gameObject.SetActive(false);
        }

        void BuildHeader(RectTransform win)
        {
            W.Img(win, "Mascot", 44f, -3f, 98f, 74f, "bag-mascot");
            Text title = W.Txt(win, "Title", 148f, 8f, 360f, 50f, "Bag & Equipment", 34, RoSoftTheme.Ink);
            title.gameObject.AddComponent<Shadow>().effectColor = new Color(1f, 1f, 1f, 0.8f);
            W.Img(win, "TitleStar", 148f + title.preferredWidth + 8f, 12f, 22f, 22f, "bag-stat-crit"); // the gold sparkle after the title
            messageText = W.Txt(win, "Message", 420f, 16f, 250f, 40f, "", 15, RoSoftTheme.GoldText, TextAnchor.MiddleCenter);
            messageText.horizontalOverflow = HorizontalWrapMode.Wrap;

            Image gold = W.Img(win, "GoldPill", 680f, 14f, 186f, 38f, "bag-gold-pill"); // coin painted on its left end
            goldText = W.Txt(gold.transform, "Gold", 40f, 0f, 104f, 35f, "", 18, W.Ink, TextAnchor.MiddleRight);
            W.Clickable(W.Img(gold.transform, "Plus", 150f, 2f, 32f, 32f, "btn-plus", true), () => Flash("Gold comes from monsters, quests and selling gear."));

            Image gem = W.Img(win, "GemPill", 876f, 14f, 172f, 36f, "bag-pill-white");
            gem.color = RoSoftTheme.Hex("#EEF5FC"); // a hint of ice blue
            W.Img(gem.transform, "Gem", 6f, 2f, 32f, 32f, "bag-gem");
            gemText = W.Txt(gem.transform, "Crystals", 40f, 0f, 90f, 35f, "", 18, W.Ink, TextAnchor.MiddleRight);
            W.Clickable(W.Img(gem.transform, "Plus", 136f, 2f, 32f, 32f, "btn-plus", true), () => Flash(PetGachaText.CrystalSource));

            W.Clickable(W.Img(win, "Close", 1066f, 10f, 46f, 46f, "bag-close", true), Close);
        }

        void FitWindow()
        {
            float scale = Mathf.Min(1f, (overlay.rect.width * 0.94f) / WinW, (overlay.rect.height - 40f) / (WinH + 12f)); // margins as in the mockup
            window.localScale = Vector3.one * Mathf.Max(0.1f, scale);
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
            goldText.text = p.Gold.ToString("N0", CultureInfo.InvariantCulture) + " G";
            gemText.text = (p.Account != null ? p.Account.crystals : 0).ToString("N0", CultureInfo.InvariantCulture);
            RefreshLoadout(p, hero);
            RefreshBag(p);
            RefreshDetail(p);
        }
    }
}
