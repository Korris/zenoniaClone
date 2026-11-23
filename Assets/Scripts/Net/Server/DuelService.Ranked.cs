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
    /// written right after, so it stands if they never come back); coming back and finishing replaces it, because every result is played from the saved start-of-match rank. Nothing in a message says the rating.
    /// </summary>
    public sealed partial class DuelService
    {
        const long RankedEveryMs = 1000, QuickFinishMs = 60000;
        readonly RankedQueue queue = new RankedQueue { RematchCooldownMs = NetLaunchArgs.DevCards ? 0L : RankedQueue.DefaultRematchCooldownMs }; // a --dev-cards test server lets the same two players meet again at once
        readonly WagerRules.PairLedger rankedPairs = new WagerRules.PairLedger();
        long nextRankedAt;

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
            queue.Add(p.PlayerId, CharKey(p), rank.mmr, PartyOf(p.PlayerId), now);
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
            if (queue.Count == 0) return;
            foreach (RankedQueue.Waiter wt in queue.All())
            {
                Entry e;
                if (!entries.TryGetValue(wt.Id, out e) || e.Player.Hero == null) { queue.Remove(wt.Id); continue; }
                DuelFail why = StartRefusal(e.Player);
                if (why != DuelFail.None && why != DuelFail.Combat) { queue.Remove(wt.Id); Refuse(e.Player, why); SendRanked(e, now); continue; } // died, left the safe map, started a trade: out of the line
                wt.Ready = why == DuelFail.None; // a fight only keeps the player from being matched for now
                wt.Party = PartyOf(wt.Id);
            }
            foreach (RankedQueue.Pair pair in queue.Match(now)) BeginRanked(pair, now);
        }

        void BeginRanked(RankedQueue.Pair pair, long now)
        {
            Entry a, b;
            if (!entries.TryGetValue(pair.A.Id, out a) || !entries.TryGetValue(pair.B.Id, out b)) { Requeue(pair.A, pair.B); return; }
            DeckError err;
            DuelFail fa = EligibleToStart(a.Player, out err), fb = EligibleToStart(b.Player, out err);
            if (fa != DuelFail.None || fb != DuelFail.None) // somebody's deck or state changed while waiting: only that player leaves the line
            {
                queue.ForgetPair(pair.A.Key, pair.B.Key);
                if (fa != DuelFail.None) Refuse(a.Player, fa, (byte)err);
                if (fb != DuelFail.None) Refuse(b.Player, fb, (byte)err);
                if (fa == DuelFail.None) queue.Add(pair.A.Id, pair.A.Key, pair.A.Mmr, pair.A.Party, pair.A.Since);
                if (fb == DuelFail.None) queue.Add(pair.B.Id, pair.B.Key, pair.B.Mmr, pair.B.Party, pair.B.Since);
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
            queue.ForgetPair(a.Key, b.Key);
            if (entries.ContainsKey(a.Id)) queue.Add(a.Id, a.Key, a.Mmr, a.Party, a.Since);
            if (entries.ContainsKey(b.Id)) queue.Add(b.Id, b.Key, b.Mmr, b.Party, b.Since);
        }

        /// <summary>The match is created: remember each side's rank as it starts (the base every result is played from) and the opponent's MMR.</summary>
        void BeginRankedRun(PvpRun run, PvpSeat[] seat, long now)
        {
            run.Ranked = true; run.StartedMs = now;
            for (int s = 0; s < 2; s++)
            {
                CardRankData r = seat[s].Owner.Rpg.State.Cards.Rank;
                RankedRules.EnsureSeason(r, RankedRules.NowUtcMs());
                run.RankBefore[s] = RankedRules.Copy(r);
            }
            run.OppMmr[0] = run.RankBefore[1].mmr; run.OppMmr[1] = run.RankBefore[0].mmr;
        }

        static DuelResult ResultOf(string wagerResult, int side) { return wagerResult == "draw" ? DuelResult.Draw : (wagerResult == "a") == (side == 0) ? DuelResult.Win : DuelResult.Lose; }

        /// <summary>The match is over: play the result into both ranks (a forfeit counts as a loss), save, tell the players.</summary>
        void SettleRanked(PvpRun run, string result, long now)
        {
            int day = WagerRules.DayKeyNow();
            bool counted = rankedPairs.Allowed(run.Key[0], run.Key[1], day);
            rankedPairs.Note(run.Key[0], run.Key[1], day);
            run.RankNotCounted = !counted;
            if (!counted) Debug.LogWarning("[ZenoniaProto] ranked: " + run.Match.Names[0] + " and " + run.Match.Names[1] + " met more than " + RankedRules.MaxMatchesPerPairPerDay + " times today: this match does not count");
            if (now - run.StartedMs < QuickFinishMs) Debug.LogWarning("[ZenoniaProto] ranked: " + run.Match.Names[0] + " vs " + run.Match.Names[1] + " ended after " + (now - run.StartedMs) / 1000 + " s (" + result + "): quick finish, watch this pair");
            for (int s = 0; s < 2; s++)
            {
                DuelResult r = ResultOf(result, s);
                int lp;
                CardRankData after = RankedRules.Apply(run.RankBefore[s], r, run.OppMmr[s], counted, out lp);
                run.LpChange[s] = lp;
                NetPlayer p = OnlineByKey(run.Key[s]);
                Debug.Log("[ZenoniaProto] ranked: " + run.Match.Names[s] + " " + r + " " + (lp >= 0 ? "+" : "") + lp + " LP -> " + after.lp + " LP, " + RankedRules.TierName(RankedRules.Tier(after.lp)) + (counted ? "" : " (not counted)") + (p == null ? " (offline: the provisional loss stands)" : ""));
                if (p == null || p.Hero == null) continue;
                p.Rpg.State.Cards.Rank = after;
                Zone.Enter(p.Hero.HomeZone);
                PushState(p);
                if (ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p);
                Entry en;
                if (entries.TryGetValue(p.PlayerId, out en)) SendRanked(en, now);
            }
            stats.Add("rankedFinished", 1);
        }

        /// <summary>A ranked player's connection dropped mid-match: write the loss into their state now (they are saved right after), unless the pair no longer counts.</summary>
        void ProvisionalRankLoss(PvpRun run, int side, NetPlayer p)
        {
            if (p == null || p.Hero == null || run.RankBefore[side] == null) return;
            bool counted = rankedPairs.Allowed(run.Key[0], run.Key[1], WagerRules.DayKeyNow());
            int lp;
            p.Rpg.State.Cards.Rank = RankedRules.Apply(run.RankBefore[side], DuelResult.Lose, run.OppMmr[side], counted, out lp);
            Debug.Log("[ZenoniaProto] ranked: " + p.Name + " dropped out mid-match: provisional loss " + lp + " LP");
        }
    }
}
