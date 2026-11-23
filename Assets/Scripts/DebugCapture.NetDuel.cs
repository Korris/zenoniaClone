using System;
using System.Collections;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --net-bot duel --duel-role challenger|rival [--duel-drop N] [--duel-resume] [--duel-nopick] [--duel-pick CARD]: the end-to-end PvP script behind Tools/net-duel-test.ps1 (phase 06 v2: every duel stakes both decks,
    /// the winner takes 1 card). The challenger invites the hero named "Rival", the rival accepts; both play with a greedy pick until the result; the winner picks CARD from the loser's deck (default caveMantis) or, with
    /// --duel-nopick, waits for the server's random pick after 30 s. --duel-drop N makes the rival quit after N of its own placements (the 60 s reconnect window, then the forfeit); --duel-resume makes a rival that
    /// comes back wait for the match it left instead of an invite. Everything goes through DuelClient, the same calls the buttons make; what comes back is logged as "duel-e2e:" lines.
    /// Server flags: --dev-cards --dev-level 60 --map oakhaven-village [--dev-duel-seed N for a known result].
    /// </summary>
    public partial class DebugCapture
    {
        static void DuelE2e(string line) { Debug.Log("[ZenoniaProto] duel-e2e: " + line); }

        void StartDuelBot()
        {
            string[] a = Environment.GetCommandLineArgs();
            string role = NetLaunchArgs.Arg(a, "--duel-role");
            int drop;
            if (!int.TryParse(NetLaunchArgs.Arg(a, "--duel-drop"), out drop)) drop = 0;
            string pick = NetLaunchArgs.Arg(a, "--duel-pick") ?? "caveMantis";
            int ranked;
            if (NetLaunchArgs.HasFlag("--duel-rank-peek")) { StartCoroutine(RankPeek()); return; }
            if (int.TryParse(NetLaunchArgs.Arg(a, "--duel-ranked"), out ranked) && ranked > 0) { StartCoroutine(RankedScript(ranked, drop)); return; }
            StartCoroutine(DuelScript(role == "challenger", NetLaunchArgs.HasFlag("--duel-resume"), NetLaunchArgs.HasFlag("--duel-nopick"), pick, drop));
        }

        IEnumerator DuelUntil(Func<bool> done, float timeout, string what)
        {
            float start = Time.realtimeSinceStartup;
            while (!done() && Time.realtimeSinceStartup - start < timeout) yield return null;
            DuelE2e((done() ? "OK " : "TIMEOUT ") + what + " after " + (Time.realtimeSinceStartup - start).ToString("F1") + " s");
        }

        static int CardTotal() { int n = 0; foreach (var kv in GameSession.Hero.Rpg.Cards.Cards) n += kv.Value; return n; }

        static string CardLine(string pick)
        {
            CardCollection c = GameSession.Hero.Rpg.Cards;
            return "cards total " + CardTotal() + ", deck " + c.Deck.Count + ", " + pick + " x" + c.Count(pick) + ", meadowSlime x" + c.Count("meadowSlime");
        }

        IEnumerator DuelScript(bool challenger, bool resume, bool noPick, string pick, int dropAfter)
        {
            string me = NetLaunchArgs.Name;
            if (challenger)
            {
                yield return DuelUntil(() => HeroNamed("Rival") != null, 40f, "see Rival");
                HeroController rival = HeroNamed("Rival");
                if (rival == null) { Application.Quit(); yield break; }
                yield return new WaitForSeconds(2f);
                DuelClient.Challenge(rival.PlayerId);
                yield return DuelUntil(() => DuelClient.Lobby.Phase == 2, 5f, "my invite is pending");
            }
            else if (!resume)
            {
                yield return DuelUntil(() => DuelClient.Lobby.Phase == 1, 60f, "an invite shows");
                DuelE2e("invite says stakes=" + DuelClient.Lobby.Stakes + " from " + DuelClient.Lobby.OtherName);
                yield return new WaitForSeconds(1f);
                DuelClient.Accept();
            }
            yield return DuelUntil(() => DuelClient.Running, 30f, "the match runs");
            if (DuelClient.View == null) { DuelE2e("no match, " + me + " done"); Application.Quit(); yield break; }
            DuelE2e("start: " + CardLine(pick) + ", opponent " + DuelClient.View.OppName + ", wagerState " + DuelClient.View.WagerState + ", moves so far " + DuelClient.View.Moves.Count);
            int placed = 0;
            float lastLog = 0f;
            while (DuelClient.View != null && !DuelClient.View.Finished)
            {
                DuelView v = DuelClient.View;
                if (v.YourTurn && DuelClient.SecondsLeft > DuelClient.MinPlaceSeconds + 0.3f && DuelBotPick(v)) { placed++; if (dropAfter > 0 && placed >= dropAfter) { DuelE2e("dropping the connection after " + placed + " placements"); Application.Quit(); yield break; } }
                if (Time.realtimeSinceStartup - lastLog > 10f) { lastLog = Time.realtimeSinceStartup; DuelE2e("turn " + v.Turn + " score " + v.Score(0) + "-" + v.Score(1) + " oppAway " + v.OppAwayMs); }
                yield return new WaitForSeconds(0.7f);
            }
            DuelView end = DuelClient.View;
            if (end == null) { Application.Quit(); yield break; }
            DuelE2e("match over: " + end.Result + " score " + end.Score(0) + "-" + end.Score(1) + " reason " + end.EndReason + " wagerState " + end.WagerState + " | during escrow " + CardLine(pick));
            if (end.MustPick)
            {
                DuelE2e("PICK SCREEN: " + end.PickCards.Count + " cards of the loser, " + end.PickMsLeft + " ms left");
                if (noPick) DuelE2e("not picking: the server takes a random card at 30 s");
                else { yield return new WaitForSeconds(1.5f); DuelClient.TakeCard(CardTable.Index(pick)); DuelE2e("took " + pick); }
            }
            else if (end.Result == DuelResult.Lose) DuelE2e("waiting for the winner to pick (wagerState " + end.WagerState + ")");
            yield return DuelUntil(() => DuelClient.View != null && DuelClient.View.WagerState >= DuelView.WagerPaid, 60f, "stakes settled");
            yield return new WaitForSeconds(2f); // the pushed state with the cards arrives
            end = DuelClient.View;
            if (end != null)
                DuelE2e("RESULT " + end.Result + " score " + end.Score(0) + "-" + end.Score(1) + " reason " + end.EndReason + " wagerState " + end.WagerState + " picked " + (end.PickedCard == DuelView.NoCard ? "none" : CardTable.ByIndex(end.PickedCard).Id) + (end.PickedAtRandom ? " (random)" : "") + " | " + CardLine(pick));
            Application.Quit();
        }

        static string RankLine()
        {
            CardRankData r = GameSession.Hero.Rpg.Cards.Rank;
            return "season " + r.season + " lp " + r.lp + " (" + RankedRules.TierName(RankedRules.Tier(r.lp)) + ") W/L/D " + r.wins + "/" + r.losses + "/" + r.draws + " best " + r.best + " mmr " + r.mmr;
        }

        /// <summary>--duel-ranked N: queues for Ranked, plays the match with the greedy pick, logs the LP the server gave, and does it again until N matches are done (the third match of one pair in a day does not count).</summary>
        IEnumerator RankedScript(int matches, int dropAfter = 0)
        {
            int placed = 0;
            yield return DuelUntil(() => DuelClient.RankedSeason > 0, 30f, "the server sent the season (season " + DuelClient.RankedSeason + ", " + DuelClient.RankedMinutesLeft + " min left)");
            yield return new WaitForSeconds(1.5f);
            DuelE2e("before: " + RankLine());
            for (int m = 1; m <= matches; m++)
            {
                DuelClient.Reset();
                yield return new WaitForSeconds(1f);
                DuelClient.QueueRanked();
                yield return DuelUntil(() => DuelClient.RankedQueued, 5f, "in the line");
                yield return DuelUntil(() => DuelClient.Running, 90f, "ranked match " + m + " starts");
                if (DuelClient.View == null) { Application.Quit(); yield break; }
                DuelE2e("match " + m + " start: ranked " + DuelClient.View.Ranked + ", wagerState " + DuelClient.View.WagerState + ", opponent " + DuelClient.View.OppName + ", cards " + CardTotal());
                while (DuelClient.View != null && !DuelClient.View.Finished)
                {
                    DuelView v = DuelClient.View;
                    if (v.YourTurn && DuelClient.SecondsLeft > DuelClient.MinPlaceSeconds + 0.3f && DuelBotPick(v)) placed++;
                    if (dropAfter > 0 && placed >= dropAfter) { DuelE2e("dropping after " + placed + " placements: " + RankLine()); Application.Quit(); yield break; } // --duel-drop N: the connection goes away mid-match
                    yield return new WaitForSeconds(0.7f);
                }
                yield return new WaitForSeconds(2f); // the pushed state with the new rank arrives
                DuelView end = DuelClient.View;
                if (end == null) { Application.Quit(); yield break; }
                DuelE2e("match " + m + " over: " + end.Result + " score " + end.Score(0) + "-" + end.Score(1) + " ranked " + end.Ranked + " lpChange " + end.LpChange + " notCounted " + end.RankNotCounted + " | " + RankLine() + " | cards " + CardTotal());
            }
            Application.Quit();
        }

        /// <summary>--duel-rank-peek: logs in, waits for the first pushed state and logs the rank (a result parked while this character was offline is applied at login), then quits.</summary>
        IEnumerator RankPeek()
        {
            yield return DuelUntil(() => DuelClient.RankedSeason > 0, 30f, "the server sent the season");
            yield return new WaitForSeconds(2f);
            DuelE2e("peek: " + RankLine());
            Application.Quit();
        }

        static bool DuelBotPick(DuelView v)
        {
            DuelBoard b = v.Board();
            int bestHand = 0, bestCell = -1, bestFlips = -1;
            for (int h = 0; h < v.Hand.Count; h++)
                for (int c = 0; c < DuelBoard.Cells; c++)
                {
                    if (!b.IsEmpty(c)) continue;
                    int flips = DuelBoard.PopCount(b.PreviewFlips(DuelView.Me, v.Hand[h], c));
                    if (flips > bestFlips) { bestFlips = flips; bestHand = h; bestCell = c; }
                }
            return bestCell >= 0 && DuelClient.Place(bestHand, bestCell);
        }
    }
}
