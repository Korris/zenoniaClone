using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The rebuilt pet gacha, "Lumi's counter" (docs/reference/ui-redesign-261116/chatgpt/pet-gacha-final-mockup.png and
    /// pet-gacha-result-states-x1-x10-no-coins-exchange.png): a full-screen place. Lumi and the egg on its dais in the middle, the two guarantee
    /// bars, the UR shard exchange, Buy 1 / 10 coins and Pull x1 / x10 below; the rate table and the pull history on the right, always visible.
    /// A pull shows its result on the dais (x1) or as ten cards (x10); a pull without coins opens the "not enough coins" paper.
    /// It is a second look of the same gacha: it reads PlayerState.Account like the old PetGachaWindow and every change goes through
    /// PetActions.Send (offline the local rules, online the server rolls: the result is read back from the account's history), texts are
    /// PetGachaText. UiScreens opens this one when the Gacha switch is on; Lumi (TownNpc) and G open it, G / Esc close it; it is a modal.
    /// It redraws when the fingerprint of what it shows changes, so a server reply shows by itself.
    /// Partial: .TopBar, .Altar (Lumi, egg, featured UR, guarantees, exchange plaque, Buy / Pull), .Rates, .History, .ResultPanel,
    /// .Dialogs (not enough coins, exchange, rules), .Testing (capture runs).
    /// </summary>
    public sealed partial class LumiCounterWindow : MonoBehaviour
    {
        const float PullTimeout = 6f; // an unanswered pull unlocks the buttons again after this long (the refusal came as a toast)
        static readonly AccountState NoAccount = new AccountState();
        static LumiCounterWindow instance;
        static int closedFrame = -1;

        PaperLayout layout;
        RectTransform content, counter;
        int shownState, openedFrame;
        // The pull that was sent and is not in the account yet: the Pull buttons stay locked meanwhile (a slow network must not pull twice).
        int pendingCount, pendingTotal, pendingCoins;
        float pendingUntil;

        public static bool IsOpen { get { return instance != null && instance.gameObject.activeSelf; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() { UiScreens.Register(UiScreen.Gacha, Open, Close, () => IsOpen); } // Lumi is already bound to the Gacha screen (UiScreens.npcScreens)

        /// <summary>The account the screen shows (an empty one before the first account answer, like the old window).</summary>
        static AccountState Account { get { PlayerState p = PetActions.Me; return p == null ? null : p.Account ?? NoAccount; } }

        public static void Open()
        {
            if (PetActions.Me == null || Time.frameCount == closedFrame) return; // closedFrame: G just closed it, the same press must not open it again
            if (instance == null) // first use, or the canvas was rebuilt with the scene
            {
                PaperLayout l = PaperLayout.Create(UiKit.Canvas(UiScreens.Root).transform, "LumiCounterWindow", PaperTheme.Navy);
                instance = l.gameObject.AddComponent<LumiCounterWindow>();
                instance.Build(l);
            }
            instance.gameObject.SetActive(true);
            instance.layout.Root.SetAsLastSibling();
            NotificationToast toasts = FindFirstObjectByType<NotificationToast>(); // "+1 gacha coin", "Needs 150 Pet Crystals": over this screen
            if (toasts != null && toasts.transform.parent == instance.layout.Root.parent) toasts.transform.SetAsLastSibling();
            instance.layout.SetBackground(PaperPiece.GachaScene);
            instance.openedFrame = Time.frameCount;
            instance.pendingCount = 0;
            instance.historyPage = 0;
            instance.resultCount = 0;
            instance.dialog = Dialog.None;
            instance.Refresh();
        }

        public static void Close()
        {
            if (!IsOpen) return;
            instance.layout.ReleaseBackground();
            instance.gameObject.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        void OnDestroy() { if (instance == this) instance = null; }

        void Build(PaperLayout l)
        {
            layout = l;
            content = l.Content;
            BuildStage();
            counter = UiKit.Rect(content, "Counter", 0f, 0f, PaperLayout.DesignWidth, PaperLayout.DesignHeight);
            BuildAltar();
            BuildRates();
            BuildHistory();
            BuildResult();
            BuildTopBar();
            BuildDialogs(); // last: the papers and their veil draw over everything
        }

        void Update()
        {
            AccountState a = Account;
            if (a == null) { Close(); return; }
            if (Time.frameCount != openedFrame && InputAdapter.WindowPressed(GameKey.PetGacha)) { closedFrame = Time.frameCount; Close(); return; }
            if (resultCount > 0 && dialog == Dialog.None && InputAdapter.WindowPressed(GameKey.Attack)) CloseResult(); // J / Space = OK
            CheckPull(a);
            TickReveal();
            if (StateOf(a) != shownState) Refresh(); // the server answered, crystals arrived, a page was turned ...
        }

        void Refresh()
        {
            AccountState a = Account;
            if (a == null) { Close(); return; }
            bool result = resultCount > 0;
            counter.gameObject.SetActive(!result);
            RefreshTopBar(a);
            RefreshAltar(a, result);
            if (!result) RefreshHistory(a);
            RefreshResult(a);
            RefreshDialogs(a);
            shownState = StateOf(a);
        }

        /// <summary>Fingerprint of everything shown: the account object itself (an online client gets a new one with each RpgState), its counters and this view's own state.</summary>
        int StateOf(AccountState a)
        {
            unchecked
            {
                int h = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(a);
                h = h * 31 + a.tickets; h = h * 31 + a.crystals; h = h * 31 + a.totalPulls; h = h * 31 + a.urShards; h = h * 31 + a.sharedShards;
                h = h * 31 + a.pitySSR; h = h * 31 + a.pityUR; h = h * 31 + a.pets.Count; h = h * 31 + a.history.Count;
                h = h * 31 + historyPage; h = h * 31 + (int)dialog; h = h * 31 + dialogNeed; h = h * 31 + resultCount; h = h * 31 + revealed;
                h = h * 31 + pendingCount; h = h * 31 + (historyFlag ? 1 : 0);
                return h;
            }
        }

        /// <summary>Pull x1 / x10, exactly what the old window sends. Without the coins: the "not enough coins" paper instead.</summary>
        void Pull(int count)
        {
            AccountState a = Account;
            if (a == null || pendingCount > 0) return;
            if (a.tickets < count) { OpenDialog(Dialog.NoCoins, count); return; }
            pendingCount = count; pendingTotal = a.totalPulls; pendingCoins = a.tickets;
            pendingUntil = Time.unscaledTime + PullTimeout;
            PetActions.Send(InvOp.GachaPull, (byte)count);
            CheckPull(Account); // offline the rules have already run
        }

        /// <summary>The pending pull is in the account (its total grew, or its coins were spent): show what the history says it gave.</summary>
        void CheckPull(AccountState a)
        {
            if (pendingCount == 0 || a == null) return;
            if (a.totalPulls >= pendingTotal + pendingCount || a.tickets <= pendingCoins - pendingCount) { int n = pendingCount; pendingCount = 0; ShowResult(a, n); }
            else if (Time.unscaledTime > pendingUntil) pendingCount = 0;
        }

        static void Buy(int coins) { PetActions.Send(InvOp.GachaBuyCoins, (byte)coins); }

        /// <summary>A picture that keeps its shape (stage props); a faint plate marks a piece that is not delivered.</summary>
        static RectTransform Art(Transform parent, string name, string piece, float x, float y, float w, float h)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            PaperArt.Draw(rt, piece, new Color(0f, 0f, 0f, 0.25f), Color.clear).preserveAspect = true;
            return rt;
        }

        /// <summary>The painted rarity label (N ... UR) of a pet on `img`; hidden for no pet.</summary>
        internal static void ShowRarity(Image img, PetDef d)
        {
            Sprite s = d != null && !d.Legacy ? PaperArt.Get(PaperPiece.PetRarityPrefix + d.Rarity.ToString().ToLowerInvariant()) : null;
            img.sprite = s;
            img.enabled = s != null;
        }

        /// <summary>The UR every guarantee and the shard exchange lead to (PetRules.UrExchangePet).</summary>
        static PetDef Featured { get { return PetConfig.Get(PetRules.UrExchangePet); } }

        static string Amount(int n) { return n.ToString("N0", System.Globalization.CultureInfo.InvariantCulture); }
    }
}
