#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// Window shell of the three Soul Gem screens (Convert to Soul Gem / Gem Socketing / Gem Removal): ChatGPT window frame, the gold pill and a close button on top,
    /// the three menu entries on the left, the chosen page on the right. It is the stand-in until phase 03's CardWindow exists: that window's left menu already lists these
    /// three entries and will host the same GemPage objects (GemConvertPage / GemSocketPage / GemRemovePage), after which this shell is deleted.
    /// Until then it opens from the F8 test key (editor and development builds), or GemWindow.Open(Page). Esc closes (UiWindows modal). Every action is an InvOp the server decides.
    /// </summary>
    public sealed class GemWindow : MonoBehaviour
    {
        public enum Page { Convert, Socket, Remove }

        const float WinW = 1124f, WinH = 626f, NavX = 8f, NavW = 152f, ContentX = 166f, ContentY = 72f;
        static GemWindow instance;

        RectTransform overlay, window;
        readonly GemPage[] pages = new GemPage[3];
        readonly Image[] navSkin = new Image[3];
        Text goldText;
        string shownKey = "";
        Page page;

        public static bool IsOpen { get { return instance != null && instance.overlay != null && instance.overlay.gameObject.activeSelf; } }

        static GemWindow() { UiWindows.RegisterModal(() => IsOpen, Close); }

        public static void Open(Page start = Page.Convert)
        {
            if (PetActions.Me == null || UiScreens.Root == null) return;
            if (instance == null)
            {
                var go = new GameObject("GemWindow");
                go.transform.SetParent(UiScreens.Root, false);
                instance = go.AddComponent<GemWindow>();
                instance.Build(UiKit.Canvas(UiScreens.Root).transform);
            }
            instance.overlay.gameObject.SetActive(true);
            instance.overlay.SetAsLastSibling();
            NotificationToast toasts = FindFirstObjectByType<NotificationToast>(); // the "Gem made" line shows over this window
            if (toasts != null && toasts.transform.parent == instance.overlay.parent) toasts.transform.SetAsLastSibling();
            instance.SetPage(start);
        }

        public static void Close()
        {
            if (!IsOpen) return;
            instance.overlay.gameObject.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        void OnDestroy() { if (instance == this) instance = null; }

        void Build(Transform canvas)
        {
            overlay = UiKit.Fill(canvas, "GemOverlay");
            UiKit.Panel(overlay, new Color(0.16f, 0.22f, 0.3f, 0.3f), true); // stops taps reaching the game (a veil, not art)
            window = UiWinKit.Anchored(overlay, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, WinW, WinH);
            GemUiArt.Img(window, "Frame", 0f, 0f, WinW, WinH, GemUiArt.Frame("window-frame"), true);
            U.Txt(window, "Title", 40f, 14f, 320f, 44f, "Monster Card", 30, U.Ink);
            GemUiArt.Img(window, "GoldPill", WinW - 330f, 14f, 240f, 44f, GemUiArt.Frame("gold-pill"));
            GemUiArt.Img(window, "GoldIcon", WinW - 322f, 18f, 36f, 36f, GemUiArt.Icon("icon-gold"));
            goldText = U.Txt(window, "Gold", WinW - 280f, 14f, 180f, 44f, "", 22, U.Ink, TextAnchor.MiddleRight);
            UiWinKit.MakeButton(GemUiArt.Img(window, "Close", WinW - 74f, 12f, 48f, 50f, GemUiArt.Frame("btn-close"), true), Close);
            string[] names = { "Convert to Soul Gem", "Gem Socketing", "Gem Removal" };
            string[] icons = { "menu-convert", "menu-socket", "menu-remove" };
            for (int i = 0; i < 3; i++) BuildNav(i, names[i], icons[i]);
            pages[0] = new GemConvertPage(); pages[1] = new GemSocketPage(); pages[2] = new GemRemovePage();
            RectTransform content = UiKit.Rect(window, "Content", ContentX, ContentY, GemPage.PageW, GemPage.PageH);
            foreach (GemPage g in pages) g.Build(content);
            overlay.gameObject.SetActive(false);
        }

        void BuildNav(int i, string text, string icon)
        {
            const float H = 68f, Pitch = 76f, PadL = 12f, Gap = 6f, IconS = 36f;
            RectTransform root = UiKit.Rect(window, "Nav" + i, NavX, ContentY + 10f + i * Pitch, NavW, H);
            navSkin[i] = root.gameObject.AddComponent<Image>();
            int index = i;
            UiWinKit.MakeButton(navSkin[i], () => SetPage((Page)index));
            GemUiArt.Img(root, "Icon", PadL, (H - IconS) * 0.5f, IconS, IconS, GemUiArt.Icon(icon));
            U.Txt(root, "Label", PadL + IconS + Gap, 4f, NavW - PadL - IconS - Gap - 4f, H - 8f, text, 16, U.Ink, TextAnchor.MiddleLeft);
        }

        void SetPage(Page p)
        {
            page = p;
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].Root.gameObject.SetActive(i == (int)p);
                GemUiArt.SetPiece(navSkin[i], GemUiArt.Frame(i == (int)p ? "menu-btn-selected" : "menu-btn"));
            }
            FitWindow();
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
            string key = GemUiModel.StateKey(p);
            if (key == shownKey) return; // the server answered, a card dropped, gold changed ...
            shownKey = key;
            RefreshGold();
            pages[(int)page].Refresh();
        }

        // ---- test key ----

        static bool KeyPressed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.F8);
#elif ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb[Key.F8].wasPressedThisFrame;
#else
            return false;
#endif
        }

        /// <summary>F8 in the editor and development builds: opens / closes the Soul Gem window. Temporary until the Card window (phase 03) has the menu entry.</summary>
        public static void DevKeyTick()
        {
            if (ReleaseBuild.IsRelease || (!Application.isEditor && !Debug.isDebugBuild) || !KeyPressed()) return;
            if (IsOpen) Close(); else Open();
        }
    }
}
