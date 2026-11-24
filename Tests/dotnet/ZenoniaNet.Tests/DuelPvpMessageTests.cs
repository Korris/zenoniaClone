using LiteNetLib.Utils;
using Xunit;
using ZenoniaProto.Net;

// Phase 06 v2: the PvP duel packets (challenge ops, lobby, stakes / pick fields of the state) round-trip, and a hostile or truncated packet is refused.
public class DuelPvpMessageTests
{
    static NetDataReader Reader(NetDataWriter w, byte expectedOp)
    {
        var r = new NetDataReader(); r.SetSource(w.CopyData());
        byte op; Assert.True(NetMessageReader.ReadOpcode(r, out op)); Assert.Equal(expectedOp, op);
        return r;
    }

    [Fact]
    public void ChallengeAndLobbyOpsRoundTripWithTheTarget()
    {
        var w = new NetDataWriter();
        foreach (DuelOp op in new[] { DuelOp.Challenge, DuelOp.CancelInvite, DuelOp.Accept, DuelOp.Decline, DuelOp.TakeCard, DuelOp.LeaveLobby })
        {
            new DuelRequestMsg { Op = op, Other = 4321, A = 1 }.Write(w);
            DuelRequestMsg m; Assert.True(DuelRequestMsg.TryRead(Reader(w, NetOp.DuelRequest), out m));
            Assert.Equal(op, m.Op); Assert.Equal(4321, m.Other); Assert.Equal(1, m.A);
        }
    }

    [Fact]
    public void TheOpcodeOfTheLobbyIsServerToClientAndUnique()
    {
        Assert.Equal(28, NetOp.DuelLobby);
        Assert.InRange((int)NetOp.DuelLobby, 10, 29);
        Assert.NotEqual(NetOp.DuelState, NetOp.DuelLobby);
        Assert.NotEqual(NetOp.DuelNotice, NetOp.DuelLobby);
    }

    [Fact]
    public void LobbyMessageRoundTripKeepsEveryField()
    {
        var w = new NetDataWriter();
        new DuelLobbyMsg { Phase = 3, Other = 777, OtherName = "Tay Gan", Stakes = true, MsLeft = 54321 }.Write(w);
        DuelLobbyMsg m; Assert.True(DuelLobbyMsg.TryRead(Reader(w, NetOp.DuelLobby), out m));
        Assert.Equal(3, m.Phase); Assert.Equal(777, m.Other); Assert.Equal("Tay Gan", m.OtherName); Assert.True(m.Stakes); Assert.Equal(54321, m.MsLeft);
        new DuelLobbyMsg { Phase = 1, OtherName = "" }.Write(w);
        Assert.True(DuelLobbyMsg.TryRead(Reader(w, NetOp.DuelLobby), out m));
        Assert.False(m.Stakes);
    }

    [Fact]
    public void LobbyMessageRefusesBadPhaseAndTrailingBytes()
    {
        var w = new NetDataWriter();
        DuelLobbyMsg m;
        new DuelLobbyMsg { Phase = 4, OtherName = "" }.Write(w);
        Assert.False(DuelLobbyMsg.TryRead(Reader(w, NetOp.DuelLobby), out m));
        new DuelLobbyMsg { Phase = 1, OtherName = "" }.Write(w); w.Put((byte)1);
        Assert.False(DuelLobbyMsg.TryRead(Reader(w, NetOp.DuelLobby), out m));
        w.Reset(); w.Put(NetOp.DuelLobby); w.Put((byte)1); // truncated
        Assert.False(DuelLobbyMsg.TryRead(Reader(w, NetOp.DuelLobby), out m));
    }

    static DuelStateMsg Picking()
    {
        return new DuelStateMsg
        {
            Result = 1, OppName = "Rival", MsLeft = 0, WagerState = 2, PickedCard = DuelStateMsg.NoCard, PickMsLeft = 30000, OppAwayMs = 4500,
            MoveCard = new byte[0], MoveCell = new byte[0], MoveSide = new byte[0], MoveAuto = new byte[0], MoveFlips = new ushort[0], Hand = new byte[0], PickCards = new byte[] { 10, 11, 12, 13, 14, 15, 16, 17 },
        };
    }

    [Fact]
    public void TheStateCarriesTheStakesStateThePickAndTheOpponentAwayTime()
    {
        var w = new NetDataWriter();
        Picking().Write(w);
        DuelStateMsg m; Assert.True(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        Assert.Equal(2, m.WagerState); Assert.Equal(DuelStateMsg.NoCard, m.PickedCard); Assert.Equal(30000, m.PickMsLeft); Assert.Equal(4500, m.OppAwayMs);
        Assert.Equal(new byte[] { 10, 11, 12, 13, 14, 15, 16, 17 }, m.PickCards);
        DuelStateMsg done = Picking(); done.WagerState = 4; done.PickedCard = 13; done.PickCards = new byte[0]; done.Write(w);
        Assert.True(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        Assert.Equal(13, m.PickedCard); Assert.Empty(m.PickCards);
    }

    [Fact]
    public void TheStateRefusesABadStakesStatePickCardOrTooManyPickCards()
    {
        var w = new NetDataWriter();
        DuelStateMsg m;
        DuelStateMsg s = Picking(); s.WagerState = 6; s.Write(w);
        Assert.False(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        s = Picking(); s.PickedCard = 100; s.Write(w);
        Assert.False(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        s = Picking(); s.PickCards[2] = 100; s.Write(w);
        Assert.False(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        w.Reset(); w.Put(NetOp.DuelState); // a hand-built packet with 9 pick cards
        s = Picking(); s.Write(w);
        var raw = new NetDataWriter(); raw.Put(w.Data, 0, w.Length - 9); raw.Put((byte)9); for (int i = 0; i < 9; i++) raw.Put((byte)i);
        var r = new NetDataReader(); r.SetSource(raw.CopyData());
        byte op; Assert.True(NetMessageReader.ReadOpcode(r, out op));
        Assert.False(DuelStateMsg.TryRead(r, out m));
    }

    [Fact]
    public void EveryFailCodeIsAcceptedInANoticeAndOneBeyondTheLastIsNot()
    {
        var w = new NetDataWriter();
        DuelNoticeMsg m;
        new DuelNoticeMsg { Kind = DuelNoticeKind.Refused, Detail = (byte)DuelFailInfo.Last }.Write(w);
        Assert.True(DuelNoticeMsg.TryRead(Reader(w, NetOp.DuelNotice), out m));
        new DuelNoticeMsg { Kind = DuelNoticeKind.Refused, Detail = (byte)((byte)DuelFailInfo.Last + 1) }.Write(w);
        Assert.False(DuelNoticeMsg.TryRead(Reader(w, NetOp.DuelNotice), out m));
    }

    [Fact]
    public void PlaceCarriesTheTurnAndTheCardTheClientSaw()
    {
        var w = new NetDataWriter();
        new DuelRequestMsg { Op = DuelOp.Place, A = 2, B = 9, Turn = 7, Card = 33 }.Write(w);
        DuelRequestMsg m; Assert.True(DuelRequestMsg.TryRead(Reader(w, NetOp.DuelRequest), out m));
        Assert.Equal(7, m.Turn); Assert.Equal(33, m.Card);
    }

    [Fact]
    public void TheRankedFlagsAndTheLpChangeSurviveTheStateAndAnImpossibleFlagIsRefused()
    {
        var w = new NetDataWriter();
        DuelStateMsg s = Picking(); s.WagerState = 0; s.Ranked = true; s.RankNotCounted = true; s.LpChange = -22; s.PickCards = new byte[0];
        s.Write(w);
        DuelStateMsg m; Assert.True(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        Assert.True(m.Ranked); Assert.True(m.RankNotCounted); Assert.Equal(-22, m.LpChange);
        s.Ranked = false; s.RankNotCounted = false; s.LpChange = 27; s.Write(w);
        Assert.True(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        Assert.False(m.Ranked); Assert.False(m.RankNotCounted); Assert.Equal(27, m.LpChange);
        // a hand-built flag byte with bit 32 set is not a state
        s.Write(w);
        var raw = w.CopyData(); raw[1 + 1 + 1] |= 32; // op, result, end reason, flags
        var r = new NetDataReader(); r.SetSource(raw);
        byte op; Assert.True(NetMessageReader.ReadOpcode(r, out op));
        Assert.False(DuelStateMsg.TryRead(r, out m));
    }

    [Fact]
    public void QueueRankedAndLeaveQueueAreRequestsAndThePlayerNeverSendsARating()
    {
        var w = new NetDataWriter();
        foreach (DuelOp op in new[] { DuelOp.QueueRanked, DuelOp.LeaveQueue })
        {
            new DuelRequestMsg { Op = op }.Write(w);
            DuelRequestMsg m; Assert.True(DuelRequestMsg.TryRead(Reader(w, NetOp.DuelRequest), out m));
            Assert.Equal(op, m.Op);
        }
        w.Reset(); w.Put(NetOp.DuelRequest); w.Put((byte)13); w.Put((byte)0); w.Put((byte)0); w.Put((byte)0); w.Put((byte)0); w.Put((ushort)0); w.Put((byte)0);
        DuelRequestMsg bad; Assert.False(DuelRequestMsg.TryRead(Reader(w, NetOp.DuelRequest), out bad)); // the op after SkipScript does not exist
    }

    [Fact]
    public void TheRankedMessageRoundTripsAndRefusesTrailingBytes()
    {
        var w = new NetDataWriter();
        new DuelRankedMsg { Queued = true, WaitSec = 42, Season = 3, MinutesLeft = 43199 }.Write(w);
        DuelRankedMsg m; Assert.True(DuelRankedMsg.TryRead(Reader(w, NetOp.DuelRanked), out m));
        Assert.True(m.Queued); Assert.Equal(42, m.WaitSec); Assert.Equal(3, m.Season); Assert.Equal(43199, m.MinutesLeft);
        w.Put((byte)1);
        Assert.False(DuelRankedMsg.TryRead(Reader(w, NetOp.DuelRanked), out m));
    }
}
