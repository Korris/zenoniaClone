using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The card duel on the zone server (phase 06 v2: player vs player only, there are no NPC duels). The server decides everything: it checks where and when a duel may start, validates the deck, shuffles and deals
    /// (DuelMatch), keeps the 20 s clock, and runs the card stakes. A client sends ids and cells only, and gets back its own DuelView (moves + its hand; never the opponent's hand).
    /// One instance per zone process, like TradeService. This file: requests, deck saving and what every duel shares; DuelService.Pvp.cs: challenges, the match, the 60 s reconnect and the winner's pick;
    /// DuelService.Wager.cs: escrow of both decks and settle with server-api. A player who dies or ends up on a hostile map forfeits.
    /// </summary>
    public sealed partial class DuelService
    {
        public static DuelService Active { get; private set; }
        /// <summary>Rollback switch: false = every duel request is refused.</summary>
        public static bool Enabled = true;
        const long CombatBlockMs = 5000;
        const int RequestsPerSecond = 10;

        sealed class Entry
        {
            public NetPlayer Player;
            public PvpRun Pvp;
            public int PvpSide;
            public long WindowStartMs; public int InWindow;
            public bool RankDue; // the Ranked season message has not been sent yet (it goes out on the first tick, after the Welcome)
        }

        readonly Dictionary<ushort, Entry> entries = new Dictionary<ushort, Entry>();
        readonly NetDataWriter w = new NetDataWriter();
        readonly ServerReplication repl;
        readonly NetStats stats;
        readonly ZonePersistence persistence;

        public DuelService(ServerReplication replication, NetStats netStats, ZonePersistence zonePersistence = null)
        {
            repl = replication; stats = netStats; persistence = zonePersistence; Active = this;
        }

        public void Stop() { if (Active == this) Active = null; }

        /// <summary>This player has a duel running (playing, picking or settling): trades are refused for them.</summary>
        public static bool IsDueling(NetPlayer p) { return Active != null && p != null && Active.Running(p.PlayerId); }

        bool Running(ushort id)
        {
            Entry e;
            return entries.TryGetValue(id, out e) && e.Pvp != null && !e.Pvp.Closed;
        }

        public void OnLeave(NetPlayer p, long nowMs)
        {
            Entry e;
            if (!entries.TryGetValue(p.PlayerId, out e)) return;
            LeavePvp(e, nowMs);
            entries.Remove(p.PlayerId);
        }

        public void OnRequest(NetPlayer p, DuelRequestMsg m, long nowMs)
        {
            Entry e;
            if (!entries.TryGetValue(p.PlayerId, out e) || p.Hero == null) return;
            stats.Add("duelRequests", 1);
            if (!RateOk(e, nowMs)) { stats.Add("duelRateLimited", 1); return; }
            if (!Enabled) { Refuse(p, DuelFail.Disabled); return; }
            switch (m.Op)
            {
                case DuelOp.SaveDeck: SaveDeck(e, m); break;
                case DuelOp.Place: Place(e, m, nowMs); break;
                case DuelOp.Forfeit: Forfeit(e, nowMs); break;
                case DuelOp.Challenge: Challenge(e, m.Other, nowMs); break;
                case DuelOp.TakeCard: TakeCard(e, m.A, nowMs); break;
                case DuelOp.QueueRanked: QueueRanked(e, nowMs); break;
                case DuelOp.LeaveQueue: LeaveRankedQueue(e, nowMs); break;
                default: LobbyRequest(e, m, nowMs); break;
            }
        }

        /// <summary>At most 10 requests a second per player: a client that floods is ignored (the packet-flood kick in ZoneServer is the hard limit).</summary>
        static bool RateOk(Entry e, long nowMs)
        {
            if (nowMs - e.WindowStartMs >= 1000L) { e.WindowStartMs = nowMs; e.InWindow = 0; }
            return ++e.InWindow <= RequestsPerSecond;
        }

        void SaveDeck(Entry e, DuelRequestMsg m)
        {
            NetPlayer p = e.Player;
            if (Running(p.PlayerId)) { Refuse(p, DuelFail.InDuel); return; }
            if (lobby.IsBusy(p.PlayerId) || queue.Contains(p.PlayerId)) { Refuse(p, DuelFail.Busy); return; }
            var ids = new List<string>(m.Cards.Length);
            foreach (byte i in m.Cards)
            {
                CardDef c = CardTable.ByIndex(i);
                if (c == null) { Refuse(p, DuelFail.DeckInvalid, (byte)DeckError.Unknown); return; }
                ids.Add(c.Id);
            }
            bool same = ids.Count == p.Rpg.State.Cards.Deck.Count;
            for (int i = 0; same && i < ids.Count; i++) same = ids[i] == p.Rpg.State.Cards.Deck[i];
            Zone.Enter(p.Hero.HomeZone);
            DeckError err = DeckRules.Save(p.Rpg.State, ids);
            if (err != DeckError.None) { Refuse(p, DuelFail.DeckInvalid, (byte)err); return; }
            if (!same) // an unchanged deck is not worth a save or a whole RpgState
            {
                if (ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p);
                PushState(p);
            }
            Send(p, new DuelNoticeMsg { Kind = DuelNoticeKind.DeckSaved });
        }

        static ulong NewSeed()
        {
            return ((ulong)Random.Range(1, int.MaxValue) << 32) ^ (ulong)Random.Range(1, int.MaxValue) ^ (ulong)System.Diagnostics.Stopwatch.GetTimestamp();
        }

        /// <summary>Where and when a duel may open: a safe map, alive, not trading, no hit taken and no attack made in the last 5 s.</summary>
        static DuelFail StartRefusal(NetPlayer p)
        {
            HeroController h = p.Hero;
            if (h == null || h.IsDead) return DuelFail.Dead;
            if (h.HomeZone == null || h.HomeZone.Map == null || h.HomeZone.Map.Hostile) return DuelFail.NotSafe;
            if (TradeService.IsTrading(p)) return DuelFail.Trading;
            if (h.MsSinceHurt < CombatBlockMs || h.MsSinceAttack < CombatBlockMs) return DuelFail.Combat;
            return DuelFail.None;
        }

        static DuelFail PlayFailure(DuelPlay r) { return r.Code == DuelPlayError.NotYourTurn ? DuelFail.NotYourTurn : r.Code == DuelPlayError.Stale ? DuelFail.StaleMove : r.Code == DuelPlayError.Over ? DuelFail.NoMatch : DuelFail.BadMove; }

        void Place(Entry e, DuelRequestMsg m, long now)
        {
            if (e.Pvp != null && !e.Pvp.Match.Finished) { PlacePvp(e, m, now); return; }
            Refuse(e.Player, DuelFail.NoMatch);
        }

        void Forfeit(Entry e, long now)
        {
            if (e.Pvp == null || e.Pvp.Match.Finished) { Refuse(e.Player, DuelFail.NoMatch); return; }
            e.Pvp.Match.Forfeit(e.PvpSide);
            EndMatch(e.Pvp, now);
        }

        /// <summary>Once per tick: the lobby clocks, turn timeouts, the 60 s reconnect, the winner's 30 s pick, and the forfeit of a player who can no longer duel.</summary>
        public void Tick(long now) { TickPvp(now); TickRanked(now); TickStakes(now); }

        static bool CanKeepDueling(HeroController h) { return h != null && !h.IsDead && h.HomeZone != null && h.HomeZone.Map != null && !h.HomeZone.Map.Hostile; }

        void PushState(NetPlayer p)
        {
            if (p.Hero == null) return;
            repl.SendRpgState(p, 0, true, "");
            p.RpgDirty = false;
        }

        void SendView(NetPlayer p, DuelView v)
        {
            if (p == null || p.Peer == null) return;
            DuelWire.ToMsg(v).Write(w);
            p.Peer.Send(w, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
        }

        void Refuse(NetPlayer p, DuelFail why, byte extra = 0)
        {
            if (p == null) return;
            stats.Add("duelRefused", 1);
            Send(p, new DuelNoticeMsg { Kind = DuelNoticeKind.Refused, Detail = (byte)why, Extra = extra });
        }

        void Send(NetPlayer p, DuelNoticeMsg m)
        {
            if (p == null || p.Peer == null) return;
            m.Write(w);
            p.Peer.Send(w, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
        }
    }
}
