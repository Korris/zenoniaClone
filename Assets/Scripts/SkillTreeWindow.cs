using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Skill tree window (win-panel gold style): header (class, Lv, SP free / total, Respec, close), 2 x 4 skill nodes with prerequisite lines,
    /// the detail pane on the right and the 3-page magazine strip below the grid for assigning. Opened with T, the bag's Skill tab or the touch
    /// "Skills" button; counts as a modal window (UiWindows.ModalOpen: hero frozen, game paused offline). Every change goes through
    /// ISkillTreeActions (offline = local state + save, online = server request), and the window redraws when the state signature changes.
    /// </summary>
    public sealed class SkillTreeWindow : MonoBehaviour
    {
        const float W = 920f, H = 540f, GridX = 64f, GridY = 72f, ColPitch = 136f, RowPitch = 172f, StripY = 410f;
        static readonly Color Good = new Color(0.55f, 0.95f, 0.6f), Bad = new Color(1f, 0.55f, 0.5f);
        static SkillTreeWindow instance;

        public static bool IsOpen { get { return instance != null && instance.overlay.gameObject.activeSelf; } }

        RectTransform overlay, win;
        Text title, level, sp, message, respecLabel;
        Button respec;
        readonly SkillTreeNodeView[] nodes = new SkillTreeNodeView[SkillDef.PerClass];
        readonly SkillTreeLinks links = new SkillTreeLinks();
        SkillTreeDetailPane detail;
        SkillLoadoutStrip strip;
        string shownClass;
        int selected, signature = -1;
        bool assignMode, respecArmed;
        float messageUntil;

        public static void Toggle(Transform root) { if (IsOpen) Close(); else Open(root); }

        public static void Open(Transform root)
        {
            if (RpgSession.Player == null || GameSession.Hero == null) return;
            if (instance == null)
            {
                var go = new GameObject("SkillTreeWindow");
                go.transform.SetParent(root, false);
                instance = go.AddComponent<SkillTreeWindow>();
                instance.Build(UiKit.Canvas(root).transform);
            }
            instance.assignMode = instance.respecArmed = false;
            instance.message.text = "";
            instance.signature = -1;
            instance.overlay.gameObject.SetActive(true);
            instance.overlay.SetAsLastSibling();
            instance.Refresh();
        }

        public static void Close() { if (instance != null) instance.overlay.gameObject.SetActive(false); }

        // Programmatic taps through the real handlers (DebugCapture --skill-ui scripts, UI smoke tests).
        public static void TapNode(int i) { if (IsOpen) instance.OnNode(i); }
        public static void TapLearn() { if (IsOpen) instance.OnLearn(); }
        public static void TapAssign() { if (IsOpen) instance.OnAssign(); }
        public static void TapSlot(int flat) { if (IsOpen) instance.OnSlot(flat); }
        public static void TapRespec() { if (IsOpen) instance.OnRespec(); }

        void OnDestroy() { if (instance == this) instance = null; }

        void Build(Transform canvas)
        {
            overlay = UiKit.Fill(canvas, "SkillTreeOverlay");
            UiKit.Panel(overlay, new Color(0f, 0f, 0f, 0.35f), true); // dims the game and swallows taps
            win = UiWinKit.Anchored(overlay, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, W, H);
            UiWinKit.Sliced(win, "panel", 0.5f, true);
            title = UiKit.Label(win, "Title", 46f, 22f, 330f, 40f, "", 25, new Color(1f, 0.93f, 0.7f), TextAnchor.MiddleLeft, true);
            level = UiKit.Label(win, "Level", 390f, 22f, 90f, 40f, "", 19, UiWinKit.Cream, TextAnchor.MiddleLeft, true);
            sp = UiKit.Label(win, "Sp", 470f, 22f, 150f, 40f, "", 21, UiWinKit.Gold, TextAnchor.MiddleLeft, true);
            respec = UiWinKit.ArtButton(win, "Respec", "btn-navy", 636f, 26f, 190f, 34f, "Respec", 16, UiWinKit.Cream, OnRespec);
            respecLabel = respec.GetComponentInChildren<Text>();
            UiWinKit.CloseButton(win, W - 78f, 22f, 38f, Close);
            UiKit.Panel(UiKit.Rect(win, "Rule", 44f, 66f, W - 88f, 1.5f), new Color(0.85f, 0.65f, 0.25f, 0.7f));

            for (int i = 0; i < nodes.Length; i++)
                nodes[i] = SkillTreeNodeView.Build(win, i, GridX + (i % 4) * ColPitch, GridY + (i / 4) * RowPitch, OnNode);
            detail = SkillTreeDetailPane.Build(win, W - 44f - SkillTreeDetailPane.Width, GridY, OnLearn, OnAssign);
            strip = SkillLoadoutStrip.Build(win, 44f, StripY, OnSlot);
            message = UiKit.Label(win, "Message", 44f, H - 56f, W - 88f, 24f, "", 15, Good, TextAnchor.MiddleLeft, true);
            overlay.gameObject.SetActive(false);
        }

        // ---- per frame ----

        void Update()
        {
            if (!IsOpen) return;
            if (RpgSession.Player == null || GameSession.Hero == null) { Close(); return; }
            if (message.text.Length > 0 && Time.unscaledTime > messageUntil) message.text = "";
            if (Signature(RpgSession.Player) != signature) Refresh();
            foreach (SkillTreeNodeView n in nodes) n.Pulse(Time.unscaledTime);
            strip.Pulse(Time.unscaledTime);
        }

        /// <summary>Cheap hash of everything the window shows, so server pushes / outside changes redraw it.</summary>
        static int Signature(PlayerState p)
        {
            int h = p.Level * 31 + p.Gold;
            for (int i = 0; i < SkillDef.PerClass; i++) h = h * 17 + p.Skills[i] * 5 + p.Loadout[i] + 1;
            return h ^ p.ClassId.GetHashCode();
        }

        void Refresh()
        {
            PlayerState p = RpgSession.Player;
            HeroController hero = GameSession.Hero;
            if (p == null || hero == null) return;
            signature = Signature(p);
            if (p.ClassId != shownClass) { shownClass = p.ClassId; selected = 0; BuildLinks(p.ClassId); }
            title.text = hero.Class.Name + " Skills";
            level.text = "Lv " + p.Level;
            sp.text = "SP " + p.SkillPointsFree + " / " + p.Level;
            int cost = SkillRespec.Cost(p.Level);
            respecLabel.text = respecArmed ? "Confirm  -" + cost + " G" : "Respec  " + cost + " G";
            respec.interactable = p.Skills.Sum > 0;
            for (int i = 0; i < nodes.Length; i++)
            {
                SkillDef d = SkillTable.Get(p.ClassId, i);
                LearnResult r = p.Skills.CanLearn(d, p.Level);
                bool locked = p.Skills[i] == 0 && (r == LearnResult.LevelTooLow || r == LearnResult.PrereqMissing);
                nodes[i].Set(d, p.Skills[i], locked, locked && r == LearnResult.LevelTooLow, r == LearnResult.Ok, i == selected, SkillIcons.Get(d));
            }
            links.Refresh(p.Skills);
            detail.Show(p, SkillTable.Get(p.ClassId, selected), assignMode, SkillIcons.Get(SkillTable.Get(p.ClassId, selected)));
            strip.Show(p, hero.Class, assignMode);
        }

        void BuildLinks(string classId)
        {
            var centers = new Vector2[nodes.Length];
            for (int i = 0; i < nodes.Length; i++) centers[i] = nodes[i].Center;
            links.Build(win, classId, centers);
        }

        // ---- input ----

        void Say(ActionResult r)
        {
            message.text = r.Text ?? "";
            message.color = r.Ok ? Good : Bad;
            messageUntil = Time.unscaledTime + 2.5f;
        }

        void OnNode(int i) { selected = i; assignMode = respecArmed = false; Refresh(); }

        void OnLearn()
        {
            Say(SkillTreeActions.Current.Learn(selected));
            respecArmed = false;
            Refresh();
        }

        void OnAssign() { assignMode = !assignMode; respecArmed = false; Refresh(); }

        void OnSlot(int flat)
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            if (assignMode)
            {
                if (flat == 0) { Say(ActionResult.Fail("Dash is fixed in the first slot")); return; }
                assignMode = false;
                Say(SkillTreeActions.Current.Assign(flat - 1, selected));
            }
            else if (flat > 0 && p.Loadout[flat - 1] >= 0) selected = p.Loadout[flat - 1];
            Refresh();
        }

        void OnRespec()
        {
            if (!respecArmed) { respecArmed = true; assignMode = false; Say(ActionResult.Fail("Tap Respec again to confirm")); Refresh(); return; }
            respecArmed = false;
            Say(SkillTreeActions.Current.Respec());
            Refresh();
        }
    }
}
