using System;
using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>One placement. Side 0 / 1 (DuelMatch sides; a DuelView re-labels them 0 = viewer). FlipMask: bit i = cell i changed owner. Auto = the server placed it because the 20 s ran out.</summary>
    public struct DuelMove
    {
        public int Turn, Side, Card, Cell, FlipMask;
        public bool Auto;
    }

    public enum DuelOutcome { Playing = 0, Side0Wins = 1, Side1Wins = 2, Draw = 3 }

    /// <summary>Why a placement was refused (callers switch on this, never on the message). Stale = the player's picture of the turn or of the hand was out of date.</summary>
    public enum DuelPlayError { None = 0, Over = 1, NotYourTurn = 2, NoSuchCard = 3, CellTaken = 4, Stale = 5, NotYet = 6 }

    public struct DuelPlay
    {
        public bool Ok;
        public string Error;
        public DuelPlayError Code;
        public DuelMove Move;
        public static DuelPlay Fail(DuelPlayError code, string why) { return new DuelPlay { Code = code, Error = why }; }
    }

    /// <summary>
    /// The rules of one card duel, no networking, no Unity. Two sides (0 and 1) with 8 cards each, a 4 x 4 board, 16 turns (each side places all 8), a hand of 5 drawn from the shuffled deck and refilled after each placement,
    /// 20 s per turn with an automatic placement on timeout. Everything comes from the seed and the moves, so a match can be replayed. The match is the ONLY writer of its state: board, score, log (Moves) and hands.
    /// Time is passed in (`nowMs`, any monotonic clock): the match never reads a clock.
    /// </summary>
    public sealed class DuelMatch
    {
        public const int DeckSize = 8, HandSize = 5, Turns = DeckSize * 2, TurnMs = 20000, WinScore = 9;

        readonly DuelRng rng;
        readonly List<int>[] hand = { new List<int>(), new List<int>() };
        readonly Queue<int>[] draw = { new Queue<int>(), new Queue<int>() };
        readonly List<DuelMove> moves = new List<DuelMove>();
        public readonly DuelBoard Board = new DuelBoard();

        public int ToMove { get; private set; }
        public long DeadlineMs { get; private set; }
        public DuelOutcome Outcome { get; private set; }
        /// <summary>The side that gave up or was dropped (-1 = the match was played out).</summary>
        public int Forfeited { get; private set; }

        public IList<DuelMove> Moves { get { return moves; } }
        public bool Finished { get { return Outcome != DuelOutcome.Playing; } }
        /// <summary>Turn being played, 1 .. 16 (16 once finished).</summary>
        public int Turn { get { return Math.Min(Turns, moves.Count + 1); } }
        public IList<int> Hand(int side) { return hand[side]; }
        /// <summary>Cards a side still has to place (hand + undrawn).</summary>
        public int CardsLeft(int side) { return hand[side].Count + draw[side].Count; }

        DuelMatch(ulong seed) { rng = new DuelRng(seed); Forfeited = -1; }

        /// <summary>Starts a match. Both decks must be 8 known card ids (DeckRules checks ownership and limits before). The first side to move is chosen by the seed.</summary>
        public static DuelMatch Create(ulong seed, IList<string> deck0, IList<string> deck1, long nowMs)
        {
            var m = new DuelMatch(seed);
            m.Deal(0, deck0);
            m.Deal(1, deck1);
            m.ToMove = m.rng.Next(2);
            m.DeadlineMs = nowMs + TurnMs;
            return m;
        }

        void Deal(int side, IList<string> deck)
        {
            if (deck == null || deck.Count != DeckSize) throw new ArgumentException("a duel deck has exactly " + DeckSize + " cards");
            var cards = new List<int>(DeckSize);
            foreach (string id in deck)
            {
                int i = CardTable.Index(id);
                if (i < 0) throw new ArgumentException("unknown card " + id);
                cards.Add(i);
            }
            for (int i = cards.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); int t = cards[i]; cards[i] = cards[j]; cards[j] = t; }
            for (int i = 0; i < cards.Count; i++) { if (i < HandSize) hand[side].Add(cards[i]); else draw[side].Enqueue(cards[i]); }
        }

        /// <summary>`side` places the card at `handIndex` of its hand on `cell`. `expectTurn` (> 0) and `expectCard` (>= 0) are what the player saw when they tapped: a different current turn or hand card refuses it as Stale. Refused (nothing changes) when the match is over, it is not that side's turn, the hand index is out of range or the cell is taken. A placement restarts the 20 s clock for the other side.</summary>
        public DuelPlay Place(int side, int handIndex, int cell, long nowMs, int expectTurn = 0, int expectCard = -1)
        {
            DuelPlay p = Play(side, handIndex, cell, false, expectTurn, expectCard);
            if (p.Ok && !Finished) DeadlineMs = nowMs + TurnMs;
            return p;
        }

        DuelPlay Play(int side, int handIndex, int cell, bool auto, int expectTurn = 0, int expectCard = -1)
        {
            if (Finished) return DuelPlay.Fail(DuelPlayError.Over, "The duel is over");
            if (side != ToMove) return DuelPlay.Fail(DuelPlayError.NotYourTurn, "Not your turn");
            if (expectTurn > 0 && expectTurn != moves.Count + 1) return DuelPlay.Fail(DuelPlayError.Stale, "The turn has moved on");
            if (handIndex < 0 || handIndex >= hand[side].Count) return DuelPlay.Fail(DuelPlayError.NoSuchCard, "No such card in your hand");
            if (expectCard >= 0 && hand[side][handIndex] != expectCard) return DuelPlay.Fail(DuelPlayError.Stale, "Your hand has changed");
            if (cell < 0 || cell >= DuelBoard.Cells || !Board.IsEmpty(cell)) return DuelPlay.Fail(DuelPlayError.CellTaken, "That cell is taken");
            int card = hand[side][handIndex];
            hand[side].RemoveAt(handIndex);
            if (draw[side].Count > 0) hand[side].Add(draw[side].Dequeue()); // refill: the new card goes to the end of the hand
            int mask = Board.Place(side, card, cell);
            var move = new DuelMove { Turn = moves.Count + 1, Side = side, Card = card, Cell = cell, FlipMask = mask, Auto = auto };
            moves.Add(move);
            if (moves.Count >= Turns) Finish();
            else ToMove = 1 - side;
            return new DuelPlay { Ok = true, Move = move };
        }

        /// <summary>When the turn's deadline passed, places a random hand card on a random empty cell for the side to move (drawn from the match's own generator, so a replay agrees). A failed play when the time has not run out.</summary>
        public DuelPlay Tick(long nowMs)
        {
            if (Finished || nowMs < DeadlineMs) return DuelPlay.Fail(DuelPlayError.NotYet, "Not yet");
            int side = ToMove;
            var free = new List<int>(DuelBoard.Cells);
            for (int c = 0; c < DuelBoard.Cells; c++) if (Board.IsEmpty(c)) free.Add(c);
            int cell = free[rng.Next(free.Count)], handIndex = rng.Next(hand[side].Count);
            DuelPlay p = Play(side, handIndex, cell, true);
            if (p.Ok && !Finished) DeadlineMs = nowMs + TurnMs;
            return p;
        }

        /// <summary>`side` gives up or was dropped: the other side wins at once.</summary>
        public void Forfeit(int side)
        {
            if (Finished || (side != 0 && side != 1)) return;
            Forfeited = side;
            Outcome = side == 0 ? DuelOutcome.Side1Wins : DuelOutcome.Side0Wins;
        }

        void Finish()
        {
            int a = Board.Score(0), b = Board.Score(1);
            Outcome = a >= WinScore ? DuelOutcome.Side0Wins : b >= WinScore ? DuelOutcome.Side1Wins : DuelOutcome.Draw;
        }
    }
}
