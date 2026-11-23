using LiteNetLib.Utils;
using Xunit;
using ZenoniaProto.Net;

// Phase 05: the three card duel packets round-trip, and a hostile or truncated packet is refused (the server kicks on a bad request, the client logs a malformed packet).
public class DuelMessageRoundTripTests
{
    static NetDataReader Reader(NetDataWriter w, byte expectedOp)
    {
        var r = new NetDataReader(); r.SetSource(w.CopyData());
        byte op; Assert.True(NetMessageReader.ReadOpcode(r, out op)); Assert.Equal(expectedOp, op);
        return r;
    }

    [Fact]
    public void OpcodesAreDistinctAndInTheirDirectionRange()
    {
        Assert.Equal(7, NetOp.DuelRequest);
        Assert.Equal(26, NetOp.DuelNotice);
        Assert.Equal(27, NetOp.DuelState);
        Assert.InRange((int)NetOp.DuelRequest, 1, 9); // client -> server
        Assert.InRange((int)NetOp.DuelNotice, 10, 29); // server -> client
        var all = new[] { NetOp.Hello, NetOp.Input, NetOp.InvAction, NetOp.Revive, NetOp.PartyRequest, NetOp.TradeRequest, NetOp.DuelRequest, NetOp.Welcome, NetOp.Reject, NetOp.Spawn, NetOp.Despawn, NetOp.Snapshot, NetOp.SelfState, NetOp.RpgState, NetOp.ZoneChanged, NetOp.EventBatch, NetOp.PartyNotice, NetOp.PartyRoster, NetOp.TradeNotice, NetOp.TradeState, NetOp.WantedStatus, NetOp.DuelNotice, NetOp.DuelState };
        Assert.Equal(all.Length, new System.Collections.Generic.HashSet<byte>(all).Count);
    }

    [Fact]
    public void RequestRoundTripForEveryOp()
    {
        var w = new NetDataWriter();
        new DuelRequestMsg { Op = DuelOp.SaveDeck, Cards = new byte[] { 0, 5, 9, 39, 1, 2, 3, 4 } }.Write(w);
        DuelRequestMsg m; Assert.True(DuelRequestMsg.TryRead(Reader(w, NetOp.DuelRequest), out m));
        Assert.Equal(DuelOp.SaveDeck, m.Op); Assert.Equal(new byte[] { 0, 5, 9, 39, 1, 2, 3, 4 }, m.Cards);
        new DuelRequestMsg { Op = DuelOp.TakeCard, A = 2 }.Write(w);
        Assert.True(DuelRequestMsg.TryRead(Reader(w, NetOp.DuelRequest), out m));
        Assert.Equal(DuelOp.TakeCard, m.Op); Assert.Equal(2, m.A); Assert.Empty(m.Cards);
        new DuelRequestMsg { Op = DuelOp.Place, A = 4, B = 15, Turn = 11, Card = 39 }.Write(w);
        Assert.True(DuelRequestMsg.TryRead(Reader(w, NetOp.DuelRequest), out m));
        Assert.Equal(DuelOp.Place, m.Op); Assert.Equal(4, m.A); Assert.Equal(15, m.B); Assert.Equal(11, m.Turn); Assert.Equal(39, m.Card);
        new DuelRequestMsg { Op = DuelOp.Forfeit }.Write(w);
        Assert.True(DuelRequestMsg.TryRead(Reader(w, NetOp.DuelRequest), out m));
        Assert.Equal(DuelOp.Forfeit, m.Op);
    }

    [Fact]
    public void RequestRefusesABadOpAnOversizedDeckAndTrailingBytes()
    {
        var w = new NetDataWriter();
        DuelRequestMsg m;
        w.Reset(); w.Put(NetOp.DuelRequest); w.Put((byte)99); w.Put((byte)0); w.Put((byte)0); w.Put((byte)0); w.Put((byte)0); w.Put((ushort)0); w.Put((byte)0);
        Assert.False(DuelRequestMsg.TryRead(Reader(w, NetOp.DuelRequest), out m));
        w.Reset(); w.Put(NetOp.DuelRequest); w.Put((byte)DuelOp.SaveDeck); w.Put((byte)0); w.Put((byte)0); w.Put((byte)0); w.Put((byte)0); w.Put((ushort)0); w.Put((byte)9); for (int i = 0; i < 9; i++) w.Put((byte)i);
        Assert.False(DuelRequestMsg.TryRead(Reader(w, NetOp.DuelRequest), out m));
        w.Reset(); w.Put(NetOp.DuelRequest); w.Put((byte)DuelOp.Forfeit); w.Put((byte)0); w.Put((byte)0); w.Put((byte)0); w.Put((byte)0); w.Put((ushort)0); w.Put((byte)0); w.Put((byte)77);
        Assert.False(DuelRequestMsg.TryRead(Reader(w, NetOp.DuelRequest), out m));
        w.Reset(); w.Put(NetOp.DuelRequest); w.Put((byte)DuelOp.SaveDeck); // truncated
        Assert.False(DuelRequestMsg.TryRead(Reader(w, NetOp.DuelRequest), out m));
    }

    [Fact]
    public void NoticeRoundTripAndBadDetailRefused()
    {
        var w = new NetDataWriter();
        new DuelNoticeMsg { Kind = DuelNoticeKind.Refused, Detail = (byte)DuelFail.DeckInvalid, Extra = 5 }.Write(w);
        DuelNoticeMsg m; Assert.True(DuelNoticeMsg.TryRead(Reader(w, NetOp.DuelNotice), out m));
        Assert.Equal(DuelNoticeKind.Refused, m.Kind); Assert.Equal((byte)DuelFail.DeckInvalid, m.Detail); Assert.Equal(5, m.Extra);
        new DuelNoticeMsg { Kind = DuelNoticeKind.DeckSaved }.Write(w);
        Assert.True(DuelNoticeMsg.TryRead(Reader(w, NetOp.DuelNotice), out m));
        Assert.Equal(DuelNoticeKind.DeckSaved, m.Kind);
        w.Reset(); w.Put(NetOp.DuelNotice); w.Put((byte)0); w.Put((byte)200); w.Put((byte)0);
        Assert.False(DuelNoticeMsg.TryRead(Reader(w, NetOp.DuelNotice), out m));
        w.Reset(); w.Put(NetOp.DuelNotice); w.Put((byte)7); w.Put((byte)0); w.Put((byte)0);
        Assert.False(DuelNoticeMsg.TryRead(Reader(w, NetOp.DuelNotice), out m));
    }

    static DuelStateMsg Sample()
    {
        return new DuelStateMsg
        {
            Result = 1, EndReason = 2, YourTurn = true, MsLeft = 17250, OppName = "Frost Scout Nia", WagerState = 2, PickedCard = 9, PickMsLeft = 21000, PickAtRandom = true,
            MoveCard = new byte[] { 3, 12, 39 }, MoveCell = new byte[] { 0, 15, 7 }, MoveSide = new byte[] { 0, 1, 0 }, MoveAuto = new byte[] { 0, 1, 0 }, MoveFlips = new ushort[] { 0, 0x8001, 0x0040 },
            Hand = new byte[] { 4, 8, 15 }, PickCards = new byte[] { 0, 1, 2, 3, 4, 5, 6, 39 },
        };
    }

    [Fact]
    public void StateRoundTripKeepsEveryField()
    {
        var w = new NetDataWriter();
        Sample().Write(w);
        DuelStateMsg m; Assert.True(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        DuelStateMsg s = Sample();
        Assert.Equal(s.Result, m.Result); Assert.Equal(s.EndReason, m.EndReason); Assert.True(m.YourTurn); Assert.True(m.PickAtRandom);
        Assert.Equal(s.WagerState, m.WagerState); Assert.Equal(s.PickedCard, m.PickedCard); Assert.Equal(s.PickMsLeft, m.PickMsLeft); Assert.Equal(s.PickCards, m.PickCards);
        Assert.Equal(s.MsLeft, m.MsLeft); Assert.Equal(s.OppName, m.OppName);
        Assert.Equal(s.MoveCard, m.MoveCard); Assert.Equal(s.MoveCell, m.MoveCell); Assert.Equal(s.MoveSide, m.MoveSide); Assert.Equal(s.MoveAuto, m.MoveAuto); Assert.Equal(s.MoveFlips, m.MoveFlips);
        Assert.Equal(s.Hand, m.Hand);
    }

    [Fact]
    public void EmptyStateAndTheLargestStateFitOnePacket()
    {
        var w = new NetDataWriter();
        new DuelStateMsg { OppName = "", MoveCard = new byte[0], MoveCell = new byte[0], MoveSide = new byte[0], MoveAuto = new byte[0], MoveFlips = new ushort[0], Hand = new byte[0], PickCards = new byte[0] }.Write(w);
        DuelStateMsg m; Assert.True(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        Assert.Equal(0, m.MoveCount);
        var full = new DuelStateMsg { OppName = new string('x', 16), MoveCard = new byte[16], MoveCell = new byte[16], MoveSide = new byte[16], MoveAuto = new byte[16], MoveFlips = new ushort[16], Hand = new byte[5], PickCards = new byte[8] };
        full.Write(w);
        Assert.True(w.Length < 200, "a full duel state is " + w.Length + " bytes");
        Assert.True(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        Assert.Equal(16, m.MoveCount);
    }

    [Fact]
    public void StateRefusesNonsense()
    {
        var w = new NetDataWriter();
        DuelStateMsg m;
        DuelStateMsg s = Sample(); s.Result = 9; s.Write(w);
        Assert.False(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        s = Sample(); s.EndReason = 3; s.Write(w);
        Assert.False(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        s = Sample(); s.MoveCard[1] = 200; s.Write(w);
        Assert.False(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        s = Sample(); s.Hand[0] = 200; s.Write(w);
        Assert.False(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        s = Sample(); s.WagerState = 6; s.Write(w);
        Assert.False(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        s = Sample(); s.PickCards[3] = 200; s.Write(w);
        Assert.False(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        w.Reset(); w.Put(NetOp.DuelState); w.Put((byte)1); // truncated
        Assert.False(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
    }
}
