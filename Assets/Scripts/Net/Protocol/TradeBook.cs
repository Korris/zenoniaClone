using System;
using System.Collections.Generic;

namespace ZenoniaProto.Net
{
    /// <summary>Where a player stands, for the "still in reach" rule.</summary>
    public struct TradeSpot { public string MapId; public float X, Y; public bool Dead; }

    /// <summary>One player's possessions as the trade rules see them: the server's real state behind it, a plain fake in the tests. TakeItem hands back an opaque item for GiveItem.</summary>
    public interface ITradeHolder
    {
        int Gold { get; }
        int BagCount { get; }
        int BagSize { get; }
        TradeSpot Spot { get; }
        /// <summary>True only for an item in the bag right now (never one that is worn).</summary>
        bool TryGetBagItem(string uid, out TradeItemView view);
        bool Wears(string uid);
        /// <summary>Out of the bag; null when it is not there.</summary>
        object TakeItem(string uid);
        void GiveItem(object item);
        void AddGold(int delta);
    }

    public struct TradeNote { public ushort To; public TradeNoticeMsg Msg; }
    public struct TradeStateNote { public ushort To; public TradeStateMsg Msg; }

    /// <summary>A trade that went through: what the caller flushes to both players and writes to the log.</summary>
    public struct TradeDone
    {
        public ushort A, B;
        public string AName, BName;
        public string[] AGaveUids, BGaveUids;
        public int AGaveGold, BGaveGold;
    }

    /// <summary>
    /// The trade rules, with no engine and no network. Each player has at most one request open, sent or received, or one trade, never both. Every change queues what to tell whom: the
    /// caller sends States, then Notes, after flushing Completed trades. Two steps: both lock, then both confirm against the same Revision; any offer change or an unlock clears every lock
    /// and confirm and moves Revision on, so what a player agreed to is always exactly what moves. Execution checks both sides against their current state, then moves everything or nothing,
    /// inside one call. Only uids and amounts come from a client; what an item is always comes from its holder.
    /// </summary>
    public sealed class TradeBook
    {
        public const int MaxItems = 6;
        public const int RequestMs = 30000;
        public const float MaxDistance = 100f;
        /// <summary>Every character is given gear under these same uids, and a mirror restocks a missing starter by itself, so it could neither arrive at the other side nor leave cleanly.</summary>
        public const string StarterUidPrefix = "starter-";

        sealed class Player { public string Name; public byte HeroClass; public ITradeHolder Holder; }
        sealed class Request { public ushort From, To; public long ExpiresAt; }
        sealed class Side
        {
            public ushort Id;
            public readonly List<TradeItemView> Items = new List<TradeItemView>(MaxItems);
            public int Gold;
            public bool Locked, Confirmed;
        }
        sealed class Session
        {
            public Side A, B;
            public uint Revision = 1;
            public Side Of(ushort id) { return A.Id == id ? A : B; }
            public Side Against(ushort id) { return A.Id == id ? B : A; }
        }

        readonly Dictionary<ushort, Player> players = new Dictionary<ushort, Player>();
        readonly Dictionary<ushort, Session> sessionOf = new Dictionary<ushort, Session>();
        readonly List<Request> requests = new List<Request>();
        readonly List<Session> sessions = new List<Session>();

        /// <summary>Notices queued since the caller last emptied this list.</summary>
        public readonly List<TradeNote> Notes = new List<TradeNote>();
        /// <summary>Full window states queued since the caller last emptied this list.</summary>
        public readonly List<TradeStateNote> States = new List<TradeStateNote>();
        /// <summary>Trades executed since the caller last emptied this list.</summary>
        public readonly List<TradeDone> Completed = new List<TradeDone>();

        public void AddPlayer(ushort id, string name, byte heroClass, ITradeHolder holder) { players[id] = new Player { Name = name ?? "", HeroClass = heroClass, Holder = holder }; }

        /// <summary>The player went offline: the request or trade ends for the other side too.</summary>
        public void RemovePlayer(ushort id)
        {
            for (int i = requests.Count - 1; i >= 0; i--)
                if (requests[i].From == id || requests[i].To == id) Close(requests[i], TradeRequestEnd.Offline);
            Session s;
            if (sessionOf.TryGetValue(id, out s)) End(s, TradeEnd.Offline);
            players.Remove(id);
        }

        /// <summary>Runs one client request; every refusal is also queued as a Failed notice to `from`.</summary>
        public TradeFail Apply(ushort from, TradeRequestMsg m, long nowMs)
        {
            string about = "";
            TradeFail fail;
            if (!players.ContainsKey(from)) return TradeFail.NoSuchPlayer;
            switch (m.Op)
            {
                case TradeOp.Request: fail = TryRequest(from, m.Target, nowMs, ref about); break;
                case TradeOp.CancelRequest: fail = CancelRequest(from); break;
                case TradeOp.Accept: fail = Accept(from, m.Target, ref about); break;
                case TradeOp.Decline: fail = Decline(from, m.Target); break;
                case TradeOp.AddItem: fail = AddItem(from, m.Uid); break;
                case TradeOp.RemoveItem: fail = RemoveItem(from, m.Uid); break;
                case TradeOp.SetGold: fail = SetGold(from, m.Gold, ref about); break;
                case TradeOp.Lock: fail = Lock(from, m.Revision, ref about); break;
                case TradeOp.Unlock: fail = Unlock(from); break;
                case TradeOp.Confirm: fail = Confirm(from, m.Revision, ref about); break;
                case TradeOp.Cancel: fail = Cancel(from); break;
                default: fail = TradeFail.NotTrading; break;
            }
            if (fail != TradeFail.None) Note(from, TradeNoticeKind.Failed, (byte)fail, m.Target, about);
            return fail;
        }

        /// <summary>Once per tick: runs out old requests and ends anything whose two players are no longer in reach of each other.</summary>
        public void Tick(long nowMs)
        {
            for (int i = requests.Count - 1; i >= 0; i--)
            {
                Request r = requests[i];
                if (nowMs >= r.ExpiresAt) Close(r, TradeRequestEnd.Expired);
                else if (ReachFail(r.From, r.To) != TradeFail.None) Close(r, TradeRequestEnd.Apart);
            }
            for (int i = sessions.Count - 1; i >= 0; i--)
            {
                TradeFail why = ReachFail(sessions[i].A.Id, sessions[i].B.Id);
                if (why != TradeFail.None) End(sessions[i], EndFor(why));
            }
        }

        // ---- questions ----

        /// <summary>The player has an open trade window: nothing may touch what they could be offering (the server's inventory requests ask this).</summary>
        public bool IsTrading(ushort id) { return sessionOf.ContainsKey(id); }

        public bool HasOpenRequest(ushort id) { return FindRequest(id) != null; }

        public int OpenTrades { get { return sessions.Count; } }

        /// <summary>These requests could lose an offered item or spent gold, or change what is about to move. Learning skills and filling the skill magazine touch neither.</summary>
        public static bool BlocksInventoryOp(InvOp op) { return op != InvOp.SkillLearn && op != InvOp.SkillAssign; }

        // ---- requests ----

        TradeFail TryRequest(ushort from, ushort to, long nowMs, ref string about)
        {
            if (to == from) return TradeFail.Self;
            Player target;
            if (!players.TryGetValue(to, out target)) return TradeFail.NoSuchPlayer;
            about = target.Name;
            if (sessionOf.ContainsKey(from)) return TradeFail.AlreadyTrading;
            Request open = FindRequest(from);
            if (open != null)
            {
                if (open.To == from) return TradeFail.AnswerFirst;
                about = players[open.To].Name;
                return TradeFail.AlreadyPending;
            }
            if (sessionOf.ContainsKey(to) || FindRequest(to) != null) return TradeFail.TargetBusy;
            TradeFail reach = ReachFail(from, to);
            if (reach != TradeFail.None) return reach;
            requests.Add(new Request { From = from, To = to, ExpiresAt = nowMs + RequestMs });
            Note(from, TradeNoticeKind.RequestOut, 0, to, target.Name, RequestMs);
            Note(to, TradeNoticeKind.RequestIn, players[from].HeroClass, from, players[from].Name, RequestMs);
            return TradeFail.None;
        }

        TradeFail CancelRequest(ushort from)
        {
            Request open = FindRequest(from);
            if (open == null || open.From != from) return TradeFail.NoRequest;
            Close(open, TradeRequestEnd.Cancelled);
            return TradeFail.None;
        }

        TradeFail Decline(ushort to, ushort from)
        {
            Request open = FindRequest(to);
            if (open == null || open.To != to || open.From != from) return TradeFail.NoRequest;
            Close(open, TradeRequestEnd.Declined);
            return TradeFail.None;
        }

        TradeFail Accept(ushort to, ushort from, ref string about)
        {
            Request open = FindRequest(to);
            if (open == null || open.To != to || open.From != from) return TradeFail.NoRequest;
            about = players[from].Name;
            TradeFail reach = ReachFail(from, to);
            if (reach != TradeFail.None) { Close(open, TradeRequestEnd.Apart); return reach; }
            Close(open, TradeRequestEnd.Accepted);
            var s = new Session { A = new Side { Id = from }, B = new Side { Id = to } };
            sessions.Add(s);
            sessionOf[from] = s; sessionOf[to] = s;
            SendStates(s);
            return TradeFail.None;
        }

        // ---- the offers ----

        TradeFail AddItem(ushort id, string uid)
        {
            Session s;
            if (!sessionOf.TryGetValue(id, out s)) return TradeFail.NotTrading;
            Side me = s.Of(id);
            ITradeHolder holder = players[id].Holder;
            if (string.IsNullOrEmpty(uid)) return TradeFail.NotOwned;
            if (IndexOf(me, uid) >= 0) return TradeFail.DuplicateItem;
            if (me.Items.Count >= MaxItems) return TradeFail.OfferFull;
            if (holder.Wears(uid)) return TradeFail.Equipped;
            TradeItemView view;
            if (!holder.TryGetBagItem(uid, out view)) return TradeFail.NotOwned;
            if (uid.StartsWith(StarterUidPrefix, StringComparison.Ordinal)) return TradeFail.Starter;
            me.Items.Add(view);
            OfferChanged(s, id);
            return TradeFail.None;
        }

        TradeFail RemoveItem(ushort id, string uid)
        {
            Session s;
            if (!sessionOf.TryGetValue(id, out s)) return TradeFail.NotTrading;
            Side me = s.Of(id);
            int i = IndexOf(me, uid);
            if (i < 0) return TradeFail.NotOffered;
            me.Items.RemoveAt(i);
            OfferChanged(s, id);
            return TradeFail.None;
        }

        TradeFail SetGold(ushort id, int gold, ref string about)
        {
            Session s;
            if (!sessionOf.TryGetValue(id, out s)) return TradeFail.NotTrading;
            Side me = s.Of(id), other = s.Against(id);
            if (gold < 0) return TradeFail.BadGold;
            if (gold > players[id].Holder.Gold) return TradeFail.NotEnoughGold;
            if (me.Gold == gold) return TradeFail.None;
            if (GoldAfter(other, gold) > int.MaxValue) { about = players[other.Id].Name; return TradeFail.GoldOverflow; }
            me.Gold = gold;
            OfferChanged(s, id);
            return TradeFail.None;
        }

        TradeFail Lock(ushort id, uint revision, ref string about)
        {
            Session s;
            if (!sessionOf.TryGetValue(id, out s)) return TradeFail.NotTrading;
            Side me = s.Of(id);
            if (revision != s.Revision) return TradeFail.StaleOffer;
            if (me.Locked) return TradeFail.AlreadyLocked;
            if (s.A.Items.Count == 0 && s.B.Items.Count == 0 && s.A.Gold == 0 && s.B.Gold == 0) return TradeFail.EmptyTrade;
            ushort who;
            TradeFail fail = Validate(s, out who);
            if (fail != TradeFail.None) { about = players[who].Name; return fail; }
            me.Locked = true;
            SendStates(s);
            return TradeFail.None;
        }

        TradeFail Unlock(ushort id)
        {
            Session s;
            if (!sessionOf.TryGetValue(id, out s)) return TradeFail.NotTrading;
            if (!s.Of(id).Locked) return TradeFail.NotLocked;
            ClearAgreement(s);
            ushort other = s.Against(id).Id;
            Note(id, TradeNoticeKind.Changed, (byte)TradeChange.YouUnlocked, other, players[other].Name);
            Note(other, TradeNoticeKind.Changed, (byte)TradeChange.TheyUnlocked, id, players[id].Name);
            SendStates(s);
            return TradeFail.None;
        }

        TradeFail Confirm(ushort id, uint revision, ref string about)
        {
            Session s;
            if (!sessionOf.TryGetValue(id, out s)) return TradeFail.NotTrading;
            Side me = s.Of(id);
            if (!s.A.Locked || !s.B.Locked) return TradeFail.NotBothLocked;
            if (revision != s.Revision) return TradeFail.StaleOffer;
            if (me.Confirmed) return TradeFail.AlreadyConfirmed;
            TradeFail reach = ReachFail(s.A.Id, s.B.Id);
            if (reach != TradeFail.None) { End(s, EndFor(reach)); return TradeFail.None; }
            me.Confirmed = true;
            if (s.A.Confirmed && s.B.Confirmed) Execute(s);
            else SendStates(s);
            return TradeFail.None;
        }

        TradeFail Cancel(ushort id)
        {
            Session s;
            if (!sessionOf.TryGetValue(id, out s)) return TradeFail.NotTrading;
            ushort other = s.Against(id).Id;
            Drop(s);
            Note(id, TradeNoticeKind.Ended, (byte)TradeEnd.YouCancelled, other, players[other].Name);
            Note(other, TradeNoticeKind.Ended, (byte)TradeEnd.TheyCancelled, id, players[id].Name);
            return TradeFail.None;
        }

        // ---- execution ----

        /// <summary>Everything that must hold for this trade to move, against the holders' state right now. `who` = the player the answer is about.</summary>
        TradeFail Validate(Session s, out ushort who)
        {
            TradeFail fail = ValidateSide(s.A, s.B, out who);
            return fail != TradeFail.None ? fail : ValidateSide(s.B, s.A, out who);
        }

        TradeFail ValidateSide(Side giver, Side taker, out ushort who)
        {
            who = giver.Id;
            ITradeHolder from = players[giver.Id].Holder, to = players[taker.Id].Holder;
            if (giver.Gold < 0) return TradeFail.BadGold;
            if (giver.Gold > from.Gold) return TradeFail.NotEnoughGold;
            var seen = new HashSet<string>();
            foreach (TradeItemView v in giver.Items)
            {
                if (!seen.Add(v.Uid)) return TradeFail.DuplicateItem;
                if (from.Wears(v.Uid)) return TradeFail.Equipped;
                TradeItemView now;
                if (!from.TryGetBagItem(v.Uid, out now)) return TradeFail.ItemGone;
                if (v.Uid.StartsWith(StarterUidPrefix, StringComparison.Ordinal)) return TradeFail.Starter;
            }
            who = taker.Id;
            if (to.BagCount - taker.Items.Count + giver.Items.Count > to.BagSize) return TradeFail.NoRoom;
            if (GoldAfter(taker, giver.Gold) > int.MaxValue) return TradeFail.GoldOverflow;
            foreach (TradeItemView v in giver.Items)
            {
                TradeItemView own;
                if (to.Wears(v.Uid) || to.TryGetBagItem(v.Uid, out own)) return TradeFail.UidClash; // random uids can collide, and a duplicate would break every lookup by uid
            }
            who = giver.Id;
            return TradeFail.None;
        }

        /// <summary>The taker's gold once the trade is done, in a width that cannot overflow.</summary>
        long GoldAfter(Side taker, int incoming) { return (long)players[taker.Id].Holder.Gold - taker.Gold + incoming; }

        void Execute(Session s)
        {
            ushort who;
            TradeFail fail = Validate(s, out who);
            if (fail == TradeFail.None && !Move(s)) { fail = TradeFail.ItemGone; who = s.A.Id; }
            if (fail != TradeFail.None)
            {
                string about = players[who].Name;
                ClearAgreement(s);
                Note(s.A.Id, TradeNoticeKind.Failed, (byte)fail, who, about);
                Note(s.B.Id, TradeNoticeKind.Failed, (byte)fail, who, about);
                SendStates(s);
                return;
            }
            Player a = players[s.A.Id], b = players[s.B.Id];
            Completed.Add(new TradeDone { A = s.A.Id, B = s.B.Id, AName = a.Name, BName = b.Name, AGaveUids = UidsOf(s.A), BGaveUids = UidsOf(s.B), AGaveGold = s.A.Gold, BGaveGold = s.B.Gold });
            Drop(s);
            Note(s.A.Id, TradeNoticeKind.Ended, (byte)TradeEnd.Completed, s.B.Id, b.Name);
            Note(s.B.Id, TradeNoticeKind.Ended, (byte)TradeEnd.Completed, s.A.Id, a.Name);
        }

        /// <summary>Validate already passed; if a holder still fails to hand an item over, everything taken goes back and nothing has moved.</summary>
        bool Move(Session s)
        {
            ITradeHolder a = players[s.A.Id].Holder, b = players[s.B.Id].Holder;
            var fromA = new List<object>(MaxItems);
            var fromB = new List<object>(MaxItems);
            if (!TakeAll(a, s.A, fromA) || !TakeAll(b, s.B, fromB))
            {
                foreach (object o in fromA) a.GiveItem(o);
                foreach (object o in fromB) b.GiveItem(o);
                return false;
            }
            foreach (object o in fromA) b.GiveItem(o);
            foreach (object o in fromB) a.GiveItem(o);
            a.AddGold(s.B.Gold - s.A.Gold);
            b.AddGold(s.A.Gold - s.B.Gold);
            return true;
        }

        static bool TakeAll(ITradeHolder holder, Side side, List<object> into)
        {
            foreach (TradeItemView v in side.Items)
            {
                object o = holder.TakeItem(v.Uid);
                if (o == null) return false;
                into.Add(o);
            }
            return true;
        }

        // ---- shared steps ----

        /// <summary>`by` changed their offer: all agreement is void, and the other player always hears of it.</summary>
        void OfferChanged(Session s, ushort by)
        {
            bool hadAgreement = s.A.Locked || s.B.Locked;
            ClearAgreement(s);
            ushort other = s.Against(by).Id;
            if (hadAgreement)
            {
                Note(by, TradeNoticeKind.Changed, (byte)TradeChange.YouChangedCleared, other, players[other].Name);
                Note(other, TradeNoticeKind.Changed, (byte)TradeChange.TheyChangedCleared, by, players[by].Name);
            }
            else Note(other, TradeNoticeKind.Changed, (byte)TradeChange.TheyChanged, by, players[by].Name);
            SendStates(s);
        }

        static void ClearAgreement(Session s)
        {
            s.A.Locked = s.A.Confirmed = s.B.Locked = s.B.Confirmed = false;
            s.Revision++;
        }

        void End(Session s, TradeEnd why)
        {
            Drop(s);
            Note(s.A.Id, TradeNoticeKind.Ended, (byte)why, s.B.Id, players[s.B.Id].Name);
            Note(s.B.Id, TradeNoticeKind.Ended, (byte)why, s.A.Id, players[s.A.Id].Name);
        }

        void Drop(Session s)
        {
            sessions.Remove(s);
            sessionOf.Remove(s.A.Id);
            sessionOf.Remove(s.B.Id);
        }

        static TradeEnd EndFor(TradeFail reach) { return reach == TradeFail.Dead ? TradeEnd.Died : reach == TradeFail.OtherMap ? TradeEnd.MapChanged : TradeEnd.TooFar; }

        /// <summary>None while both are alive, on one map and within MaxDistance.</summary>
        TradeFail ReachFail(ushort a, ushort b)
        {
            TradeSpot p = players[a].Holder.Spot, q = players[b].Holder.Spot;
            if (p.Dead || q.Dead) return TradeFail.Dead;
            if (p.MapId != q.MapId) return TradeFail.OtherMap;
            float dx = p.X - q.X, dy = p.Y - q.Y;
            return dx * dx + dy * dy > MaxDistance * MaxDistance ? TradeFail.TooFar : TradeFail.None;
        }

        void Close(Request r, TradeRequestEnd end)
        {
            requests.Remove(r);
            Note(r.From, TradeNoticeKind.OutClosed, (byte)end, r.To, players[r.To].Name);
            Note(r.To, TradeNoticeKind.InClosed, (byte)end, r.From, players[r.From].Name);
        }

        Request FindRequest(ushort id)
        {
            for (int i = 0; i < requests.Count; i++) if (requests[i].From == id || requests[i].To == id) return requests[i];
            return null;
        }

        static int IndexOf(Side side, string uid)
        {
            for (int i = 0; i < side.Items.Count; i++) if (side.Items[i].Uid == uid) return i;
            return -1;
        }

        static string[] UidsOf(Side side)
        {
            var uids = new string[side.Items.Count];
            for (int i = 0; i < uids.Length; i++) uids[i] = side.Items[i].Uid;
            return uids;
        }

        void SendStates(Session s)
        {
            States.Add(new TradeStateNote { To = s.A.Id, Msg = StateFor(s, s.A, s.B) });
            States.Add(new TradeStateNote { To = s.B.Id, Msg = StateFor(s, s.B, s.A) });
        }

        TradeStateMsg StateFor(Session s, Side me, Side other)
        {
            Player them = players[other.Id];
            return new TradeStateMsg { Other = other.Id, Name = them.Name, HeroClass = them.HeroClass, Revision = s.Revision, Mine = View(me), Theirs = View(other) };
        }

        static TradeOfferView View(Side side) { return new TradeOfferView { Items = side.Items.ToArray(), Gold = side.Gold, Locked = side.Locked, Confirmed = side.Confirmed }; }

        void Note(ushort to, TradeNoticeKind kind, byte detail, ushort other, string name, int msLeft = 0)
        {
            Notes.Add(new TradeNote { To = to, Msg = new TradeNoticeMsg { Kind = kind, Detail = detail, Other = other, Name = name, MsLeft = (ushort)msLeft } });
        }
    }
}
