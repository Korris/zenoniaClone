using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The rebuilt Wanted board (docs/reference/ui-redesign-261116/chatgpt/wanted-board.png and wanted-board-poster-states-*.png): a full-screen
    /// place, the town square with Guard Rook beside the board. Left: the poster of today's boss (alive / respawning with its countdown / claimed,
    /// the fixed daily bounty, Show on Map). Right: the three-day rotation, the boss's four skills with how to dodge them, the level advice.
    /// Bottom: the countdown to the next boss. There is nothing to accept: the board informs and guides; the zone pays the bounty on the kill.
    /// It never shows how many players are fighting (user decision).
    /// Opened by Guard Rook / Leila (WantedBossBoard.TryOpen) and by a tap on the HUD card, through UiScreens (switch UiScreen.Wanted); only
    /// while the server's WantedStatus is known (PaperWanted.Known): offline the two NPCs keep their usual talk. Reads WantedBossTracker,
    /// sends nothing. Partial: .Poster (left paper), .ScheduleSkills (right column).
    /// </summary>
    public sealed partial class WantedBoardWindow : MonoBehaviour
    {
        static WantedBoardWindow instance;

        PaperLayout layout;
        RectTransform content;
        Text timerText, myLevelText, adviceText;
        int shownVersion = -1, shownSecond = -1, shownLevel = -1;
        WantedPhase shownPhase;
        bool shownGuide;

        public static bool IsOpen { get { return instance != null && instance.gameObject.activeSelf; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() { UiScreens.Register(UiScreen.Wanted, Open, Close, () => IsOpen); }

        public static void Open()
        {
            if (!PaperWanted.Known) return; // offline / no status yet: there is no board to show
            if (instance == null) // first use, or the canvas was rebuilt with the scene
            {
                PaperLayout l = PaperLayout.Create(UiKit.Canvas(UiScreens.Root).transform, "WantedBoardWindow", PaperTheme.WoodDark);
                instance = l.gameObject.AddComponent<WantedBoardWindow>();
                instance.Build(l);
            }
            instance.gameObject.SetActive(true);
            instance.layout.Root.SetAsLastSibling();
            instance.layout.SetBackground(PaperPiece.WantedScene);
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
            BuildRook();
            BuildPoster();
            BuildSchedule();
            BuildSkills();
            BuildAdvice();
            RectTransform timer = UiKit.Rect(content, "Timer", 325f, 575f, 488f, 44f);
            PaperArt.Draw(timer, PaperPiece.WantedTimerBar, PaperTheme.WoodDark, PaperTheme.BtnGoldEdge, 14f);
            timerText = PaperTheme.Title(timer, "Text", 62f, 0f, 396f, 42f, "", PaperTheme.SectionSize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter);
            timerText.horizontalOverflow = HorizontalWrapMode.Overflow;
            PaperKit.TitleBoard(content, 449f, -6f, 240f, 74f, "WANTED BOARD");
            PaperKit.CloseButton(content, 1072f, 14f, 46f, Close);
        }

        /// <summary>Guard Rook stands left of the board with his name plate, as on the mockup (whoever opened the board).</summary>
        void BuildRook()
        {
            PaperWanted.Piece(content, "GuardRook", 34f, 322f, 162f, 231f, PaperPiece.WantedRook, Color.clear).preserveAspect = true;
            RectTransform plate = UiKit.Rect(content, "RookName", 44f, 268f, 124f, 30f);
            PaperArt.Draw(plate, PaperPiece.WantedPlaqueDark, PaperTheme.WoodDark, PaperTheme.BtnGoldEdge, 10f);
            PaperTheme.Caption(plate, "Guard Rook", PaperTheme.BodySize, PaperTheme.TitleGold);
            PaperKit.Mark(content, PaperMark.AlertGold, 106f, 240f, 36f);
        }

        /// <summary>"Recommended Lv. 25+, bring a party." and the hero's own level, under the skills.</summary>
        void BuildAdvice()
        {
            RectTransform strip = UiKit.Rect(content, "Advice", 580f, 504f, 334f, 40f);
            PaperArt.Draw(strip, PaperPiece.WantedSectionTag, PaperTheme.Paper, PaperTheme.BtnGoldEdge, 10f);
            adviceText = PaperTheme.Body(strip, "Text", 22f, 0f, 290f, 38f, "", 14, PaperTheme.Ink, PaperInk.Plain, TextAnchor.MiddleCenter, true);
            RectTransform mine = UiKit.Rect(content, "MyLevel", 922f, 506f, 128f, 36f);
            PaperArt.Draw(mine, PaperPiece.WantedPlaqueDark, PaperTheme.WoodDark, PaperTheme.BtnGoldEdge, 10f);
            myLevelText = PaperTheme.Caption(mine, "", PaperTheme.SectionSize, PaperTheme.TitleGold);
        }

        void Update()
        {
            if (!PaperWanted.Known) { Close(); return; } // disconnected while reading the board
            HeroController hero = GameSession.Hero;
            if (WantedBossTracker.Status.Version != shownVersion || PaperWanted.Phase != shownPhase || hero.Rpg.Level != shownLevel || WantedGuide.On != shownGuide) Refresh();
            else RefreshClocks();
        }

        /// <summary>Everything the board shows; called on open and when the status, the claim, the level or the guide switch changed.</summary>
        void Refresh()
        {
            WantedBossDef today = WantedBossTracker.Today;
            shownVersion = WantedBossTracker.Status.Version;
            shownPhase = PaperWanted.Phase;
            shownLevel = GameSession.Hero.Rpg.Level;
            shownGuide = WantedGuide.On;
            shownSecond = -1;
            RefreshPoster(today, shownPhase);
            RefreshSchedule(WantedBossTracker.Schedule());
            RefreshSkills(today);
            adviceText.text = "<color=#C0392B>Recommended Lv. " + today.BossLevel + "+</color>, bring a party.";
            myLevelText.text = "My Lv. " + shownLevel;
            RefreshClocks();
        }

        /// <summary>The two countdowns: texts are rebuilt once a second.</summary>
        void RefreshClocks()
        {
            int reset = PaperWanted.Seconds(WantedBossTracker.ResetLeftMs), respawn = PaperWanted.Seconds(WantedBossTracker.RespawnLeftMs);
            int second = reset * 4096 + respawn % 4096;
            if (second == shownSecond) return;
            shownSecond = second;
            timerText.text = "Next wanted in " + PaperWanted.Clock(reset, true) + "  (Change at 05:00)";
            if (shownPhase == WantedPhase.Respawning) statusText.text = "Respawns in " + PaperWanted.Clock(respawn, false);
        }
    }
}
