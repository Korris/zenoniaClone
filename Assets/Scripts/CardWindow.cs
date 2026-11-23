using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// The Card window (mockups docs/reference/card-gem-ui/01..05): ChatGPT window frame, the gold pill and a close button on top, the shared left menu (My Cards / Monster Album /
    /// Convert to Soul Gem / Gem Socketing / Gem Removal / Card Duel) and the chosen page on the right. Pages are GemPage objects built once and redrawn when
    /// GemUiModel.StateKey changes (the server answered, a card dropped, gold changed). Card Duel is listed but not playable until its phase: DuelReady stays false and the entry is greyed.
    /// Opens from the More column of the HUD (UiScreens.Open(UiScreen.Card)); Esc closes (UiWindows modal). Every action is an InvOp the server decides.
    /// </summary>
    public sealed class CardWindow : MonoBehaviour
    {
        public enum Page { MyCards, Album, Convert, Socket, Remove, Duel }

        /// <summary>The Card Duel phase flips this when its page exists; until then the menu entry is greyed and says "coming soon".</summary>
        public static bool DuelReady = false;

        const float WinW = 1124f, WinH = 626f, NavX = 8f, NavW = 152f, ContentX = 166f, ContentY = 72f;
        static CardWindow instance;

        RectTransform overlay, window;
        GemPage[] pages;
        CardMyCardsPage myCards;
        CardAlbumPage album;
        readonly Image[] navSkin = new Image[6];
        Text goldText;
        string shownKey = "";
        Page page;
        CardConfirmBox confirm;

        public static bool IsOpen { get { return instance != null && instance.overlay != null && instance.overlay.gameObject.activeSelf; } }

        static CardWindow() { UiWindows.RegisterModal(() => IsOpen, Close); }

        public static void Open(Page start = Page.MyCards)
        {
            if (PetActions.Me == null || UiScreens.Root == null) return;
            if (instance == null)
            {
                var go = new GameObject("CardWindow");
                go.transform.SetParent(UiScreens.Root, false);
                instance = go.AddComponent<CardWindow>();
                instance.Build(UiKit.Canvas(UiScreens.Root).transform);
            }
            instance.overlay.gameObject.SetActive(true);
            instance.overlay.SetAsLastSibling();
            NotificationToast toasts = FindFirstObjectByType<NotificationToast>(); // "Sold 1 x ..." shows over this window
            if (toasts != null && toasts.transform.parent == instance.overlay.parent) toasts.transform.SetAsLastSibling();
            instance.SetPage(start);
        }

        public static void Close()
        {
            if (!IsOpen) return;
            CardConfirmBox.Close();
            instance.overlay.gameObject.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        /// <summary>"View in Album" / "View in My Cards": jumps to the other list with that card chosen.</summary>
        public static void Show(Page p, string cardId)
        {
            if (!IsOpen) return;
            if (p == Page.Album) instance.album.Focus(cardId); else if (p == Page.MyCards) instance.myCards.Focus(cardId);
            instance.SetPage(p);
        }

        void OnDestroy() { CardConfirmBox.Forget(confirm); if (instance == this) instance = null; }

        void Build(Transform canvas)
        {
            overlay = UiKit.Fill(canvas, "CardOverlay");
            UiKit.Panel(overlay, new Color(0.16f, 0.22f, 0.3f, 0.3f), true); // stops taps reaching the game (a veil, not art)
            window = UiWinKit.Anchored(overlay, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, WinW, WinH);
            GemUiArt.Img(window, "Frame", 0f, 0f, WinW, WinH, GemUiArt.Frame("window-frame"), true);
            U.Txt(window, "Title", 40f, 14f, 320f, 44f, "Monster Card", 30, U.Ink);
            GemUiArt.Img(window, "GoldPill", WinW - 330f, 14f, 240f, 44f, GemUiArt.Frame("gold-pill"));
            GemUiArt.Img(window, "GoldIcon", WinW - 322f, 18f, 36f, 36f, GemUiArt.Icon("icon-gold"));
            goldText = U.Txt(window, "Gold", WinW - 280f, 14f, 180f, 44f, "", 22, U.Ink, TextAnchor.MiddleRight);
            UiWinKit.MakeButton(GemUiArt.Img(window, "Close", WinW - 74f, 12f, 48f, 50f, GemUiArt.Frame("btn-close"), true), Close);
            string[] names = { "My Cards", "Monster Album", "Convert to Soul Gem", "Gem Socketing", "Gem Removal", "Card Duel" };
            string[] icons = { "menu-my-cards", "menu-album", "menu-convert", "menu-socket", "menu-remove", "menu-duel" };
            for (int i = 0; i < 6; i++) BuildNav(i, names[i], icons[i]);
            myCards = new CardMyCardsPage(); album = new CardAlbumPage();
            pages = new GemPage[] { myCards, album, new GemConvertPage(), new GemSocketPage(), new GemRemovePage(), null };
            RectTransform content = UiKit.Rect(window, "Content", ContentX, ContentY, GemPage.PageW, GemPage.PageH);
            foreach (GemPage g in pages) if (g != null) g.Build(content);
            confirm = new CardConfirmBox(UiKit.Fill(window, "ConfirmLayer"), WinW, WinH);
            overlay.gameObject.SetActive(false);
        }

        void BuildNav(int i, string text, string icon)
        {
            const float H = 68f, Pitch = 76f, PadL = 12f, Gap = 6f, IconS = 36f;
            bool off = i == (int)Page.Duel && !DuelReady;
            RectTransform root = UiKit.Rect(window, "Nav" + i, NavX, ContentY + 10f + i * Pitch, NavW, H);
            navSkin[i] = root.gameObject.AddComponent<Image>();
            int index = i;
            UiWinKit.MakeButton(navSkin[i], () => OnNav((Page)index));
            Image ic = GemUiArt.Img(root, "Icon", PadL, (H - IconS) * 0.5f, IconS, IconS, GemUiArt.Icon(icon));
            Text label = U.Txt(root, "Label", PadL + IconS + Gap, 4f, NavW - PadL - IconS - Gap - 4f, H - 8f, off ? text + "\n(coming soon)" : text, 16, off ? U.InkSoft : U.Ink, TextAnchor.MiddleLeft);
            if (off) { ic.color = new Color(1f, 1f, 1f, 0.45f); label.fontSize = 15; }
        }

        void OnNav(Page p)
        {
            if (p == Page.Duel && !DuelReady) { RpgHud.Toast("Card Duel is coming soon", new Color(1f, 0.9f, 0.6f)); return; }
            SetPage(p);
        }

        void SetPage(Page p)
        {
            CardConfirmBox.Close();
            page = p;
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null) pages[i].Root.gameObject.SetActive(i == (int)p);
                bool off = i == (int)Page.Duel && !DuelReady;
                GemUiArt.SetPiece(navSkin[i], GemUiArt.Frame(i == (int)p ? "menu-btn-selected" : "menu-btn"));
                if (off) navSkin[i].color = new Color(1f, 1f, 1f, 0.6f);
            }
            FitWindow();
            pages[(int)p].ClearPending();
            pages[(int)p].OnShow();
            shownKey = GemUiModel.StateKey(PetActions.Me);
            RefreshGold();
        }

        void FitWindow()
        {
            float scale = Mathf.Min(1f, (overlay.rect.width * 0.96f) / WinW, (overlay.rect.height - 8f) / WinH);
            window.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }

        void RefreshGold()
        {
            PlayerState p = PetActions.Me;
            goldText.text = p != null ? p.Gold.ToString("N0", CultureInfo.InvariantCulture) + " G" : "";
        }

        void Update()
        {
            PlayerState p = PetActions.Me;
            if (p == null) { Close(); return; }
            GemPage current = pages[(int)page];
            string key = GemUiModel.StateKey(p);
            if (key != shownKey)
            {
                shownKey = key; // the server answered, a card dropped, gold changed ...
                RefreshGold();
                current.ClearPending();
                current.Refresh();
            }
            current.Tick();
        }
    }
}
