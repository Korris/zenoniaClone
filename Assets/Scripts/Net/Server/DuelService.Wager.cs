using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The card stakes on the zone (phase 06 v2), as the glue between the duel and StakeMatch (Net/Protocol, tested against a fake server-api). Before the match BOTH 8-card decks are put in escrow at server-api
    /// (both characters, one transaction, rev + 1 each) and only then does the match start. After it the winner picks one card of the loser's deck (DuelService.Pvp.cs) and the stakes are settled: both decks come back,
    /// the winner plus the picked card and the loser minus it (a draw gives everything back).
    /// The escrow and the payout are applied to the character's own copy (run.Owner / PvpSeat.Owner: its NetPlayer, which still holds the state after the connection and hero are gone), never to "whoever is online":
    /// a player who closes the game right after Accept or right after Take still has his cards moved, and his final save is written after that. The queue keeps every other write of the two characters back until
    /// the API's answer is known, and never gives up a call whose outcome is unknown (CharacterSaveQueue.Wager.cs).
    /// An escrow that is still unanswered after EscrowPatienceMs is reported to the players as ApiBusy and abandoned: if it lands later, StakeMatch takes the decks out of the copies and settles it as a draw.
    /// On a dev server (no accounts) the move happens in memory only. API switch off (403 wager_disabled): the match is played with nothing at stake.
    /// </summary>
    public sealed partial class DuelService
    {
        const long EscrowPatienceMs = 25000;

        /// <summary>A match whose escrow has been asked of the API and not answered yet.</summary>
        sealed class PendingStake
        {
            public StakeMatch Stake;
            public PvpSeat[] Seat;
            public LobbyStart Start;
            public long Since;
        }

        readonly List<PendingStake> pendingStakes = new List<PendingStake>();
        readonly Dictionary<string, StakeMatch> stakes = new Dictionary<string, StakeMatch>();

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

        /// <summary>The zone's copy of a character's cards, reached through its NetPlayer. Only the hero-bound part (zone context, the push to the client) needs a hero.</summary>
        sealed class StakeHolder : IStakeHolder
        {
            readonly DuelService svc;
            readonly NetPlayer p;

            public StakeHolder(DuelService service, NetPlayer player) { svc = service; p = player; }

            public List<string> TakeDeck(int day)
            {
                CardCollection c = p.Rpg.State.Cards;
                List<string> lockedKinds = c.EscrowDeck();
                if (c.Daily.day != day) c.Daily = new CardDailyData { day = day };
                c.Daily.wagers++;
                Refresh();
                return lockedKinds;
            }

            public void GiveCards(string[] cards, IList<string> lockedKinds, string[] deck)
            {
                CardCollection c = p.Rpg.State.Cards;
                c.ReturnCards(cards, lockedKinds);
                c.Deck.Clear(); c.Deck.AddRange(deck); c.TrimDeck(); // a relogged player loaded without the held cards, hence without a deck
                Refresh();
                if (ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p); // a player who has left is written by his final save, which captures this state
            }

            void Refresh()
            {
                if (p.Hero == null) return;
                Zone.Enter(p.Hero.HomeZone);
                svc.PushState(p);
            }
        }

        static IStakeHolder HolderOfOwner(DuelService svc, NetPlayer owner) { return owner == null ? null : new StakeHolder(svc, owner); }

        // the NetPlayer that holds a side's state now: the run's owner once the match runs (a player who came back is the new one), the seat's before that
        IStakeHolder HolderOf(PendingStake pe, int side)
        {
            foreach (PvpRun run in runs) if (run.MatchId == pe.Stake.MatchId) return HolderOfOwner(this, run.Owner[side]);
            return HolderOfOwner(this, pe.Seat[side].Owner);
        }

        void BeginStakes(PvpSeat[] seat, LobbyStart s, long now)
        {
            NetPlayer a = seat[0].Player, b = seat[1].Player;
            int day = WagerRules.DayKeyNow();
            WagerError err = WagerRules.CanEnter(a.Rpg.State, day);
            if (err == WagerError.None) err = WagerRules.CanEnter(b.Rpg.State, day);
            if (err == WagerError.None && !pairs.Allowed(seat[0].Key, seat[1].Key, day)) err = WagerError.PairLimit;
            if (err != WagerError.None) { FailStart(seat, s, FailOf(err), now); return; }
            if (persistence != null && (!persistence.ApiHealthy || a.Saved == null || b.Saved == null)) { FailStart(seat, s, DuelFail.ApiBusy, now); return; }
            var pe = new PendingStake { Seat = seat, Start = s, Since = now };
            pe.Stake = new StakeMatch(persistence != null ? persistence.Queue : null, NewMatchId(), a.Saved, b.Saved, seat[0].Key, seat[1].Key, seat[0].Deck, seat[1].Deck, side => HolderOf(pe, side));
            if (persistence != null) pendingStakes.Add(pe);
            pe.Stake.Escrow(day, (m, r) => OnEscrow(m, r, pe, day));
        }

        void OnEscrow(StakeMatch m, ZoneApiReply r, PendingStake pe, int day)
        {
            pendingStakes.Remove(pe);
            long now = Now();
            PvpSeat[] seat = pe.Seat;
            for (int i = 0; i < 2; i++) seat[i].Player = OnlineByKey(seat[i].Key); // a player may have come back on a new connection, or gone
            if (!r.Ok)
            {
                if (m.Abandoned) { Debug.LogWarning("[ZenoniaProto] duel: abandoned escrow " + m.MatchId + " ended: " + r.Code + " (status " + r.Status + "), nothing was taken"); return; }
                if (r.Code == "wager_disabled") // the emergency switch is off at the API: PvP still plays, no card is taken
                {
                    Debug.LogWarning("[ZenoniaProto] duel: server-api has Card Wager switched off: this duel is played with nothing at stake");
                    lobby.Finish(pe.Start.A);
                    for (int i = 0; i < 2; i++) Refuse(seat[i].Player, DuelFail.NoStakes); // the invite promised "winner takes 1 card": both are told nothing is at stake
                    StartMatch(seat, false, null, now);
                    return;
                }
                Debug.LogWarning("[ZenoniaProto] duel: escrow " + m.MatchId + " not made: " + r.Code + " (status " + r.Status + ")");
                lobby.Finish(pe.Start.A);
                DuelFail why = FailOf(r);
                for (int i = 0; i < 2; i++) Refuse(seat[i].Player, why);
                Flush(now);
                return;
            }
            pairs.Note(seat[0].Key, seat[1].Key, day);
            for (int i = 0; i < 2; i++) seat[i].LockedKinds = m.LockedKinds[i];
            if (m.Abandoned) { Debug.LogWarning("[ZenoniaProto] duel: escrow " + m.MatchId + " landed after it was abandoned: the decks go back as a draw"); return; } // StakeMatch settles it
            lobby.Finish(pe.Start.A);
            if (seat[0].Player == null && seat[1].Player == null) // nobody is left to play it
            {
                m.Settle("draw", null, (mm, rr) => Debug.Log("[ZenoniaProto] duel: " + mm.MatchId + " returned as a draw: " + (rr.Ok ? "ok" : rr.Code + " (status " + rr.Status + ")")));
                Flush(now);
                return;
            }
            stakes[m.MatchId] = m;
            StartMatch(seat, true, m.MatchId, now);
        }

        /// <summary>Once per tick: an escrow that has no answer for EscrowPatienceMs is abandoned (the API is slow or down): both players hear ApiBusy and are free; the escrow itself is still replayed until it is known.</summary>
        void TickStakes(long now)
        {
            if (pendingStakes.Count == 0) return;
            foreach (PendingStake pe in pendingStakes.ToArray())
            {
                if (pe.Stake.Abandoned || now - pe.Since < EscrowPatienceMs) continue;
                pe.Stake.Abandon();
                Debug.LogWarning("[ZenoniaProto] duel: escrow " + pe.Stake.MatchId + " has no answer yet: the players are told, the escrow is replayed until the API answers");
                lobby.Finish(pe.Start.A);
                for (int i = 0; i < 2; i++) Refuse(OnlineByKey(pe.Seat[i].Key), DuelFail.ApiBusy);
                Flush(now);
            }
        }

        /// <summary>The result is known (and the winner's card chosen): give both decks back. Dev server: in memory; else server-api, retried by the queue under the same matchId until it answers.</summary>
        void SettleStakes(PvpRun run, string result, long now)
        {
            string pick = run.Pick != null ? run.Pick.Picked : null;
            run.StakeState = DuelView.WagerSettling;
            StakeMatch m;
            if (!stakes.TryGetValue(run.MatchId, out m))
            {
                Debug.LogError("[ZenoniaProto] duel: " + run.MatchId + " has no stake record: server-api returns the held decks when they expire");
                run.StakeState = DuelView.WagerFailed;
                SendBoth(run, now);
                Close(run);
                return;
            }
            m.Settle(result, pick, (mm, r) => OnSettled(run, r));
            if (!run.Closed) SendBoth(run, now);
        }

        void OnSettled(PvpRun run, ZoneApiReply r)
        {
            stakes.Remove(run.MatchId);
            if (r.Ok)
            {
                stats.Add("wagersPaid", 1);
                if (r.Credits != null)
                    foreach (WagerCredit c in r.Credits)
                        Debug.Log("[ZenoniaProto] duel: " + run.MatchId + " settled for " + c.CharacterId + ": " + c.Cards.Length + " cards" + (c.Apply ? "" : " (not applied here: the character is not on this zone)"));
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
    }
}
