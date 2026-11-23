using System;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the PvP duel and the Card Wager screens (phase 06) when two real clients are not at hand. They feed DuelClient the same messages the server would send (DuelLobbyMsg, a PvP DuelView built by
    /// the real DuelPvpMatch), so what is drawn is the real window code. Use with --duel-seed (cards + deck) and --map oakhaven-village:
    ///   --duel-pvp invite-wager | invite-plain | waiting | wager-pick | wager-locked | wager-confirm | battle | result | result-lost | result-draw
    /// </summary>
    public partial class DebugCapture
    {
        static void DebugDuelPvp(string mode)
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            p.Level = Mathf.Max(p.Level, 30);
            int mine = CardTable.Index("meadowSlime"), theirs = CardTable.Index("meadowMantis");
            switch (mode)
            {
                case "invite-wager": Lobby(1, true, DuelLobby.NoCard, DuelLobby.NoCard, false, false, false, false, 11000); break;
                case "invite-plain": Lobby(1, false, DuelLobby.NoCard, DuelLobby.NoCard, false, false, false, false, 11000); break;
                case "waiting": Lobby(2, true, DuelLobby.NoCard, DuelLobby.NoCard, false, false, false, false, 9000); break;
                case "wager-pick": Lobby(3, true, mine, DuelLobby.NoCard, false, false, false, false, 96000); break;
                case "wager-locked": Lobby(3, true, mine, theirs, true, true, false, false, 80000); break;
                case "wager-confirm": Lobby(3, true, mine, theirs, true, true, true, false, 70000); break;
                case "flip-demo": FlipDemo(); break;
                case "battle": Match(10, mine, theirs, DuelView.WagerHeld, false); break;
                case "result": Match(16, mine, theirs, DuelView.WagerPaid, true); break;
                case "result-lost": Match(16, mine, theirs, DuelView.WagerPaid, false); break;
                case "result-draw": Match(16, mine, theirs, DuelView.WagerPaid, null); break;
            }
        }

        static void Lobby(int phase, bool wager, int myCard, int theirCard, bool myLock, bool theirLock, bool myConf, bool theirConf, int msLeft)
        {
            DuelClient.ApplyLobby(new DuelLobbyMsg
            {
                Phase = (byte)phase, Other = 2, OtherName = "Rival Knight", Wager = wager, MyCard = (byte)myCard, TheirCard = (byte)theirCard,
                MyLocked = myLock, TheirLocked = theirLock, MyConfirmed = myConf, TheirConfirmed = theirConf, MsLeft = (ushort)msLeft,
            });
        }

        // plays the real PvP match with a greedy pick for both sides until `moves` placements are made; `wantWin` true / false / null (draw) searches a seed with that result when it is played out
        static void Match(int moves, int mine, int theirs, int wagerState, bool? wantWin)
        {
            PlayerState p = RpgSession.Player;
            CardWindow.Open(CardWindow.Page.Duel);
            for (ulong seed = 1; seed < 3000; seed++)
            {
                DuelPvpMatch m = DuelPvpMatch.Start(seed, p.Cards.Deck, DuelNpcTable.Get(1).Deck, "You", "Rival Knight", 0);
                while (m.Match.Moves.Count < moves && !m.Finished) Greedy(m);
                if (moves < DuelMatch.Turns || !wantWin.HasValue && m.Match.Outcome == DuelOutcome.Draw || wantWin.HasValue && (wantWin.Value ? m.Match.Outcome == DuelOutcome.Side0Wins : m.Match.Outcome == DuelOutcome.Side1Wins))
                {
                    var reward = new DuelReward { DailyGold = wantWin == true ? 300 : wantWin == false ? 100 : 150 };
                    DuelClient.Apply(m.View(0, m.Match.DeadlineMs - 17000, mine, theirs, wagerState, reward));
                    return;
                }
            }
            Debug.LogWarning("[ZenoniaProto] duel-pvp: no seed gives that result");
        }

        // a position where the next greedy move flips 2 or more cards: the view before it is shown at once, the view after it 3 s later (--shots then catch the animation frame by frame)
        static void FlipDemo()
        {
            PlayerState p = RpgSession.Player;
            CardWindow.Open(CardWindow.Page.Duel);
            for (ulong seed = 1; seed < 3000; seed++)
            {
                DuelPvpMatch m = DuelPvpMatch.Start(seed, p.Cards.Deck, DuelNpcTable.Get(1).Deck, "You", "Rival Knight", 0);
                while (m.Match.Moves.Count < 8 && !m.Finished) Greedy(m);
                int side = m.Match.ToMove, bestHand = 0, bestCell = -1, bestFlips = -1;
                for (int h = 0; h < m.Match.Hand(side).Count; h++)
                    for (int c = 0; c < DuelBoard.Cells; c++)
                    {
                        if (!m.Match.Board.IsEmpty(c)) continue;
                        int flips = DuelNpcAi.PopCount(m.Match.Board.PreviewFlips(side, m.Match.Hand(side)[h], c));
                        if (flips > bestFlips) { bestFlips = flips; bestHand = h; bestCell = c; }
                    }
                if (bestFlips < 2) continue;
                DuelView before = m.View(0, m.Match.DeadlineMs - 17000, -1, -1, 0);
                m.Place(side, bestHand, bestCell, 0);
                DuelView after = m.View(0, m.Match.DeadlineMs - 17000, -1, -1, 0);
                DuelClient.Apply(before);
                new GameObject("FlipDemoTimer").AddComponent<FlipDemoTimer>().Arm(after, Time.realtimeSinceStartup + 3f);
                Debug.Log("[ZenoniaProto] flip-demo: seed " + seed + ", " + (side == 0 ? "you place" : "the rival places") + " " + DuelBoard.Coord(bestCell) + " and flips " + bestFlips + " cards at t=3 s");
                return;
            }
        }

        sealed class FlipDemoTimer : MonoBehaviour
        {
            DuelView after; float at;
            public void Arm(DuelView v, float when) { after = v; at = when; }
            void Update() { if (after != null && Time.realtimeSinceStartup >= at) { DuelClient.Apply(after); Debug.Log("[ZenoniaProto] flip-demo: move applied at " + Time.realtimeSinceStartup.ToString("F3")); after = null; Destroy(gameObject); } }
        }

        static void Greedy(DuelPvpMatch m)
        {
            int side = m.Match.ToMove, bestHand = 0, bestCell = -1, bestFlips = -1;
            for (int h = 0; h < m.Match.Hand(side).Count; h++)
                for (int c = 0; c < DuelBoard.Cells; c++)
                {
                    if (!m.Match.Board.IsEmpty(c)) continue;
                    int flips = DuelNpcAi.PopCount(m.Match.Board.PreviewFlips(side, m.Match.Hand(side)[h], c));
                    if (flips > bestFlips) { bestFlips = flips; bestHand = h; bestCell = c; }
                }
            m.Place(side, bestHand, bestCell, 0);
        }
    }
}
