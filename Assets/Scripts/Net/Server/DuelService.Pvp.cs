using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Player vs player duels on the zone (phase 06 v2): the challenge lobby (DuelLobby: invite, accept / decline), starting the match (DuelPvpMatch: both decks checked again, both heroes still on a safe map, both
    /// decks in escrow at server-api first), playing it, the 60 s reconnect window (the match lives by character, so a player who comes back is put into it), and the end: the WINNER picks any 1 card of the loser's deck
    /// within 30 s (a random one after that), then the stakes are settled once (DuelService.Wager.cs). A draw takes nothing. The run stays open until the settle is answered, so nobody edits a deck or starts another duel meanwhile.
    /// </summary>
    public sealed partial class DuelService
    {
        sealed class PvpRun
        {
            public DuelPvpMatch Match;
            public string MatchId;
            public readonly string[] Key = new string[2];
            public readonly NetPlayer[] Player = new NetPlayer[2];  // the live connection of each side (null while it is away)
            public readonly NetPlayer[] Owner = new NetPlayer[2];   // the NetPlayer that holds the character's state here: the escrow / payout is applied to it even after the connection dropped
            public readonly string[][] Deck = new string[2][];      // the 8 staked cards of each side
            public readonly List<string>[] LockedKinds = { new List<string>(), new List<string>() };
            public bool Stakes, Ended, Closed;
            // Ranked (DuelService.Ranked.cs): no stakes; the rank each side had when the match began and the LP each gained / lost, so the final result is always played from the same start
            public bool Ranked, RankNotCounted, Counted;
            public int DayKey;
            public readonly string[] Account = new string[2];   // the account of each side (Ranked pair limit)
            public long StartedMs;
            public readonly CardRankData[] RankBefore = new CardRankData[2];
            public readonly int[] OppMmr = new int[2], LpChange = new int[2];
            public int StakeState;
            public DuelPickRound Pick;
            public DuelRng PickRng;

            public DuelView ViewFor(int side, long now)
            {
                DuelView v = Match.View(side, now);
                v.WagerState = Stakes ? StakeState : DuelView.WagerNone;
                v.Ranked = Ranked; v.LpChange = LpChange[side]; v.RankNotCounted = RankNotCounted;
                if (Pick == null) return v;
                v.PickMsLeft = Pick.MsLeft(now);
                if (Pick.Done) { v.PickedCard = CardTable.Index(Pick.Picked); v.PickedAtRandom = Pick.AtRandom; }
                else if (StakeState == DuelView.WagerPicking && side == Pick.Winner) foreach (string id in Pick.LoserDeck) v.PickCards.Add(CardTable.Index(id));
                return v;
            }
        }

        readonly DuelLobby lobby = new DuelLobby();
        readonly List<PvpRun> runs = new List<PvpRun>();
        readonly Dictionary<string, PvpRun> runByChar = new Dictionary<string, PvpRun>();
        readonly WagerRules.PairLedger pairs = new WagerRules.PairLedger();

        /// <summary>The character behind a connection: the persisted id, or the name on a dev server (no accounts there).</summary>
        static string CharKey(NetPlayer p) { return p.Saved != null ? p.Saved.Id : "n:" + p.Name; }

        public void OnJoin(NetPlayer p)
        {
            var e = new Entry { Player = p, RankDue = true };
            entries[p.PlayerId] = e;
            ApplyPendingRank(p);
            PvpRun run;
            if (!runByChar.TryGetValue(CharKey(p), out run) || run.Closed) return;
            int side = run.Key[0] == CharKey(p) ? 0 : 1;
            run.Player[side] = p; run.Owner[side] = p; e.Pvp = run; e.PvpSide = side;
            if (!run.Match.Finished) run.Match.SetBack(side);
            Debug.Log("[ZenoniaProto] duel: " + p.Name + " is back in the duel");
            SendBoth(run, Now());
        }

        static long Now() { return (long)(ZoneHost.Active != null ? ZoneHost.Active.ClockMs : Time.realtimeSinceStartupAsDouble * 1000.0); }

        void LeavePvp(Entry e, long now)
        {
            ushort id = e.Player.PlayerId;
            if (lobby.PhaseOf(id) != LobbyPhase.None && lobby.PhaseOf(id) != LobbyPhase.Starting) { lobby.Leave(id); Flush(now); }
            queue.Remove(id);
            if (e.Pvp == null || e.Pvp.Closed) return;
            e.Pvp.Player[e.PvpSide] = null;
            if (e.Pvp.Match.Finished) return; // the match is decided: the pick timer and the settle go on without them
            if (e.Pvp.Ranked) ProvisionalRankLoss(e.Pvp, e.PvpSide, e.Player); // the character is saved right after this call: a ranked player who never comes back has lost
            e.Pvp.Match.SetAway(e.PvpSide, now);
            Debug.Log("[ZenoniaProto] duel: " + e.Player.Name + " dropped out of a duel: " + (DuelPvpMatch.ReconnectMs / 1000) + " s to come back");
            SendBoth(e.Pvp, now);
        }

        // ---- the lobby ----

        static DuelFail FailOf(LobbyFail f)
        {
            switch (f)
            {
                case LobbyFail.Self: return DuelFail.SelfInvite;
                case LobbyFail.Busy: return DuelFail.TargetBusy;
                case LobbyFail.Cooldown: return DuelFail.InviteCooldown;
                case LobbyFail.NoInvite: return DuelFail.NoInvite;
                case LobbyFail.NoLobby: return DuelFail.NoLobby;
                case LobbyFail.Declined: return DuelFail.Declined;
                case LobbyFail.Cancelled: return DuelFail.Cancelled;
                case LobbyFail.Expired: return DuelFail.Expired;
                default: return DuelFail.OpponentLeft;
            }
        }

        static DuelFail FailOf(WagerError e)
        {
            switch (e)
            {
                case WagerError.Level: return DuelFail.WagerLevel;
                case WagerError.DailyLimit: case WagerError.PairLimit: return DuelFail.WagerDaily;
                default: return DuelFail.WagerRefused;
            }
        }

        /// <summary>None when this player may open or join a duel now (map, alive, not trading, no fight, not in a duel, valid deck); else why not.</summary>
        DuelFail Eligible(NetPlayer p, out DeckError deck)
        {
            deck = DeckError.None;
            if (Running(p.PlayerId)) return DuelFail.InDuel;
            if (queue.Contains(p.PlayerId)) return DuelFail.Busy; // waiting in the Ranked line
            return EligibleToStart(p, out deck);
        }

        // at the moment of starting nobody is "in a duel" yet, but the lobby itself counts as busy: only the map / fight / deck checks apply
        DuelFail EligibleToStart(NetPlayer p, out DeckError deck)
        {
            deck = DeckError.None;
            DuelFail why = StartRefusal(p);
            if (why != DuelFail.None) return why;
            deck = DeckRules.Validate(p.Rpg.State.Cards.Deck, p.Rpg.State.Cards, true);
            return deck != DeckError.None ? DuelFail.DeckInvalid : DuelFail.None;
        }

        void Challenge(Entry e, ushort otherId, long now)
        {
            NetPlayer p = e.Player;
            Entry t;
            if (otherId == p.PlayerId) { Refuse(p, DuelFail.SelfInvite); return; }
            if (!entries.TryGetValue(otherId, out t) || t.Player.Hero == null || t.Player.Hero.HomeZone != p.Hero.HomeZone) { Refuse(p, DuelFail.NoTarget); return; }
            DeckError err;
            DuelFail mine = Eligible(p, out err);
            if (mine != DuelFail.None) { Refuse(p, mine, (byte)err); return; }
            DuelFail theirs = Eligible(t.Player, out err);
            if (theirs != DuelFail.None) { Refuse(p, theirs == DuelFail.InDuel ? DuelFail.TargetBusy : DuelFail.TargetNotReady); return; }
            bool stakes = WagerRules.Enabled;
            if (stakes)
            {
                int day = WagerRules.DayKeyNow();
                WagerError we = WagerRules.CanEnter(p.Rpg.State, day);
                if (we != WagerError.None) { Refuse(p, FailOf(we)); return; }
                if (WagerRules.CanEnter(t.Player.Rpg.State, day) != WagerError.None) { Refuse(p, DuelFail.TargetNotReady); return; }
                if (!pairs.Allowed(CharKey(p), CharKey(t.Player), day)) { Refuse(p, DuelFail.WagerDaily); return; }
            }
            LobbyFail f = lobby.Invite(p.PlayerId, otherId, stakes, now);
            if (f != LobbyFail.None) Refuse(p, FailOf(f));
            Flush(now);
        }

        void LobbyRequest(Entry e, DuelRequestMsg m, long now)
        {
            NetPlayer p = e.Player;
            ushort id = p.PlayerId;
            LobbyFail f = LobbyFail.None;
            switch (m.Op)
            {
                case DuelOp.CancelInvite: f = lobby.Cancel(id); break;
                case DuelOp.Decline: f = lobby.Decline(id); break;
                case DuelOp.Accept:
                    DeckError err;
                    DuelFail why = Eligible(p, out err);
                    if (why != DuelFail.None) { lobby.Decline(id); Refuse(p, why, (byte)err); break; }
                    f = lobby.Accept(id);
                    break;
                case DuelOp.LeaveLobby: f = lobby.Leave(id, LobbyFail.Left); break;
            }
            if (f != LobbyFail.None) Refuse(p, FailOf(f));
            Flush(now);
        }

        /// <summary>Everything the lobby produced since the last call: matches to start, end notices, and the new view of every player it touched.</summary>
        void Flush(long now)
        {
            if (lobby.Starts.Count > 0)
            {
                LobbyStart[] starts = lobby.Starts.ToArray();
                lobby.Starts.Clear();
                foreach (LobbyStart s in starts) BeginPvp(s, now);
            }
            foreach (LobbyEnd end in lobby.Ends)
            {
                Entry to;
                if (entries.TryGetValue(end.To, out to)) Refuse(to.Player, FailOf(end.Why));
            }
            lobby.Ends.Clear();
            var sent = new HashSet<ushort>();
            foreach (ushort id in lobby.Changed)
            {
                Entry to;
                if (!sent.Add(id) || !entries.TryGetValue(id, out to)) continue;
                SendLobby(to.Player, now);
            }
            lobby.Changed.Clear();
        }

        void SendLobby(NetPlayer p, long now)
        {
            if (p.Peer == null) return;
            LobbyView v = lobby.ViewOf(p.PlayerId, now);
            Entry other;
            var msg = new DuelLobbyMsg
            {
                Phase = (byte)v.Phase, Other = v.Other, Stakes = v.Stakes, MsLeft = (ushort)System.Math.Min(60000, v.MsLeft),
                OtherName = v.Phase != LobbyPhase.None && entries.TryGetValue(v.Other, out other) ? other.Player.Name : "",
            };
            msg.Write(w);
            p.Peer.Send(w, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
        }

        // ---- starting and playing ----

        void BeginPvp(LobbyStart s, long now)
        {
            Entry a, b;
            if (!entries.TryGetValue(s.A, out a) || !entries.TryGetValue(s.B, out b)) { lobby.Finish(s.A); Flush(now); return; }
            DeckError err;
            DuelFail fa = EligibleToStart(a.Player, out err), fb = EligibleToStart(b.Player, out err);
            if (fa != DuelFail.None || fb != DuelFail.None)
            {
                lobby.Finish(s.A);
                Refuse(a.Player, fa != DuelFail.None ? fa : DuelFail.TargetNotReady, (byte)err);
                Refuse(b.Player, fb != DuelFail.None ? fb : DuelFail.TargetNotReady, (byte)err);
                Flush(now);
                return;
            }
            var side = new[] { new PvpSeat(a.Player), new PvpSeat(b.Player) };
            if (s.Stakes && WagerRules.Enabled) BeginStakes(side, s, now); else StartMatch(side, false, null, now);
        }

        /// <summary>Who plays one side, captured when the match is agreed: the connection (null once it drops), the character's NetPlayer, the name and a copy of the deck (a player who leaves while the decks are being held still has a side).</summary>
        struct PvpSeat
        {
            public NetPlayer Player, Owner;
            public string Key, Name, Account;
            public string[] Deck;
            public List<string> LockedKinds;

            public PvpSeat(NetPlayer p) { Player = p; Owner = p; Key = CharKey(p); Account = AccountKey(p); Name = p.Name; Deck = p.Rpg.State.Cards.Deck.ToArray(); LockedKinds = new List<string>(); }
        }

        /// <summary>Creates the match. `stakes` = both decks are in escrow (EscrowDeck already applied to each Owner). A side that is not online any more starts as dropped (60 s to come back).</summary>
        void StartMatch(PvpSeat[] side, bool stakes, string matchId, long now, bool ranked = false)
        {
            var run = new PvpRun { MatchId = matchId ?? NewMatchId(), Stakes = stakes, StakeState = stakes ? DuelView.WagerHeld : DuelView.WagerNone, PickRng = new DuelRng(NewSeed()) };
            run.Match = DuelPvpMatch.Start(NetLaunchArgs.DevDuelSeed != 0UL ? NetLaunchArgs.DevDuelSeed : NewSeed(), side[0].Deck, side[1].Deck, side[0].Name, side[1].Name, now);
            for (int s = 0; s < 2; s++)
            {
                run.Key[s] = side[s].Key; run.Account[s] = side[s].Account; run.Deck[s] = side[s].Deck; run.LockedKinds[s] = side[s].LockedKinds; run.Owner[s] = side[s].Owner;
                Entry e;
                NetPlayer p = side[s].Player;
                if (p != null && entries.TryGetValue(p.PlayerId, out e)) { run.Player[s] = p; e.Pvp = run; e.PvpSide = s; }
                else run.Match.SetAway(s, now);
                runByChar[run.Key[s]] = run;
            }
            if (ranked) BeginRankedRun(run, side, now);
            runs.Add(run);
            stats.Add("pvpStarted", 1);
            Debug.Log("[ZenoniaProto] duel: PvP " + side[0].Name + " vs " + side[1].Name + (stakes ? " (winner takes 1 card, " + run.MatchId + ")" : ranked ? " (RANKED, no cards at stake)" : " (no cards at stake)"));
            for (int s = 0; s < 2; s++) if (side[s].Player != null) lobby.Finish(side[s].Player.PlayerId);
            SendBoth(run, now);
            Flush(now);
        }

        static string NewMatchId() { return "duel-" + System.Guid.NewGuid().ToString("N"); }

        void SendBoth(PvpRun run, long now)
        {
            for (int s = 0; s < 2; s++) if (run.Player[s] != null) SendView(run.Player[s], run.ViewFor(s, now));
        }

        void PlacePvp(Entry e, DuelRequestMsg m, long now)
        {
            PvpRun run = e.Pvp;
            DuelPlay r = run.Match.Place(e.PvpSide, m.A, m.B, now, m.Turn, m.Card);
            if (!r.Ok) { Refuse(e.Player, PlayFailure(r)); SendView(e.Player, run.ViewFor(e.PvpSide, now)); return; }
            if (run.Match.Finished) EndMatch(run, now); else SendBoth(run, now);
        }

        void TickPvp(long now)
        {
            lobby.Tick(now);
            Flush(now);
            foreach (PvpRun run in runs.ToArray())
            {
                if (run.Closed) continue;
                if (!run.Match.Finished)
                {
                    for (int s = 0; s < 2; s++)
                        if (run.Player[s] != null && !CanKeepDueling(run.Player[s].Hero)) run.Match.Forfeit(s);
                    bool changed = run.Match.Finished || run.Match.Tick(now);
                    if (run.Match.Finished) EndMatch(run, now); else if (changed) SendBoth(run, now);
                }
                else if (run.Pick != null && run.StakeState == DuelView.WagerPicking && run.Pick.Tick(now, run.PickRng))
                {
                    Debug.Log("[ZenoniaProto] duel: " + run.MatchId + " the winner did not pick in time: " + run.Pick.Picked + " is taken at random");
                    SettleStakes(run, run.Match.WagerResult, now);
                }
            }
        }

        /// <summary>The winner chose the card to take (the "Take" button of the pick screen).</summary>
        void TakeCard(Entry e, byte cardIndex, long now)
        {
            PvpRun run = e.Pvp;
            if (run == null || run.Pick == null || run.StakeState != DuelView.WagerPicking || e.PvpSide != run.Pick.Winner) { Refuse(e.Player, DuelFail.NoPick); return; }
            CardDef c = CardTable.ByIndex(cardIndex);
            if (c == null || !run.Pick.Take(c.Id)) { Refuse(e.Player, DuelFail.NoPick); return; }
            Debug.Log("[ZenoniaProto] duel: " + e.Player.Name + " takes " + c.Id + " (" + run.MatchId + ")");
            SettleStakes(run, run.Match.WagerResult, now);
        }

        /// <summary>The match is over. No stakes: the final views go out and the run closes. Stakes: a draw settles at once, a win opens the winner's pick (30 s).</summary>
        void EndMatch(PvpRun run, long now)
        {
            if (run.Ended) return;
            run.Ended = true;
            stats.Add("pvpFinished", 1);
            string result = run.Match.WagerResult;
            if (run.Ranked) SettleRanked(run, result, now);
            if (!run.Stakes) { SendBoth(run, now); Close(run); return; }
            if (result == "draw") { SettleStakes(run, result, now); return; }
            int winner = result == "a" ? 0 : 1;
            run.Pick = new DuelPickRound(winner, run.Deck[1 - winner], now);
            run.StakeState = DuelView.WagerPicking;
            Debug.Log("[ZenoniaProto] duel: " + run.MatchId + " won by " + run.Match.Names[winner] + ", picking from " + string.Join(",", run.Pick.LoserDeck));
            SendBoth(run, now);
        }

        /// <summary>The run is fully done (nothing left to pick or settle): the players are free again.</summary>
        void Close(PvpRun run)
        {
            if (run.Closed) return;
            run.Closed = true;
            runs.Remove(run);
            for (int s = 0; s < 2; s++)
            {
                PvpRun mine;
                if (runByChar.TryGetValue(run.Key[s], out mine) && mine == run) runByChar.Remove(run.Key[s]);
                Entry e;
                if (run.Player[s] != null && entries.TryGetValue(run.Player[s].PlayerId, out e) && e.Pvp == run) e.Pvp = null;
            }
        }
    }
}
