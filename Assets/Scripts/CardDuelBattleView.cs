using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;
using U = ZenoniaProto.GemUiWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// Battle screen of the Card Duel, behaviour. It draws ONE DuelView (DuelClient.View): the board, both scores, the cards left and the battle log are all derived from that view's move list
    /// (DuelView.Board / Score / CardsLeft / LogLine), so they cannot disagree. A tap on a hand card selects it, a tap on an empty cell places it (DuelClient.Place); the server (or the offline host) answers
    /// with the next view. Flips animate as a scale-X turn that swaps the owner ring half-way; the animation is only a picture of a change the view already holds.
    /// </summary>
    public sealed partial class CardDuelBattleView
    {
        const float FlipSeconds = 0.4f, WaitSeconds = 1.5f;

        int shownVersion = -1, selected;
        float waitUntil;
        readonly int[] shownOwner = new int[16];
        readonly float[] flipAt = new float[16];
        int lastCell = -1;

        public bool Visible { get { return root != null && root.gameObject.activeSelf; } }

        public void Show(bool on)
        {
            if (root == null || root.gameObject.activeSelf == on) return;
            root.gameObject.SetActive(on);
            if (on) { shownVersion = -1; selected = 0; for (int i = 0; i < 16; i++) { shownOwner[i] = -1; flipAt[i] = -10f; } lastCell = -1; }
        }

        /// <summary>Per frame while visible: redraw on a new view, run the flip animation and the timer.</summary>
        public void Tick()
        {
            DuelView v = DuelClient.View;
            if (v == null) return;
            if (shownVersion != DuelClient.Version) { Redraw(v); shownVersion = DuelClient.Version; }
            AnimateFlips();
            if (v.Finished) { timerText.text = ""; timerFill.fillAmount = 0f; return; }
            float left = DuelClient.SecondsLeft;
            timerFill.fillAmount = Mathf.Clamp01(left / (DuelMatch.TurnMs / 1000f));
            timerText.text = Mathf.CeilToInt(left).ToString(CultureInfo.InvariantCulture);
            if (waitUntil > 0f && Time.unscaledTime > waitUntil) { waitUntil = 0f; Redraw(v); } // the server refused: buttons work again
        }

        bool CanPlace(DuelView v) { return v.YourTurn && !v.Finished && waitUntil <= 0f; }

        void OnHand(int i)
        {
            DuelView v = DuelClient.View;
            if (v == null || i >= v.Hand.Count) return;
            selected = i;
            Redraw(v);
        }

        void OnCell(int c)
        {
            DuelView v = DuelClient.View;
            if (v == null || !CanPlace(v) || selected >= v.Hand.Count || v.Board().CardAt(c) != DuelBoard.Empty) return;
            waitUntil = Time.unscaledTime + WaitSeconds;
            DuelClient.Place(selected, c);
            Redraw(v);
        }

        void Redraw(DuelView v)
        {
            if (waitUntil > 0f && shownVersion != DuelClient.Version) waitUntil = 0f; // a new view = the answer arrived
            if (selected >= v.Hand.Count) selected = Mathf.Max(0, v.Hand.Count - 1);
            DuelBoard b = v.Board();
            DuelNpcDef npc = DuelNpcTable.Get(v.Npc);
            string opp = string.IsNullOrEmpty(v.OppName) ? "Opponent" : v.OppName;
            mineScore.text = b.Score(DuelView.Me).ToString(CultureInfo.InvariantCulture);
            theirScore.text = b.Score(DuelView.Them).ToString(CultureInfo.InvariantCulture);
            theirLabel.text = "NPC";
            turnText.text = v.Finished ? "Final" : "Turn " + v.Turn + " / " + DuelMatch.Turns;
            GemUiArt.SetPiece(statePill, GemUiArt.Frame(v.Finished ? "btn-blue" : v.YourTurn ? "btn-teal" : "btn-blue"));
            stateText.text = v.Finished ? "Duel over" : v.YourTurn ? "Your Turn" : "Waiting...";
            GemUiArt.SetPiece(oppPortrait, npc != null ? npc.Portrait : null);
            oppName.text = opp;
            oppLeft.text = "Cards left: " + v.CardsLeft(DuelView.Them);
            myLeft.text = "Your cards left: " + v.CardsLeft(DuelView.Me);
            handHeader.text = "My Hand (" + v.Hand.Count + ")";
            DrawLog(v);
            DrawCells(v, b);
            DrawHand(v);
            DrawSelected(v);
            DrawBanner(v);
        }

        void DrawLog(DuelView v)
        {
            int first = Mathf.Max(0, v.Moves.Count - LogRows);
            for (int i = 0; i < LogRows; i++)
            {
                int m = first + i;
                bool has = m < v.Moves.Count;
                logTurn[i].text = has ? "Turn " + (m + 1) : "";
                logText[i].text = has ? v.LogLine(m) : "";
                logTurn[i].transform.parent.Find("Chip").GetComponent<Image>().enabled = has;
            }
        }

        void DrawCells(DuelView v, DuelBoard b)
        {
            lastCell = v.Moves.Count > 0 ? v.Moves[v.Moves.Count - 1].Cell : -1;
            bool canPlace = CanPlace(v) && selected < v.Hand.Count;
            for (int c = 0; c < 16; c++)
            {
                int card = b.CardAt(c), owner = b.OwnerAt(c);
                bool empty = card == DuelBoard.Empty;
                GemUiArt.SetPiece(validMark[c], empty && canPlace ? B("cell-valid") : null);
                cell[c].Root.gameObject.SetActive(!empty);
                if (empty) { shownOwner[c] = -1; continue; }
                cell[c].Set(CardTable.ByIndex(card), 0, false);
                if (shownOwner[c] >= 0 && shownOwner[c] != owner && flipAt[c] < 0f) flipAt[c] = Time.unscaledTime; // changed hands since the last view: turn it
                else if (shownOwner[c] < 0) shownOwner[c] = owner;
                GemUiArt.SetPiece(lastMark[c], c == lastCell ? "cards/card/select-frame-gold" : null);
                ApplyOwner(c, shownOwner[c] >= 0 ? shownOwner[c] : owner);
            }
        }

        void ApplyOwner(int c, int owner) { GemUiArt.SetPiece(ownerOverlay[c], owner == DuelView.Me ? "cards/card/owner-frame-mine" : "cards/card/owner-frame-enemy"); }

        void AnimateFlips()
        {
            DuelView v = DuelClient.View;
            if (v == null) return;
            DuelBoard b = null;
            for (int c = 0; c < 16; c++)
            {
                if (flipAt[c] < 0f) { cell[c].Root.localScale = Vector3.one; continue; }
                float t = (Time.unscaledTime - flipAt[c]) / FlipSeconds;
                if (b == null) b = v.Board();
                if (t >= 1f) { flipAt[c] = -10f; shownOwner[c] = b.OwnerAt(c); ApplyOwner(c, shownOwner[c]); cell[c].Root.localScale = Vector3.one; continue; }
                if (t >= 0.5f && shownOwner[c] != b.OwnerAt(c)) { shownOwner[c] = b.OwnerAt(c); ApplyOwner(c, shownOwner[c]); } // the colour changes while the card is edge-on
                cell[c].Root.localScale = new Vector3(Mathf.Max(0.05f, Mathf.Abs(Mathf.Cos(t * Mathf.PI))), 1f, 1f);
            }
        }

        void DrawHand(DuelView v)
        {
            for (int i = 0; i < 5; i++)
            {
                CardDef c = i < v.Hand.Count ? CardTable.ByIndex(v.Hand[i]) : null;
                hand[i].Set(c, 0, c != null && i == selected, !CanPlace(v) && !v.Finished);
            }
        }

        void DrawSelected(DuelView v)
        {
            CardDef c = selected < v.Hand.Count ? CardTable.ByIndex(v.Hand[selected]) : null;
            big.Set(c, 0, false);
            if (c == null) big.Root.gameObject.SetActive(false);
            int[] e = c == null ? new int[4] : new[] { c.Top, c.Right, c.Bottom, c.Left };
            for (int i = 0; i < 4; i++) statNum[i].text = c == null ? "" : e[i].ToString(CultureInfo.InvariantCulture);
            hint.text = v.Finished ? "" : v.YourTurn ? "Tap a card in your hand, then tap an empty cell. A higher side value flips the card next to it." : "The opponent is thinking.";
        }

        void DrawBanner(DuelView v)
        {
            bannerRoot.gameObject.SetActive(v.Finished);
            forfeit.Root.gameObject.SetActive(!v.Finished); // nothing left to give up once the result is shown
            if (!v.Finished) return;
            string piece = v.Result == DuelResult.Win ? "banner-win" : v.Result == DuelResult.Draw ? "banner-draw" : "banner-lose";
            GemUiArt.SetPiece(bannerArt, B(piece));
            bannerTitle.text = v.Result == DuelResult.Win ? "Victory!" : v.Result == DuelResult.Draw ? "Draw" : "Defeat";
            int mine = v.Score(DuelView.Me), theirs = v.Score(DuelView.Them);
            string why = v.EndReason == 1 ? "You left the duel." : v.EndReason == 2 ? "The opponent left." : mine + " - " + theirs;
            string reward = v.RewardFirst + v.RewardDaily > 0 ? "\nFirst win +" + v.RewardFirst.ToString("N0", CultureInfo.InvariantCulture) + " G" + (v.RewardDaily > 0 ? "   Daily task +" + v.RewardDaily + " G" : "") : "";
            bannerSub.text = why + reward;
            bannerButton.Set("Continue", true);
        }
    }
}
