using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The Card Wager on the zone (phase 06): before the match the two cards are put in escrow at server-api (both characters, one transaction, rev + 1 each, ZonePersistence.EscrowWager) and only then does the
    /// match start; after it the winner is paid (or both get their own back on a draw) with SettleWager, retried by the queue under the same matchId until the API answers. The card is taken from the player's copy
    /// here the same way the API took it (the API's own rev is adopted by the queue), so a card that dropped while waiting is not lost. A player who leaves while the cards are held still has a side in the match
    /// and may come back; a failed escrow after a lost answer is settled as a draw, so a card is never stuck. On a dev server (no accounts) the move happens in memory only.
    /// </summary>
    public sealed partial class DuelService
    {
        static DuelFail FailOf(ZoneApiReply r)
        {
            switch (r.Code)
            {
                case "wager_disabled": return DuelFail.WagerOff;
                case "wager_level": return DuelFail.WagerLevel;
                case "wager_daily_limit": case "wager_pair_limit": return DuelFail.WagerDaily;
                case "wager_rarity_mismatch": return DuelFail.CardMismatch;
                case "wager_unknown_card": case "wager_boss_card": case "wager_rarity": case "wager_no_spare": return DuelFail.WagerCard;
                case "not_online": return DuelFail.OpponentLeft;
                case "wager_account_age": case "wager_self": case "wager_stack_full": return DuelFail.WagerRefused;
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

        void BeginWager(PvpSeat[] seat, LobbyStart s, long now)
        {
            NetPlayer a = seat[0].Player, b = seat[1].Player;
            int day = DuelNpcRun.DayKeyNow();
            CardDef ca = CardTable.ByIndex(s.CardA), cb = CardTable.ByIndex(s.CardB);
            WagerError err = WagerRules.CanEnter(a.Rpg.State, day);
            if (err == WagerError.None) err = WagerRules.CanEnter(b.Rpg.State, day);
            if (err == WagerError.None) err = ca == null ? WagerError.UnknownCard : WagerRules.CheckCard(a.Rpg.State, ca.Id);
            if (err == WagerError.None) err = cb == null ? WagerError.UnknownCard : WagerRules.CheckCard(b.Rpg.State, cb.Id);
            if (err == WagerError.None && !WagerRules.Matches(ca, cb)) err = WagerError.Mismatch;
            if (err == WagerError.None && !pairs.Allowed(seat[0].Key, seat[1].Key, day)) err = WagerError.PairLimit;
            if (err != WagerError.None) { FailStart(seat, s, FailOf(err), now); return; }
            string[] cards = { ca.Id, cb.Id };
            string matchId = NewMatchId();
            if (persistence == null) // dev server: the cards move in memory only
            {
                TakeCards(seat, cards, day);
                lobby.Finish(s.A);
                StartMatch(seat, cards, matchId, now);
                return;
            }
            if (!persistence.ApiHealthy || a.Saved == null || b.Saved == null) { FailStart(seat, s, DuelFail.ApiBusy, now); return; }
            persistence.EscrowWager(a, b, ca.Id, cb.Id, matchId, r => OnEscrow(r, seat, cards, matchId, s, day));
        }

        void OnEscrow(ZoneApiReply r, PvpSeat[] seat, string[] cards, string matchId, LobbyStart s, int day)
        {
            long now = Now();
            if (!r.Ok)
            {
                Debug.LogWarning("[ZenoniaProto] duel: wager escrow " + matchId + " not made: " + r.Code + " (status " + r.Status + ")" + (r.MayHaveLanded ? ", may have landed: settling as a draw" : ""));
                if (r.MayHaveLanded) SettleLoose(matchId, seat[0].Key, seat[1].Key);
                lobby.Finish(s.A);
                DuelFail why = FailOf(r);
                for (int i = 0; i < 2; i++) Refuse(OnlineByKey(seat[i].Key), why);
                Flush(now);
                return;
            }
            for (int i = 0; i < 2; i++) seat[i].Player = OnlineByKey(seat[i].Key); // a player may have come back on a new connection, or gone
            TakeCards(seat, cards, day);
            lobby.Finish(s.A);
            if (seat[0].Player == null && seat[1].Player == null) { SettleLoose(matchId, seat[0].Key, seat[1].Key); Flush(now); return; } // nobody is left to play it
            StartMatch(seat, cards, matchId, now);
        }

        // the card each player put in leaves their copy here (the API took it from the stored one), and the day's wager count and the pair count go up
        void TakeCards(PvpSeat[] seat, string[] cards, int day)
        {
            pairs.Note(seat[0].Key, seat[1].Key, day);
            for (int i = 0; i < 2; i++)
            {
                NetPlayer p = seat[i].Player;
                if (p == null || p.Hero == null) continue;
                CardCollection c = p.Rpg.State.Cards;
                c.Remove(cards[i]);
                if (c.Daily.day != day) c.Daily = new CardDailyData { day = day };
                c.Daily.wagers++;
                Zone.Enter(p.Hero.HomeZone);
                PushState(p);
            }
        }

        /// <summary>A settle with nobody to wait for (the escrow may exist but no match will be played): both get their own card back.</summary>
        void SettleLoose(string matchId, string keyA, string keyB)
        {
            if (persistence == null) return;
            persistence.SettleWager(matchId, "draw", keyA, keyB, r => Debug.Log("[ZenoniaProto] duel: wager " + matchId + " returned as a draw: " + (r.Ok ? "ok" : r.Code + " (status " + r.Status + ")")));
        }

        void SettleWager(PvpRun run, long now)
        {
            string result = run.Match.WagerResult;
            string toA = result == "a" || result == "draw" ? run.Key[0] : null, toB = result == "b" || result == "draw" ? run.Key[1] : null;
            run.WagerState = DuelView.WagerSettling;
            if (persistence == null) // dev server: in memory
            {
                if (result == "draw") { Give(toA, run.CardIdx[0]); Give(toB, run.CardIdx[1]); }
                else { string winner = toA ?? toB; Give(winner, run.CardIdx[0]); Give(winner, run.CardIdx[1]); }
                run.WagerState = DuelView.WagerPaid;
                return;
            }
            persistence.SettleWager(run.MatchId, result, toA, toB, r => OnSettled(run, r));
        }

        void OnSettled(PvpRun run, ZoneApiReply r)
        {
            if (r.Ok)
            {
                stats.Add("wagersPaid", 1);
                if (r.Credits != null)
                    foreach (WagerCredit c in r.Credits)
                    {
                        Debug.Log("[ZenoniaProto] duel: wager " + run.MatchId + " paid to " + c.CharacterId + ": " + string.Join(",", c.Cards) + (c.Apply ? "" : " (not applied here: the API's copy is newer)"));
                        if (c.Apply) foreach (string card in c.Cards) Give(c.CharacterId, CardTable.Index(card));
                    }
                run.WagerState = DuelView.WagerPaid;
            }
            else
            {
                Debug.LogError("[ZenoniaProto] duel: wager " + run.MatchId + " NOT settled: " + r.Code + " (status " + r.Status + "); server-api returns held cards when they expire");
                run.WagerState = DuelView.WagerFailed;
            }
            SendBoth(run, Now());
        }

        // adds one card to the character here (when they are online) and tells them
        void Give(string key, int cardIndex)
        {
            CardDef card = CardTable.ByIndex(cardIndex);
            NetPlayer p = key == null || card == null ? null : OnlineByKey(key);
            if (p == null || p.Hero == null) return;
            p.Rpg.State.Cards.Add(card.Id);
            Zone.Enter(p.Hero.HomeZone);
            PushState(p);
        }
    }
}
