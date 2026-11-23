using System.Collections.Generic;
using System.Text;

namespace ZenoniaProto.Rpg
{
    public enum DuelResult { Playing = 0, Win = 1, Lose = 2, Draw = 3 }

    /// <summary>
    /// What one player sees of a duel: the move list (side 0 = the viewer), their own hand, whose turn, the time left, the result and the rewards. The BOARD, the SCORE, the cards left and the
    /// BATTLE LOG are all derived from the move list here, and from nothing else, so they can never disagree (the mockup's log said a card was flipped while the board still showed it
    /// unflipped). Flips come from each move's FlipMask as the server decided them: a viewer never re-applies the rule. Built by DuelView.From (server, offline host) or read off the wire.
    /// </summary>
    public sealed class DuelView
    {
        public const int Me = 0, Them = 1;

        public int Npc;
        public string OppName = "";
        public DuelResult Result;
        /// <summary>0 = played out, 1 = the viewer gave up / was dropped, 2 = the opponent did.</summary>
        public int EndReason;
        public bool YourTurn;
        /// <summary>Milliseconds left of the turn at the moment this view was made (the receiver adds its own clock).</summary>
        public int MsLeft;
        public int RewardFirst, RewardDaily;
        public readonly List<DuelMove> Moves = new List<DuelMove>();
        public readonly List<int> Hand = new List<int>();

        public bool Finished { get { return Result != DuelResult.Playing; } }
        public int Turn { get { return System.Math.Min(DuelMatch.Turns, Moves.Count + 1); } }

        /// <summary>The board rebuilt from the moves (owner 0 = viewer, 1 = opponent).</summary>
        public DuelBoard Board()
        {
            var b = new DuelBoard();
            foreach (DuelMove m in Moves)
            {
                b.Set(m.Cell, m.Card, m.Side);
                for (int c = 0; c < DuelBoard.Cells; c++) if ((m.FlipMask & (1 << c)) != 0) b.SetOwner(c, m.Side);
            }
            return b;
        }

        public int Score(int side) { return Board().Score(side); }

        public int CardsLeft(int side)
        {
            int placed = 0;
            foreach (DuelMove m in Moves) if (m.Side == side) placed++;
            return DuelMatch.DeckSize - placed;
        }

        /// <summary>"You placed Thorn Boar at B2, flipping Mire Toad (B3)." for move i. A timeout placement says so.</summary>
        public string LogLine(int i)
        {
            if (i < 0 || i >= Moves.Count) return "";
            DuelMove m = Moves[i];
            CardDef card = CardTable.ByIndex(m.Card);
            var sb = new StringBuilder();
            sb.Append(m.Side == Me ? "You" : (string.IsNullOrEmpty(OppName) ? "Opponent" : OppName));
            sb.Append(" placed ").Append(card != null ? card.Name : "?").Append(" at ").Append(DuelBoard.Coord(m.Cell));
            if (m.Auto) sb.Append(" (time ran out)");
            var flipped = new List<string>();
            for (int c = 0; c < DuelBoard.Cells; c++)
                if ((m.FlipMask & (1 << c)) != 0) flipped.Add(NameAt(c, i) + " (" + DuelBoard.Coord(c) + ")");
            if (flipped.Count > 0) sb.Append(", flipping ").Append(string.Join(", ", flipped.ToArray()));
            return sb.Append('.').ToString();
        }

        // the card sitting on `cell` before move `before`
        string NameAt(int cell, int before)
        {
            for (int j = 0; j < before; j++)
                if (Moves[j].Cell == cell) { CardDef c = CardTable.ByIndex(Moves[j].Card); return c != null ? c.Name : "?"; }
            return "?";
        }

        /// <summary>One side's view of a match. `mySide` is the DuelMatch side of the viewer; moves are re-labelled so 0 = viewer. The opponent's hand is never copied.</summary>
        public static DuelView From(DuelMatch match, int mySide, int npc, string oppName, long nowMs, int rewardFirst = 0, int rewardDaily = 0)
        {
            var v = new DuelView { Npc = npc, OppName = oppName ?? "", RewardFirst = rewardFirst, RewardDaily = rewardDaily };
            foreach (DuelMove m in match.Moves) { DuelMove r = m; r.Side = m.Side == mySide ? Me : Them; v.Moves.Add(r); }
            foreach (int c in match.Hand(mySide)) v.Hand.Add(c);
            v.YourTurn = !match.Finished && match.ToMove == mySide;
            v.MsLeft = match.Finished ? 0 : (int)System.Math.Max(0L, System.Math.Min(60000L, match.DeadlineMs - nowMs));
            if (match.Finished)
            {
                v.Result = match.Outcome == DuelOutcome.Draw ? DuelResult.Draw : (match.Outcome == DuelOutcome.Side0Wins) == (mySide == 0) ? DuelResult.Win : DuelResult.Lose;
                v.EndReason = match.Forfeited < 0 ? 0 : match.Forfeited == mySide ? 1 : 2;
            }
            return v;
        }
    }
}
