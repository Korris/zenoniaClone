using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// "Choose Your Reward Card" (Taurus V4, BIBLE Q04 phase 5): after the player wins the first duel, Orin shows his 8 Common practice cards (QuestDef.RewardPickCards), the player taps one (gold ring + its name and
    /// four numbers below) and confirms. Confirm hands the quest in with the chosen INDEX (QuestTracker.TurnIn -> InvOp.QuestTurnIn Slot): the zone checks the index against its own list, pays one copy once and
    /// closes the quest, so a second confirm, a replayed packet or a lost reply can never give a second card. Cancel / Esc only close the panel: talk to Orin again to choose. Only kit pieces (panel, card frames,
    /// teal / blue buttons); built the first time it is needed, drawn over the world.
    /// </summary>
    public sealed class CardRewardPickPanel : MonoBehaviour
    {
        const float W = 560f, H = 500f, Pad = 24f, CardW = 92f, CardH = 122f, GapX = 20f, GapY = 12f, GridY = 96f, Caption = 20f; // caption = card + 20 wide = the 112 pitch: "Amber Sporeling" stays on one line
        const int Cols = 4;
        static CardRewardPickPanel instance;

        RectTransform overlay, box;
        Text selectedLine;
        U.PieceButton choose;
        readonly GemCardView[] slot = new GemCardView[8];
        QuestDef quest;
        int selected = -1;
        float fitW, fitH;

        public static bool IsOpen { get { return instance != null && instance.overlay.gameObject.activeSelf; } }

        static CardRewardPickPanel() { UiWindows.RegisterModal(() => IsOpen, Close); }

        /// <summary>Opens the pick screen for a quest with RewardPickCards (Orin's ReadyLines end here instead of an automatic turn-in).</summary>
        public static void Open(QuestDef q)
        {
            if (q == null || q.RewardPickCards == null || UiScreens.Root == null) return;
            if (instance == null)
            {
                var obj = new GameObject("CardRewardPickPanel");
                obj.transform.SetParent(UiScreens.Root, false);
                instance = obj.AddComponent<CardRewardPickPanel>();
                instance.Build(UiKit.Canvas(UiScreens.Root).transform);
            }
            instance.Show(q);
        }

        /// <summary>Capture only (--reward-pick N): taps card N.</summary>
        public static void DebugSelect(int i) { if (instance != null) instance.Select(i); }

        public static void Close() { if (instance != null) { instance.quest = null; instance.overlay.gameObject.SetActive(false); } }

        void OnDestroy() { if (instance == this) instance = null; }

        void Build(Transform canvas)
        {
            overlay = UiKit.Fill(canvas, "RewardPickOverlay");
            UiKit.Panel(overlay, new Color(0.16f, 0.22f, 0.3f, 0.35f), true); // taps stop here (a veil, not art)
            box = UiWinKit.Anchored(overlay, "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0f, 0f, W, H);
            U.Panel(box, "Frame", 0f, 0f, W, H, true).raycastTarget = true;
            U.Txt(box, "Title", Pad, 16f, W - 2f * Pad, 38f, "Choose Your Reward Card", 26, U.Ink, TextAnchor.MiddleCenter);
            U.Txt(box, "Sub", Pad, 56f, W - 2f * Pad, 30f, "Pick 1 card from Orin's practice deck. It is yours to keep.", U.TextNormal, U.InkSoft, TextAnchor.MiddleCenter);
            float x0 = (W - (Cols * CardW + (Cols - 1) * GapX)) * 0.5f;
            for (int i = 0; i < slot.Length; i++)
            {
                int index = i;
                slot[i] = new GemCardView(box, "Card" + i, x0 + (i % Cols) * (CardW + GapX), GridY + (i / Cols) * (CardH + Caption + GapY), CardW, CardH, () => Select(index), true, false, 18);
            }
            float gridEnd = GridY + 2f * (CardH + Caption) + GapY;
            selectedLine = U.Txt(box, "Selected", Pad, gridEnd + 8f, W - 2f * Pad, 28f, "", U.TextNormal + 2, U.Ink, TextAnchor.MiddleCenter);
            float bw = 210f, gap = 20f, x = (W - bw * 2f - gap) * 0.5f;
            choose = new U.PieceButton(box, "Choose", x, H - 20f - U.ButtonH, bw, U.ButtonH, GemUiArt.Frame("btn-teal"), "Choose a card", Confirm);
            new U.PieceButton(box, "Later", x + bw + gap, H - 20f - U.ButtonH, bw, U.ButtonH, GemUiArt.Frame("btn-blue"), "Later", Close);
            NameTags.Occlude(box); // IMGUI name tags would draw over the box
            overlay.gameObject.SetActive(false);
        }

        void Show(QuestDef q)
        {
            quest = q; selected = -1;
            for (int i = 0; i < slot.Length; i++) slot[i].Set(i < q.RewardPickCards.Length ? CardTable.Get(q.RewardPickCards[i]) : null, 0, false);
            Refresh();
            overlay.SetAsLastSibling();
            overlay.gameObject.SetActive(true);
            fitW = 0f; // refit on the next frame
        }

        void Select(int i)
        {
            if (quest == null || i >= quest.RewardPickCards.Length) return;
            selected = selected == i ? -1 : i;
            for (int k = 0; k < quest.RewardPickCards.Length; k++) slot[k].Set(CardTable.Get(quest.RewardPickCards[k]), 0, k == selected);
            Refresh();
        }

        void Refresh()
        {
            CardDef c = quest != null && selected >= 0 ? CardTable.Get(quest.RewardPickCards[selected]) : null;
            selectedLine.text = c == null ? "Tap a card to see it here" : c.Name + "   " + c.Top + " / " + c.Right + " / " + c.Bottom + " / " + c.Left + "  (top / right / bottom / left)";
            choose.Set(c == null ? "Choose a card" : "Choose this card", c != null);
        }

        void Confirm()
        {
            if (quest == null || selected < 0) return;
            QuestDef q = quest; int pick = selected;
            Close(); // closed first: a second tap on the same button finds no quest, so only one request leaves
            QuestTracker.TurnIn(q, (byte)pick);
        }

        void Update()
        {
            if (!overlay.gameObject.activeSelf || (overlay.rect.width == fitW && overlay.rect.height == fitH)) return;
            fitW = overlay.rect.width; fitH = overlay.rect.height;
            box.localScale = Vector3.one * Mathf.Max(0.1f, Mathf.Min(1f, fitW * 0.96f / W, (fitH - 8f) / H));
        }
    }
}
