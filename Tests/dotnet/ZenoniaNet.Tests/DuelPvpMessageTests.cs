using LiteNetLib.Utils;
using Xunit;
using ZenoniaProto.Net;

// Phase 06: the PvP duel packets (challenge ops, lobby, wager fields of the state) round-trip, and a hostile or truncated packet is refused.
public class DuelPvpMessageTests
{
    static NetDataReader Reader(NetDataWriter w, byte expectedOp)
    {
        var r = new NetDataReader(); r.SetSource(w.CopyData());
        byte op; Assert.True(NetMessageReader.ReadOpcode(r, out op)); Assert.Equal(expectedOp, op);
        return r;
    }

    [Fact]
    public void ChallengeAndLobbyOpsRoundTripWithTheTargetAndTheWagerFlag()
    {
        var w = new NetDataWriter();
        foreach (DuelOp op in new[] { DuelOp.Challenge, DuelOp.CancelInvite, DuelOp.Accept, DuelOp.Decline, DuelOp.WagerPick, DuelOp.WagerLock, DuelOp.WagerConfirm, DuelOp.LeaveLobby })
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
        var s = new DuelLobbyMsg { Phase = 3, Other = 777, OtherName = "Tay Gan", Wager = true, MyLocked = true, TheirConfirmed = true, MyCard = 5, TheirCard = DuelLobby.NoCard, MsLeft = 54321 };
        s.Write(w);
        DuelLobbyMsg m; Assert.True(DuelLobbyMsg.TryRead(Reader(w, NetOp.DuelLobby), out m));
        Assert.Equal(3, m.Phase); Assert.Equal(777, m.Other); Assert.Equal("Tay Gan", m.OtherName); Assert.True(m.Wager); Assert.True(m.MyLocked); Assert.False(m.TheirLocked); Assert.False(m.MyConfirmed); Assert.True(m.TheirConfirmed);
        Assert.Equal(5, m.MyCard); Assert.Equal(DuelLobby.NoCard, m.TheirCard); Assert.Equal(54321, m.MsLeft);
    }

    [Fact]
    public void LobbyMessageRefusesBadPhaseCardAndTrailingBytes()
    {
        var w = new NetDataWriter();
        DuelLobbyMsg m;
        new DuelLobbyMsg { Phase = 5, MyCard = 255, TheirCard = 255, OtherName = "" }.Write(w);
        Assert.False(DuelLobbyMsg.TryRead(Reader(w, NetOp.DuelLobby), out m));
        new DuelLobbyMsg { Phase = 3, MyCard = 100, TheirCard = 255, OtherName = "" }.Write(w);
        Assert.False(DuelLobbyMsg.TryRead(Reader(w, NetOp.DuelLobby), out m));
        new DuelLobbyMsg { Phase = 1, MyCard = 255, TheirCard = 255, OtherName = "" }.Write(w); w.Put((byte)1);
        Assert.False(DuelLobbyMsg.TryRead(Reader(w, NetOp.DuelLobby), out m));
        w.Reset(); w.Put(NetOp.DuelLobby); w.Put((byte)1); // truncated
        Assert.False(DuelLobbyMsg.TryRead(Reader(w, NetOp.DuelLobby), out m));
    }

    static DuelStateMsg Pvp()
    {
        return new DuelStateMsg
        {
            Npc = DuelStateMsg.PlayerOpponent, Result = 0, OppName = "Rival", YourTurn = true, MsLeft = 12000, WagerMine = 7, WagerTheirs = 20, WagerState = 1, OppAwayMs = 4500,
            MoveCard = new byte[0], MoveCell = new byte[0], MoveSide = new byte[0], MoveAuto = new byte[0], MoveFlips = new ushort[0], Hand = new byte[] { 1, 2, 3, 4, 5 },
        };
    }

    [Fact]
    public void PvpStateCarriesTheWagerCardsStateAndOpponentAwayTime()
    {
        var w = new NetDataWriter();
        Pvp().Write(w);
        DuelStateMsg m; Assert.True(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        Assert.Equal(DuelStateMsg.PlayerOpponent, m.Npc); Assert.Equal(7, m.WagerMine); Assert.Equal(20, m.WagerTheirs); Assert.Equal(1, m.WagerState); Assert.Equal(4500, m.OppAwayMs);
        DuelStateMsg none = Pvp(); none.WagerMine = none.WagerTheirs = DuelStateMsg.NoCard; none.WagerState = 0; none.Write(w);
        Assert.True(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        Assert.Equal(DuelStateMsg.NoCard, m.WagerMine);
    }

    [Fact]
    public void PvpStateRefusesABadWagerStateOrCard()
    {
        var w = new NetDataWriter();
        DuelStateMsg m;
        DuelStateMsg s = Pvp(); s.WagerState = 5; s.Write(w);
        Assert.False(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
        s = Pvp(); s.WagerMine = 100; s.Write(w);
        Assert.False(DuelStateMsg.TryRead(Reader(w, NetOp.DuelState), out m));
    }

    [Fact]
    public void EveryNewFailCodeIsAcceptedInANoticeAndOneBeyondTheLastIsNot()
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
}
