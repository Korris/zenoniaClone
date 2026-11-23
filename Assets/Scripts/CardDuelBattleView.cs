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
    /// with the next view. New moves are PLAYED (CardDuelBattleView.Flip.cs: place 230 ms, then every captured card turns 180 degrees, owner swaps at 90) and only then do score, log and the turn unlock; the animation is only a picture of a change the view already holds.
    /// </summary>
    public sealed partial class CardDuelBattleView
    {
        const float WaitSeconds = 1.5f;

        int shownVersion = -1, selected;
        float waitUntil;
        int lastCell = -1, shownSecs = -1;

        public bool Visible { get { return root != null && root.gameObject.activeSelf; } }

        public void Show(bool on)
        {
            if (root == null || root.gameObject.activeSelf == on) return;
            root.gameObject.SetActive(on);
            if (on) { shownVersion = -1; selected = 0; lastCell = -1; ResetPresentation(); }
        }

        /// <summary>Per frame while visible: redraw on a new view, run the flip animation and the timer.</summary>
        public void Tick()
        {
            DuelView v = DuelClient.View;
            if (v == null) return;
            if (shownVersion != DuelClient.Version) { OnNewView(v); shownVersion = DuelClient.Version; }
            StepPresentation(v);
            if (v.Finished) { timerText.text = ""; timerFill.fillAmount = 0f; return; }
            float left = DuelClient.SecondsLeft;
            timerFill.fillAmount = Mathf.Clamp01(left / (DuelMatch.TurnMs / 1000f));
            int secs = Mathf.CeilToInt(left);
            if (secs != shownSecs) { shownSecs = secs; timerText.text = secs.ToString(CultureInfo.InvariantCulture); } // a new string only when the number changes
            if ((left < DuelClient.MinPlaceSeconds) != lockedByClock) { lockedByClock = left < DuelClient.MinPlaceSeconds; Redraw(v); }
            if (v.OppAwayMs > 0) UpdateAway(v);
            if (waitUntil > 0f && Time.unscaledTime > waitUntil) { waitUntil = 0f; Redraw(v); } // the server refused: buttons work again
        }

        bool lockedByClock;
        int shownAway = -1;

        /// <summary>The opponent's connection dropped: their line counts the seconds up to the 60 s after which they forfeit.</summary>
        void UpdateAway(DuelView v)
        {
            int s = Mathf.FloorToInt(DuelClient.OppAwaySeconds);
            if (s == shownAway) return;
            shownAway = s;
            oppLeft.text = "Disconnected " + s + " s / " + (int)(Rpg.DuelPvpMatch.ReconnectMs / 1000) + " s";
            oppLeft.color = U.Bad;
        }

        // the placement is refused client-side when under half a second is left: the server's clock is a ping ahead, and the answer would be "turn over"
        bool CanPlace(DuelView v) { return v.YourTurn && !v.Finished && !Animating(v) && waitUntil <= 0f && DuelClient.SecondsLeft >= DuelClient.MinPlaceSeconds; }

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
            if (v == null || !CanPlace(v) || selected >= v.Hand.Count || dispCard[c] != DuelBoard.Empty) return;
            if (!DuelClient.Place(selected, c)) return;
            waitUntil = Time.unscaledTime + WaitSeconds;
            Redraw(v);
        }

        void Redraw(DuelView v)
        {
            if (waitUntil > 0f && shownVersion != DuelClient.Version) waitUntil = 0f; // a new view = the answer arrived
            if (selected >= v.Hand.Count) selected = Mathf.Max(0, v.Hand.Count - 1);
            DuelBoard b = v.Board(presented); // what is on screen: moves still being played are not in the score yet
            DuelNpcDef npc = DuelNpcTable.Get(v.Npc);
            string opp = string.IsNullOrEmpty(v.OppName) ? "Opponent" : v.OppName;
            mineScore.text = b.Score(DuelView.Me).ToString(CultureInfo.InvariantCulture);
            theirScore.text = b.Score(DuelView.Them).ToString(CultureInfo.InvariantCulture);
            theirLabel.text = v.IsPvp ? "Player" : "NPC";
            bool over = v.Finished && !Animating(v);
            turnText.text = over ? "Final" : "Turn " + Mathf.Min(DuelMatch.Turns, presented + 1) + " / " + DuelMatch.Turns;
            GemUiArt.SetPiece(statePill, GemUiArt.Frame(v.Finished ? "btn-blue" : v.YourTurn ? "btn-teal" : "btn-blue"));
            stateText.text = v.Finished ? "Duel over" : v.YourTurn ? "Your Turn" : "Waiting...";
            GemUiArt.SetPiece(oppPortrait, npc != null ? npc.Portrait : v.IsPvp ? GemUiArt.Icon("menu-duel") : null); // a player has no portrait art: the duel icon stands in
            oppName.text = opp;
            shownAway = -1;
            oppLeft.text = v.OppAwayMs > 0 && !v.Finished ? "Disconnected" : "Cards left: " + v.CardsLeft(DuelView.Them, presented);
            oppLeft.color = v.OppAwayMs > 0 && !v.Finished ? U.Bad : U.InkSoft;
            myLeft.text = "Your cards left: " + v.CardsLeft(DuelView.Me, presented);
            handHeader.text = "My Hand (" + v.Hand.Count + ")";
            DrawLog(v);
            DrawCells(v, b);
            DrawHand(v);
            DrawSelected(v);
            DrawBanner(v);
        }

        void DrawLog(DuelView v)
        {
            int first = Mathf.Max(0, presented - LogRows);
            for (int i = 0; i < LogRows; i++)
            {
                int m = first + i;
                bool has = m < presented;
                logTurn[i].text = has ? "Turn " + (m + 1) : "";
                logText[i].text = has ? v.LogLine(m) : "";
                logTag[i].enabled = has;
            }
        }

        void DrawCells(DuelView v, DuelBoard b)
        {
            lastCell = presented > 0 ? v.Moves[presented - 1].Cell : -1;
            bool canPlace = CanPlace(v) && selected < v.Hand.Count;
            for (int c = 0; c < 16; c++)
            {
                int card = dispCard[c];
                bool empty = card == DuelBoard.Empty;
                GemUiArt.SetPiece(validMark[c], empty && canPlace ? B("cell-valid") : null);
                cell[c].Root.gameObject.SetActive(!empty);
                if (empty) continue;
                cell[c].Set(CardTable.ByIndex(card), 0, false);
                GemUiArt.SetPiece(lastMark[c], c == lastCell && !Animating(v) ? "cards/card/select-frame-gold" : null);
                lastMark[c].preserveAspect = false;
                ApplyOwner(c, dispOwner[c]);
            }
        }

        void ApplyOwner(int c, int owner)
        {
            GemUiArt.SetPiece(ownerOverlay[c], owner == DuelView.Me ? "cards/card/owner-frame-mine" : "cards/card/owner-frame-enemy");
            ownerOverlay[c].preserveAspect = false; // the ring hugs the (stretched) card
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
            wagerLine.text = WagerLine(v);
            hint.text = v.Finished ? "" : v.YourTurn ? "Tap a hand card, then an empty cell." : "The opponent is thinking.";
        }

        static string CardName(int index) { CardDef c = CardTable.ByIndex(index); return c != null ? c.Name : "?"; }

        /// <summary>"Wager: your A vs their B" while it plays (empty without a Card Wager).</summary>
        static string WagerLine(DuelView v)
        {
            if (!v.HasWager) return "";
            return "Card Wager: your " + CardName(v.WagerMine) + " vs " + CardName(v.WagerTheirs);
        }

        /// <summary>The wager's outcome in words once the match is over (the payout may still be settling).</summary>
        static string WagerResult(DuelView v)
        {
            if (!v.HasWager) return "";
            if (v.WagerState == DuelView.WagerSettling) return "\nSettling the cards...";
            if (v.WagerState == DuelView.WagerFailed) return "\nCard payout delayed: it is sorted out automatically.";
            string mine = CardName(v.WagerMine), theirs = CardName(v.WagerTheirs);
            return v.Result == DuelResult.Win ? "\nWager won: +" + theirs : v.Result == DuelResult.Lose ? "\nWager lost: " + mine : "\nWager draw: cards returned";
        }

        void DrawBanner(DuelView v)
        {
            bool over = v.Finished && !Animating(v); // the result waits for the last card to turn
            bannerRoot.gameObject.SetActive(over);
            forfeit.Root.gameObject.SetActive(!v.Finished); // nothing left to give up once the result is shown
            if (!over) return;
            string piece = v.Result == DuelResult.Win ? "banner-win" : v.Result == DuelResult.Draw ? "banner-draw" : "banner-lose";
            GemUiArt.SetPiece(bannerArt, B(piece));
            bannerTitle.text = v.Result == DuelResult.Win ? "Victory!" : v.Result == DuelResult.Draw ? "Draw" : "Defeat";
            int mine = v.Score(DuelView.Me), theirs = v.Score(DuelView.Them);
            string why = v.EndReason == 1 ? "You left the duel." : v.EndReason == 2 ? "The opponent left." : mine + " - " + theirs;
            string reward = "";
            if (v.IsPvp) reward = v.RewardDaily > 0 ? "\nReward +" + v.RewardDaily.ToString("N0", CultureInfo.InvariantCulture) + " G" : v.EndReason == 1 ? "" : "\nNo reward (5 paid matches a day)";
            else if (v.RewardFirst + v.RewardDaily > 0) reward = "\nFirst win +" + v.RewardFirst.ToString("N0", CultureInfo.InvariantCulture) + " G" + (v.RewardDaily > 0 ? "   Daily task +" + v.RewardDaily + " G" : "");
            bannerSub.text = why + reward + WagerResult(v);
            bannerButton.Set("Continue", true);
        }
    }
}
