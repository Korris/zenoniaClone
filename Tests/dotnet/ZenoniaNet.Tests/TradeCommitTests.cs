using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenoniaProto.Net;

/// <summary>TradeBook with a real world server behind it: confirmed goods stay put until the write is answered, nothing can change meanwhile, and a player leaving neither cancels nor duplicates.</summary>
public class TradeCommitTests
{
    const ushort A = 1, B = 2;

    sealed class Holder : ITradeHolder
    {
        public readonly List<string> Bag = new List<string>();
        public int GoldHeld;
        public int Gold => GoldHeld;
        public int BagCount => Bag.Count;
        public int BagSize => 50; // PlayerState.InventorySize
        public TradeSpot Spot => new TradeSpot { MapId = "m" };
        public bool TryGetBagItem(string uid, out TradeItemView view) { view = new TradeItemView { Uid = uid, Name = uid }; return Bag.Contains(uid); }
        public bool Wears(string uid) => false;
        public object TakeItem(string uid) => Bag.Remove(uid) ? uid : null;
        public void GiveItem(object item) { Bag.Add((string)item); }
        public void AddGold(int delta) { GoldHeld += delta; }
    }

    readonly TradeBook book = new TradeBook { CommitFirst = true };
    readonly Holder a = new Holder { GoldHeld = 1000 }, b = new Holder { GoldHeld = 500 };

    public TradeCommitTests()
    {
        a.Bag.Add("sword");
        b.Bag.Add("shield");
        book.AddPlayer(A, "Ann", 0, a);
        book.AddPlayer(B, "Bob", 1, b);
    }

    TradeFail Do(ushort from, TradeOp op, string uid = null, int gold = 0, ushort target = 0)
    {
        uint rev = book.States.Count > 0 ? book.States[book.States.Count - 1].Msg.Revision : 1u;
        return book.Apply(from, new TradeRequestMsg { Op = op, Uid = uid, Gold = gold, Target = target, Revision = rev }, 0);
    }

    /// <summary>Both offered, locked and confirmed: A gives the sword and 300, B gives 100.</summary>
    TradeCommitRequest Confirmed()
    {
        Assert.Equal(TradeFail.None, Do(A, TradeOp.Request, target: B));
        Assert.Equal(TradeFail.None, Do(B, TradeOp.Accept, target: A));
        Assert.Equal(TradeFail.None, Do(A, TradeOp.AddItem, "sword"));
        Assert.Equal(TradeFail.None, Do(A, TradeOp.SetGold, gold: 300));
        Assert.Equal(TradeFail.None, Do(B, TradeOp.SetGold, gold: 100));
        Assert.Equal(TradeFail.None, Do(A, TradeOp.Lock));
        Assert.Equal(TradeFail.None, Do(B, TradeOp.Lock));
        Assert.Equal(TradeFail.None, Do(A, TradeOp.Confirm));
        Assert.Empty(book.Commits);
        Assert.Equal(TradeFail.None, Do(B, TradeOp.Confirm));
        return Assert.Single(book.Commits);
    }

    void AssertNothingMoved()
    {
        Assert.Equal(new[] { "sword" }, a.Bag);
        Assert.Equal(new[] { "shield" }, b.Bag);
        Assert.Equal(1000, a.GoldHeld);
        Assert.Equal(500, b.GoldHeld);
    }

    void AssertTraded()
    {
        Assert.Empty(a.Bag);
        Assert.Equal(new[] { "shield", "sword" }, b.Bag.OrderBy(x => x));
        Assert.Equal(800, a.GoldHeld);
        Assert.Equal(700, b.GoldHeld);
    }

    [Fact]
    public void BothConfirmedAsksForTheWriteAndMovesNothingYet()
    {
        TradeCommitRequest c = Confirmed();
        Assert.Equal(new[] { "sword" }, c.AGaveUids);
        Assert.Empty(c.BGaveUids);
        Assert.Equal(300, c.AGaveGold);
        Assert.Equal(100, c.BGaveGold);
        AssertNothingMoved();
        Assert.Empty(book.Completed);
        Assert.True(book.IsTrading(A)); // the bag is still blocked for everything else
    }

    [Fact]
    public void AWrittenTradeMovesHereAndCompletes()
    {
        TradeCommitRequest c = Confirmed();
        Assert.True(book.FinishCommit(c.Token, true));
        AssertTraded();
        Assert.Single(book.Completed);
        Assert.False(book.IsTrading(A));
        Assert.False(book.IsTrading(B));
    }

    [Fact]
    public void ARefusedWriteMovesNothingAndLeavesTheOffersUnlocked()
    {
        TradeCommitRequest c = Confirmed();
        book.Notes.Clear();
        Assert.True(book.FinishCommit(c.Token, false));
        AssertNothingMoved();
        Assert.Empty(book.Completed);
        Assert.True(book.IsTrading(A));
        Assert.Equal(2, book.Notes.Count(n => n.Msg.Kind == TradeNoticeKind.Failed && n.Msg.Detail == (byte)TradeFail.SaveFailed));
        TradeStateMsg last = book.States[book.States.Count - 1].Msg;
        Assert.False(last.Mine.Locked);
        Assert.False(last.Mine.Confirmed);
        Assert.Equal(TradeFail.None, Do(A, TradeOp.Cancel)); // no longer frozen
    }

    [Theory]
    [InlineData(TradeOp.Cancel)]
    [InlineData(TradeOp.Unlock)]
    [InlineData(TradeOp.RemoveItem)]
    [InlineData(TradeOp.SetGold)]
    [InlineData(TradeOp.AddItem)]
    public void NothingChangesAFrozenTrade(TradeOp op)
    {
        TradeCommitRequest c = Confirmed();
        Assert.Equal(TradeFail.Committing, Do(A, op, "sword", 1));
        Assert.Equal(TradeFail.Committing, Do(B, op, "shield", 1));
        Assert.True(book.FinishCommit(c.Token, true));
        AssertTraded();
    }

    [Fact]
    public void WalkingAwayDoesNotBreakAFrozenTrade()
    {
        TradeCommitRequest c = Confirmed();
        book.Tick(0);
        Assert.True(book.IsTrading(A));
        Assert.True(book.FinishCommit(c.Token, true));
        AssertTraded();
    }

    [Fact]
    public void LeavingWhileTheWriteIsOutStillMovesTheGoodsWhenItLands()
    {
        TradeCommitRequest c = Confirmed();
        book.RemovePlayer(A);
        AssertNothingMoved();
        Assert.True(book.FinishCommit(c.Token, true));
        AssertTraded(); // A's holder took part: its last save carries this
        Assert.False(book.IsTrading(B));
        Assert.Equal(TradeFail.NoSuchPlayer, Do(B, TradeOp.Request, target: A)); // and now A is really gone
    }

    [Fact]
    public void LeavingAndARefusalEndsTheTradeWithNothingMoved()
    {
        TradeCommitRequest c = Confirmed();
        book.RemovePlayer(B);
        book.Notes.Clear();
        Assert.True(book.FinishCommit(c.Token, false));
        AssertNothingMoved();
        Assert.False(book.IsTrading(A));
        Assert.Contains(book.Notes, n => n.To == A && n.Msg.Kind == TradeNoticeKind.Ended && n.Msg.Detail == (byte)TradeEnd.Offline);
    }

    [Fact]
    public void BothLeavingAndItLandingStillMovesBoth()
    {
        TradeCommitRequest c = Confirmed();
        book.RemovePlayer(A);
        book.RemovePlayer(B);
        Assert.True(book.FinishCommit(c.Token, true));
        AssertTraded();
    }

    [Fact]
    public void AnAnswerCountsOnlyOnce()
    {
        TradeCommitRequest c = Confirmed();
        Assert.True(book.FinishCommit(c.Token, true));
        Assert.False(book.FinishCommit(c.Token, true));
        AssertTraded();
    }

    [Fact]
    public void WithoutAWorldServerItMovesAtOnceAsBefore()
    {
        var plain = new TradeBook();
        var x = new Holder { GoldHeld = 10 };
        var y = new Holder();
        plain.AddPlayer(A, "x", 0, x);
        plain.AddPlayer(B, "y", 0, y);
        plain.Apply(A, new TradeRequestMsg { Op = TradeOp.Request, Target = B }, 0);
        plain.Apply(B, new TradeRequestMsg { Op = TradeOp.Accept, Target = A }, 0);
        uint rev() => plain.States[plain.States.Count - 1].Msg.Revision;
        plain.Apply(A, new TradeRequestMsg { Op = TradeOp.SetGold, Gold = 5 }, 0);
        plain.Apply(A, new TradeRequestMsg { Op = TradeOp.Lock, Revision = rev() }, 0);
        plain.Apply(B, new TradeRequestMsg { Op = TradeOp.Lock, Revision = rev() }, 0);
        plain.Apply(A, new TradeRequestMsg { Op = TradeOp.Confirm, Revision = rev() }, 0);
        plain.Apply(B, new TradeRequestMsg { Op = TradeOp.Confirm, Revision = rev() }, 0);
        Assert.Equal(5, x.GoldHeld);
        Assert.Equal(5, y.GoldHeld);
        Assert.Empty(plain.Commits);
    }

    [Fact]
    public void TheNewRefusalsSurviveTheWireAndHaveText()
    {
        foreach (TradeFail f in new[] { TradeFail.Committing, TradeFail.SaveFailed })
        {
            var w = new LiteNetLib.Utils.NetDataWriter();
            new TradeNoticeMsg { Kind = TradeNoticeKind.Failed, Detail = (byte)f, Name = "Bob" }.Write(w);
            var r = new LiteNetLib.Utils.NetDataReader(w.CopyData());
            byte op;
            Assert.True(NetMessageReader.ReadOpcode(r, out op));
            TradeNoticeMsg back;
            Assert.True(TradeNoticeMsg.TryRead(r, out back));
            Assert.NotEqual("", TradeText.Of(back));
        }
    }

    [Fact]
    public void TheNewRejectReasonSurvivesTheWire()
    {
        var w = new LiteNetLib.Utils.NetDataWriter();
        NetMessageWriter.Reject(w, new RejectMsg { Reason = RejectReason.Unavailable, Text = "later" });
        var r = new LiteNetLib.Utils.NetDataReader(w.CopyData());
        byte op;
        Assert.True(NetMessageReader.ReadOpcode(r, out op));
        RejectMsg back;
        Assert.True(NetMessageReader.TryReject(r, out back));
        Assert.Equal(RejectReason.Unavailable, back.Reason);
    }
}
