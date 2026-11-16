using UnityEngine;
using UnityEngine.EventSystems;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The rebuilt pet screen, "Pet Camp" (docs/reference/ui-redesign-261116/chatgpt/pet-final-mockup-pet-camp-team.png, pet-camp-team-states-*.png and
    /// pet-camp-collection-growth-capture-tabs.png): a full-screen place. Team = five straw nests by role (Combat, Support 1-2, Stat 1-2) with the team's
    /// total and the four Bonds, the selected pet on a pinned paper and the shelf of owned pets; the plank menu on the left switches to Collection,
    /// Growth and Capture and opens the Gacha.
    /// It is a second look of the same pet screen: it reads PlayerState.Account like the old PetWindow and every change goes through PetActions.Send
    /// (offline the local rules, online the server), shared texts and checks are PetViewModel. UiScreens opens this one when the Pet switch is on.
    /// P toggles and Esc closes; it is a modal. It redraws when the fingerprint of the pet state changes, so a server reply shows by itself.
    /// Partial: .TopBar (title, wallet, side menu), .Nests, .Bonds, .DetailBoard, .OwnedShelf, .Actions (taps, keys, drag), .Collection, .Growth,
    /// .Capture, .Testing (capture runs).
    /// </summary>
    public sealed partial class PetCampWindow : MonoBehaviour
    {
        public enum Tab { Team, Collection, Growth, Capture }

        static PetCampWindow instance;
        static int closedFrame = -1;

        PaperLayout layout;
        RectTransform content;
        readonly RectTransform[] pages = new RectTransform[4];
        Tab tab;
        int selected = -1, menuSlot = -1, shownState, openedFrame;
        bool placing;

        public static bool IsOpen { get { return instance != null && instance.gameObject.activeSelf; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() { UiScreens.Register(UiScreen.Pet, Open, Close, () => IsOpen); }

        public static void Open()
        {
            PlayerState p = PetActions.Me;
            if (p == null || Time.frameCount == closedFrame) return; // closedFrame: P just closed it, the same press must not open it again
            if (instance == null) // first use, or the canvas was rebuilt with the scene
            {
                PaperLayout l = PaperLayout.Create(UiKit.Canvas(UiScreens.Root).transform, "PetCampWindow", PaperTheme.Navy);
                instance = l.gameObject.AddComponent<PetCampWindow>();
                instance.Build(l);
            }
            instance.gameObject.SetActive(true);
            instance.layout.Root.SetAsLastSibling();
            NotificationToast toasts = FindFirstObjectByType<NotificationToast>(); // "Frost Slime reached level 2", "Needs 3 Pet Food": over this screen
            if (toasts != null && toasts.transform.parent == instance.layout.Root.parent) toasts.transform.SetAsLastSibling();
            instance.layout.SetBackground(PaperPiece.PetScene);
            instance.selected = p.PetId != "" ? PetConfig.IndexOf(p.Pet) : instance.FirstOwned(p.Account); // like the old window: starts on the combat pet
            instance.placing = false;
            instance.menuSlot = -1;
            instance.shelfPage = 0;
            instance.openedFrame = Time.frameCount;
            instance.SetTab(instance.tab);
        }

        public static void Close()
        {
            if (!IsOpen) return;
            instance.CancelDrag();
            instance.rulesPopup.gameObject.SetActive(false);
            instance.layout.ReleaseBackground();
            instance.gameObject.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        void OnDestroy() { if (instance == this) instance = null; }

        void Build(PaperLayout l)
        {
            layout = l;
            content = l.Content;
            string[] names = { "TeamPage", "CollectionPage", "GrowthPage", "CapturePage" };
            for (int i = 0; i < pages.Length; i++) pages[i] = UiKit.Rect(content, names[i], 0f, 0f, PaperLayout.DesignWidth, PaperLayout.DesignHeight);
            RectTransform team = pages[(int)Tab.Team];
            BuildNests(team);
            BuildBonds(team);
            BuildBoard(team);
            BuildShelf(team);
            BuildNestOverlays(team); // the hint, the Remove / Details pair and the rules paper draw over the nests, the Bond cards and the shelf
            BuildCollection(pages[(int)Tab.Collection]);
            BuildGrowth(pages[(int)Tab.Growth]);
            BuildCapture(pages[(int)Tab.Capture]);
            BuildTopBar();
            BuildDragGhost();
        }

        int FirstOwned(AccountState a)
        {
            if (a == null) return -1;
            for (int i = 0; i < PetConfig.All.Length; i++) if (a.OwnsPet(PetConfig.All[i].Id)) return i;
            return -1;
        }

        /// <summary>The species the tabs show: the selection, or the first species when nothing is selected (Collection, Growth and Capture always show one).</summary>
        PetDef Shown { get { return PetConfig.All[Mathf.Clamp(selected, 0, PetConfig.All.Length - 1)]; } }

        void SetTab(Tab t)
        {
            tab = t;
            for (int i = 0; i < pages.Length; i++) pages[i].gameObject.SetActive(i == (int)t);
            placing = false;
            menuSlot = -1;
            rulesPopup.gameObject.SetActive(false);
            Refresh();
        }

        void Update()
        {
            PlayerState p = PetActions.Me;
            if (p == null) { Close(); return; }
            ReadKeys();
            if (IsOpen && PetViewModel.StateKey(p, p.Account) != shownState) Refresh(); // the server answered, a pet was caught, food dropped ...
        }

        void Refresh()
        {
            PlayerState p = PetActions.Me;
            if (p == null) { Close(); return; }
            AccountState a = p.Account;
            RefreshTopBar(p, a);
            if (tab == Tab.Team)
            {
                RefreshNests(p, a);
                RefreshBonds(p);
                RefreshBoard(p, a);
                RefreshShelf(p, a);
            }
            else if (tab == Tab.Collection) RefreshCollection(p, a);
            else if (tab == Tab.Growth) RefreshGrowth(p, a);
            else RefreshCapture(p, a);
            shownState = PetViewModel.StateKey(p, a);
        }

        static void Toast(string text) { RpgHud.Toast(text, new Color(1f, 0.7f, 0.5f)); }

        /// <summary>Stars of a species for this account (1 when there is no account yet, like the old window).</summary>
        static int StarsOf(AccountState a, PetDef d) { return a != null && a.OwnsPet(d.Id) ? a.StarsOf(d.Id) : 1; }
    }
}
