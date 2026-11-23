using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// The winner's pick screen of the Card Duel ("Winner takes 1 card", FF8 One rule): the loser's 8 deck cards face up in two rows of four, tap one to select it, "Take this card" to take it. 30 s (the clock in the
    /// corner); after that the server takes a random card. Everything drawn is a piece of the ChatGPT card kit (panel, result banner, card frames, teal button, clock) with box-model spacing: panel padding 24, banner on top,
    /// heading + clock row, a 4 x 2 grid of cards (gap 14), hint line, the button. It only draws DuelView.PickCards and sends DuelClient.TakeCard; the server checks the card is in that deck.
    /// </summary>
    public sealed class CardDuelPickPanel
    {
        public const float W = 570f, H = 546f;
        const float Pad = 24f, CardW = 94f, CardH = 124f, GapX = 46f, GapY = 12f, GridY = 162f, ResendSeconds = 1.5f;
        const int Cols = 4, Count = 8;

        readonly RectTransform box;
        readonly GemCardView[] slot = new GemCardView[Count];
        readonly Text heading, secs, hint;
        readonly U.PieceButton take;
        readonly int[] ids = new int[Count];
        int shown, selected = -1;
        float sentAt = -10f;
        bool waiting;

        public bool Visible { get { return box.gameObject.activeSelf; } }

        public CardDuelPickPanel(RectTransform parent, float x, float y)
        {
            box = UiKit.Rect(parent, "PickPanel", x, y, W, H);
            U.Panel(box, "Frame", 0f, 0f, W, H, true).raycastTarget = true; // taps stop here
            float bw = 240f, bh = bw * 180f / 400f;
            GemUiArt.Img(box, "Banner", (W - bw) * 0.5f, 6f, bw, bh, "cards/board/banner-win");
            Text title = U.Txt(box, "Title", (W - 220f) * 0.5f, 6f + bh * 0.34f, 220f, 48f, "Victory!", 28, Color.white, TextAnchor.MiddleCenter);
            var rim = title.gameObject.AddComponent<Outline>(); rim.effectColor = new Color(0.4f, 0.2f, 0.05f, 0.9f); rim.effectDistance = new Vector2(1.5f, -1.5f);
            heading = U.Txt(box, "Heading", Pad, 6f + bh + 2f, W - 2f * Pad - 90f, 32f, "", 20, U.Ink, TextAnchor.MiddleLeft);
            TradeArt.Plain(UiKit.Rect(box, "Clock", W - Pad - 76f, 6f + bh + 6f, 24f, 24f), "clock");
            secs = U.Txt(box, "Secs", W - Pad - 50f, 6f + bh + 2f, 50f, 32f, "", U.TextNormal + 2, U.Ink, TextAnchor.MiddleRight);
            for (int i = 0; i < Count; i++)
            {
                int index = i;
                float cx = Pad + (W - 2f * Pad - Cols * CardW - (Cols - 1) * GapX) * 0.5f + (i % Cols) * (CardW + GapX), cy = GridY + (i / Cols) * (CardH + GapY);
                slot[i] = new GemCardView(box, "Card" + i, cx, cy, CardW, CardH, () => Select(index), false, false, 20);
            }
            float gridEnd = GridY + 2f * CardH + GapY;
            hint = U.Txt(box, "Hint", Pad, gridEnd + 6f, W - 2f * Pad, 40f, "Any card can be taken, even Locked, SR, SSR and Boss cards. After 30 s a random card is taken.", U.TextSmall, U.InkSoft, TextAnchor.UpperCenter);
            take = new U.PieceButton(box, "Take", (W - 240f) * 0.5f, H - 20f - U.ButtonH, 240f, U.ButtonH, GemUiArt.Frame("btn-teal"), "Take this card", Take);
            box.gameObject.SetActive(false);
        }

        public void Hide() { if (Visible) box.gameObject.SetActive(false); }

        /// <summary>Draws the loser's staked deck from this view (view.MustPick). The selection survives a redraw of the same pick.</summary>
        public void Show(DuelView v)
        {
            if (!Visible) { selected = -1; sentAt = -10f; waiting = false; box.gameObject.SetActive(true); }
            shown = Mathf.Min(Count, v.PickCards.Count);
            for (int i = 0; i < Count; i++)
            {
                ids[i] = i < shown ? v.PickCards[i] : -1;
                slot[i].Set(i < shown ? CardTable.ByIndex(ids[i]) : null, 0, i == selected);
            }
            heading.text = "Choose 1 card from " + (string.IsNullOrEmpty(v.OppName) ? "your opponent" : v.OppName) + "'s deck";
            RefreshButton();
        }

        void Select(int i)
        {
            if (i >= shown) return;
            selected = selected == i ? -1 : i;
            for (int k = 0; k < shown; k++) slot[k].Set(CardTable.ByIndex(ids[k]), 0, k == selected);
            RefreshButton();
        }

        void RefreshButton()
        {
            bool canTake = selected >= 0 && Time.unscaledTime - sentAt > ResendSeconds;
            take.Set(selected < 0 ? "Pick a card" : "Take this card", canTake);
        }

        void Take()
        {
            if (selected < 0 || selected >= shown || Time.unscaledTime - sentAt <= ResendSeconds) return;
            sentAt = Time.unscaledTime; waiting = true;
            DuelClient.TakeCard(ids[selected]);
            RefreshButton();
        }

        /// <summary>Per frame while visible: the 30 s clock, and the button coming back if the server refused a pick.</summary>
        public void Tick()
        {
            if (!Visible) return;
            string s = Mathf.CeilToInt(DuelClient.PickSecondsLeft).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (secs.text != s) secs.text = s;
            if (waiting && Time.unscaledTime - sentAt > ResendSeconds) { waiting = false; RefreshButton(); }
        }
    }
}
