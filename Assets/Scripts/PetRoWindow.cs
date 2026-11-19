using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using PW = ZenoniaProto.PetRoWidgets;
using W = ZenoniaProto.InventoryRoWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// The RO Soft Fantasy Pet window, built to the approved mockup docs/reference/ui-style-ro-soft-fantasy/05-pet.png (the default under --ui-style ro;
    /// --ui-style classic keeps the old PetWindow, --ui-new pet the Adventure Paper Pet Camp). Header: paw, "Pet", ?, Gold / Pet Crystals / gacha coins /
    /// Pet Food / balls (normal / rare), close. Left menu: Team / Collection / Growth / Capture. Team: the Combat + two Support slots on pedestals with
    /// swap buttons, the two Stat slots as chips, Team Bonus, team level; the pet list with filters, sort and pages. Right: the selected pet's card,
    /// Info / Skills / Bond tabs, Level up / Star up / Set to team.
    /// Same data and actions as the other two looks: PlayerState.Team + AccountState, every change through PetActions.Send (online: the server decides;
    /// the window redraws when PetViewModel.StateKey changes). P toggles, Esc closes (UiWindows modal).
    /// Partial: .Team, .List, .Detail (+ .DetailTabs), .Pages (Collection / Growth / Capture), .Testing.
    /// </summary>
    public sealed partial class PetRoWindow : MonoBehaviour
    {
        public enum Page { Team, Collection, Growth, Capture }

        const float WinW = 1124f, WinH = 626f, MessageSeconds = 2.2f;
        /// <summary>Inner white boxes at 80 % alpha: their blue rim reads as softly as the mockup's (ChatGPT round 1: "borders too sharp").</summary>
        static readonly Color SoftBox = new Color(1f, 1f, 1f, 0.8f);
        const float NavX = 8f, NavW = 152f, CentreX = 166f, CentreW = 543f, RightX = 716f, RightW = 401f, PanelY = 72f;
        static PetRoWindow instance;
        static int closedFrame = -1;

        RectTransform overlay, window, teamPage, listPage;
        readonly RectTransform[] pages = new RectTransform[3]; // Collection, Growth, Capture
        readonly PW.NavButton[] nav = new PW.NavButton[4];
        Text goldText, crystalText, coinText, foodText, ballText, messageText;
        RectTransform messagePill;
        float messageUntil;
        int selected = -1, shownState, openedFrame;
        Page page;

        public static bool IsOpen { get { return instance != null && instance.overlay.gameObject.activeSelf; } }

        static PetRoWindow() { UiWindows.RegisterModal(() => IsOpen, Close); }

        /// <summary>The RO look opens this window (UiScreens.Open) unless the classic look or the paper Pet Camp is chosen.</summary>
        public static bool Wanted { get { return UiStyle.Ro && !UiScreens.UseNew(UiScreen.Pet); } }

        public static void Open()
        {
            PlayerState p = PetActions.Me;
            if (p == null || UiScreens.Root == null || Time.frameCount == closedFrame) return; // closedFrame: P just closed it
            if (instance == null)
            {
                var go = new GameObject("PetRoWindow");
                go.transform.SetParent(UiScreens.Root, false);
                instance = go.AddComponent<PetRoWindow>();
                instance.Build(UiKit.Canvas(UiScreens.Root).transform);
            }
            instance.overlay.gameObject.SetActive(true);
            instance.overlay.SetAsLastSibling();
            NotificationToast toasts = FindFirstObjectByType<NotificationToast>(); // "Frost Slime reached level 2" shows over this window
            if (toasts != null && toasts.transform.parent == instance.overlay.parent) toasts.transform.SetAsLastSibling();
            instance.selected = p.PetId != "" ? PetConfig.IndexOf(p.Pet) : instance.FirstOwned(p.Account); // starts on the combat pet like the other looks
            instance.openedFrame = Time.frameCount;
            instance.messageUntil = 0f;
            instance.listPageIndex = 0;
            instance.detailTab = 0;
            instance.SetPage(Page.Team);
        }

        public static void Close()
        {
            if (!IsOpen) return;
            instance.CloseDropdown();
            instance.overlay.gameObject.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        void OnDestroy() { if (instance == this) instance = null; }

        // ---- build ----

        void Build(Transform canvas)
        {
            overlay = UiKit.Fill(canvas, "PetRoOverlay");
            UiKit.Panel(overlay, new Color(0.16f, 0.22f, 0.3f, 0.22f), true); // stops taps reaching the game
            window = UiWinKit.Anchored(overlay, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, WinW, WinH);
            W.Img(window, "Frame", 0f, 0f, WinW, WinH, "bag-window", true);
            BuildHeader();
            BuildNav(W.Img(window, "Nav", NavX, PanelY, NavW, WinH - PanelY - 10f, "bag-section").rectTransform);
            teamPage = W.Img(window, "TeamBox", CentreX, PanelY, CentreW, 292f, "bag-section").rectTransform;
            listPage = W.Img(window, "ListBox", CentreX, PanelY + 300f, CentreW, WinH - PanelY - 310f, "bag-section").rectTransform;
            BuildTeam(teamPage);
            BuildList(listPage);
            string[] names = { "CollectionPage", "GrowthPage", "CapturePage" };
            for (int i = 0; i < pages.Length; i++) pages[i] = W.Img(window, names[i], CentreX, PanelY, CentreW, WinH - PanelY - 10f, "bag-section").rectTransform;
            BuildCollection(pages[0]);
            BuildGrowth(pages[1]);
            BuildCapture(pages[2]);
            BuildDetail(W.Img(window, "Right", RightX, PanelY, RightW, WinH - PanelY - 10f, "bag-section").rectTransform);
            BuildPopups();
            Image msg = W.Img(window, "Message", WinW * 0.5f - 260f, WinH - 22f, 520f, 36f, "bag-pill-white");
            msg.pixelsPerUnitMultiplier = 104f / 72f;
            messagePill = msg.rectTransform;
            messageText = W.Txt(messagePill, "Text", 14f, 0f, 492f, 34f, "", 15, W.Ink, TextAnchor.MiddleCenter);
            overlay.gameObject.SetActive(false);
        }

        void BuildHeader()
        {
            W.Img(window, "Paw", 16f, 2f, 66f, 66f, "pet-title-paw");
            Text title = W.Txt(window, "Title", 88f, 8f, 120f, 52f, "Pet", 34, RoSoftTheme.Ink);
            title.gameObject.AddComponent<Shadow>().effectColor = new Color(1f, 1f, 1f, 0.8f);
            W.Clickable(W.Img(window, "Help", 88f + title.preferredWidth + 10f, 20f, 30f, 30f, "pet-btn-help", true), () => Flash(PetViewModel.RulesText()));

            Image gold = W.Img(window, "GoldPill", 298f, 14f, 172f, 41f, "bag-gold-pill"); // the coin is painted on its left end
            goldText = W.Txt(gold.transform, "Gold", 44f, 0f, 88f, 38f, "", 17, W.Ink, TextAnchor.MiddleRight);
            Plus(gold.rectTransform, 135f, () => Flash("Gold comes from monsters, quests and selling gear."));
            crystalText = Counter(476f, 132f, "Crystals", W.Art("bag-gem"), () => Flash(PetGachaText.CrystalSource));
            coinText = Counter(618f, 132f, "Coins", PW.Picture("Sprites/ui/paper/kit/icon-gacha-coin"), OpenGachaShop);
            foodText = Counter(760f, 124f, "Food", W.Art("pet-ico-food"), () => Flash("Pet Food: " + PetViewModel.Pct(PetRules.MonsterFoodChance) + " from monsters, " + PetRules.BossFood + " per boss, " + PetRules.WantedFood + " per Wanted Boss."));
            ballText = Counter(894f, 152f, "Balls", PW.Picture("Sprites/ui/paper/kit/icon-capture-ball"), () => SetPage(Page.Capture));
            W.Clickable(W.Img(window, "Close", 1058f, 8f, 54f, 54f, "bag-close", true), Close);
        }

        /// <summary>A white counter pill: icon on the left end, number right-aligned, "+" on the right end.</summary>
        Text Counter(float x, float w, string name, Sprite icon, UnityEngine.Events.UnityAction plus)
        {
            Image pill = W.Img(window, name + "Pill", x, 15f, w, 40f, "bag-pill-white");
            pill.pixelsPerUnitMultiplier = 104f / 80f;
            Image ico = UiKit.Rect(pill.transform, "Icon", 3f, 1f, 37f, 37f).gameObject.AddComponent<Image>();
            ico.sprite = icon; ico.preserveAspect = true; ico.raycastTarget = false; ico.enabled = icon != null;
            Text t = W.Txt(pill.transform, "Value", 40f, 0f, w - 40f - 38f, 39f, "", 18, W.Ink, TextAnchor.MiddleRight);
            Plus(pill.rectTransform, w - 34f, plus);
            return t;
        }

        static void Plus(RectTransform pill, float x, UnityEngine.Events.UnityAction onClick) { W.Clickable(W.Img(pill, "Plus", x, 3f, 34f, 34f, "btn-plus", true), onClick); }

        void BuildNav(RectTransform panel)
        {
            string[] icons = { "pet-nav-team", "pet-nav-collection", "pet-nav-growth", "pet-nav-capture" };
            string[] labels = { "Team", "Collection", "Growth", "Capture" };
            for (int i = 0; i < nav.Length; i++)
            {
                Page pg = (Page)i;
                nav[i] = new PW.NavButton(panel, "Nav" + labels[i], 6f, 14f + i * 66f, NavW - 12f, icons[i], labels[i], () => SetPage(pg));
            }
            Image cat = W.Img(panel, "Mascot", 18f, panel.sizeDelta.y - 100f, 116f, 74f, "pet-mascot-watermark");
            cat.color = new Color(1f, 1f, 1f, 0.55f);
        }

        void OpenGachaShop()
        {
            Close();
            UiScreens.Open(UiScreen.Gacha); // coins are bought with Pet Crystals at the gacha
        }

        // ---- page, selection, messages ----

        void SetPage(Page pg)
        {
            page = pg;
            CloseDropdown();
            for (int i = 0; i < nav.Length; i++) nav[i].Set(i == (int)pg);
            teamPage.gameObject.SetActive(pg == Page.Team);
            listPage.gameObject.SetActive(pg == Page.Team);
            for (int i = 0; i < pages.Length; i++) pages[i].gameObject.SetActive((int)pg == i + 1);
            Refresh();
        }

        int FirstOwned(AccountState a)
        {
            if (a == null) return -1;
            for (int i = 0; i < PetConfig.All.Length; i++) if (a.OwnsPet(PetConfig.All[i].Id)) return i;
            return -1;
        }

        PetDef Selected { get { return PetConfig.ByIndex(selected); } }

        void Select(int petIndex) { selected = petIndex; Refresh(); }

        void Flash(string text)
        {
            messageText.text = text;
            messageUntil = Time.unscaledTime + MessageSeconds + text.Length * 0.02f;
            messageText.fontSize = 15;
            while (messageText.fontSize > 10 && messageText.preferredWidth > 492f * 2f) messageText.fontSize--;
            messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        static string N(int v) { return v.ToString("N0", CultureInfo.InvariantCulture); }

        void Update()
        {
            PlayerState p = PetActions.Me;
            if (p == null) { Close(); return; }
            if (Time.frameCount != openedFrame && InputAdapter.WindowPressed(GameKey.PetWindow)) { closedFrame = Time.frameCount; Close(); return; }
            messagePill.gameObject.SetActive(Time.unscaledTime < messageUntil);
            if (PetViewModel.StateKey(p, p.Account) != shownState) Refresh(); // the server answered, a pet was caught, food dropped ...
        }

        void FitWindow()
        {
            float scale = Mathf.Min(1f, (overlay.rect.width * 0.94f) / WinW, (overlay.rect.height - 40f) / (WinH + 12f));
            window.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }

        void Refresh()
        {
            PlayerState p = PetActions.Me;
            if (p == null) { Close(); return; }
            AccountState a = p.Account;
            FitWindow();
            goldText.text = N(p.Gold) + " G";
            crystalText.text = N(a != null ? a.crystals : 0);
            coinText.text = N(a != null ? a.tickets : 0);
            foodText.text = N(a != null ? a.food : 0);
            ballText.text = (a != null ? a.balls : 0) + " / " + (a != null ? a.rareBalls : 0);
            if (page == Page.Team) { RefreshTeam(p, a); RefreshList(p, a); }
            else if (page == Page.Collection) RefreshCollection(p, a);
            else if (page == Page.Growth) RefreshGrowth(p, a);
            else RefreshCapture(p, a);
            RefreshDetail(p, a);
            shownState = PetViewModel.StateKey(p, a);
        }

        /// <summary>Sends an action; the reply (offline at once, online from the server) changes StateKey and Update redraws.</summary>
        static void Send(InvOp op, byte slot, string uid = "") { PetActions.Send(op, slot, uid); }
    }
}
