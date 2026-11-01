using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The trade rules (TradeBook) on the wire, over the real PlayerState of each connected player. Requests are applied as they arrive and all their consequences go out in the same tick:
    /// a finished trade pushes both players' full RPG state first, so the bag mirrors are already right when the window closes. Every completed trade goes to the server log, standing in
    /// next to the ledger of the world server. With real accounts (ZonePersistence) a confirmed trade is written first, both characters in one API transaction, and only then moves here:
    /// the players are told once it is durable, a refusal moves nothing, and a player leaving meanwhile changes nothing (TradeBook.FinishCommit). On a dev server it moves at once and nothing is saved.
    /// </summary>
    public sealed class TradeService
    {
        public static TradeService Active { get; private set; }

        public readonly TradeBook Book = new TradeBook();

        readonly Dictionary<ushort, NetPlayer> online = new Dictionary<ushort, NetPlayer>();
        readonly NetDataWriter w = new NetDataWriter();
        readonly ServerReplication repl;
        readonly NetStats stats;
        readonly ZonePersistence persistence;
        float nextRefusalLog;

        public TradeService(ServerReplication replication, NetStats netStats, ZonePersistence zonePersistence = null)
        {
            repl = replication; stats = netStats; persistence = zonePersistence; Active = this;
            Book.CommitFirst = persistence != null;
        }

        public void Stop() { if (Active == this) Active = null; }

        /// <summary>This player has a trade open: whatever could take away what they are offering is refused.</summary>
        public static bool IsTrading(NetPlayer p) { return Active != null && Active.Book.IsTrading(p.PlayerId); }

        public void OnJoin(NetPlayer p)
        {
            online[p.PlayerId] = p;
            Book.AddPlayer(p.PlayerId, p.Name, (byte)p.Hero.Class.Id, new Holder(p));
        }

        public void OnLeave(NetPlayer p)
        {
            Book.RemovePlayer(p.PlayerId);
            Flush();
            online.Remove(p.PlayerId);
        }

        public void OnRequest(NetPlayer p, TradeRequestMsg m, float nowMs)
        {
            stats.Add("tradeRequests", 1);
            TradeFail fail = Book.Apply(p.PlayerId, m, (long)nowMs);
            if (fail != TradeFail.None) NoteRefusal(p, m, fail);
            Flush();
        }

        /// <summary>A trace of who tried what, for spotting a bad client. At most ten lines a second however much it sends, and a uid is the client's own text, so it cannot start a line of its own.</summary>
        void NoteRefusal(NetPlayer p, TradeRequestMsg m, TradeFail fail)
        {
            stats.Add("tradeRefused", 1);
            float now = Time.realtimeSinceStartup;
            if (now < nextRefusalLog) return;
            nextRefusalLog = now + 0.1f;
            Debug.Log("[ZenoniaProto] trade: refused " + m.Op + " from " + p.Name + ": " + fail + " (uid '" + (m.Uid ?? "").Replace('\n', ' ').Replace('\r', ' ') + "', gold " + m.Gold + ")");
        }

        /// <summary>Once per tick: runs out old requests and ends trades whose players moved apart, changed map or died.</summary>
        public void Tick(float nowMs)
        {
            Book.Tick((long)nowMs);
            Flush();
        }

        void StartCommit(TradeCommitRequest c)
        {
            NetPlayer a, b;
            if (!online.TryGetValue(c.A, out a) || !online.TryGetValue(c.B, out b) || a.Saved == null || b.Saved == null || !persistence.ApiHealthy)
            {
                stats.Add("tradeCommitNotStarted", 1);
                Book.FinishCommit(c.Token, false); // cannot be written now: better a clear no than a window that hangs. Nothing went out, so nothing can have happened.
                return;
            }
            a.Rpg.BagHeld = b.Rpg.BagHeld = true;
            persistence.CommitTrade(a, b, c, ok =>
            {
                a.Rpg.BagHeld = b.Rpg.BagHeld = false;
                if (!Book.FinishCommit(c.Token, ok)) Debug.LogError("[ZenoniaProto] trade: written by the API but not moved here: " + a.Name + " / " + b.Name);
                if (!ok) Debug.LogWarning("[ZenoniaProto] trade: refused by the API, nothing moved: " + a.Name + " / " + b.Name);
                Flush();
            });
        }

        void Flush()
        {
            if (Book.Commits.Count > 0)
            {
                TradeCommitRequest[] commits = Book.Commits.ToArray();
                Book.Commits.Clear();
                foreach (TradeCommitRequest c in commits) StartCommit(c);
            }
            foreach (TradeDone d in Book.Completed) Finish(d);
            Book.Completed.Clear();
            foreach (TradeStateNote n in Book.States)
            {
                NetPlayer to;
                if (!online.TryGetValue(n.To, out to)) continue;
                n.Msg.Write(w);
                to.Peer.Send(w, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
            }
            Book.States.Clear();
            foreach (TradeNote n in Book.Notes)
            {
                NetPlayer to;
                if (!online.TryGetValue(n.To, out to)) continue; // the player the notice is about has just gone
                n.Msg.Write(w);
                to.Peer.Send(w, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
                stats.Add("tradeNotices", 1);
            }
            Book.Notes.Clear();
        }

        void Finish(TradeDone d)
        {
            stats.Add("tradesCompleted", 1);
            Debug.Log("[ZenoniaProto] trade: completed " + d.AName + " (" + d.A + ") gave [" + string.Join(",", d.AGaveUids) + "] + " + d.AGaveGold + " G; " + d.BName + " (" + d.B + ") gave [" + string.Join(",", d.BGaveUids) + "] + " + d.BGaveGold + " G");
            Push(d.A);
            Push(d.B);
        }

        void Push(ushort id)
        {
            NetPlayer p;
            if (!online.TryGetValue(id, out p) || p.Hero == null) return;
            repl.SendRpgState(p, 0, true, "");
            p.RpgDirty = false;
        }

        /// <summary>The trade rules' view of one connected player.</summary>
        sealed class Holder : ITradeHolder
        {
            readonly NetPlayer p;

            public Holder(NetPlayer player) { p = player; }

            PlayerState State { get { return p.Rpg.State; } }

            public int Gold { get { return State.Gold; } }
            public int BagCount { get { return State.Inventory.Count; } }
            public int BagSize { get { return PlayerState.InventorySize; } }

            public TradeSpot Spot
            {
                get
                {
                    HeroController h = p.Hero;
                    if (h == null) return new TradeSpot { Dead = true };
                    return new TradeSpot { MapId = h.HomeZone != null && h.HomeZone.Map != null ? h.HomeZone.Map.Id : "", X = h.Position.x, Y = h.Position.y, Dead = h.IsDead };
                }
            }

            public bool TryGetBagItem(string uid, out TradeItemView view)
            {
                Item item = State.Inventory.Find(i => i.uid == uid);
                view = item != null ? ViewOf(item) : default(TradeItemView);
                return item != null;
            }

            public bool Wears(string uid)
            {
                foreach (string slot in Slots.All) { Item e = State.GetEquipped(slot); if (e != null && e.uid == uid) return true; }
                return false;
            }

            public object TakeItem(string uid)
            {
                Item item = State.Inventory.Find(i => i.uid == uid);
                if (item != null) State.Inventory.Remove(item);
                return item;
            }

            public void GiveItem(object item) { State.Inventory.Add((Item)item); }

            public void AddGold(int delta) { State.Gold += delta; }

            static TradeItemView ViewOf(Item i)
            {
                ItemStats s = i.stats ?? new ItemStats();
                return new TradeItemView
                {
                    Uid = i.uid, TemplateId = i.templateId, Slot = i.slot, Name = i.name, Rarity = i.rarity, ClassId = i.classId, ItemLevel = i.itemLevel, Enhance = i.enhance, Durability = i.durability,
                    Atk = s.atk, Def = s.def, MaxHp = s.maxHp, MaxMp = s.maxMp, CritChance = s.critChance, SpeedMult = s.speedMult,
                };
            }
        }
    }
}
