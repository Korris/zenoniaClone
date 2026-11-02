using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib.Utils;
using Xunit;
using ZenoniaProto.Net;

public class TradeBookTests
{
    const ushort A = 1, B = 2, C = 3;

    sealed class FakeItem { public string Uid; public int Enhance; }

    sealed class Holder : ITradeHolder
    {
        public readonly List<FakeItem> Bag = new List<FakeItem>();
        public readonly List<string> Worn = new List<string>();
        public int GoldHeld;
        public TradeSpot Where = new TradeSpot { MapId = "m", X = 0f, Y = 0f };
        public int Size = 20;
        /// <summary>The next TakeItem of this uid fails even though the item is there, to prove the all-or-nothing path.</summary>
        public string FailTake;

        public int Gold => GoldHeld;
        public int BagCount => Bag.Count;
        public int BagSize => Size;
        public TradeSpot Spot => Where;

        public bool TryGetBagItem(string uid, out TradeItemView view)
        {
            FakeItem f = Bag.Find(i => i.Uid == uid);
            view = f == null ? default(TradeItemView) : new TradeItemView { Uid = f.Uid, Name = "item " + f.Uid, Enhance = f.Enhance };
            return f != null;
        }

        public bool Wears(string uid) => Worn.Contains(uid);

        public object TakeItem(string uid)
        {
            if (uid == FailTake) return null;
            FakeItem f = Bag.Find(i => i.Uid == uid);
            if (f != null) Bag.Remove(f);
            return f;
        }

        public void GiveItem(object item) { Bag.Add((FakeItem)item); }
        public void AddGold(int delta) { GoldHeld += delta; }
        public Holder With(params string[] uids) { foreach (string u in uids) Bag.Add(new FakeItem { Uid = u }); return this; }
    }

    sealed class World
    {
        public readonly TradeBook Book = new TradeBook();
        public readonly Dictionary<ushort, Holder> Holders = new Dictionary<ushort, Holder>();

        public World() { foreach (ushort id in new[] { A, B, C }) Add(id); }

        public void Add(ushort id)
        {
            var h = new Holder { GoldHeld = 1000 };
            Holders[id] = h;
            Book.AddPlayer(id, "P" + id, (byte)id, h);
        }

        public Holder this[ushort id] => Holders[id];

        public TradeFail Do(ushort from, TradeOp op, ushort target = 0, string uid = "", int gold = 0, long now = 0)
        {
            uint rev = Book.States.Where(s => s.To == from).Select(s => s.Msg.Revision).DefaultIfEmpty(0u).Last();
            return Book.Apply(from, new TradeRequestMsg { Op = op, Target = target, Uid = uid, Gold = gold, Revision = rev }, now);
        }

        /// <summary>The Lock / Confirm a real client sends: naming the revision it was last told.</summary>
        public TradeFail Lock(ushort id) => Do(id, TradeOp.Lock);
        public TradeFail Confirm(ushort id) => Do(id, TradeOp.Confirm);

        public void Open(ushort a = A, ushort b = B)
        {
            Assert.Equal(TradeFail.None, Do(a, TradeOp.Request, b));
            Assert.Equal(TradeFail.None, Do(b, TradeOp.Accept, a));
        }

        public void Add(ushort who, params string[] uids) { foreach (string u in uids) Assert.Equal(TradeFail.None, Do(who, TradeOp.AddItem, 0, u)); }

        public TradeOfferView Mine(ushort id) => Book.States.Last(s => s.To == id).Msg.Mine;
        public TradeOfferView Theirs(ushort id) => Book.States.Last(s => s.To == id).Msg.Theirs;
        public List<TradeNoticeMsg> Told(ushort id) => Book.Notes.Where(n => n.To == id).Select(n => n.Msg).ToList();
        public bool Heard(ushort id, TradeNoticeKind kind, byte detail) => Told(id).Any(m => m.Kind == kind && m.Detail == detail);

        public void LockBoth() { Assert.Equal(TradeFail.None, Lock(A)); Assert.Equal(TradeFail.None, Lock(B)); }
        public void ConfirmBoth() { Assert.Equal(TradeFail.None, Confirm(A)); Assert.Equal(TradeFail.None, Confirm(B)); }
    }

    static World Trading() { var w = new World(); w.Open(); return w; }

    // ---- request ----

    [Fact]
    public void RequestTellsBothSidesWithTheTimeToAnswer()
    {
        var w = new World();
        Assert.Equal(TradeFail.None, w.Do(A, TradeOp.Request, B));
        TradeNoticeMsg sent = w.Told(A).Single(), got = w.Told(B).Single();
        Assert.Equal(TradeNoticeKind.RequestOut, sent.Kind); Assert.Equal(B, sent.Other); Assert.Equal("P2", sent.Name); Assert.Equal(30000, sent.MsLeft);
        Assert.Equal(TradeNoticeKind.RequestIn, got.Kind); Assert.Equal(A, got.Other); Assert.Equal("P1", got.Name); Assert.Equal(30000, got.MsLeft); Assert.Equal(A, got.Detail); // the class travels too
        Assert.False(w.Book.IsTrading(A));
    }

    [Fact]
    public void RequestToYourselfOrNobodyIsRefused()
    {
        var w = new World();
        Assert.Equal(TradeFail.Self, w.Do(A, TradeOp.Request, A));
        Assert.Equal(TradeFail.NoSuchPlayer, w.Do(A, TradeOp.Request, 99));
        Assert.True(w.Heard(A, TradeNoticeKind.Failed, (byte)TradeFail.Self));
    }

    [Fact]
    public void RequesterCanCancelOnlyTheirOwnRequest()
    {
        var w = new World();
        w.Do(A, TradeOp.Request, B);
        Assert.Equal(TradeFail.NoRequest, w.Do(B, TradeOp.CancelRequest));
        Assert.Equal(TradeFail.None, w.Do(A, TradeOp.CancelRequest));
        Assert.True(w.Heard(B, TradeNoticeKind.InClosed, (byte)TradeRequestEnd.Cancelled));
        Assert.Equal(TradeFail.NoRequest, w.Do(B, TradeOp.Accept, A));
    }

    [Fact]
    public void DeclineClosesItForBoth()
    {
        var w = new World();
        w.Do(A, TradeOp.Request, B);
        Assert.Equal(TradeFail.None, w.Do(B, TradeOp.Decline, A));
        Assert.True(w.Heard(A, TradeNoticeKind.OutClosed, (byte)TradeRequestEnd.Declined));
        Assert.False(w.Book.HasOpenRequest(A));
    }

    [Fact]
    public void RequestExpiresAfterThirtySeconds()
    {
        var w = new World();
        w.Do(A, TradeOp.Request, B, now: 1000);
        w.Book.Tick(30999);
        Assert.True(w.Book.HasOpenRequest(A));
        w.Book.Tick(31000);
        Assert.False(w.Book.HasOpenRequest(A));
        Assert.True(w.Heard(A, TradeNoticeKind.OutClosed, (byte)TradeRequestEnd.Expired));
        Assert.True(w.Heard(B, TradeNoticeKind.InClosed, (byte)TradeRequestEnd.Expired));
        Assert.Equal(TradeFail.NoRequest, w.Do(B, TradeOp.Accept, A, now: 31000));
    }

    [Fact]
    public void OnlyTheAddresseeCanAcceptAndTheAnswerMustNameTheRequester()
    {
        var w = new World();
        w.Do(A, TradeOp.Request, B);
        Assert.Equal(TradeFail.NoRequest, w.Do(C, TradeOp.Accept, A));
        Assert.Equal(TradeFail.NoRequest, w.Do(A, TradeOp.Accept, A));
        Assert.Equal(TradeFail.NoRequest, w.Do(B, TradeOp.Accept, C));
        Assert.Equal(TradeFail.None, w.Do(B, TradeOp.Accept, A));
        Assert.True(w.Book.IsTrading(A)); Assert.True(w.Book.IsTrading(B));
        Assert.Equal(B, w.Book.States.Last(s => s.To == A).Msg.Other);
    }

    [Fact]
    public void OneRequestOrTradePerPlayer()
    {
        var w = new World();
        w.Do(A, TradeOp.Request, B);
        Assert.Equal(TradeFail.AlreadyPending, w.Do(A, TradeOp.Request, C));
        Assert.Equal(TradeFail.AnswerFirst, w.Do(B, TradeOp.Request, C));
        Assert.Equal(TradeFail.TargetBusy, w.Do(C, TradeOp.Request, A));
        Assert.Equal(TradeFail.TargetBusy, w.Do(C, TradeOp.Request, B));
        w.Do(B, TradeOp.Accept, A);
        Assert.Equal(TradeFail.AlreadyTrading, w.Do(A, TradeOp.Request, C));
        Assert.Equal(TradeFail.TargetBusy, w.Do(C, TradeOp.Request, A));
        Assert.Equal(1, w.Book.OpenTrades);
    }

    [Theory]
    [InlineData("far")]
    [InlineData("map")]
    [InlineData("dead")]
    public void RequestNeedsBothInReach(string how)
    {
        var w = new World();
        if (how == "far") w[B].Where.X = 100.5f;
        if (how == "map") w[B].Where.MapId = "other";
        if (how == "dead") w[B].Where.Dead = true;
        TradeFail want = how == "far" ? TradeFail.TooFar : how == "map" ? TradeFail.OtherMap : TradeFail.Dead;
        Assert.Equal(want, w.Do(A, TradeOp.Request, B));
        Assert.False(w.Book.HasOpenRequest(A));
    }

    [Fact]
    public void ExactlyOneHundredUnitsIsStillInReach()
    {
        var w = new World();
        w[B].Where.X = 100f;
        Assert.Equal(TradeFail.None, w.Do(A, TradeOp.Request, B));
    }

    [Fact]
    public void WalkingOffEndsAnOpenRequest()
    {
        var w = new World();
        w.Do(A, TradeOp.Request, B);
        w[A].Where.Y = 300f;
        w.Book.Tick(1);
        Assert.False(w.Book.HasOpenRequest(A));
        Assert.True(w.Heard(B, TradeNoticeKind.InClosed, (byte)TradeRequestEnd.Apart));
    }

    [Fact]
    public void AcceptingOutOfReachClosesTheRequest()
    {
        var w = new World();
        w.Do(A, TradeOp.Request, B);
        w[A].Where.Y = 300f;
        Assert.Equal(TradeFail.TooFar, w.Do(B, TradeOp.Accept, A));
        Assert.False(w.Book.HasOpenRequest(A));
        Assert.False(w.Book.IsTrading(A));
    }

    // ---- ends ----

    [Theory]
    [InlineData("far", TradeEnd.TooFar)]
    [InlineData("map", TradeEnd.MapChanged)]
    [InlineData("dead", TradeEnd.Died)]
    public void LosingReachEndsTheTradeForBoth(string how, TradeEnd want)
    {
        World w = Trading();
        if (how == "far") w[A].Where.X = 101f;
        if (how == "map") w[A].Where.MapId = "other";
        if (how == "dead") w[B].Where.Dead = true;
        w.Book.Tick(1);
        Assert.False(w.Book.IsTrading(A)); Assert.False(w.Book.IsTrading(B));
        Assert.True(w.Heard(A, TradeNoticeKind.Ended, (byte)want)); Assert.True(w.Heard(B, TradeNoticeKind.Ended, (byte)want));
    }

    [Fact]
    public void DisconnectEndsTheTradeAndRequestsForTheOtherSide()
    {
        World w = Trading();
        w.Book.Notes.Clear();
        w.Book.RemovePlayer(A);
        Assert.False(w.Book.IsTrading(B));
        Assert.True(w.Heard(B, TradeNoticeKind.Ended, (byte)TradeEnd.Offline));

        var r = new World();
        r.Do(A, TradeOp.Request, B);
        r.Book.RemovePlayer(A);
        Assert.True(r.Heard(B, TradeNoticeKind.InClosed, (byte)TradeRequestEnd.Offline));
        Assert.False(r.Book.HasOpenRequest(B));
    }

    [Fact]
    public void CancelEndsItAtAnyStepAndMovesNothing()
    {
        World w = Trading();
        w[A].With("a1");
        w.Add(A, "a1");
        w.LockBoth();
        Assert.Equal(TradeFail.None, w.Confirm(A));
        Assert.Equal(TradeFail.None, w.Do(B, TradeOp.Cancel));
        Assert.True(w.Heard(A, TradeNoticeKind.Ended, (byte)TradeEnd.TheyCancelled));
        Assert.True(w.Heard(B, TradeNoticeKind.Ended, (byte)TradeEnd.YouCancelled));
        Assert.Single(w[A].Bag);
        Assert.Empty(w[B].Bag);
        Assert.False(w.Book.IsTrading(A));
    }

    // ---- offers ----

    [Fact]
    public void BothSeeTheItemTheServerDescribes()
    {
        World w = Trading();
        w[A].Bag.Add(new FakeItem { Uid = "a1", Enhance = 7 });
        w.Add(A, "a1");
        Assert.Equal(7, w.Mine(A).Items[0].Enhance);
        Assert.Equal(7, w.Theirs(B).Items[0].Enhance);
        Assert.Equal("item a1", w.Theirs(B).Items[0].Name);
    }

    [Fact]
    public void APetGoesOverTheWireLikeOtherGear()
    {
        var msg = new TradeStateMsg { Mine = new TradeOfferView { Items = new[] { new TradeItemView { Uid = "pet-spirit-zzzzzz", TemplateId = "pet-spirit", Slot = "pet", Name = "Spirit", Rarity = "rare", ClassId = "", ItemLevel = 1, Durability = 73 } } }, Theirs = new TradeOfferView { Items = new TradeItemView[0] }, Name = "x" };
        var w = new NetDataWriter();
        msg.Write(w);
        TradeStateMsg m;
        Assert.True(TradeStateMsg.TryRead(Reader(w, NetOp.TradeState), out m));
        Assert.Equal("pet", m.Mine.Items[0].Slot); Assert.Equal("pet-spirit", m.Mine.Items[0].TemplateId); Assert.Equal("pet-spirit-zzzzzz", m.Mine.Items[0].Uid); Assert.Equal(73, m.Mine.Items[0].Durability);
    }

    [Fact]
    public void ItemsThatCannotBeOfferedAreRefused()
    {
        World w = Trading();
        w[A].With("a1", "starter-weapon");
        w[A].Worn.Add("worn1");
        w[B].With("b1");
        Assert.Equal(TradeFail.NotOwned, w.Do(A, TradeOp.AddItem, 0, "nothing"));
        Assert.Equal(TradeFail.NotOwned, w.Do(A, TradeOp.AddItem, 0, ""));
        Assert.Equal(TradeFail.NotOwned, w.Do(A, TradeOp.AddItem, 0, "b1")); // somebody else's
        Assert.Equal(TradeFail.Equipped, w.Do(A, TradeOp.AddItem, 0, "worn1"));
        Assert.Equal(TradeFail.Starter, w.Do(A, TradeOp.AddItem, 0, "starter-weapon"));
        Assert.Equal(TradeFail.None, w.Do(A, TradeOp.AddItem, 0, "a1"));
        Assert.Equal(TradeFail.DuplicateItem, w.Do(A, TradeOp.AddItem, 0, "a1"));
        Assert.Single(w.Mine(A).Items);
    }

    [Fact]
    public void AnOfferHoldsAtMostSixItems()
    {
        World w = Trading();
        w[A].With("1", "2", "3", "4", "5", "6", "7");
        w.Add(A, "1", "2", "3", "4", "5", "6");
        Assert.Equal(TradeFail.OfferFull, w.Do(A, TradeOp.AddItem, 0, "7"));
        Assert.Equal(6, w.Mine(A).Items.Length);
    }

    [Fact]
    public void GoldOfferMustBeRealAndOwned()
    {
        World w = Trading();
        Assert.Equal(TradeFail.BadGold, w.Do(A, TradeOp.SetGold, gold: -1));
        Assert.Equal(TradeFail.BadGold, w.Do(A, TradeOp.SetGold, gold: int.MinValue));
        Assert.Equal(TradeFail.NotEnoughGold, w.Do(A, TradeOp.SetGold, gold: 1001));
        Assert.Equal(TradeFail.NotEnoughGold, w.Do(A, TradeOp.SetGold, gold: int.MaxValue));
        Assert.Equal(TradeFail.None, w.Do(A, TradeOp.SetGold, gold: 1000));
        Assert.Equal(1000, w.Mine(A).Gold);
    }

    [Fact]
    public void GoldOfferThatWouldOverflowTheTakerIsRefused()
    {
        World w = Trading();
        w[B].GoldHeld = int.MaxValue - 10;
        Assert.Equal(TradeFail.GoldOverflow, w.Do(A, TradeOp.SetGold, gold: 11));
        Assert.Equal(TradeFail.None, w.Do(A, TradeOp.SetGold, gold: 10));
        // what the taker gives away counts against the limit
        Assert.Equal(TradeFail.None, w.Do(B, TradeOp.SetGold, gold: 100));
        Assert.Equal(TradeFail.None, w.Do(A, TradeOp.SetGold, gold: 110));
    }

    [Fact]
    public void RemovingAnItemNotOfferedIsRefused()
    {
        World w = Trading();
        w[A].With("a1");
        Assert.Equal(TradeFail.NotOffered, w.Do(A, TradeOp.RemoveItem, 0, "a1"));
        w.Add(A, "a1");
        Assert.Equal(TradeFail.None, w.Do(A, TradeOp.RemoveItem, 0, "a1"));
        Assert.Empty(w.Mine(A).Items);
        Assert.Single(w[A].Bag); // only ever an offer: nothing has left the bag
    }

    [Fact]
    public void OfferOpsNeedATrade()
    {
        var w = new World();
        w[A].With("a1");
        foreach (TradeOp op in new[] { TradeOp.AddItem, TradeOp.RemoveItem, TradeOp.SetGold, TradeOp.Lock, TradeOp.Unlock, TradeOp.Confirm, TradeOp.Cancel })
            Assert.Equal(TradeFail.NotTrading, w.Do(A, op, 0, "a1", 5));
    }

    // ---- lock and confirm ----

    [Fact]
    public void LockingShowsOnBothSidesAndTwiceIsRefused()
    {
        World w = Trading();
        w.Do(A, TradeOp.SetGold, gold: 5);
        Assert.Equal(TradeFail.None, w.Lock(A));
        Assert.True(w.Mine(A).Locked); Assert.True(w.Theirs(B).Locked); Assert.False(w.Mine(B).Locked);
        Assert.Equal(TradeFail.AlreadyLocked, w.Lock(A));
    }

    [Fact]
    public void AnEmptyTradeCannotBeLocked()
    {
        World w = Trading();
        Assert.Equal(TradeFail.EmptyTrade, w.Lock(A));
        Assert.False(w.Mine(A).Locked);
    }

    [Fact]
    public void ConfirmNeedsBothLocked()
    {
        World w = Trading();
        w.Do(A, TradeOp.SetGold, gold: 5);
        Assert.Equal(TradeFail.NotBothLocked, w.Confirm(A));
        w.Lock(A);
        Assert.Equal(TradeFail.NotBothLocked, w.Confirm(A));
        Assert.Equal(TradeFail.NotBothLocked, w.Confirm(B));
        w.Lock(B);
        Assert.Equal(TradeFail.None, w.Confirm(A));
        Assert.True(w.Mine(A).Confirmed); Assert.True(w.Theirs(B).Confirmed);
        Assert.Equal(TradeFail.AlreadyConfirmed, w.Confirm(A));
        Assert.True(w.Book.IsTrading(A));
    }

    [Fact]
    public void UnlockClearsBothLocksAndConfirmsAndTellsBoth()
    {
        World w = Trading();
        w.Do(A, TradeOp.SetGold, gold: 5);
        w.LockBoth();
        w.Confirm(A);
        w.Book.Notes.Clear();
        Assert.Equal(TradeFail.None, w.Do(B, TradeOp.Unlock));
        foreach (ushort id in new[] { A, B })
        {
            Assert.False(w.Mine(id).Locked); Assert.False(w.Mine(id).Confirmed);
            Assert.False(w.Theirs(id).Locked); Assert.False(w.Theirs(id).Confirmed);
        }
        Assert.True(w.Heard(A, TradeNoticeKind.Changed, (byte)TradeChange.TheyUnlocked));
        Assert.True(w.Heard(B, TradeNoticeKind.Changed, (byte)TradeChange.YouUnlocked));
        Assert.Equal(TradeFail.NotLocked, w.Do(B, TradeOp.Unlock));
    }

    public static IEnumerable<object[]> Changes() => new[]
    {
        new object[] { "add theirs" }, new object[] { "remove theirs" }, new object[] { "gold theirs" }, new object[] { "add mine" }, new object[] { "remove mine" }, new object[] { "gold mine" },
    };

    [Theory]
    [MemberData(nameof(Changes))]
    public void AnyOfferChangeClearsBothLocksAndConfirmsAndWarnsBoth(string what)
    {
        World w = Trading();
        w[A].With("a1", "a2"); w[B].With("b1", "b2");
        w.Add(A, "a1"); w.Add(B, "b1");
        w.LockBoth();
        w.Confirm(A);
        w.Book.Notes.Clear();
        ushort by = what.EndsWith("mine") ? A : B;
        string uid = what.StartsWith("add") ? (by == A ? "a2" : "b2") : (by == A ? "a1" : "b1");
        TradeFail f = what.StartsWith("gold") ? w.Do(by, TradeOp.SetGold, gold: 3) : what.StartsWith("add") ? w.Do(by, TradeOp.AddItem, 0, uid) : w.Do(by, TradeOp.RemoveItem, 0, uid);
        Assert.Equal(TradeFail.None, f);
        foreach (ushort id in new[] { A, B })
        {
            Assert.False(w.Mine(id).Locked); Assert.False(w.Mine(id).Confirmed);
            Assert.False(w.Theirs(id).Locked); Assert.False(w.Theirs(id).Confirmed);
        }
        ushort other = by == A ? B : A;
        Assert.True(w.Heard(by, TradeNoticeKind.Changed, (byte)TradeChange.YouChangedCleared));
        Assert.True(w.Heard(other, TradeNoticeKind.Changed, (byte)TradeChange.TheyChangedCleared));
        Assert.Equal(TradeFail.NotBothLocked, w.Confirm(A));
        Assert.Equal(TradeFail.NotBothLocked, w.Confirm(B));
    }

    [Fact]
    public void TheOtherSideHearsOfAChangeEvenWithNoLockHeld()
    {
        World w = Trading();
        w.Book.Notes.Clear();
        w.Do(A, TradeOp.SetGold, gold: 5);
        Assert.True(w.Heard(B, TradeNoticeKind.Changed, (byte)TradeChange.TheyChanged));
        Assert.Empty(w.Told(A));
    }

    [Fact]
    public void SettingTheSameGoldChangesNothing()
    {
        World w = Trading();
        w.Do(A, TradeOp.SetGold, gold: 5);
        w.LockBoth();
        Assert.Equal(TradeFail.None, w.Do(A, TradeOp.SetGold, gold: 5));
        Assert.True(w.Mine(A).Locked); Assert.True(w.Mine(B).Locked);
    }

    /// <summary>The swap at the last moment: a press sent while the offers were being changed must not count for the new offers.</summary>
    [Fact]
    public void ALockOrConfirmNamingOldOffersIsRefused()
    {
        World w = Trading();
        w[A].With("good", "junk");
        w.Add(A, "good");
        uint seen = w.Book.States.Last(s => s.To == B).Msg.Revision;
        w.Do(A, TradeOp.RemoveItem, 0, "good");
        w.Do(A, TradeOp.AddItem, 0, "junk");
        Assert.Equal(TradeFail.StaleOffer, w.Book.Apply(B, new TradeRequestMsg { Op = TradeOp.Lock, Revision = seen }, 0));
        Assert.False(w.Mine(B).Locked);

        w.LockBoth();
        uint before = seen;
        Assert.Equal(TradeFail.StaleOffer, w.Book.Apply(B, new TradeRequestMsg { Op = TradeOp.Confirm, Revision = before }, 0));
        Assert.False(w.Mine(B).Confirmed);
    }

    // ---- execution ----

    [Fact]
    public void ExecutionMovesEverythingAndKeepsEnhanceLevelAndUid()
    {
        World w = Trading();
        w[A].Bag.Add(new FakeItem { Uid = "sword", Enhance = 9 });
        w[A].With("a2", "keep");
        w[B].With("b1");
        w.Add(A, "sword", "a2"); w.Add(B, "b1");
        w.Do(A, TradeOp.SetGold, gold: 300); w.Do(B, TradeOp.SetGold, gold: 100);
        w.LockBoth();
        w.ConfirmBoth();

        Assert.Equal(new[] { "keep", "b1" }, w[A].Bag.Select(i => i.Uid));
        Assert.Equal(new[] { "sword", "a2" }, w[B].Bag.Select(i => i.Uid));
        Assert.Equal(9, w[B].Bag[0].Enhance);
        Assert.Equal(800, w[A].GoldHeld); Assert.Equal(1200, w[B].GoldHeld);
        Assert.False(w.Book.IsTrading(A)); Assert.False(w.Book.IsTrading(B));
        Assert.True(w.Heard(A, TradeNoticeKind.Ended, (byte)TradeEnd.Completed)); Assert.True(w.Heard(B, TradeNoticeKind.Ended, (byte)TradeEnd.Completed));
        TradeDone done = w.Book.Completed.Single();
        Assert.Equal(new[] { "sword", "a2" }, done.AGaveUids); Assert.Equal(new[] { "b1" }, done.BGaveUids);
        Assert.Equal(300, done.AGaveGold); Assert.Equal(100, done.BGaveGold); Assert.Equal("P1", done.AName); Assert.Equal("P2", done.BName);
    }

    [Fact]
    public void AGiftNeedsNothingInReturn()
    {
        World w = Trading();
        w[A].With("a1");
        w.Add(A, "a1");
        w.LockBoth();
        w.ConfirmBoth();
        Assert.Empty(w[A].Bag); Assert.Single(w[B].Bag);
    }

    /// <summary>One side has confirmed, then the world moved under the trade: the second Confirm must find out, nothing moves, and the trade stays open with its locks gone.</summary>
    static void ExpectRefusal(World w, TradeFail want, ushort about, Action breakIt)
    {
        List<string> aBag = null, bBag = null;
        int aGold = 0, bGold = 0;
        Assert.Equal(TradeFail.None, w.Confirm(A));
        breakIt();
        aBag = w[A].Bag.Select(i => i.Uid).ToList(); bBag = w[B].Bag.Select(i => i.Uid).ToList(); aGold = w[A].GoldHeld; bGold = w[B].GoldHeld;
        w.Book.Notes.Clear();
        Assert.Equal(TradeFail.None, w.Confirm(B)); // the press itself was fine: both hear why it did not go through
        Assert.Equal(aBag, w[A].Bag.Select(i => i.Uid).ToList()); Assert.Equal(bBag, w[B].Bag.Select(i => i.Uid).ToList());
        Assert.Equal(aGold, w[A].GoldHeld); Assert.Equal(bGold, w[B].GoldHeld);
        Assert.Empty(w.Book.Completed);
        foreach (ushort id in new[] { A, B })
        {
            TradeNoticeMsg failed = w.Told(id).Single(m => m.Kind == TradeNoticeKind.Failed);
            Assert.Equal((byte)want, failed.Detail);
            Assert.Equal("P" + about, failed.Name);
            Assert.False(w.Mine(id).Locked); Assert.False(w.Mine(id).Confirmed);
        }
        Assert.True(w.Book.IsTrading(A));
    }

    static World ReadyGift(int gold = 0)
    {
        World w = Trading();
        w[A].With("a1");
        w.Add(A, "a1");
        if (gold > 0) w.Do(A, TradeOp.SetGold, gold: gold);
        w.LockBoth();
        return w;
    }

    [Fact]
    public void RefusedWhenTheReceiverHasNoRoom()
    {
        World w = ReadyGift();
        ExpectRefusal(w, TradeFail.NoRoom, B, () => { for (int i = 0; i < 20; i++) w[B].With("fill" + i); });
    }

    [Fact]
    public void TheReceiversOwnOfferedItemsLeaveBeforeTheRoomIsCounted()
    {
        World w = Trading();
        w[A].With("a1", "a2");
        for (int i = 0; i < 20; i++) w[B].With("b" + i);
        w.Add(A, "a1", "a2"); w.Add(B, "b0", "b1");
        w.LockBoth();
        w.ConfirmBoth();
        Assert.Equal(20, w[B].Bag.Count);
        Assert.Contains(w[B].Bag, i => i.Uid == "a2");
    }

    [Fact]
    public void RefusedAtLockWhenThereIsAlreadyNoRoom()
    {
        World w = Trading();
        w[A].With("a1");
        w.Add(A, "a1");
        for (int i = 0; i < 20; i++) w[B].With("b" + i);
        Assert.Equal(TradeFail.NoRoom, w.Lock(A));
        Assert.False(w.Mine(A).Locked);
    }

    [Fact]
    public void RefusedWhenTheGiverNoLongerHasTheGold()
    {
        World w = ReadyGift(500);
        ExpectRefusal(w, TradeFail.NotEnoughGold, A, () => w[A].GoldHeld = 499);
    }

    [Fact]
    public void RefusedWhenTheItemIsGone()
    {
        World w = ReadyGift();
        ExpectRefusal(w, TradeFail.ItemGone, A, () => w[A].Bag.Clear());
    }

    [Fact]
    public void RefusedWhenTheItemWasPutOn()
    {
        World w = ReadyGift();
        ExpectRefusal(w, TradeFail.Equipped, A, () => { w[A].Bag.Clear(); w[A].Worn.Add("a1"); });
    }

    [Fact]
    public void RefusedWhenTheReceiversGoldWouldOverflow()
    {
        World w = ReadyGift(500);
        ExpectRefusal(w, TradeFail.GoldOverflow, B, () => w[B].GoldHeld = int.MaxValue - 100);
    }

    [Fact]
    public void RefusedWhenTheReceiverAlreadyHoldsThatUid()
    {
        World w = ReadyGift();
        ExpectRefusal(w, TradeFail.UidClash, B, () => w[B].With("a1"));
        World x = ReadyGift();
        ExpectRefusal(x, TradeFail.UidClash, B, () => x[B].Worn.Add("a1"));
    }

    [Fact]
    public void AConfirmOutOfReachEndsTheTradeAndMovesNothing()
    {
        World w = ReadyGift(5);
        Assert.Equal(TradeFail.None, w.Confirm(A));
        w[B].Where.X = 500f; // no tick has seen it yet
        Assert.Equal(TradeFail.None, w.Confirm(B));
        Assert.Empty(w.Book.Completed);
        Assert.False(w.Book.IsTrading(A));
        Assert.True(w.Heard(A, TradeNoticeKind.Ended, (byte)TradeEnd.TooFar));
        Assert.Single(w[A].Bag); Assert.Empty(w[B].Bag);
        Assert.Equal(1000, w[A].GoldHeld);
    }

    [Fact]
    public void ARefusalOfYourOwnGoldSaysYou()
    {
        World w = Trading();
        w.Do(A, TradeOp.SetGold, gold: 99999);
        Assert.Equal("You do not have that much gold", TradeText.Of(w.Told(A).Last()));
    }

    [Fact]
    public void AHolderFailingMidMovePutsEverythingBack()
    {
        World w = Trading();
        w[A].With("a1", "a2"); w[B].With("b1");
        w.Add(A, "a1", "a2"); w.Add(B, "b1");
        w.Do(A, TradeOp.SetGold, gold: 10);
        w.LockBoth();
        w[B].FailTake = "b1"; // A's items have already come out when this one fails
        w.ConfirmBoth();
        Assert.Equal(new[] { "a1", "a2" }, w[A].Bag.Select(i => i.Uid).OrderBy(x => x));
        Assert.Equal(new[] { "b1" }, w[B].Bag.Select(i => i.Uid));
        Assert.Equal(1000, w[A].GoldHeld); Assert.Equal(1000, w[B].GoldHeld);
        Assert.Empty(w.Book.Completed);
        Assert.True(w.Heard(A, TradeNoticeKind.Failed, (byte)TradeFail.ItemGone));
    }

    [Fact]
    public void ThereIsARefusalForTheFixedInventoryRequests()
    {
        foreach (InvOp op in new[] { InvOp.Equip, InvOp.Unequip, InvOp.Use, InvOp.Discard, InvOp.Sort, InvOp.Enhance, InvOp.SkillRespec, InvOp.PotionBuy, InvOp.PetBuy, InvOp.QuestAccept, InvOp.QuestTurnIn })
            Assert.True(TradeBook.BlocksInventoryOp(op), op.ToString());
        Assert.False(TradeBook.BlocksInventoryOp(InvOp.SkillLearn));
        Assert.False(TradeBook.BlocksInventoryOp(InvOp.SkillAssign));
    }

    [Fact]
    public void ATradeCanFollowAFinishedOne()
    {
        World w = ReadyGift();
        w.ConfirmBoth();
        w.Book.Notes.Clear(); w.Book.States.Clear();
        w.Open(B, A);
        Assert.Equal(TradeFail.None, w.Do(B, TradeOp.AddItem, 0, "a1")); // the item is B's now
        Assert.Single(w.Mine(B).Items);
    }

    // ---- conservation ----

    /// <summary>Many random players doing random (often illegal) things: uids and gold can only ever change hands, and no uid is ever in two places.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RandomTradesConserveEverything(int seed)
    {
        var rng = new Random(seed);
        var book = new TradeBook();
        var holders = new Dictionary<ushort, Holder>();
        var allUids = new HashSet<string>();
        long totalGold = 0;
        const int Players = 12;
        for (ushort id = 1; id <= Players; id++)
        {
            var h = new Holder { GoldHeld = rng.Next(0, 5000), Size = 20 };
            int items = rng.Next(0, 15);
            for (int i = 0; i < items; i++) { string u = "p" + id + "-" + i; h.With(u); allUids.Add(u); }
            holders[id] = h;
            book.AddPlayer(id, "P" + id, 0, h);
            totalGold += h.GoldHeld;
        }
        var revision = new Dictionary<ushort, uint>();
        int done = 0;
        long now = 0;
        for (int step = 0; step < 200000; step++)
        {
            now += rng.Next(0, 5);
            ushort from = (ushort)rng.Next(1, Players + 1), to = (ushort)rng.Next(1, Players + 1);
            Holder h = holders[from];
            var m = new TradeRequestMsg { Target = to, Revision = revision.TryGetValue(from, out uint r) ? r : 0u };
            switch (rng.Next(0, 16))
            {
                case 0: case 1: m.Op = TradeOp.Request; break;
                case 2: m.Op = TradeOp.CancelRequest; break;
                case 3: case 4: m.Op = TradeOp.Accept; break;
                case 5: m.Op = TradeOp.Decline; break;
                case 6: case 7: m.Op = TradeOp.AddItem; m.Uid = rng.Next(0, 4) == 0 ? "p" + to + "-" + rng.Next(0, 15) : h.Bag.Count > 0 ? h.Bag[rng.Next(h.Bag.Count)].Uid : "none"; break;
                case 8: m.Op = TradeOp.RemoveItem; m.Uid = h.Bag.Count > 0 ? h.Bag[rng.Next(h.Bag.Count)].Uid : "none"; break;
                case 9: m.Op = TradeOp.SetGold; m.Gold = rng.Next(0, 6) == 0 ? (rng.Next(0, 2) == 0 ? -rng.Next(1, 100) : int.MaxValue - rng.Next(0, 5)) : rng.Next(0, Math.Max(1, h.GoldHeld)); break;
                case 10: case 11: case 12: m.Op = TradeOp.Lock; break;
                case 13: m.Op = rng.Next(0, 4) == 0 ? TradeOp.Unlock : TradeOp.Lock; break;
                case 14: m.Op = TradeOp.Confirm; break;
                default: m.Op = rng.Next(0, 20) == 0 ? TradeOp.Cancel : TradeOp.Confirm; break;
            }
            book.Apply(from, m, now);
            book.Tick(now);
            foreach (TradeStateNote s in book.States) revision[s.To] = s.Msg.Revision;
            book.States.Clear();
            book.Notes.Clear();
            done += book.Completed.Count;
            book.Completed.Clear();
            if (step % 500 == 0) Check(holders, allUids, totalGold);
        }
        Check(holders, allUids, totalGold);
        Assert.True(done > 100, "only " + done + " trades went through: the test did not exercise execution");
    }

    static void Check(Dictionary<ushort, Holder> holders, HashSet<string> allUids, long totalGold)
    {
        List<string> uids = holders.Values.SelectMany(h => h.Bag.Select(i => i.Uid)).ToList();
        Assert.Equal(allUids.Count, uids.Count);
        Assert.Equal(uids.Count, uids.Distinct().Count());
        Assert.True(allUids.SetEquals(uids));
        Assert.Equal(totalGold, holders.Values.Sum(h => (long)h.GoldHeld));
        foreach (Holder h in holders.Values) { Assert.True(h.GoldHeld >= 0); Assert.True(h.Bag.Count <= h.Size); }
    }

    // ---- client state and text ----

    [Fact]
    public void TheClientStateFollowsTheNotices()
    {
        var w = new World();
        var s = new TradeClientState();
        w.Do(A, TradeOp.Request, B);
        foreach (TradeNoticeMsg m in w.Told(B)) s.Apply(m, 100.0);
        Assert.Equal(A, s.Incoming.Value.Other);
        Assert.Equal(30100.0, s.Incoming.Value.ExpiresAtMs);
        Assert.Equal(A, s.Incoming.Value.HeroClass);
        w.Book.Notes.Clear();
        w.Do(B, TradeOp.Accept, A);
        foreach (TradeNoticeMsg m in w.Told(B)) s.Apply(m, 200.0);
        Assert.False(s.Incoming.HasValue);
        s.Apply(w.Book.States.Last(x => x.To == B).Msg);
        Assert.Equal(A, s.Session.Value.Other);

        int warnings = s.WarningCount;
        w.Book.Notes.Clear();
        w.Do(A, TradeOp.SetGold, gold: 5);
        foreach (TradeNoticeMsg m in w.Told(B)) s.Apply(m, 300.0);
        Assert.Equal(warnings + 1, s.WarningCount);

        w.Book.Notes.Clear();
        w.Do(A, TradeOp.Cancel);
        foreach (TradeNoticeMsg m in w.Told(B)) s.Apply(m, 300.0);
        Assert.False(s.Session.HasValue);
    }

    [Fact]
    public void EveryNoticeHasWordsWhereTheUiNeedsThem()
    {
        foreach (TradeFail f in Enum.GetValues(typeof(TradeFail)))
        {
            if (f == TradeFail.None) continue;
            Assert.False(string.IsNullOrEmpty(TradeText.Of(new TradeNoticeMsg { Kind = TradeNoticeKind.Failed, Detail = (byte)f, Name = "X" })), f.ToString());
        }
        foreach (TradeEnd e in Enum.GetValues(typeof(TradeEnd)))
            Assert.False(string.IsNullOrEmpty(TradeText.Of(new TradeNoticeMsg { Kind = TradeNoticeKind.Ended, Detail = (byte)e, Name = "X" })), e.ToString());
        foreach (TradeChange c in Enum.GetValues(typeof(TradeChange)))
            Assert.False(string.IsNullOrEmpty(TradeText.Banner(c, "X")), c.ToString());
    }

    // ---- wire ----

    static NetDataReader Reader(NetDataWriter w, byte op)
    {
        var r = new NetDataReader(w.CopyData());
        Assert.Equal(op, r.GetByte());
        return r;
    }

    [Fact]
    public void RequestRoundTripsEveryOpAndRefusesBadPackets()
    {
        var w = new NetDataWriter();
        foreach (TradeOp op in Enum.GetValues(typeof(TradeOp)))
        {
            new TradeRequestMsg { Op = op, Target = 513, Uid = "sword-abc", Gold = -7, Revision = 42 }.Write(w);
            TradeRequestMsg m;
            Assert.True(TradeRequestMsg.TryRead(Reader(w, NetOp.TradeRequest), out m));
            Assert.Equal(op, m.Op); Assert.Equal(513, m.Target); Assert.Equal("sword-abc", m.Uid); Assert.Equal(-7, m.Gold); Assert.Equal(42u, m.Revision);
        }
        w.Reset(); w.Put(NetOp.TradeRequest); w.Put((byte)99); w.Put((ushort)1); w.Put("x"); w.Put(0); w.Put(0u);
        TradeRequestMsg bad;
        Assert.False(TradeRequestMsg.TryRead(Reader(w, NetOp.TradeRequest), out bad));
        w.Reset(); w.Put(NetOp.TradeRequest); w.Put((byte)0); // truncated
        Assert.False(TradeRequestMsg.TryRead(Reader(w, NetOp.TradeRequest), out bad));
    }

    [Fact]
    public void AnOverlongUidIsNotAcceptedAsItIs()
    {
        var w = new NetDataWriter();
        w.Reset(); w.Put(NetOp.TradeRequest); w.Put((byte)TradeOp.AddItem); w.Put((ushort)0); w.Put(new string('x', 5000)); w.Put(0); w.Put(0u);
        TradeRequestMsg m;
        Assert.True(TradeRequestMsg.TryRead(Reader(w, NetOp.TradeRequest), out m));
        Assert.True(m.Uid.Length <= NetProtocol.MaxItemUidLength);
    }

    [Fact]
    public void NoticeRoundTripsAndRefusesDetailsOutOfRange()
    {
        var w = new NetDataWriter();
        new TradeNoticeMsg { Kind = TradeNoticeKind.Failed, Detail = (byte)TradeFail.NoRoom, Other = 9, Name = "Luna", MsLeft = 12 }.Write(w);
        TradeNoticeMsg m;
        Assert.True(TradeNoticeMsg.TryRead(Reader(w, NetOp.TradeNotice), out m));
        Assert.Equal(TradeNoticeKind.Failed, m.Kind); Assert.Equal((byte)TradeFail.NoRoom, m.Detail); Assert.Equal(9, m.Other); Assert.Equal("Luna", m.Name); Assert.Equal(12, m.MsLeft);

        foreach (var (kind, top) in new[] { (TradeNoticeKind.OutClosed, 6), (TradeNoticeKind.InClosed, 6), (TradeNoticeKind.Failed, (int)TradeFail.SaveFailed + 1), (TradeNoticeKind.Changed, 5), (TradeNoticeKind.Ended, 7) })
        {
            new TradeNoticeMsg { Kind = kind, Detail = (byte)top, Name = "" }.Write(w);
            Assert.False(TradeNoticeMsg.TryRead(Reader(w, NetOp.TradeNotice), out m), kind.ToString());
        }
        w.Reset(); w.Put(NetOp.TradeNotice); w.Put((byte)99); w.Put((byte)0); w.Put((ushort)0); w.Put(""); w.Put((ushort)0);
        Assert.False(TradeNoticeMsg.TryRead(Reader(w, NetOp.TradeNotice), out m));
    }

    [Fact]
    public void StateRoundTripsFullOffers()
    {
        TradeItemView[] Six(string who) => Enumerable.Range(0, TradeBook.MaxItems).Select(i => new TradeItemView
        {
            Uid = who + i, TemplateId = "sword", Slot = "weapon", Name = "Mythic Blade of " + who, Rarity = "epic", ClassId = "warrior", ItemLevel = 40 + i, Enhance = i, Atk = 300 + i, Def = 1, MaxHp = 2, MaxMp = 3, CritChance = 0.125f, SpeedMult = 0.5f,
        }).ToArray();
        var msg = new TradeStateMsg
        {
            Other = 77, Name = "Luna", HeroClass = 4, Revision = 123,
            Mine = new TradeOfferView { Items = Six("a"), Gold = 1250, Locked = true, Confirmed = false },
            Theirs = new TradeOfferView { Items = Six("b"), Gold = int.MaxValue, Locked = false, Confirmed = true },
        };
        var w = new NetDataWriter();
        msg.Write(w);
        TradeStateMsg m;
        Assert.True(TradeStateMsg.TryRead(Reader(w, NetOp.TradeState), out m));
        Assert.Equal(77, m.Other); Assert.Equal("Luna", m.Name); Assert.Equal(4, m.HeroClass); Assert.Equal(123u, m.Revision);
        Assert.Equal(1250, m.Mine.Gold); Assert.True(m.Mine.Locked); Assert.False(m.Mine.Confirmed);
        Assert.Equal(int.MaxValue, m.Theirs.Gold); Assert.False(m.Theirs.Locked); Assert.True(m.Theirs.Confirmed);
        Assert.Equal(6, m.Theirs.Items.Length);
        TradeItemView v = m.Theirs.Items[5];
        Assert.Equal("b5", v.Uid); Assert.Equal("sword", v.TemplateId); Assert.Equal("weapon", v.Slot); Assert.Equal("Mythic Blade of b", v.Name); Assert.Equal("epic", v.Rarity); Assert.Equal("warrior", v.ClassId);
        Assert.Equal(45, v.ItemLevel); Assert.Equal(5, v.Enhance); Assert.Equal(305, v.Atk); Assert.Equal(1, v.Def); Assert.Equal(2, v.MaxHp); Assert.Equal(3, v.MaxMp); Assert.Equal(0.125f, v.CritChance); Assert.Equal(0.5f, v.SpeedMult);
    }

    [Fact]
    public void StateRefusesTooManyItemsAndTruncation()
    {
        var w = new NetDataWriter();
        w.Reset(); w.Put(NetOp.TradeState); w.Put((ushort)1); w.Put("x"); w.Put((byte)0); w.Put(1u); w.Put((byte)(TradeBook.MaxItems + 1));
        TradeStateMsg m;
        Assert.False(TradeStateMsg.TryRead(Reader(w, NetOp.TradeState), out m));
        w.Reset(); w.Put(NetOp.TradeState); w.Put((ushort)1);
        Assert.False(TradeStateMsg.TryRead(Reader(w, NetOp.TradeState), out m));
    }
}
