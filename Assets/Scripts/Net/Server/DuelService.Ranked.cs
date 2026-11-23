using LiteNetLib;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Ranked on the zone (phase 07). A player joins the line (RankedQueue, in memory: one zone process hosts every map) after the server checks Lv 20+, a valid 8-card deck and a safe map; once a second the line is matched by the
    /// hidden MMR (window 150, +100 every 30 s waited; never the same party, never the opponent just met) and a match starts like a casual PvP duel (DuelPvpMatch, same clock, 60 s reconnect, same duel music) but with NO cards at
    /// stake: no escrow, no pick. When it ends the server plays the result into each side's rank from the rank they had at the start (RankedRules.Apply): MMR / LP / record / best. Anti-collusion: the same two characters count
    /// for LP at most twice a day (the third match is played but changes nothing and tells both players so), and very quick finishes are logged. A player who drops mid-match is saved with a provisional loss (the character is
    /// written right after, so it stands if they never come back); coming back and finishing replaces it, because every result is played from the saved start-of-match rank. A player who is still OFFLINE when the match ends
    /// (the opponent forfeited, say) has the real result parked in RankedPending and applied when they log in again. Same-account characters never meet, the daily pair limit is per account pair, and a forfeit before
    /// RankedRules.MinMovesForForfeit moves placed by hand counts for nobody. Nothing in a message says the rating.
    /// </summary>
    public sealed partial class DuelService
    {
        const long RankedEveryMs = 1000, QuickFinishMs = 60000;
        readonly RankedQueue queue = new RankedQueue { RematchCooldownMs = NetLaunchArgs.DevCards ? 0L : RankedQueue.DefaultRematchCooldownMs }; // a --dev-cards test server lets the same two players meet again at once
        readonly WagerRules.PairLedger rankedPairs = new WagerRules.PairLedger(RankedRules.MaxMatchesPerPairPerDay);
        readonly RankedPending rankedPending = new RankedPending();
        long nextRankedAt;

        /// <summary>The account behind a connection (server-api userId); a dev server has no accounts, so each name is its own account.</summary>
        static string AccountKey(NetPlayer p) { return string.IsNullOrEmpty(p.UserId) ? CharKey(p) : p.UserId; }

        static int PartyOf(ushort id) { return PartyService.Active != null ? PartyService.Active.Book.PartyIdOf(id) : 0; }

        void QueueRanked(Entry e, long now)
        {
            NetPlayer p = e.Player;
            if (queue.Contains(p.PlayerId)) { SendRanked(e, now); return; }
            if (Running(p.PlayerId)) { Refuse(p, DuelFail.InDuel); return; }
            if (lobby.IsBusy(p.PlayerId)) { Refuse(p, DuelFail.Busy); return; }
            RankedError re = RankedRules.CanQueue(p.Rpg.State);
            if (re != RankedError.None) { Refuse(p, re == RankedError.Level ? DuelFail.RankedLevel : DuelFail.Disabled); return; }
            DeckError err;
            DuelFail why = Eligible(p, out err);
            if (why != DuelFail.None) { Refuse(p, why, (byte)err); return; }
            CardRankData rank = p.Rpg.State.Cards.Rank;
            if (RankedRules.EnsureSeason(rank, RankedRules.NowUtcMs())) { Zone.Enter(p.Hero.HomeZone); PushState(p); if (ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p); }
            queue.Add(p.PlayerId, CharKey(p), rank.mmr, PartyOf(p.PlayerId), now, AccountKey(p));
            stats.Add("rankedQueued", 1);
            Debug.Log("[ZenoniaProto] ranked: " + p.Name + " joins the line (" + queue.Count + " waiting)");
            SendRanked(e, now);
        }

        void LeaveRankedQueue(Entry e, long now)
        {
            if (queue.Remove(e.Player.PlayerId)) Debug.Log("[ZenoniaProto] ranked: " + e.Player.Name + " leaves the line");
            SendRanked(e, now);
        }

        void SendRanked(Entry e, long now)
        {
            NetPlayer p = e.Player;
            if (p.Peer == null) return;
            long utc = RankedRules.NowUtcMs();
            var m = new DuelRankedMsg
            {
                Queued = queue.Contains(p.PlayerId), WaitSec = (ushort)System.Math.Min(65535L, queue.WaitedMs(p.PlayerId, now) / 1000L),
                Season = (ushort)RankedRules.Season(utc), MinutesLeft = (ushort)System.Math.Min(65535, RankedRules.MinutesLeft(utc)),
            };
            m.Write(w);
            p.Peer.Send(w, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
        }

        /// <summary>Once a second: the first Ranked message of every new player, drop anyone who can no longer wait, then make the matches.</summary>
        void TickRanked(long now)
        {
            foreach (Entry e in entries.Values)
                if (e.RankDue && e.Player.Peer != null && e.Player.Rpg != null) { e.RankDue = false; SendRanked(e, now); }
            if (now < nextRankedAt) return;
            nextRankedAt = now + RankedEveryMs;
            if (queue.Count == 0 || !RankedRules.Enabled) return; // switched off: nobody is matched (the line empties as players leave)
            foreach (RankedQueue.Waiter wt in queue.All())
            {
                Entry e;
                if (!entries.TryGetValue(wt.Id, out e) || e.Player.Hero == null) { queue.Remove(wt.Id); continue; }
                DuelFail why = StartRefusal(e.Player);
                if (why != DuelFail.None && why != DuelFail.Combat) { queue.Remove(wt.Id); Refuse(e.Player, why); SendRanked(e, now); continue; } // died, left the safe map, started a trade: out of the line
                CardRankData rk = e.Player.Rpg.State.Cards.Rank;
                if (RankedRules.EnsureSeason(rk, RankedRules.NowUtcMs())) { Zone.Enter(e.Player.Hero.HomeZone); PushState(e.Player); if (ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(e.Player); } // the season turned while waiting
                wt.Mmr = rk.mmr;
                wt.Ready = why == DuelFail.None; // a fight only keeps the player from being matched for now
                wt.Party = PartyOf(wt.Id);
            }
            foreach (RankedQueue.Pair pair in queue.Match(now)) BeginRanked(pair, now);
        }

        void BeginRanked(RankedQueue.Pair pair, long now)
        {
            Entry a, b;
            if (!entries.TryGetValue(pair.A.Id, out a) || !entries.TryGetValue(pair.B.Id, out b)) { Requeue(pair.A, pair.B); return; }
            DeckError errA, errB;
            DuelFail fa = EligibleToStart(a.Player, out errA), fb = EligibleToStart(b.Player, out errB);
            if (fa != DuelFail.None || fb != DuelFail.None) // somebody's deck or state changed while waiting: only that player leaves the line
            {
                queue.ForgetPair(pair.A, pair.B);
                if (fa != DuelFail.None) Refuse(a.Player, fa, (byte)errA);
                if (fb != DuelFail.None) Refuse(b.Player, fb, (byte)errB);
                if (fa == DuelFail.None) queue.Add(pair.A.Id, pair.A.Key, pair.A.Mmr, pair.A.Party, pair.A.Since, pair.A.Account);
                if (fb == DuelFail.None) queue.Add(pair.B.Id, pair.B.Key, pair.B.Mmr, pair.B.Party, pair.B.Since, pair.B.Account);
                SendRanked(a, now); SendRanked(b, now);
                return;
            }
            Debug.Log("[ZenoniaProto] ranked: matched " + a.Player.Name + " (" + pair.A.Mmr + ") with " + b.Player.Name + " (" + pair.B.Mmr + ") after " + (now - pair.A.Since) / 1000 + " s / " + (now - pair.B.Since) / 1000 + " s");
            var seat = new[] { new PvpSeat(a.Player), new PvpSeat(b.Player) };
            StartMatch(seat, false, null, now, true);
            SendRanked(a, now); SendRanked(b, now);
        }

        // the pair came apart before it started (one of them left): whoever is still here keeps their place in line
        void Requeue(RankedQueue.Waiter a, RankedQueue.Waiter b)
        {
            queue.ForgetPair(a, b);
            if (entries.ContainsKey(a.Id)) queue.Add(a.Id, a.Key, a.Mmr, a.Party, a.Since, a.Account);
            if (entries.ContainsKey(b.Id)) queue.Add(b.Id, b.Key, b.Mmr, b.Party, b.Since, b.Account);
        }

        /// <summary>The match is created: remember each side's rank as it starts (the base every result is played from) and the opponent's MMR.</summary>
        void BeginRankedRun(PvpRun run, PvpSeat[] seat, long now)
        {
            run.Ranked = true; run.StartedMs = now;
            run.DayKey = WagerRules.DayKeyNow();
            run.Counted = rankedPairs.Allowed(run.Account[0], run.Account[1], run.DayKey); // decided once, here: the drop-out loss and the final result use the same answer
            for (int s = 0; s < 2; s++)
            {
                CardRankData r = seat[s].Owner.Rpg.State.Cards.Rank;
                RankedRules.EnsureSeason(r, RankedRules.NowUtcMs());
                run.RankBefore[s] = RankedRules.Copy(r);
            }
            run.OppMmr[0] = run.RankBefore[1].mmr; run.OppMmr[1] = run.RankBefore[0].mmr;
        }

        static DuelResult ResultOf(string wagerResult, int side) { return wagerResult == "draw" ? DuelResult.Draw : (wagerResult == "a") == (side == 0) ? DuelResult.Win : DuelResult.Lose; }

        /// <summary>Moves the players placed by hand (the server's 20 s auto-placements do not count).</summary>
        static int MovesByHand(PvpRun run)
        {
            int n = 0;
            foreach (DuelMove m in run.Match.Match.Moves) if (!m.Auto) n++;
            return n;
        }

        /// <summary>The match is over: play the result into both ranks (a forfeit counts as a loss), save, tell the players. A side that is offline gets its result parked and applied when it logs in.</summary>
        void SettleRanked(PvpRun run, string result, long now)
        {
            bool early = RankedRules.EarlyForfeit(run.Match.Match.Forfeited, MovesByHand(run));
            bool counted = run.Counted && !early;
            if (counted) rankedPairs.Note(run.Account[0], run.Account[1], run.DayKey);
            run.RankNotCounted = !counted;
            if (early) Debug.LogWarning("[ZenoniaProto] ranked: " + run.Match.Names[0] + " vs " + run.Match.Names[1] + " ended by a forfeit before " + RankedRules.MinMovesForForfeit + " moves were placed: no LP / MMR change for either side");
            else if (!run.Counted) Debug.LogWarning("[ZenoniaProto] ranked: " + run.Match.Names[0] + " and " + run.Match.Names[1] + " (one account pair) met more than " + RankedRules.MaxMatchesPerPairPerDay + " times today: this match does not count");
            if (now - run.StartedMs < QuickFinishMs) Debug.LogWarning("[ZenoniaProto] ranked: " + run.Match.Names[0] + " vs " + run.Match.Names[1] + " ended after " + (now - run.StartedMs) / 1000 + " s (" + result + "): quick finish, watch this pair");
            for (int s = 0; s < 2; s++)
            {
                DuelResult r = ResultOf(result, s);
                int lp;
                CardRankData after = RankedRules.Apply(run.RankBefore[s], r, run.OppMmr[s], counted, out lp);
                run.LpChange[s] = lp;
                NetPlayer p = OnlineByKey(run.Key[s]);
                bool here = p != null && p.Hero != null;
                Debug.Log("[ZenoniaProto] ranked: " + run.Match.Names[s] + " " + r + " " + (lp >= 0 ? "+" : "") + lp + " LP -> " + after.lp + " LP, " + RankedRules.TierName(RankedRules.Tier(after.lp)) + (counted ? "" : " (not counted)") + (here ? "" : " (offline: parked until they log in)"));
                if (!here) { rankedPending.Put(run.Key[s], after, RankedRules.NowUtcMs()); continue; }
                p.Rpg.State.Cards.Rank = after;
                Zone.Enter(p.Hero.HomeZone);
                PushState(p);
                if (ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p);
                Entry en;
                if (entries.TryGetValue(p.PlayerId, out en)) SendRanked(en, now);
            }
            stats.Add("rankedFinished", 1);
        }

        /// <summary>A player just logged in: a Ranked result that was parked while they were offline replaces the provisional loss they were saved with (and is written at once).</summary>
        void ApplyPendingRank(NetPlayer p)
        {
            CardRankData r;
            if (p.Rpg == null || !rankedPending.TryTake(CharKey(p), RankedRules.NowUtcMs(), out r)) return;
            p.Rpg.State.Cards.Rank = r;
            Debug.Log("[ZenoniaProto] ranked: " + p.Name + " is back: the result of the match that ended while they were away is applied (" + r.lp + " LP)");
            if (ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p);
        }

        /// <summary>A ranked player's connection dropped mid-match: write the loss into their state now (they are saved right after), unless the match does not count (so far).</summary>
        void ProvisionalRankLoss(PvpRun run, int side, NetPlayer p)
        {
            if (p == null || p.Hero == null || run.RankBefore[side] == null) return;
            bool counted = run.Counted && MovesByHand(run) >= RankedRules.MinMovesForForfeit; // a forfeit this early would not count
            int lp;
            p.Rpg.State.Cards.Rank = RankedRules.Apply(run.RankBefore[side], DuelResult.Lose, run.OppMmr[side], counted, out lp);
            Debug.Log("[ZenoniaProto] ranked: " + p.Name + " dropped out mid-match: provisional loss " + lp + " LP");
        }
    }
}
