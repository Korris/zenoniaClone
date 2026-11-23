using System;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the PvP duel screens (phase 06 v2) when two real clients are not at hand. They feed DuelClient the same messages the server would send (DuelLobbyMsg, a PvP DuelView built by the real
    /// DuelPvpMatch), so what is drawn is the real window code. Use with --duel-seed (cards + deck) and --map oakhaven-village:
    ///   --duel-pvp invite | invite-off | waiting | battle | pick | lose-waiting | won-taken | lost-taken | draw | flip-demo
    /// </summary>
    public partial class DebugCapture
    {
        static readonly string[] RivalDeck = { "gloomWolf", "snowFox", "frostBear", "frostSlime", "iceGoblin", "spectralWolf", "hollowTreant", "sandRaider" };

        static void DebugDuelPvp(string mode)
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return;
            p.Level = Mathf.Max(p.Level, 30);
            DuelClient.DebugPretendOnline = true;
            int taken = CardTable.Index("frostBear");
            switch (mode)
            {
                case "invite": Lobby(1, true, 11000); break;
                case "invite-off": Lobby(1, false, 11000); break;
                case "waiting": Lobby(2, true, 9000); break;
                case "flip-demo": FlipDemo(); break;
                case "battle": Match(10, DuelView.WagerHeld, true, DuelView.NoCard, 0); break;
                case "pick": Match(16, DuelView.WagerPicking, true, DuelView.NoCard, 24000); break;
                case "lose-waiting": Match(16, DuelView.WagerPicking, false, DuelView.NoCard, 21000); break;
                case "won-taken": Match(16, DuelView.WagerPaid, true, taken, 0); break;
                case "lost-taken": Match(16, DuelView.WagerPaid, false, taken, 0); break;
                case "draw": Match(16, DuelView.WagerPaid, null, DuelView.NoCard, 0); break;
            }
        }

        static void Lobby(int phase, bool stakes, int msLeft)
        {
            DuelClient.ApplyLobby(new DuelLobbyMsg { Phase = (byte)phase, Other = 2, OtherName = "Rival Knight", Stakes = stakes, MsLeft = (ushort)msLeft });
        }

        // plays the real PvP match with a greedy pick for both sides until `moves` placements are made; `wantWin` true / false / null (draw) searches a seed with that result when it is played out.
        // The stakes fields are filled the way the zone does: `state` = DuelView.Wager*, `picked` = the card taken, `pickMs` = the winner's time left; the winner gets the loser's 8 deck cards to choose from.
        static void Match(int moves, int state, bool? wantWin, int picked, int pickMs)
        {
            PlayerState p = RpgSession.Player;
            CardWindow.Open(CardWindow.Page.Duel);
            for (ulong seed = 1; seed < 3000; seed++)
            {
                DuelPvpMatch m = DuelPvpMatch.Start(seed, p.Cards.Deck, RivalDeck, "You", "Rival Knight", 0);
                while (m.Match.Moves.Count < moves && !m.Finished) Greedy(m);
                if (moves < DuelMatch.Turns || !wantWin.HasValue && m.Match.Outcome == DuelOutcome.Draw || wantWin.HasValue && (wantWin.Value ? m.Match.Outcome == DuelOutcome.Side0Wins : m.Match.Outcome == DuelOutcome.Side1Wins))
                {
                    DuelView v = m.View(0, m.Match.DeadlineMs - 17000);
                    v.WagerState = state; v.PickedCard = picked; v.PickMsLeft = pickMs;
                    if (state == DuelView.WagerPicking && wantWin == true) foreach (string id in RivalDeck) v.PickCards.Add(CardTable.Index(id));
                    DuelClient.Apply(v);
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
                DuelPvpMatch m = DuelPvpMatch.Start(seed, p.Cards.Deck, RivalDeck, "You", "Rival Knight", 0);
                while (m.Match.Moves.Count < 8 && !m.Finished) Greedy(m);
                int side = m.Match.ToMove, bestHand = 0, bestCell = -1, bestFlips = -1;
                for (int h = 0; h < m.Match.Hand(side).Count; h++)
                    for (int c = 0; c < DuelBoard.Cells; c++)
                    {
                        if (!m.Match.Board.IsEmpty(c)) continue;
                        int flips = DuelBoard.PopCount(m.Match.Board.PreviewFlips(side, m.Match.Hand(side)[h], c));
                        if (flips > bestFlips) { bestFlips = flips; bestHand = h; bestCell = c; }
                    }
                if (bestFlips < 2) continue;
                DuelView before = m.View(0, m.Match.DeadlineMs - 17000);
                m.Place(side, bestHand, bestCell, 0);
                DuelView after = m.View(0, m.Match.DeadlineMs - 17000);
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
                    int flips = DuelBoard.PopCount(m.Match.Board.PreviewFlips(side, m.Match.Hand(side)[h], c));
                    if (flips > bestFlips) { bestFlips = flips; bestHand = h; bestCell = c; }
                }
            m.Place(side, bestHand, bestCell, 0);
        }
    }
}
