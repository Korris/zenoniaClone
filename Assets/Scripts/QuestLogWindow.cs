using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The list of quests (the Quests button under the minimap): what to hand in first, then what is under way, what can be taken, what is finished. Same art as the quest offer window.
    /// Display only, online and offline alike: it reads the hero's QuestLog and redraws when QuestTracker saw it change. Modal (UiWindows.ModalOpen); Esc / X close it.
    /// </summary>
    public sealed class QuestLogWindow : MonoBehaviour
    {
        const float W = 460f, H = 336f, ListX = 26f, ListY = 72f, ListW = W - ListX * 2f, ListH = H - ListY - 26f, RowH = 86f, RowGap = 6f;
        static readonly Color ReadyColor = new Color(0.55f, 1f, 0.55f), ActiveColor = new Color(1f, 0.85f, 0.35f), DoneColor = new Color(0.65f, 0.65f, 0.65f);
        static readonly QuestState[] ShowOrder = { QuestState.ReadyToTurnIn, QuestState.Active, QuestState.Available, QuestState.Done };
        static QuestLogWindow instance;

        public static bool IsOpen { get { return instance != null && instance.open; } }

        RectTransform root, content;
        ScrollRect scroll;
        bool open;
        int shownStamp;

        public static void Create(Transform canvas)
        {
            instance = UiKit.Fill(canvas, "QuestLogWindow").gameObject.AddComponent<QuestLogWindow>(); // full-screen layer: children anchor to the real screen
            instance.Build();
        }

        void OnDestroy() { if (instance == this) instance = null; }

        public static void Open()
        {
            if (instance == null || QuestTracker.Log == null) return;
            instance.open = true;
            instance.root.gameObject.SetActive(true);
            instance.Fill();
            instance.scroll.verticalNormalizedPosition = 1f;
        }

        public static void Close()
        {
            if (instance == null) return;
            instance.open = false;
            instance.root.gameObject.SetActive(false);
        }

        void Build()
        {
            root = UiWinKit.Anchored(transform, "Window", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 8f, W, H);
            UiWinKit.Sliced(root, "panel", W / 639f, true); // swallows taps so they never reach the world
            UiWinKit.Plain(UiKit.Rect(root, "QuestIcon", 30f, 18f, 40f, 40f), "quest-icon");
            UiKit.Label(root, "Title", 82f, 16f, 270f, 44f, "Quests", 22, new Color(1f, 0.93f, 0.7f), TextAnchor.MiddleLeft, true);
            UiWinKit.CloseButton(root, W - 62f, 21f, 32f, Close);
            UiKit.Panel(UiKit.Rect(root, "Rule", 30f, 64f, W - 60f, 1.5f), new Color(0.85f, 0.65f, 0.25f, 0.7f));

            RectTransform viewport = UiKit.Rect(root, "List", ListX, ListY, ListW, ListH);
            UiKit.Panel(viewport, Color.clear, true); // a drag anywhere in the list scrolls it
            viewport.gameObject.AddComponent<RectMask2D>();
            content = UiKit.Rect(viewport, "Content", 0f, 0f, ListW, 0f);
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            root.gameObject.SetActive(false);
        }

        void Update()
        {
            if (open && shownStamp != QuestTracker.Stamp) Fill();
        }

        void Fill()
        {
            shownStamp = QuestTracker.Stamp;
            QuestLog log = QuestTracker.Log;
            if (log == null) { Close(); return; }
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            int rows = 0;
            foreach (QuestState state in ShowOrder)
                foreach (QuestDef q in QuestCatalog.All)
                    if (log.StateOf(q) == state) AddRow(rows++, q, state, log.ProgressOf(q));
            content.sizeDelta = new Vector2(ListW, rows * (RowH + RowGap));
        }

        void AddRow(int index, QuestDef q, QuestState state, int progress)
        {
            RectTransform row = UiKit.Rect(content, "Row" + index, 0f, index * (RowH + RowGap), ListW, RowH);
            UiWinKit.Sliced(row, "section", 0.3f);
            UiKit.Label(row, "Title", 12f, 5f, ListW - 134f, 26f, q.Title, 16, state == QuestState.Done ? DoneColor : Color.white, TextAnchor.MiddleLeft, true);
            UiKit.Label(row, "Status", ListW - 122f, 5f, 110f, 26f, StatusText(state), 14, StatusColor(state), TextAnchor.MiddleRight, true);
            UiKit.Label(row, "Goal", 12f, 31f, ListW - 24f, 24f, GoalText(q, state, progress), 14, state == QuestState.Done ? DoneColor : UiWinKit.Cream);
            UiKit.Label(row, "Reward", 12f, 55f, ListW - 24f, 24f, "Reward: " + q.RewardGold + " G, " + q.RewardStones + " stones, " + q.RewardHpPotions + " HP potions", 13, state == QuestState.Done ? DoneColor : UiWinKit.Gold);
        }

        static string StatusText(QuestState state)
        {
            switch (state)
            {
                case QuestState.ReadyToTurnIn: return "Hand in";
                case QuestState.Active: return "In progress";
                case QuestState.Done: return "Completed";
                default: return "Available";
            }
        }

        static Color StatusColor(QuestState state)
        {
            switch (state)
            {
                case QuestState.ReadyToTurnIn: return ReadyColor;
                case QuestState.Active: return ActiveColor;
                case QuestState.Done: return DoneColor;
                default: return UiWinKit.Cream;
            }
        }

        static string GoalText(QuestDef q, QuestState state, int progress)
        {
            switch (state)
            {
                case QuestState.ReadyToTurnIn: return "Return to " + q.GiverName;
                case QuestState.Active: return "Defeat " + q.MonsterName + "  " + progress + " / " + q.Count;
                case QuestState.Done: return "Defeat " + q.MonsterName + "  " + q.Count + " / " + q.Count;
                default: return "Talk to " + q.GiverName;
            }
        }
    }
}
