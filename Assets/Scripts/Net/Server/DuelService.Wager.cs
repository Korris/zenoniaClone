using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The card stakes on the zone (phase 06 v2). Before the match BOTH 8-card decks are put in escrow at server-api (both characters, one transaction, rev + 1 each, ZonePersistence.EscrowWager) and only then does the
    /// match start. After it the winner picks one card of the loser's deck (DuelService.Pvp.cs) and SettleStakes gives both decks back, the winner plus the picked card and the loser minus it (a draw gives everything back),
    /// retried by the queue under the same matchId until the API answers. The decks leave the player's copy here the same way the API took them (the API's own rev is adopted by the queue), so a card that dropped while
    /// waiting is not lost. A player who leaves while the decks are held still has a side in the match and may come back; a failed escrow after a lost answer is settled as a draw, so a card is never stuck.
    /// On a dev server (no accounts) the move happens in memory only. API switch off (403 wager_disabled): the match is played with nothing at stake.
    /// </summary>
    public sealed partial class DuelService
    {
        static DuelFail FailOf(ZoneApiReply r)
        {
            switch (r.Code)
            {
                case "wager_level": return DuelFail.WagerLevel;
                case "wager_daily_limit": case "wager_pair_limit": return DuelFail.WagerDaily;
                case "wager_account_age": return DuelFail.AccountAge;
                case "wager_self": return DuelFail.SameAccount;
                case "not_online": return DuelFail.OpponentLeft;
                case "wager_deck": case "wager_unknown_card": case "wager_invalid": return DuelFail.WagerRefused;
                default: return r.Status == 0 || r.Status >= 500 || r.Transient ? DuelFail.ApiBusy : DuelFail.WagerRefused;
            }
        }

        NetPlayer OnlineByKey(string key)
        {
            foreach (Entry e in entries.Values) if (CharKey(e.Player) == key) return e.Player;
            return null;
        }

        void FailStart(PvpSeat[] seat, LobbyStart s, DuelFail why, long now)
        {
            lobby.Finish(s.A);
            foreach (PvpSeat x in seat) Refuse(x.Player, why);
            Flush(now);
        }

        void BeginStakes(PvpSeat[] seat, LobbyStart s, long now)
        {
            NetPlayer a = seat[0].Player, b = seat[1].Player;
            int day = WagerRules.DayKeyNow();
            WagerError err = WagerRules.CanEnter(a.Rpg.State, day);
            if (err == WagerError.None) err = WagerRules.CanEnter(b.Rpg.State, day);
            if (err == WagerError.None && !pairs.Allowed(seat[0].Key, seat[1].Key, day)) err = WagerError.PairLimit;
            if (err != WagerError.None) { FailStart(seat, s, FailOf(err), now); return; }
            string matchId = NewMatchId();
            if (persistence == null) // dev server: the cards move in memory only
            {
                TakeDecks(seat, day);
                lobby.Finish(s.A);
                StartMatch(seat, true, matchId, now);
                return;
            }
            if (!persistence.ApiHealthy || a.Saved == null || b.Saved == null) { FailStart(seat, s, DuelFail.ApiBusy, now); return; }
            persistence.EscrowWager(a, b, matchId, r => OnEscrow(r, seat, matchId, s, day));
        }

        void OnEscrow(ZoneApiReply r, PvpSeat[] seat, string matchId, LobbyStart s, int day)
        {
            long now = Now();
            for (int i = 0; i < 2; i++) seat[i].Player = OnlineByKey(seat[i].Key); // a player may have come back on a new connection, or gone
            if (!r.Ok)
            {
                if (r.Code == "wager_disabled") // the emergency switch is off at the API: PvP still plays, no card is taken
                {
                    Debug.LogWarning("[ZenoniaProto] duel: server-api has Card Wager switched off: this duel is played with nothing at stake");
                    lobby.Finish(s.A);
                    StartMatch(seat, false, null, now);
                    return;
                }
                Debug.LogWarning("[ZenoniaProto] duel: escrow " + matchId + " not made: " + r.Code + " (status " + r.Status + ")" + (r.MayHaveLanded ? ", may have landed: settling as a draw" : ""));
                if (r.MayHaveLanded) SettleLoose(matchId, seat[0].Key, seat[1].Key);
                lobby.Finish(s.A);
                DuelFail why = FailOf(r);
                for (int i = 0; i < 2; i++) Refuse(seat[i].Player, why);
                Flush(now);
                return;
            }
            TakeDecks(seat, day);
            lobby.Finish(s.A);
            if (seat[0].Player == null && seat[1].Player == null) { SettleLoose(matchId, seat[0].Key, seat[1].Key); Flush(now); return; } // nobody is left to play it
            StartMatch(seat, true, matchId, now);
        }

        // the deck each player staked leaves their copy here (the API took it from the stored one), and the day's duel count and the pair count go up
        void TakeDecks(PvpSeat[] seat, int day)
        {
            pairs.Note(seat[0].Key, seat[1].Key, day);
            for (int i = 0; i < 2; i++)
            {
                NetPlayer p = seat[i].Owner;
                if (p == null || p.Hero == null) continue;
                CardCollection c = p.Rpg.State.Cards;
                seat[i].LockedKinds = c.EscrowDeck();
                if (c.Daily.day != day) c.Daily = new CardDailyData { day = day };
                c.Daily.wagers++;
                Zone.Enter(p.Hero.HomeZone);
                PushState(p);
            }
        }

        /// <summary>A settle with nobody to wait for (the escrow may exist but no match will be played): both get their own deck back.</summary>
        void SettleLoose(string matchId, string keyA, string keyB)
        {
            if (persistence == null) return;
            persistence.SettleWager(matchId, "draw", null, keyA, keyB, r => Debug.Log("[ZenoniaProto] duel: " + matchId + " returned as a draw: " + (r.Ok ? "ok" : r.Code + " (status " + r.Status + ")")));
        }

        /// <summary>The cards a side gets at the end: its own deck, plus the picked card for the winner, minus it for the loser. A draw gives every deck back.</summary>
        static string[] Payout(PvpRun run, int side, string result, string pick)
        {
            var cards = new System.Collections.Generic.List<string>(run.Deck[side]);
            if (result == "draw" || pick == null) return cards.ToArray();
            bool winner = (result == "a") == (side == 0);
            if (winner) cards.Add(pick); else cards.Remove(pick); // List.Remove drops the first copy only
            return cards.ToArray();
        }

        /// <summary>The result is known (and the winner's card chosen): give both decks back. Dev server: in memory; else server-api, retried by the queue under the same matchId.</summary>
        void SettleStakes(PvpRun run, string result, long now)
        {
            string pick = run.Pick != null ? run.Pick.Picked : null;
            run.StakeState = DuelView.WagerSettling;
            if (persistence == null)
            {
                for (int s = 0; s < 2; s++) Give(run, s, Payout(run, s, result, pick));
                run.StakeState = DuelView.WagerPaid;
                SendBoth(run, now);
                Close(run);
                return;
            }
            persistence.SettleWager(run.MatchId, result, pick, run.Key[0], run.Key[1], r => OnSettled(run, r));
            SendBoth(run, now);
        }

        void OnSettled(PvpRun run, ZoneApiReply r)
        {
            if (r.Ok)
            {
                stats.Add("wagersPaid", 1);
                if (r.Credits != null)
                    foreach (WagerCredit c in r.Credits)
                    {
                        Debug.Log("[ZenoniaProto] duel: " + run.MatchId + " settled for " + c.CharacterId + ": " + c.Cards.Length + " cards" + (c.Apply ? "" : " (not applied here: the API's copy is newer)"));
                        if (c.Apply) Give(run, c.Side, c.Cards);
                    }
                run.StakeState = DuelView.WagerPaid;
            }
            else
            {
                Debug.LogError("[ZenoniaProto] duel: " + run.MatchId + " NOT settled: " + r.Code + " (status " + r.Status + "); server-api returns held decks when they expire");
                run.StakeState = DuelView.WagerFailed;
            }
            SendBoth(run, Now());
            Close(run);
        }

        // gives the cards back to the side's character here (when they are online) and tells them; the deck list is put back and cut to the copies owned
        void Give(PvpRun run, int side, string[] cards)
        {
            NetPlayer p = OnlineByKey(run.Key[side]);
            if (p == null || p.Hero == null) return;
            CardCollection c = p.Rpg.State.Cards;
            c.ReturnCards(cards, run.LockedKinds[side]);
            c.Deck.Clear(); c.Deck.AddRange(run.Deck[side]); c.TrimDeck(); // a relogged player loaded without the held cards, hence without a deck
            Zone.Enter(p.Hero.HomeZone);
            PushState(p);
            if (ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p);
        }
    }
}
