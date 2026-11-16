using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The rebuilt bag, "Armory Carousel" (docs/reference/ui-redesign-261116/chatgpt/bag-final-mockup.png and the two bag-state-* mockups):
    /// a full-screen place. The hero stands in the middle of seven gear pedestals; below, a shelf of item cards (tap, double tap or drag
    /// a card up to wear it) or, switched with the Grid button, a board with every bag slot; on the right a pinned paper comparing the
    /// selected item with what is worn, or showing a worn item with its enhance preview.
    /// It is a second look of the same bag: what is listed is BagModel, every change to the player goes through BagActions (offline:
    /// PlayerState + save, online: the server), exactly like the old window (InventoryPanel). UiScreens opens this one when the Bag switch is on.
    /// B toggles and Esc closes (RpgSession / WindowsHost); it is a modal, so the game is paused offline and the hero ignores the keys.
    /// It redraws when the player's state fingerprint changes, so a server reply or a pickup shows without anyone calling it.
    /// Partial: .TopBar (title, resources, Skill, Close, Stats popup), .Pedestals (carousel), .Shelf, .GridView, .Filters,
    /// .ComparePanel + .CompareShow (right paper), .Actions, .Input (keys, taps), .Drag.
    /// </summary>
    public sealed partial class ArmoryWindow : MonoBehaviour
    {
        const float MessageSeconds = 1.8f;
        static ArmoryWindow instance;

        readonly BagModel model = new BagModel { ListCoin = false, ListEmptySupplies = false, OwnClassOnly = false, PadToBag = false, PageSize = ShelfCards, Sort = BagSort.Rarity };
        PaperLayout layout;
        RectTransform content;
        Text toastText;
        GameObject toast;
        float toastUntil;
        bool confirmDiscard;
        int shownState;

        public static bool IsOpen { get { return instance != null && instance.gameObject.activeSelf; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() { UiScreens.Register(UiScreen.Bag, Open, Close, () => IsOpen); }

        public static void Open()
        {
            if (RpgSession.Player == null || GameSession.Hero == null) return;
            if (instance == null) // first use, or the canvas was rebuilt with the scene
            {
                PaperLayout l = PaperLayout.Create(UiKit.Canvas(UiScreens.Root).transform, "ArmoryWindow", PaperTheme.Navy);
                instance = l.gameObject.AddComponent<ArmoryWindow>();
                instance.Build(l);
            }
            instance.gameObject.SetActive(true);
            instance.layout.Root.SetAsLastSibling();
            instance.layout.SetBackground(PaperPiece.BagScene); // the armory room; the flat navy stays until sheet A1 is cut
            instance.confirmDiscard = false;
            instance.toastUntil = 0f;
            instance.Refresh();
        }

        public static void Close()
        {
            if (!IsOpen) return;
            instance.CancelDrag();
            instance.statsPopup.gameObject.SetActive(false);
            instance.sortBox.SetOpen(false);
            instance.heroView.Stop();
            instance.layout.ReleaseBackground();
            instance.gameObject.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (heroView != null) Destroy(heroView.gameObject); // the portrait stage lives in the world, outside this canvas object
        }

        void Build(PaperLayout l)
        {
            layout = l;
            content = l.Content;
            BuildCarousel();  // behind everything: the hero's sword may reach under the panels
            BuildShelf();
            BuildGrid();
            BuildFooter();
            BuildCompare();
            BuildFilters();   // after the shelf, the board and the paper: the open Sort list draws over them
            BuildTopBar();
            BuildDragGhost();
            toast = UiKit.Rect(content, "Toast", 0f, 0f, 360f, 26f).gameObject;
            PaperArt.Plate((RectTransform)toast.transform, new Color(PaperTheme.Plate.r, PaperTheme.Plate.g, PaperTheme.Plate.b, 0.94f), PaperTheme.Gold, 13f);
            toastText = PaperTheme.Caption(toast.transform, "", PaperTheme.BodySize, PaperTheme.TextLight);
            toast.SetActive(false);
            ApplyView();
        }

        void Update()
        {
            PlayerState p = RpgSession.Player;
            if (p == null || GameSession.Hero == null) { Close(); return; }
            if (toast.activeSelf && Time.unscaledTime > toastUntil) toast.SetActive(false);
            heroView.Tick(Time.unscaledDeltaTime);
            ReadKeys();
            if (IsOpen && StateOf(p) != shownState) Refresh(); // the server answered, a potion was drunk from the hotbar, loot arrived ...
        }

        /// <summary>Message over the top rail of the shelf / board for a moment ("Equipped Steel Sword", "Requires level 30").</summary>
        void Flash(string text)
        {
            toastText.text = text;
            float w = Mathf.Clamp(toastText.preferredWidth + 36f, 160f, 520f);
            RectTransform rt = (RectTransform)toast.transform;
            rt.sizeDelta = new Vector2(w, 26f);
            rt.anchoredPosition = new Vector2(ListCentreX - w * 0.5f, -(ListTop - 6f));
            rt.SetAsLastSibling();
            toast.SetActive(true);
            toastUntil = Time.unscaledTime + MessageSeconds;
        }

        void Refresh()
        {
            PlayerState p = RpgSession.Player;
            HeroController hero = GameSession.Hero;
            if (p == null || hero == null) { Close(); return; }
            model.Rebuild(p);
            if (model.Worn < 0 && model.CellCount > 0) model.Page = Mathf.Clamp(model.Cell / model.PageSize, 0, model.PageCount - 1);
            sortBox.SetOpen(false);
            RefreshTopBar(p);
            RefreshCarousel(p, hero);
            if (gridView) RefreshGrid(p); else RefreshShelf(p);
            RefreshFooter(p);
            RefreshFilters(p);
            RefreshCompare(p);
            shownState = StateOf(p);
        }

        /// <summary>
        /// Fingerprint of everything this screen shows of the player: a change means "redraw". Cheap enough for every frame (no allocation):
        /// the state object itself (an online client gets a new one with each RpgState), counters, and each item's identity and + level.
        /// </summary>
        static int StateOf(PlayerState p)
        {
            unchecked
            {
                int h = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(p);
                h = h * 31 + p.Level; h = h * 31 + p.Gold; h = h * 31 + p.UpgradeStone;
                for (int s = 0; s < PotionCatalog.Sizes; s++) { h = h * 31 + p.PotionCount(true, s); h = h * 31 + p.PotionCount(false, s); }
                h = h * 31 + p.PotionPick(true) * 4 + p.PotionPick(false);
                h = h * 31 + p.MaxHp; h = h * 31 + p.Def; h = h * 31 + Mathf.RoundToInt(p.DamageAtk);
                h = h * 31 + (p.PetId ?? "").GetHashCode() + p.PetStars;
                foreach (string slot in Slots.All) h = h * 31 + ItemState(p.GetEquipped(slot));
                h = h * 31 + p.Inventory.Count;
                for (int i = 0; i < p.Inventory.Count; i++) h = h * 31 + ItemState(p.Inventory[i]);
                return h;
            }
        }

        static int ItemState(Item item) { return item == null ? 0 : unchecked((item.uid ?? "").GetHashCode() * 17 + item.enhance); }
    }
}
