using System;
using System.Collections.Generic;

namespace ZenoniaProto.Net
{
    /// <summary>The zone's own copy of one staked character: what an escrow and a payout are applied to. Reached through the character's NetPlayer, NOT its connection or hero, so it works for a player who has left.</summary>
    public interface IStakeHolder
    {
        /// <summary>The API took the staked deck out of the stored character: take the same cards out of this copy (and count the duel for today). Returns the kinds whose Lock flag left with their last copy.</summary>
        List<string> TakeDeck(int day);
        /// <summary>Put the cards back (own deck, the winner's picked card, or the deck minus the picked card), restore the deck list and the Lock flags, and let the owner know / save.</summary>
        void GiveCards(string[] cards, IList<string> lockedKinds, string[] deck);
    }

    public enum StakePhase { Idle, Escrowing, Held, Settling, Paid, Failed, Refused }

    /// <summary>
    /// The card stakes of one PvP match on the zone (phase 06 v2), apart from any Unity object so the tests can drive it against a fake server-api. It owns the three rules that keep a card from being made or lost:
    ///  1. the escrow / payout is applied to the character's copy (IStakeHolder) from the definite answer of the API, once, whether or not the player is still connected;
    ///  2. an escrow whose answer is lost is replayed until the API says (CharacterSaveQueue.Wager.cs never gives up an unknown outcome); if it turns out that it did land after the match was abandoned, the decks are taken and then settled as a draw;
    ///  3. a payout is applied only when the queue marks the credit Apply, and at most once per side (`paid`).
    /// Dev server (no accounts, queue == null): the same moves happen in memory.
    /// </summary>
    public sealed class StakeMatch
    {
        readonly CharacterSaveQueue queue;
        readonly SavedCharacter[] saved;
        readonly Func<int, IStakeHolder> holderOf;
        readonly bool[] paid = new bool[2];

        public readonly string MatchId;
        public readonly string[] Key = new string[2];
        public readonly string[][] Deck = new string[2][];
        /// <summary>Kinds whose Lock flag went with their last copy, per side (filled when the escrow is applied; the same list objects are handed on to the match).</summary>
        public readonly List<string>[] LockedKinds = { new List<string>(), new List<string>() };
        public StakePhase Phase { get; private set; }
        /// <summary>Nobody waits for this match any more (the escrow took too long and the players were told so): when the escrow turns out to have landed, the decks go back as a draw.</summary>
        public bool Abandoned { get; private set; }

        public StakeMatch(CharacterSaveQueue saveQueue, string matchId, SavedCharacter a, SavedCharacter b, string keyA, string keyB, string[] deckA, string[] deckB, Func<int, IStakeHolder> holder)
        {
            queue = saveQueue; MatchId = matchId; saved = new[] { a, b }; holderOf = holder;
            Key[0] = keyA; Key[1] = keyB; Deck[0] = deckA; Deck[1] = deckB;
        }

        /// <summary>The API takes both decks (or the dev server just moves them). `done` runs once, with the definite answer; Ok = the decks are out of both copies.</summary>
        public void Escrow(int day, Action<StakeMatch, ZoneApiReply> done)
        {
            Phase = StakePhase.Escrowing;
            if (queue == null) { OnEscrow(new ZoneApiReply { Status = 200, Rev = -1, RevB = -1, CurrentRev = -1 }, day, done); return; }
            queue.EscrowWager(MatchId, saved[0], saved[1], r => OnEscrow(r, day, done));
        }

        void OnEscrow(ZoneApiReply r, int day, Action<StakeMatch, ZoneApiReply> done)
        {
            if (!r.Ok) { Phase = StakePhase.Refused; done(this, r); return; }
            for (int s = 0; s < 2; s++)
            {
                IStakeHolder h = holderOf(s);
                LockedKinds[s].Clear();
                if (h != null) LockedKinds[s].AddRange(h.TakeDeck(day));
            }
            Phase = StakePhase.Held;
            if (Abandoned) Settle("draw", null, (m, rr) => { }); // the escrow landed after all: both get their own deck back
            done(this, r);
        }

        /// <summary>Called when the players were told the match will not start while the escrow is still unanswered.</summary>
        public void Abandon() { Abandoned = true; }

        /// <summary>The cards a side gets at the end: its own deck, plus the picked card for the winner, minus it for the loser. A draw gives every deck back.</summary>
        public string[] Payout(int side, string result, string pick)
        {
            var cards = new List<string>(Deck[side]);
            if (result == "draw" || pick == null) return cards.ToArray();
            bool winner = (result == "a") == (side == 0);
            if (winner) cards.Add(pick); else cards.Remove(pick); // List.Remove drops the first copy only
            return cards.ToArray();
        }

        /// <summary>Gives the decks back (`result` a / b / draw, `pick` = the card the winner takes). `done` runs once with the definite answer; the payout was applied before it.</summary>
        public void Settle(string result, string pick, Action<StakeMatch, ZoneApiReply> done)
        {
            if (Phase == StakePhase.Settling || Phase == StakePhase.Paid) return;
            Phase = StakePhase.Settling;
            if (queue == null)
            {
                for (int s = 0; s < 2; s++) Pay(s, Payout(s, result, pick));
                Phase = StakePhase.Paid;
                done(this, new ZoneApiReply { Status = 200, Rev = -1, RevB = -1, CurrentRev = -1, Credits = new WagerCredit[0] });
                return;
            }
            queue.SettleWager(MatchId, result, pick, Key[0], Key[1], r => OnSettle(r, done));
        }

        void OnSettle(ZoneApiReply r, Action<StakeMatch, ZoneApiReply> done)
        {
            if (r.Ok)
            {
                if (r.Credits != null)
                    foreach (WagerCredit c in r.Credits)
                        if (c.Apply && c.Side >= 0 && c.Side < 2) Pay(c.Side, c.Cards);
                Phase = StakePhase.Paid;
            }
            else Phase = StakePhase.Failed;
            done(this, r);
        }

        void Pay(int side, string[] cards)
        {
            if (paid[side]) return;
            paid[side] = true;
            IStakeHolder h = holderOf(side);
            if (h != null) h.GiveCards(cards, LockedKinds[side], Deck[side]);
        }

        public bool Paid(int side) { return paid[side]; }
    }
}
