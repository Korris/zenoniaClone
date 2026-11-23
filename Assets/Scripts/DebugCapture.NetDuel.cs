using System;
using System.Collections;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --net-bot duel --duel-role challenger|rival [--duel-wager] [--duel-drop N]: the end-to-end PvP script behind Tools/net-duel-test.ps1. The challenger invites the hero named "Rival" (the wager if --duel-wager),
    /// the rival accepts; with a wager both pick the same card, Lock, Confirm; then both play with a greedy pick until the result. --duel-drop N makes the rival quit after N of its own placements (the 60 s
    /// reconnect window, then the forfeit). Everything goes through DuelClient, the same calls the buttons make; what comes back is logged as "duel-e2e:" lines. Server flags: --dev-cards --dev-level 60 --map oakhaven-village.
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
            StartCoroutine(DuelScript(role == "challenger", NetLaunchArgs.HasFlag("--duel-wager"), drop));
        }

        IEnumerator DuelUntil(Func<bool> done, float timeout, string what)
        {
            float start = Time.realtimeSinceStartup;
            while (!done() && Time.realtimeSinceStartup - start < timeout) yield return null;
            DuelE2e((done() ? "OK " : "TIMEOUT ") + what + " after " + (Time.realtimeSinceStartup - start).ToString("F1") + " s");
        }

        IEnumerator DuelScript(bool challenger, bool wager, int dropAfter)
        {
            string me = NetLaunchArgs.Name;
            if (challenger)
            {
                yield return DuelUntil(() => HeroNamed("Rival") != null, 40f, "see Rival");
                HeroController rival = HeroNamed("Rival");
                if (rival == null) { Application.Quit(); yield break; }
                yield return new WaitForSeconds(2f);
                DuelClient.Challenge(rival.PlayerId, wager);
                yield return DuelUntil(() => DuelClient.Lobby.Phase == 2, 5f, "my invite is pending (wager " + wager + ")");
            }
            else
            {
                yield return DuelUntil(() => DuelClient.Lobby.Phase == 1, 60f, "an invite shows");
                DuelE2e("invite says wager=" + DuelClient.Lobby.Wager + " from " + DuelClient.Lobby.OtherName);
                yield return new WaitForSeconds(1f);
                DuelClient.Accept();
            }
            if (wager)
            {
                yield return DuelUntil(() => DuelClient.Lobby.Phase == 3, 15f, "wager panel open");
                int card = CardTable.Index("meadowSlime");
                DuelClient.WagerPick(card);
                yield return DuelUntil(() => DuelClient.Lobby.MyCard == card && DuelClient.Lobby.TheirCard != DuelLobby.NoCard, 10f, "both cards picked");
                DuelClient.WagerLock();
                yield return DuelUntil(() => DuelClient.Lobby.MyLocked && DuelClient.Lobby.TheirLocked, 10f, "both locked");
                DuelClient.WagerConfirm();
            }
            yield return DuelUntil(() => DuelClient.Running, 30f, "the match runs (wager " + (DuelClient.View != null && DuelClient.View.HasWager) + ")");
            if (DuelClient.View == null) { DuelE2e("no match, " + me + " done"); Application.Quit(); yield break; }
            int cardsBefore = GameSession.Hero.Rpg.Cards.Count("meadowSlime"), placed = 0;
            DuelE2e("start: meadowSlime x" + cardsBefore + ", gold " + GameSession.Hero.Rpg.Gold + ", opponent " + DuelClient.View.OppName);
            float lastLog = 0f;
            while (DuelClient.View != null && !DuelClient.View.Finished)
            {
                DuelView v = DuelClient.View;
                if (v.YourTurn && DuelClient.SecondsLeft > DuelClient.MinPlaceSeconds + 0.3f && DuelBotPick(v)) { placed++; if (dropAfter > 0 && placed >= dropAfter) { DuelE2e("dropping the connection after " + placed + " placements"); Application.Quit(); yield break; } }
                if (Time.realtimeSinceStartup - lastLog > 10f) { lastLog = Time.realtimeSinceStartup; DuelE2e("turn " + v.Turn + " score " + v.Score(0) + "-" + v.Score(1) + " oppAway " + v.OppAwayMs); }
                yield return new WaitForSeconds(0.7f);
            }
            yield return new WaitForSeconds(4f); // the payout settles (wager) and the final state arrives
            DuelView end = DuelClient.View;
            if (end != null)
                DuelE2e("RESULT " + end.Result + " score " + end.Score(0) + "-" + end.Score(1) + " reason " + end.EndReason + " reward " + end.RewardDaily + " wagerState " + end.WagerState + " | meadowSlime x" + GameSession.Hero.Rpg.Cards.Count("meadowSlime") + " (was " + cardsBefore + "), gold " + GameSession.Hero.Rpg.Gold);
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
                    int flips = DuelNpcAi.PopCount(b.PreviewFlips(DuelView.Me, v.Hand[h], c));
                    if (flips > bestFlips) { bestFlips = flips; bestHand = h; bestCell = c; }
                }
            return bestCell >= 0 && DuelClient.Place(bestHand, bestCell);
        }
    }
}
