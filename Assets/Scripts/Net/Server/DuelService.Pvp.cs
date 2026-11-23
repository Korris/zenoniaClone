using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Player vs player duels on the zone (phase 06): the challenge lobby (DuelLobby: invite, accept / decline, the Card Wager panel), starting the match (DuelPvpMatch: both decks checked again, both heroes still on a safe
    /// map), playing it, the 60 s reconnect window (the match lives by character, so a player who comes back is put into it), and paying the result (PvP gold, then the wager). The inviter decides wager or not.
    /// </summary>
    public sealed partial class DuelService
    {
        sealed class PvpRun
        {
            public DuelPvpMatch Match;
            public string MatchId;
            public readonly string[] Key = new string[2];
            public readonly NetPlayer[] Player = new NetPlayer[2];
            public readonly int[] CardIdx = { DuelView.NoCard, DuelView.NoCard };
            public readonly DuelReward[] Reward = new DuelReward[2];
            public bool Wager, Done;
            public int WagerState;

            public DuelView ViewFor(int side, long now)
            {
                return Match.View(side, now, Wager ? CardIdx[side] : DuelView.NoCard, Wager ? CardIdx[1 - side] : DuelView.NoCard, Wager ? WagerState : DuelView.WagerNone, Reward[side]);
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
            var e = new Entry { Player = p };
            entries[p.PlayerId] = e;
            PvpRun run;
            if (!runByChar.TryGetValue(CharKey(p), out run) || run.Match.Finished) return;
            int side = run.Key[0] == CharKey(p) ? 0 : 1;
            run.Player[side] = p; e.Pvp = run; e.PvpSide = side;
            run.Match.SetBack(side);
            Debug.Log("[ZenoniaProto] duel: " + p.Name + " is back in the duel");
            SendBoth(run, Now());
        }

        static long Now() { return (long)(ZoneHost.Active != null ? ZoneHost.Active.ClockMs : Time.realtimeSinceStartup * 1000f); }

        void LeavePvp(Entry e, long now)
        {
            ushort id = e.Player.PlayerId;
            if (lobby.PhaseOf(id) != LobbyPhase.None && lobby.PhaseOf(id) != LobbyPhase.Starting) { lobby.Leave(id); Flush(now); }
            if (e.Pvp == null || e.Pvp.Match.Finished) return;
            e.Pvp.Player[e.PvpSide] = null;
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
                case LobbyFail.NoPick: return DuelFail.NoPick;
                case LobbyFail.NotLocked: return DuelFail.NotLocked;
                case LobbyFail.Mismatch: return DuelFail.CardMismatch;
                case LobbyFail.Declined: return DuelFail.Declined;
                case LobbyFail.Cancelled: return DuelFail.Cancelled;
                case LobbyFail.Expired: return DuelFail.Expired;
                default: return DuelFail.OpponentLeft;
            }
        }

        /// <summary>None when this player may open or join a duel now (map, alive, not trading, no fight, not in a duel, valid deck); else why not.</summary>
        DuelFail Eligible(NetPlayer p, out DeckError deck)
        {
            deck = DeckError.None;
            if (Running(p.PlayerId)) return DuelFail.InDuel;
            DuelFail why = StartRefusal(p);
            if (why != DuelFail.None) return why;
            deck = DeckRules.Validate(p.Rpg.State.Cards.Deck, p.Rpg.State.Cards, true);
            return deck != DeckError.None ? DuelFail.DeckInvalid : DuelFail.None;
        }

        static DuelFail FailOf(WagerError e)
        {
            switch (e)
            {
                case WagerError.Disabled: return DuelFail.WagerOff;
                case WagerError.Level: return DuelFail.WagerLevel;
                case WagerError.DailyLimit: case WagerError.PairLimit: return DuelFail.WagerDaily;
                case WagerError.Mismatch: return DuelFail.CardMismatch;
                default: return DuelFail.WagerCard;
            }
        }

        void Challenge(Entry e, ushort otherId, bool wager, long now)
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
            if (wager)
            {
                int day = DuelNpcRun.DayKeyNow();
                WagerError we = WagerRules.CanEnter(p.Rpg.State, day);
                if (we != WagerError.None) { Refuse(p, FailOf(we)); return; }
                if (WagerRules.CanEnter(t.Player.Rpg.State, day) != WagerError.None) { Refuse(p, DuelFail.TargetNotReady); return; }
                if (!pairs.Allowed(CharKey(p), CharKey(t.Player), day)) { Refuse(p, DuelFail.WagerDaily); return; }
            }
            LobbyFail f = lobby.Invite(p.PlayerId, otherId, wager, now);
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
                    f = lobby.Accept(id, now);
                    break;
                case DuelOp.WagerPick: f = PickCard(p, m.A); break;
                case DuelOp.WagerLock: f = lobby.Lock(id); break;
                case DuelOp.WagerConfirm: f = lobby.Confirm(id); break;
                case DuelOp.LeaveLobby: f = lobby.Leave(id, LobbyFail.Left); break;
            }
            if (f != LobbyFail.None) Refuse(p, FailOf(f));
            Flush(now);
        }

        LobbyFail PickCard(NetPlayer p, byte cardIndex)
        {
            CardDef c = CardTable.ByIndex(cardIndex);
            WagerError we = c == null ? WagerError.UnknownCard : WagerRules.CheckCard(p.Rpg.State, c.Id);
            if (we != WagerError.None) { Refuse(p, FailOf(we)); return LobbyFail.None; }
            return lobby.Pick(p.PlayerId, cardIndex);
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
                Phase = (byte)v.Phase, Other = v.Other, Wager = v.Wager, MyCard = v.MyCard, TheirCard = v.TheirCard, MyLocked = v.MyLocked, TheirLocked = v.TheirLocked,
                MyConfirmed = v.MyConfirmed, TheirConfirmed = v.TheirConfirmed, MsLeft = (ushort)System.Math.Min(60000, v.MsLeft),
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
            if (s.Wager) BeginWager(side, s, now); else StartMatch(side, null, null, now);
        }

        /// <summary>Who plays one side, captured when the match is agreed: the connection (null once it drops), the character, the name and a copy of the deck (a player who leaves while the cards are being held still has a side).</summary>
        struct PvpSeat
        {
            public NetPlayer Player;
            public string Key, Name;
            public string[] Deck;

            public PvpSeat(NetPlayer p) { Player = p; Key = CharKey(p); Name = p.Name; Deck = p.Rpg.State.Cards.Deck.ToArray(); }
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

        /// <summary>Creates the match. `wagerCards` = the card ids that were put in escrow (null = no wager). A side that is not online any more starts as dropped (60 s to come back).</summary>
        void StartMatch(PvpSeat[] side, string[] wagerCards, string matchId, long now)
        {
            var run = new PvpRun { MatchId = matchId ?? NewMatchId(), Wager = wagerCards != null, WagerState = wagerCards != null ? DuelView.WagerHeld : 0 };
            run.Match = DuelPvpMatch.Start(NewSeed(), side[0].Deck, side[1].Deck, side[0].Name, side[1].Name, now);
            if (wagerCards != null) { run.CardIdx[0] = CardTable.Index(wagerCards[0]); run.CardIdx[1] = CardTable.Index(wagerCards[1]); }
            for (int s = 0; s < 2; s++)
            {
                run.Key[s] = side[s].Key;
                Entry e;
                NetPlayer p = side[s].Player;
                if (p != null && entries.TryGetValue(p.PlayerId, out e)) { run.Player[s] = p; e.Pvp = run; e.PvpSide = s; e.Run = null; }
                else run.Match.SetAway(s, now);
                runByChar[run.Key[s]] = run;
            }
            runs.Add(run);
            stats.Add("pvpStarted", 1);
            Debug.Log("[ZenoniaProto] duel: PvP " + side[0].Name + " vs " + side[1].Name + (run.Wager ? " (Card Wager " + wagerCards[0] + " / " + wagerCards[1] + ", " + run.MatchId + ")" : ""));
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
            if (run.Match.Finished) FinishPvp(run, now); else SendBoth(run, now);
        }

        void TickPvp(long now)
        {
            lobby.Tick(now);
            Flush(now);
            foreach (PvpRun run in runs.ToArray())
            {
                if (run.Done) continue;
                for (int s = 0; s < 2; s++)
                    if (run.Player[s] != null && !CanKeepDueling(run.Player[s].Hero)) run.Match.Forfeit(s);
                bool changed = run.Match.Finished || run.Match.Tick(now);
                if (run.Match.Finished) FinishPvp(run, now); else if (changed) SendBoth(run, now);
            }
        }

        /// <summary>The match is over: PvP gold to each side that is here, then the wager is paid (DuelService.Wager.cs), then the final views go out.</summary>
        void FinishPvp(PvpRun run, long now)
        {
            if (run.Done) return;
            run.Done = true;
            runs.Remove(run);
            for (int s = 0; s < 2; s++) if (runByChar.TryGetValue(run.Key[s], out PvpRun mine) && mine == run) runByChar.Remove(run.Key[s]);
            stats.Add("pvpFinished", 1);
            for (int s = 0; s < 2; s++)
            {
                NetPlayer p = run.Player[s];
                if (p == null || p.Hero == null) continue;
                DuelView v = run.Match.View(s, now);
                run.Reward[s] = DuelRewards.ApplyPvpResult(p.Rpg.State, v.Result, v.EndReason == 1, DuelNpcRun.DayKeyNow());
                Debug.Log("[ZenoniaProto] duel: PvP result for " + p.Name + ": " + v.Result + ", " + run.Reward[s].DailyGold + " G");
                if (run.Reward[s].DailyGold > 0)
                {
                    Zone.Enter(p.Hero.HomeZone);
                    if (ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p);
                    PushState(p);
                }
            }
            if (run.Wager) SettleWager(run, now);
            SendBoth(run, now);
        }
    }
}
