using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// One duel between two players (phase 06), no networking and no clock of its own: side 0 is the inviter, side 1 the receiver, the rules are DuelMatch's. On top of it: a player who drops off the
    /// connection has ReconnectMs (60 s) to come back and loses when it runs out (the turn clock keeps running meanwhile: DuelMatch places for the side to move when its 20 s are up, connected or not);
    /// leaving or giving up loses at once. If both are away the one who left first loses first. The result is also the Card Wager's outcome ("a" / "b" / "draw" = side 0 wins / side 1 wins / draw).
    /// </summary>
    public sealed class DuelPvpMatch
    {
        public const long ReconnectMs = 60000;

        public readonly DuelMatch Match;
        public readonly string[] Names;
        readonly long[] awaySince = { -1, -1 };

        DuelPvpMatch(DuelMatch match, string nameA, string nameB) { Match = match; Names = new[] { nameA ?? "", nameB ?? "" }; }

        /// <summary>Both decks must already be valid 8-card decks (DeckRules). The side that moves first is chosen by the seed.</summary>
        public static DuelPvpMatch Start(ulong seed, IList<string> deckA, IList<string> deckB, string nameA, string nameB, long nowMs)
        {
            return new DuelPvpMatch(DuelMatch.Create(seed, deckA, deckB, nowMs), nameA, nameB);
        }

        public bool Finished { get { return Match.Finished; } }

        public DuelPlay Place(int side, int handIndex, int cell, long nowMs, int expectTurn = 0, int expectCard = -1)
        {
            return Match.Place(side, handIndex, cell, nowMs, expectTurn, expectCard);
        }

        public void Forfeit(int side) { Match.Forfeit(side); }

        /// <summary>The side's connection dropped (idempotent: the first moment counts).</summary>
        public void SetAway(int side, long nowMs) { if (awaySince[side] < 0) awaySince[side] = nowMs; }

        /// <summary>The side is back (the same character joined again).</summary>
        public void SetBack(int side) { awaySince[side] = -1; }

        public bool IsAway(int side) { return awaySince[side] >= 0; }

        public long AwayMs(int side, long nowMs) { return awaySince[side] < 0 ? 0L : System.Math.Max(0L, nowMs - awaySince[side]); }

        /// <summary>Runs whatever is due: a side whose 60 s ran out forfeits, else the turn timeout places for the side to move. True when the match changed.</summary>
        public bool Tick(long nowMs)
        {
            if (Match.Finished) return false;
            int loser = -1;
            for (int s = 0; s < 2; s++)
                if (awaySince[s] >= 0 && nowMs - awaySince[s] >= ReconnectMs && (loser < 0 || awaySince[s] < awaySince[loser])) loser = s;
            if (loser >= 0) { Match.Forfeit(loser); return true; }
            return Match.Tick(nowMs).Ok;
        }

        /// <summary>"a" (side 0 won), "b" (side 1 won), "draw"; null while it plays.</summary>
        public string WagerResult
        {
            get
            {
                switch (Match.Outcome)
                {
                    case DuelOutcome.Side0Wins: return "a";
                    case DuelOutcome.Side1Wins: return "b";
                    case DuelOutcome.Draw: return "draw";
                    default: return null;
                }
            }
        }

        /// <summary>What `side` sees: moves re-labelled so 0 = them, their own hand, the clock, the opponent's away time. `wagerState` / the cards are the Card Wager's (DuelView.Wager*).</summary>
        public DuelView View(int side, long nowMs, int wagerMine = DuelView.NoCard, int wagerTheirs = DuelView.NoCard, int wagerState = 0, DuelReward reward = default(DuelReward))
        {
            DuelView v = DuelView.From(Match, side, DuelView.PlayerOpponent, Names[1 - side], nowMs, 0, reward.DailyGold);
            v.WagerMine = wagerMine; v.WagerTheirs = wagerTheirs; v.WagerState = wagerState;
            v.OppAwayMs = (int)System.Math.Min(60000L, AwayMs(1 - side, nowMs));
            return v;
        }
    }
}
