using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The card duel on the zone server. The server decides everything: it checks where and when a duel may start, validates the deck, shuffles and deals (DuelMatch), keeps the 20 s clock, moves the NPC,
    /// and pays the result once. A client sends ids and cells only, and gets back its own DuelView (moves + its hand; never the opponent's hand). One instance per zone process, like TradeService.
    /// This file: the practice NPC duel and what both modes share; DuelService.Pvp.cs: challenges, the wager panel, player vs player matches and the 60 s reconnect; DuelService.Wager.cs: escrow and settle with server-api.
    /// A player who dies or ends up on a hostile map forfeits (an NPC wins, nothing is paid).
    /// </summary>
    public sealed partial class DuelService
    {
        public static DuelService Active { get; private set; }
        /// <summary>Rollback switch: false = every duel request is refused.</summary>
        public static bool Enabled = true;
        const long MinStartGapMs = 400, CombatBlockMs = 5000;
        const int RequestsPerSecond = 10;

        sealed class Entry
        {
            public NetPlayer Player;
            public DuelNpcRun Run;
            public PvpRun Pvp;
            public int PvpSide;
            public float WindowStartMs; public int InWindow;
            public long LastStartMs = -100000;
        }

        readonly Dictionary<ushort, Entry> entries = new Dictionary<ushort, Entry>();
        readonly NetDataWriter w = new NetDataWriter();
        readonly ServerReplication repl;
        readonly NetStats stats;
        readonly ZonePersistence persistence;

        public DuelService(ServerReplication replication, NetStats netStats, ZonePersistence zonePersistence = null)
        {
            repl = replication; stats = netStats; persistence = zonePersistence; Active = this;
            lobby.CardsMatch = (a, b) => WagerRules.Matches(CardTable.ByIndex(a), CardTable.ByIndex(b));
        }

        public void Stop() { if (Active == this) Active = null; }

        /// <summary>This player has a duel running (NPC or player): trades are refused for them.</summary>
        public static bool IsDueling(NetPlayer p) { return Active != null && p != null && Active.Running(p.PlayerId); }

        bool Running(ushort id)
        {
            Entry e;
            return entries.TryGetValue(id, out e) && ((e.Run != null && !e.Run.Finished) || (e.Pvp != null && !e.Pvp.Match.Finished));
        }

        public void OnLeave(NetPlayer p, float nowMs)
        {
            Entry e;
            if (!entries.TryGetValue(p.PlayerId, out e)) return;
            LeavePvp(e, (long)nowMs);
            if (e.Run != null && !e.Run.Finished) { e.Run.Session.Forfeit(); e.Run.Settle(0); Debug.Log("[ZenoniaProto] duel: " + p.Name + " left in the middle of a duel, no reward"); }
            entries.Remove(p.PlayerId);
        }

        public void OnRequest(NetPlayer p, DuelRequestMsg m, float nowMs)
        {
            Entry e;
            if (!entries.TryGetValue(p.PlayerId, out e) || p.Hero == null) return;
            stats.Add("duelRequests", 1);
            if (!RateOk(e, nowMs)) { stats.Add("duelRateLimited", 1); return; }
            if (!Enabled) { Refuse(p, DuelFail.Disabled); return; }
            long now = (long)nowMs;
            switch (m.Op)
            {
                case DuelOp.SaveDeck: SaveDeck(e, m); break;
                case DuelOp.PlayNpc: Start(e, m.A, now); break;
                case DuelOp.Place: Place(e, m, now); break;
                case DuelOp.Forfeit: Forfeit(e, now); break;
                case DuelOp.Challenge: Challenge(e, m.Other, m.A != 0, now); break;
                default: LobbyRequest(e, m, now); break;
            }
        }

        /// <summary>At most 10 requests a second per player: a client that floods is ignored (the packet-flood kick in ZoneServer is the hard limit).</summary>
        static bool RateOk(Entry e, float nowMs)
        {
            if (nowMs - e.WindowStartMs >= 1000f) { e.WindowStartMs = nowMs; e.InWindow = 0; }
            return ++e.InWindow <= RequestsPerSecond;
        }

        void SaveDeck(Entry e, DuelRequestMsg m)
        {
            NetPlayer p = e.Player;
            if (Running(p.PlayerId)) { Refuse(p, DuelFail.InDuel); return; }
            if (lobby.IsBusy(p.PlayerId)) { Refuse(p, DuelFail.Busy); return; }
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

        void Start(Entry e, int npc, long now)
        {
            NetPlayer p = e.Player;
            if (Running(p.PlayerId)) { Refuse(p, DuelFail.InDuel); return; }
            if (lobby.IsBusy(p.PlayerId)) { Refuse(p, DuelFail.Busy); return; }
            DuelFail why = StartRefusal(p);
            if (why != DuelFail.None) { Refuse(p, why); return; }
            if (DuelNpcTable.Get(npc) == null) { Refuse(p, DuelFail.NoSuchNpc); return; }
            if (now - e.LastStartMs < MinStartGapMs) return; // a double tap on Play
            DeckError err;
            ulong seed = NewSeed();
            DuelNpcRun run = DuelNpcRun.Start(p.Rpg.State, npc, seed, now, out err);
            if (run == null) { Refuse(p, DuelFail.DeckInvalid, (byte)err); return; }
            e.Run = run; e.Pvp = null; e.LastStartMs = now;
            stats.Add("duelsStarted", 1);
            Debug.Log("[ZenoniaProto] duel: " + p.Name + " vs NPC " + DuelNpcTable.Get(npc).Id);
            SendState(e, now);
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
            NetPlayer p = e.Player;
            if (e.Pvp != null && !e.Pvp.Match.Finished) { PlacePvp(e, m, now); return; }
            if (e.Run == null || e.Run.Finished) { Refuse(p, DuelFail.NoMatch); return; }
            DuelPlay r = e.Run.Session.PlayerPlace(m.A, m.B, now, m.Turn, m.Card);
            if (!r.Ok) { Refuse(p, PlayFailure(r)); SendState(e, now); return; } // the view resyncs a client that was behind
            if (e.Run.Finished) Finish(e, now); else SendState(e, now);
        }

        void Forfeit(Entry e, long now)
        {
            if (e.Pvp != null && !e.Pvp.Match.Finished) { e.Pvp.Match.Forfeit(e.PvpSide); FinishPvp(e.Pvp, now); return; }
            if (e.Run == null || e.Run.Finished) { Refuse(e.Player, DuelFail.NoMatch); return; }
            e.Run.Session.Forfeit();
            Finish(e, now);
        }

        /// <summary>Once per tick: NPC moves, turn timeouts, the lobby clocks, and the forfeit of a player who can no longer duel.</summary>
        public void Tick(float nowMs)
        {
            long now = (long)nowMs;
            TickPvp(now);
            foreach (Entry e in new List<Entry>(entries.Values))
            {
                if (e.Run == null || e.Run.Finished) continue;
                HeroController h = e.Player.Hero;
                if (!CanKeepDueling(h)) { e.Run.Session.Forfeit(); Finish(e, now); continue; }
                if (e.Run.Session.Tick(now)) { if (e.Run.Finished) Finish(e, now); else SendState(e, now); }
            }
        }

        static bool CanKeepDueling(HeroController h) { return h != null && !h.IsDead && h.HomeZone != null && h.HomeZone.Map != null && !h.HomeZone.Map.Hostile; }

        /// <summary>The NPC match is over: pay it once, push the state (gold) and the final view with the reward lines.</summary>
        void Finish(Entry e, long now)
        {
            NetPlayer p = e.Player;
            if (e.Run.Settle(DuelNpcRun.DayKeyNow()))
            {
                DuelReward r = e.Run.Reward;
                stats.Add("duelsFinished", 1);
                DuelResult result = e.Run.View(now).Result;
                Debug.Log("[ZenoniaProto] duel: " + p.Name + " finished vs NPC " + e.Run.Session.Def.Id + ": " + result + ", reward " + r.FirstWinGold + " + " + r.DailyGold + " G");
                if ((r.Total > 0 || result == DuelResult.Win) && p.Hero != null) // a win moves the daily counter even with no gold: saved at once so a crash does not take it back
                {
                    Zone.Enter(p.Hero.HomeZone);
                    if (ZonePersistence.Active != null) ZonePersistence.Active.RequestSave(p);
                    PushState(p);
                }
            }
            SendState(e, now);
        }

        void PushState(NetPlayer p)
        {
            if (p.Hero == null) return;
            repl.SendRpgState(p, 0, true, "");
            p.RpgDirty = false;
        }

        void SendState(Entry e, long now)
        {
            if (e.Run == null || e.Player.Peer == null) return;
            SendView(e.Player, e.Run.View(now));
        }

        void SendView(NetPlayer p, DuelView v)
        {
            if (p == null || p.Peer == null) return;
            DuelWire.ToMsg(v).Write(w);
            p.Peer.Send(w, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
        }

        void Refuse(NetPlayer p, DuelFail why, byte extra = 0)
        {
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
