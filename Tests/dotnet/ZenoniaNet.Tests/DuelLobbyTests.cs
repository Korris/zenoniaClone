using System.Linq;
using Xunit;
using ZenoniaProto.Net;

/// <summary>Phase 06: the challenge flow between two players (invite, accept / decline / cancel, the Card Wager panel). Pure state machine, time is passed in.</summary>
public class DuelLobbyTests
{
    const ushort A = 1, B = 2, C = 3;
    readonly DuelLobby lobby = new DuelLobby();

    void Reach(bool wager)
    {
        Assert.Equal(LobbyFail.None, lobby.Invite(A, B, wager, 1000));
        Assert.Equal(LobbyFail.None, lobby.Accept(B, 2000));
    }

    [Fact]
    public void AnInviteIsSeenAsOutgoingByTheInviterAndIncomingByTheReceiverWithTheWagerFlag()
    {
        Assert.Equal(LobbyFail.None, lobby.Invite(A, B, true, 1000));
        LobbyView a = lobby.ViewOf(A, 1000), b = lobby.ViewOf(B, 1000);
        Assert.Equal(LobbyPhase.Outgoing, a.Phase); Assert.Equal(LobbyPhase.Incoming, b.Phase);
        Assert.True(a.Wager); Assert.True(b.Wager); // the receiver sees "Card Wager" before Accept
        Assert.Equal(B, a.Other); Assert.Equal(A, b.Other);
        Assert.Equal(15000, b.MsLeft);
        Assert.Contains(A, lobby.Changed); Assert.Contains(B, lobby.Changed);
    }

    [Fact]
    public void ThePlainInviterChoiceIsKept()
    {
        lobby.Invite(A, B, false, 0);
        Assert.False(lobby.ViewOf(B, 0).Wager);
    }

    [Fact]
    public void AcceptingAPlainInviteStartsTheMatchAtOnce()
    {
        Reach(false);
        LobbyStart s = Assert.Single(lobby.Starts);
        Assert.Equal(A, s.A); Assert.Equal(B, s.B); Assert.False(s.Wager);
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
        Assert.Equal(LobbyFail.NoInvite, lobby.Accept(A, 0));
        Assert.Equal(LobbyFail.NoInvite, lobby.Accept(C, 0));
        Assert.Empty(lobby.Starts);
    }

    [Fact]
    public void AWagerAcceptOpensThePickPanelAndStartsNothingYet()
    {
        Reach(true);
        Assert.Empty(lobby.Starts);
        Assert.Equal(LobbyPhase.Pick, lobby.PhaseOf(A)); Assert.Equal(LobbyPhase.Pick, lobby.PhaseOf(B));
        Assert.Equal(DuelLobby.NoCard, lobby.ViewOf(A, 2000).MyCard);
    }

    [Fact]
    public void PickLockConfirmStartsTheMatchWithBothCards()
    {
        Reach(true);
        Assert.Equal(LobbyFail.None, lobby.Pick(A, 5)); Assert.Equal(LobbyFail.None, lobby.Pick(B, 9));
        Assert.Equal(9, lobby.ViewOf(A, 2000).TheirCard); // both look at both cards
        Assert.Equal(LobbyFail.None, lobby.Lock(A)); Assert.Equal(LobbyFail.None, lobby.Lock(B));
        Assert.Equal(LobbyFail.None, lobby.Confirm(A));
        Assert.Empty(lobby.Starts);
        Assert.True(lobby.ViewOf(B, 2000).TheirConfirmed);
        Assert.Equal(LobbyFail.None, lobby.Confirm(B));
        LobbyStart s = Assert.Single(lobby.Starts);
        Assert.True(s.Wager); Assert.Equal(5, s.CardA); Assert.Equal(9, s.CardB);
        Assert.Equal(LobbyPhase.Starting, lobby.PhaseOf(B));
    }

    [Fact]
    public void LockNeedsAPickAndConfirmNeedsBothLocks()
    {
        Reach(true);
        Assert.Equal(LobbyFail.NoPick, lobby.Lock(A));
        lobby.Pick(A, 5);
        Assert.Equal(LobbyFail.NotLocked, lobby.Confirm(A));
        lobby.Lock(A);
        Assert.Equal(LobbyFail.NotLocked, lobby.Confirm(A)); // B has not locked
        Assert.Equal(LobbyFail.NoPick, lobby.Pick(A, DuelLobby.NoCard));
    }

    [Fact]
    public void ChangingACardClearsYourLockAndBothConfirmations()
    {
        Reach(true);
        lobby.Pick(A, 5); lobby.Pick(B, 9); lobby.Lock(A); lobby.Lock(B); lobby.Confirm(A);
        lobby.Pick(B, 11); // B changes their mind after A confirmed
        LobbyView a = lobby.ViewOf(A, 2000), b = lobby.ViewOf(B, 2000);
        Assert.False(a.MyConfirmed); Assert.False(b.TheirConfirmed); Assert.False(b.MyLocked);
        Assert.True(a.MyLocked); // A's own card did not change: still locked
        Assert.Equal(LobbyFail.NotLocked, lobby.Confirm(B)); // B must lock the new card again
        lobby.Lock(B);
        Assert.Equal(LobbyFail.None, lobby.Confirm(B));
        Assert.Empty(lobby.Starts); // A has to confirm again, after seeing the new card
        Assert.Equal(LobbyFail.None, lobby.Confirm(A));
        Assert.Equal(11, Assert.Single(lobby.Starts).CardB);
    }

    [Fact]
    public void MismatchedRaritiesCannotBeConfirmed()
    {
        lobby.CardsMatch = (x, y) => x / 10 == y / 10;
        Reach(true);
        lobby.Pick(A, 5); lobby.Pick(B, 25); lobby.Lock(A); lobby.Lock(B);
        Assert.Equal(LobbyFail.Mismatch, lobby.Confirm(A));
        Assert.Empty(lobby.Starts);
        lobby.Pick(B, 6); lobby.Lock(B);
        Assert.Equal(LobbyFail.None, lobby.Confirm(A));
    }

    [Fact]
    public void LeavingThePanelEndsItForTheOtherPlayerToo()
    {
        Reach(true);
        Assert.Equal(LobbyFail.None, lobby.Leave(A));
        LobbyEnd e = Assert.Single(lobby.Ends);
        Assert.Equal(B, e.To); Assert.Equal(LobbyFail.Left, e.Why);
        Assert.False(lobby.IsBusy(B));
        Assert.Equal(LobbyFail.NoLobby, lobby.Pick(B, 3));
    }

    [Fact]
    public void AStartedLobbyCannotBeLeftWhileTheServerHoldsTheCards()
    {
        Reach(true);
        lobby.Pick(A, 1); lobby.Pick(B, 2); lobby.Lock(A); lobby.Lock(B); lobby.Confirm(A); lobby.Confirm(B);
        Assert.Equal(LobbyFail.Busy, lobby.Leave(A));
        Assert.Equal(LobbyPhase.Starting, lobby.PhaseOf(A));
        lobby.Tick(10_000_000); // no expiry while starting
        Assert.Equal(LobbyPhase.Starting, lobby.PhaseOf(A));
    }

    [Fact]
    public void ThePickPanelExpiresAfterOneMinute()
    {
        Reach(true);
        lobby.Tick(2000 + DuelLobby.PickMs - 1);
        Assert.True(lobby.IsBusy(A));
        lobby.Tick(2000 + DuelLobby.PickMs);
        Assert.False(lobby.IsBusy(A));
        Assert.Equal(2, lobby.Ends.Count(e => e.Why == LobbyFail.Expired));
    }

    [Fact]
    public void ARepeatedPickOfTheSameCardStillResetsTheConfirmations()
    {
        Reach(true);
        lobby.Pick(A, 5); lobby.Pick(B, 9); lobby.Lock(A); lobby.Lock(B); lobby.Confirm(A);
        lobby.Pick(A, 5); // nothing changed in the card, but a pick is a pick: nobody may confirm what they did not see settle
        Assert.False(lobby.ViewOf(B, 2000).TheirConfirmed);
        Assert.False(lobby.ViewOf(A, 2000).MyLocked);
    }
}
