using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The rebuilt skill screen, "Training Hall" (docs/reference/ui-redesign-261116/chatgpt/skill-final-mockup-training-hall.png and
    /// skill-states-assign-respec-advanced-max-rank-no-sp.png): a full-screen place. The eight skills of the class stand as statues on a
    /// two-tier shelf (tree order, as the old window's 2 x 4 grid); a scroll on the right tells what the selected one does and what the
    /// next rank adds, with Rank Up and Assign; the Skill Magazine belt below holds Dash + the eight slots, grouped by HUD page.
    /// It is a second look of the same tree: nothing here decides a rule. Every change goes through ISkillTreeActions (offline: PlayerState
    /// + save, online: the server), every "why not" comes from SkillTreeActions.Reason, every number from SkillTable / SkillDescribe /
    /// SkillRespec, exactly like the old window (SkillTreeWindow). UiScreens opens this one when the Skill switch is on.
    /// T toggles and Esc closes (WindowsHost); it is a modal, so the game is paused offline and the hero ignores the keys.
    /// It redraws when the player's state fingerprint changes, so a server reply shows without anyone calling it.
    /// Partial: .TopBar (title, class, SP, gold, Respec, Inventory, Close, tier planks, hero), .Shelves (statues, requirement sign),
    /// .DetailScroll, .MagazineBelt, .Respec (the question), .Input (taps, keys), .Drag, .Capture.
    /// </summary>
    public sealed partial class TrainingHallWindow : MonoBehaviour
    {
        const float MessageSeconds = 2.5f;
        /// <summary>Met / unmet on dark plates (pills, the requirement sign); on paper the theme's LabelGreen / Red are used.</summary>
        public static readonly Color GoodOnDark = PaperTheme.Hex("#9BE58F"), BadOnDark = PaperTheme.Hex("#FF8E7C");
        static TrainingHallWindow instance;

        PaperLayout layout;
        RectTransform content;
        Text toastText;
        Image toastPlate;
        GameObject toast;
        float toastUntil;
        string shownClass;
        int selected, shownState;
        bool assignMode;

        public static bool IsOpen { get { return instance != null && instance.gameObject.activeSelf; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() { UiScreens.Register(UiScreen.Skill, Open, Close, () => IsOpen); }

        public static void Open()
        {
            if (RpgSession.Player == null || GameSession.Hero == null) return;
            if (instance == null) // first use, or the canvas was rebuilt with the scene
            {
                PaperLayout l = PaperLayout.Create(UiKit.Canvas(UiScreens.Root).transform, "TrainingHallWindow", PaperTheme.Slate);
                instance = l.gameObject.AddComponent<TrainingHallWindow>();
                instance.Build(l);
            }
            instance.gameObject.SetActive(true);
            instance.layout.Root.SetAsLastSibling();
            instance.layout.SetBackground(PaperPiece.HallScene); // the hall; the flat slate stays when the scene is missing
            instance.assignMode = false;
            instance.respecPopup.SetActive(false);
            instance.toastUntil = 0f;
            instance.Refresh();
        }

        public static void Close()
        {
            if (!IsOpen) return;
            instance.CancelDrag();
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
            BuildHero();   // behind everything: he stands on the hall's rug
            BuildShelves();
            BuildBelt();
            BuildScroll();
            BuildTopBar();
            BuildSign();   // floats over the shelf and the top bar's lower edge
            BuildDragGhost();
            toast = UiKit.Rect(content, "Toast", 0f, 0f, 360f, 26f).gameObject;
            toastPlate = PaperArt.Plate((RectTransform)toast.transform, PaperTheme.Plate, PaperTheme.Gold, 13f);
            toastText = PaperTheme.Caption(toast.transform, "", PaperTheme.BodySize, PaperTheme.TextLight);
            toast.SetActive(false);
            BuildRespec(); // last: its veil covers the whole screen
        }

        void Update()
        {
            PlayerState p = RpgSession.Player;
            if (p == null || GameSession.Hero == null) { Close(); return; }
            float t = Time.unscaledTime;
            if (toast.activeSelf && t > toastUntil) toast.SetActive(false);
            heroView.Tick(Time.unscaledDeltaTime);
            ReadKeys();
            if (!IsOpen) return;
            if (StateOf(p) != shownState) Refresh(); // the server answered, the hero levelled up, gold changed ...
            foreach (PaperSkillStatue s in statues) s.Pulse(t);
            PulseBelt(t);
        }

        /// <summary>The answer of an action, for a moment, on the scroll just above its buttons ("Rank 4", "Need 3000 G to respec"): clear of the statues and the requirement sign.</summary>
        void Say(ActionResult r)
        {
            if (string.IsNullOrEmpty(r.Text)) return;
            toastText.text = r.Text;
            toastText.color = r.Ok ? PaperTheme.TextLight : BadOnDark;
            float w = Mathf.Clamp(toastText.preferredWidth + 36f, 160f, FlowW);
            RectTransform rt = (RectTransform)toast.transform;
            rt.sizeDelta = new Vector2(w, 26f);
            rt.anchoredPosition = new Vector2(ScrollX + (ScrollW - w) * 0.5f, -(ScrollY + ButtonY - 34f));
            PaperArt.Redraw(toastPlate, null, new Color(PaperTheme.Plate.r, PaperTheme.Plate.g, PaperTheme.Plate.b, 0.94f), r.Ok ? PaperTheme.Gold : PaperTheme.Red, 13f);
            toast.SetActive(true);
            toastUntil = Time.unscaledTime + MessageSeconds;
        }

        void Refresh()
        {
            PlayerState p = RpgSession.Player;
            HeroController hero = GameSession.Hero;
            if (p == null || hero == null) { Close(); return; }
            if (p.ClassId != shownClass) { shownClass = p.ClassId; selected = 0; assignMode = false; }
            if (assignMode && p.Skills[selected] <= 0) assignMode = false; // a respec arrived while a slot was being picked
            SkillDef d = SkillTable.Get(p.ClassId, selected);
            RefreshTopBar(p, hero, d);
            RefreshShelves(p, d);
            RefreshScroll(p, d);
            RefreshBelt(p, hero, d);
            RefreshRespec(p);
            shownState = StateOf(p);
        }

        /// <summary>
        /// Fingerprint of everything this screen shows of the player: a change means "redraw". No allocation, cheap enough for every
        /// frame: the state object itself (an online client gets a new one with each RpgState), level, gold, ranks and the magazine.
        /// </summary>
        static int StateOf(PlayerState p)
        {
            unchecked
            {
                int h = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(p);
                h = h * 31 + p.Level; h = h * 31 + p.Gold; h = h * 31 + p.SkillPointsFree;
                for (int i = 0; i < SkillDef.PerClass; i++) h = h * 17 + p.Skills[i] * 5 + p.Loadout[i] + 1;
                return h ^ p.ClassId.GetHashCode();
            }
        }
    }
}
