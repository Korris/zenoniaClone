using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using W = ZenoniaProto.InventoryRoWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// "Pet Gacha Counter", the RO Soft Fantasy pet gacha, built to the approved mockup docs/reference/ui-style-ro-soft-fantasy/06-gacha.png
    /// (ChatGPT-painted pieces gacha-* plus the shared bag-* / btn-* kit in Resources/Sprites/ui/ro-soft). Default Gacha window while
    /// UiStyle.Ro is on (PetGachaWindow forwards Open / Close / IsOpen here); `--ui-style classic` keeps the old window, `--ui-new gacha`
    /// Lumi's Adventure Paper counter.
    /// Header: paw badge, title, rules (?), Gacha Coins and Pet Crystals with +, close. Left: Lumi and her bubble, the SSR / UR guarantee bars,
    /// the UR shard exchange, Buy Gacha Coins. Centre: ribbon, featured pet on the pedestal, a carousel of the gacha's pets, Pull x1 / x10.
    /// Right: Drop rates / Pull history tabs and the pets of this gacha.
    /// Every number is the real one: rates and pity are PetRules / PetGacha (what the server rolls with), counters are PlayerState.Account,
    /// every action is PetActions.Send (offline the local rules, online the server; a pull's result is read back from the account history).
    /// Layout: positions are measured on the 1671 x 941 mockup and mapped by X / Y / D below, so the window can be checked against it.
    /// Partial: .Left, .Centre, .Right, .Result (pull result), .Popups (buy coins, rules), .Testing (capture runs).
    /// </summary>
    public sealed partial class RoGachaWindow : MonoBehaviour
    {
        // mockup frame (35, 20) .. (1640, 912) -> window 1160 x 645 canvas units
        const float S = 0.7227f, OX = 35f, OY = 20f;
        const float WinW = 1160f, WinH = 645f;
        const float PullTimeout = 6f; // an unanswered pull unlocks the buttons again (the refusal came as a toast)
        static readonly AccountState NoAccount = new AccountState();
        static RoGachaWindow instance;
        static int closedFrame = -1;

        RectTransform overlay, window;
        Text coinText, crystalText, messageText;
        float messageUntil;
        int shownState, openedFrame;
        int pendingCount, pendingTotal, pendingCoins;
        float pendingUntil;

        static float X(float mx) { return (mx - OX) * S; }
        static float Y(float my) { return (my - OY) * S; }
        static float D(float len) { return len * S; }

        public static bool IsOpen { get { return instance != null && instance.overlay.gameObject.activeSelf; } }

        static AccountState Account { get { PlayerState p = PetActions.Me; return p == null ? null : p.Account ?? NoAccount; } }

        /// <summary>The UR every guarantee and the shard exchange lead to.</summary>
        static PetDef Featured { get { return PetConfig.Get(PetRules.UrExchangePet); } }

        public static void Open()
        {
            if (PetActions.Me == null || Time.frameCount == closedFrame) return;
            if (instance == null) // first use, or the canvas was rebuilt with the scene
            {
                var go = new GameObject("RoGachaWindow");
                go.transform.SetParent(UiScreens.Root, false);
                instance = go.AddComponent<RoGachaWindow>();
                instance.Build(UiKit.Canvas(UiScreens.Root).transform);
            }
            instance.overlay.gameObject.SetActive(true);
            instance.overlay.SetAsLastSibling();
            instance.ToastsOnTop();
            instance.openedFrame = Time.frameCount;
            instance.pendingCount = 0;
            instance.resultCount = 0;
            instance.popup = Popup.None;
            instance.historyPage = 0;
            instance.preview = Featured;
            instance.carouselPage = 0;
            instance.messageUntil = 0f;
            instance.Refresh();
        }

        /// <summary>"Needs 150 Pet Crystals" and other refusals show over this window (a pull result card goes over them instead).</summary>
        void ToastsOnTop()
        {
            NotificationToast toasts = FindFirstObjectByType<NotificationToast>();
            if (toasts != null && toasts.transform.parent == overlay.parent) toasts.transform.SetAsLastSibling();
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
            overlay = UiKit.Fill(canvas, "GachaOverlay");
            UiKit.Panel(overlay, new Color(0.16f, 0.22f, 0.3f, 0.22f), true); // stops taps reaching the game
            window = UiWinKit.Anchored(overlay, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, WinW, WinH);
            W.Img(window, "Frame", 0f, 0f, WinW, WinH, "bag-window", true).color = RoSoftTheme.Hex("#EEF4FF"); // the mockup's pale ice-blue glass
            BuildHeader();
            BuildLeft();
            BuildCentre();
            BuildRight();
            BuildResult();
            BuildPopups(); // last: over everything
            overlay.gameObject.SetActive(false);
        }

        void BuildHeader()
        {
            W.Img(window, "PawBadge", X(55f), Y(22f), D(88f), D(88f), "gacha-paw-badge");
            Text title = W.Txt(window, "Title", X(162f), Y(42f), 330f, D(52f), "Pet Gacha Counter", 30, Color.white);
            Outline to = title.gameObject.AddComponent<Outline>(); // white title with a navy rim, as the mockup
            to.effectColor = RoSoftTheme.Hex("#4A6BB0"); to.effectDistance = new Vector2(1.1f, -1.1f);
            title.gameObject.AddComponent<Shadow>().effectColor = new Color(0.15f, 0.25f, 0.5f, 0.35f);
            Image help = W.Img(window, "Help", X(162f) + title.preferredWidth + 10f, Y(68f) - D(21f), D(42f), D(42f), "gacha-help", true);
            W.Clickable(help, () => OpenPopup(Popup.Rules, 0));
            messageText = W.Txt(window, "Message", X(530f), Y(48f), 175f, 30f, "", 13, RoSoftTheme.GoldText, TextAnchor.MiddleCenter);
            messageText.horizontalOverflow = HorizontalWrapMode.Wrap;

            coinText = CurrencyPill("Coins", X(1020f), "gacha-coin", () => OpenPopup(Popup.Buy, 0));
            crystalText = CurrencyPill("Crystals", X(1285f), "gacha-crystal", () => Flash(PetGachaText.CrystalSource));
            W.Clickable(W.Img(window, "Close", X(1552f), Y(25f), D(80f), D(80f), "bag-close", true), Close);
        }

        /// <summary>White pill with the currency icon on its left end, the amount and a + button on its right end.</summary>
        Text CurrencyPill(string name, float x, string icon, UnityEngine.Events.UnityAction plus)
        {
            float w = D(232f), h = D(48f);
            Image pill = W.Img(window, name + "Pill", x, Y(44f), w, h, "bag-pill-white");
            pill.pixelsPerUnitMultiplier = pill.sprite != null ? pill.sprite.rect.height / (2f * h) : 1f; // ends stay half circles
            W.Img(pill.transform, "Icon", -2f, -3f, h + 6f, h + 6f, icon);
            Text t = W.Txt(pill.transform, "Amount", h + 4f, 0f, w - 2f * h - 14f, h, "", 17, W.Ink, TextAnchor.MiddleCenter);
            W.Clickable(W.Img(pill.transform, "Plus", w - h - 3f, -2f, h + 4f, h + 4f, "btn-plus", true), plus);
            return t;
        }

        void Flash(string text)
        {
            messageText.text = text;
            messageUntil = Time.unscaledTime + 2.5f;
        }

        void Update()
        {
            AccountState a = Account;
            if (a == null) { Close(); return; }
            if (resultCount > 0 && popup == Popup.None && InputAdapter.WindowPressed(GameKey.Attack)) CloseResult(); // J / Space = OK
            CheckPull(a);
            TickReveal();
            if (messageUntil > 0f && Time.unscaledTime > messageUntil) { messageUntil = 0f; messageText.text = ""; }
            if (StateOf(a) != shownState) Refresh(); // the server answered, crystals arrived, a page was turned ...
        }

        void Refresh()
        {
            AccountState a = Account;
            if (a == null) { Close(); return; }
            FitWindow();
            coinText.text = Amount(a.tickets);
            crystalText.text = Amount(a.crystals);
            RefreshLeft(a);
            RefreshCentre(a);
            RefreshRight(a);
            RefreshResult(a);
            RefreshPopups(a);
            shownState = StateOf(a);
        }

        void FitWindow()
        {
            float scale = Mathf.Min(1f, (overlay.rect.width * 0.96f) / WinW, (overlay.rect.height - 24f) / WinH);
            window.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }

        /// <summary>Fingerprint of everything shown: the account object (online a new one comes with each state), its counters and this view's own state.</summary>
        int StateOf(AccountState a)
        {
            unchecked
            {
                int h = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(a);
                h = h * 31 + a.tickets; h = h * 31 + a.crystals; h = h * 31 + a.totalPulls; h = h * 31 + a.urShards;
                h = h * 31 + a.pitySSR; h = h * 31 + a.pityUR; h = h * 31 + a.pets.Count; h = h * 31 + a.history.Count;
                h = h * 31 + historyPage; h = h * 31 + (int)tab; h = h * 31 + (int)popup; h = h * 31 + popupNeed;
                h = h * 31 + resultCount; h = h * 31 + revealed; h = h * 31 + pendingCount; h = h * 31 + carouselPage;
                h = h * 31 + (preview != null ? preview.Id.GetHashCode() : 0);
                return h;
            }
        }

        /// <summary>Pull x1 / x10. Without the coins: the Buy Gacha Coins popup, telling how many are missing.</summary>
        void Pull(int count)
        {
            AccountState a = Account;
            if (a == null || pendingCount > 0) return;
            if (a.tickets < count) { OpenPopup(Popup.Buy, count); return; }
            pendingCount = count; pendingTotal = a.totalPulls; pendingCoins = a.tickets;
            pendingUntil = Time.unscaledTime + PullTimeout;
            PetActions.Send(InvOp.GachaPull, (byte)count);
            CheckPull(Account); // offline the rules have already run
            Refresh();
        }

        /// <summary>The pending pull is in the account (its total grew, or its coins were spent): show what the history says it gave.</summary>
        void CheckPull(AccountState a)
        {
            if (pendingCount == 0 || a == null) return;
            if (a.totalPulls >= pendingTotal + pendingCount || a.tickets <= pendingCoins - pendingCount) { int n = pendingCount; pendingCount = 0; ShowResult(a, n); }
            else if (Time.unscaledTime > pendingUntil) pendingCount = 0;
        }

        // ---- small shared builders ----

        static string Amount(int n) { return n.ToString("N0", CultureInfo.InvariantCulture); }

        /// <summary>Rarity pill N ... UR (painted gacha-pill-*), white bold label with a soft dark rim.</summary>
        static InventoryRoBoxes.PillBox RarityPill(Transform parent, string name, float x, float y, float h, PetRarity r, int size, float minWidth)
        {
            var p = InventoryRoBoxes.Pill(parent, name, x, y, h, "gacha-pill-" + r.ToString().ToLowerInvariant(), r.ToString(), size, Color.white, minWidth);
            W.Rim(p.Label);
            return p;
        }

        static void SetRarity(InventoryRoBoxes.PillBox p, PetRarity r)
        {
            W.SetArt(p.Skin, "gacha-pill-" + r.ToString().ToLowerInvariant());
            if (p.Skin.sprite != null) p.Skin.pixelsPerUnitMultiplier = p.Skin.sprite.rect.height / (2f * p.Root.sizeDelta.y);
            p.Label.text = r.ToString();
        }

        /// <summary>A pet card: the cream gacha-card tinted toward the rarity colour (gold glow piece when selected).</summary>
        static void PetCard(W.Cell c, PetRarity r, bool selected)
        {
            c.SetState(selected, false, PetArt.RarityColor(r));
            if (selected) return;
            W.SetArt(c.Back, "gacha-card");
            c.Back.color = Color.Lerp(Color.white, CardTint[(int)r], 0.45f);
        }

        /// <summary>Card tints of the mockup, matching the painted rarity pills: N green, R blue, SR purple, SSR gold, UR pink-gold.</summary>
        static readonly Color[] CardTint = { RoSoftTheme.Hex("#7ED36A"), RoSoftTheme.Hex("#6AB2F2"), RoSoftTheme.Hex("#B48AEB"), RoSoftTheme.Hex("#F5C452"), RoSoftTheme.Hex("#F59AD2") };

        /// <summary>A rounded progress bar: light track, gold / purple fill sized to value / max, "value / max" on it.</summary>
        sealed class Bar
        {
            readonly Image fill;
            readonly Text text;
            readonly float w, h;

            public Bar(Transform parent, string name, float x, float y, float w, float h, string fillPiece, int size)
            {
                this.w = w; this.h = h;
                Image track = W.Img(parent, name, x, y, w, h, "gacha-bar-track");
                Pill(track, h);
                fill = W.Img(track.transform, "Fill", 0f, 0f, w, h, fillPiece);
                Pill(fill, h);
                text = W.Txt(track.transform, "Text", 0f, -1f, w - 12f, h, "", size, W.Ink, TextAnchor.MiddleRight);
            }

            public void Set(int value, int max, bool centre = false)
            {
                float f = max > 0 ? Mathf.Clamp01(value / (float)max) : 0f;
                fill.enabled = f > 0f;
                fill.rectTransform.sizeDelta = new Vector2(Mathf.Max(h, w * f), h); // never thinner than its round ends
                text.text = value + " / " + max;
                text.alignment = centre ? TextAnchor.MiddleCenter : TextAnchor.MiddleRight;
            }

            static void Pill(Image img, float h) { if (img.sprite != null && img.type == Image.Type.Sliced) img.pixelsPerUnitMultiplier = img.sprite.rect.height / (2f * h); }
        }
    }
}
