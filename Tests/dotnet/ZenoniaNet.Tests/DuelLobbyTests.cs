using Xunit;
using ZenoniaProto.Net;

/// <summary>Phase 06 v2: the challenge flow between two players (invite, accept / decline / cancel). Pure state machine, time is passed in. There is no card picking before a duel any more: Accept starts the match.</summary>
public class DuelLobbyTests
{
    const ushort A = 1, B = 2, C = 3;
    readonly DuelLobby lobby = new DuelLobby();

    [Fact]
    public void AnInviteIsSeenAsOutgoingByTheInviterAndIncomingByTheReceiverWithTheStakesFlag()
    {
        Assert.Equal(LobbyFail.None, lobby.Invite(A, B, true, 1000));
        LobbyView a = lobby.ViewOf(A, 1000), b = lobby.ViewOf(B, 1000);
        Assert.Equal(LobbyPhase.Outgoing, a.Phase); Assert.Equal(LobbyPhase.Incoming, b.Phase);
        Assert.True(a.Stakes); Assert.True(b.Stakes); // the receiver reads "winner takes 1 card" before Accept
        Assert.Equal(B, a.Other); Assert.Equal(A, b.Other);
        Assert.Equal(15000, b.MsLeft);
        Assert.Contains(A, lobby.Changed); Assert.Contains(B, lobby.Changed);
    }

    [Fact]
    public void WithTheSwitchOffTheInviteCarriesNoStakes()
    {
        lobby.Invite(A, B, false, 0);
        Assert.False(lobby.ViewOf(B, 0).Stakes);
    }

    [Fact]
    public void AcceptingStartsTheMatchAtOnce()
    {
        lobby.Invite(A, B, true, 1000);
        Assert.Equal(LobbyFail.None, lobby.Accept(B));
        LobbyStart s = Assert.Single(lobby.Starts);
        Assert.Equal(A, s.A); Assert.Equal(B, s.B); Assert.True(s.Stakes);
        Assert.Equal(LobbyPhase.Starting, lobby.PhaseOf(A));
        lobby.Finish(A);
        Assert.False(lobby.IsBusy(A)); Assert.False(lobby.IsBusy(B));
    }

    [Fact]
    public void OnePendingInvitePerPlayerAndNoSelfInvite()
    {
        Assert.Equal(LobbyFail.Self, lobby.Invite(A, A, false, 0));
        lobby.Invite(A, B, false, 0);
        Assert.Equal(LobbyFail.Busy, lobby.Invite(C, B, false, 0)); // B already has one
        Assert.Equal(LobbyFail.Busy, lobby.Invite(A, C, false, 0)); // A already sent one
        Assert.Equal(LobbyFail.Busy, lobby.Invite(B, C, false, 0));
    }

    [Fact]
    public void DeclineEndsItAndTellsTheInviterAndTheCooldownHolds30Seconds()
    {
        lobby.Invite(A, B, false, 1000);
        Assert.Equal(LobbyFail.None, lobby.Decline(B));
        LobbyEnd e = Assert.Single(lobby.Ends);
        Assert.Equal(A, e.To); Assert.Equal(LobbyFail.Declined, e.Why);
        Assert.Equal(LobbyFail.Cooldown, lobby.Invite(A, B, false, 20000));
        Assert.Equal(LobbyFail.None, lobby.Invite(A, B, false, 31001));
        Assert.Equal(LobbyFail.None, lobby.Decline(B));
        Assert.Equal(LobbyFail.None, lobby.Invite(A, C, false, 31002)); // another player is no cooldown
    }

    [Fact]
    public void CancelByTheInviterFreesThemToInviteAgainAtOnce()
    {
        lobby.Invite(A, B, false, 1000);
        Assert.Equal(LobbyFail.NoInvite, lobby.Cancel(B)); // only the inviter cancels
        Assert.Equal(LobbyFail.None, lobby.Cancel(A));
        Assert.Equal(LobbyFail.Cancelled, Assert.Single(lobby.Ends).Why);
        Assert.Equal(LobbyFail.None, lobby.Invite(A, B, true, 1500));
    }

    [Fact]
    public void AnUnansweredInviteExpiresAfter15SecondsForBoth()
    {
        lobby.Invite(A, B, false, 1000);
        lobby.Tick(15999);
        Assert.True(lobby.IsBusy(A));
        lobby.Tick(16000);
        Assert.False(lobby.IsBusy(A)); Assert.False(lobby.IsBusy(B));
        Assert.Equal(2, lobby.Ends.Count);
        Assert.All(lobby.Ends, e => Assert.Equal(LobbyFail.Expired, e.Why));
    }

    [Fact]
    public void OnlyTheReceiverAccepts()
    {
        lobby.Invite(A, B, false, 0);
        Assert.Equal(LobbyFail.NoInvite, lobby.Accept(A));
        Assert.Equal(LobbyFail.NoInvite, lobby.Accept(C));
        Assert.Empty(lobby.Starts);
    }

    [Fact]
    public void LeavingTheInviteEndsItForTheOtherPlayerToo()
    {
        lobby.Invite(A, B, true, 0);
        Assert.Equal(LobbyFail.None, lobby.Leave(A));
        LobbyEnd e = Assert.Single(lobby.Ends);
        Assert.Equal(B, e.To); Assert.Equal(LobbyFail.Left, e.Why);
        Assert.False(lobby.IsBusy(B));
        Assert.Equal(LobbyFail.NoLobby, lobby.Leave(B));
    }

    [Fact]
    public void AStartedLobbyCannotBeLeftWhileTheServerHoldsTheDecks()
    {
        lobby.Invite(A, B, true, 0);
        lobby.Accept(B);
        Assert.Equal(LobbyFail.Busy, lobby.Leave(A));
        Assert.Equal(LobbyPhase.Starting, lobby.PhaseOf(A));
        lobby.Tick(10_000_000); // no expiry while starting
        Assert.Equal(LobbyPhase.Starting, lobby.PhaseOf(A));
    }
}
