using System.Collections.Generic;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// "Winner takes 1 card" (FF8 "One" rule, the user's final rule of 23/11): after a decided PvP duel the winner picks ANY ONE card of the loser's 8-card deck used in that match (SR / SSR / boss included).
    /// Pure, no clock of its own: the winner has PickMs (30 s) and then the card is taken at random (the server picks, so a player who walked away still settles). A draw has no round. Only a card of the staked
    /// deck can be taken (by id: a deck with two copies of a card still has just that card to name; one copy goes).
    /// </summary>
    public sealed class DuelPickRound
    {
        public const long PickMs = 30000;

        public readonly int Winner;
        public readonly string[] LoserDeck;
        public readonly long DeadlineMs;
        /// <summary>The card id taken, null until it is.</summary>
        public string Picked { get; private set; }
        public bool AtRandom { get; private set; }

        public DuelPickRound(int winnerSide, IList<string> loserDeck, long nowMs)
        {
            Winner = winnerSide;
            LoserDeck = new string[loserDeck.Count];
            loserDeck.CopyTo(LoserDeck, 0);
            DeadlineMs = nowMs + PickMs;
        }

        public bool Done { get { return Picked != null; } }

        public int MsLeft(long nowMs) { return Done ? 0 : (int)System.Math.Max(0L, System.Math.Min(PickMs, DeadlineMs - nowMs)); }

        /// <summary>The winner chose `cardId`. False (nothing changes) when it is not in the loser's staked deck or a card was already taken: a second pick never replaces the first.</summary>
        public bool Take(string cardId)
        {
            if (Done || cardId == null || System.Array.IndexOf(LoserDeck, cardId) < 0) return false;
            Picked = cardId;
            return true;
        }

        /// <summary>The 30 s ran out: a random card of the loser's deck is taken. True when it took one now.</summary>
        public bool Tick(long nowMs, DuelRng rng)
        {
            if (Done || nowMs < DeadlineMs || LoserDeck.Length == 0) return false;
            Picked = LoserDeck[rng.Next(LoserDeck.Length)];
            AtRandom = true;
            return true;
        }
    }
}
