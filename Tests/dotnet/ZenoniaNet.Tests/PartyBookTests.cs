using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenoniaProto.Net;

public class PartyBookTests
{
    const ushort A = 1, B = 2, C = 3, D = 4, E = 5, F = 6, G = 7;

    static PartyBook Book()
    {
        var b = new PartyBook();
        foreach (ushort id in new[] { A, B, C, D, E, F, G }) b.AddPlayer(id, "P" + id);
        return b;
    }

    static PartyFail Do(PartyBook b, ushort from, PartyOp op, ushort target = 0, long now = 0)
    {
        return b.Apply(from, new PartyRequestMsg { Op = op, Target = target }, now);
    }

    static void Join(PartyBook b, ushort inviter, ushort invitee)
    {
        Assert.Equal(PartyFail.None, Do(b, inviter, PartyOp.Invite, invitee));
        Assert.Equal(PartyFail.None, Do(b, invitee, PartyOp.Accept, inviter));
        b.Notes.Clear();
    }

    static List<PartyNoticeMsg> Take(PartyBook b, ushort to)
    {
        return b.Notes.Where(n => n.To == to).Select(n => n.Msg).ToList();
    }

    static bool Has(PartyBook b, ushort to, PartyNoticeKind kind, byte detail = 0)
    {
        return Take(b, to).Any(m => m.Kind == kind && m.Detail == detail);
    }

    [Fact]
    public void InviteTellsBothSidesWithTheTimeToAnswer()
    {
        PartyBook b = Book();
        Assert.Equal(PartyFail.None, Do(b, A, PartyOp.Invite, B));
        PartyNoticeMsg sent = Take(b, A).Single(), got = Take(b, B).Single();
        Assert.Equal(PartyNoticeKind.InviteOut, sent.Kind); Assert.Equal(B, sent.Other); Assert.Equal("P2", sent.Name); Assert.Equal(30000, sent.MsLeft);
        Assert.Equal(PartyNoticeKind.InviteIn, got.Kind); Assert.Equal(A, got.Other); Assert.Equal("P1", got.Name); Assert.Equal(30000, got.MsLeft);
        Assert.Equal(0, b.PartyIdOf(A)); Assert.Equal(0, b.PartyIdOf(B));
    }

    [Fact]
    public void TheInviteeLearnsTheInvitersClass()
    {
        var b = new PartyBook();
        b.AddPlayer(A, "Ly", 4); b.AddPlayer(B, "Tay", 1);
        Do(b, A, PartyOp.Invite, B);
        var s = new PartyClientState { MyId = B };
        s.Apply(Take(b, B).Single(), 0.0);
        Assert.Equal(4, s.Incoming.Value.HeroClass);
    }

    [Fact]
    public void AcceptMakesTheInviterLeader()
    {
        PartyBook b = Book();
        Do(b, A, PartyOp.Invite, B); b.Notes.Clear();
        Assert.Equal(PartyFail.None, Do(b, B, PartyOp.Accept, A));
        Assert.True(Has(b, A, PartyNoticeKind.OutClosed, (byte)PartyInviteEnd.Accepted));
        Assert.True(Has(b, B, PartyNoticeKind.InClosed, (byte)PartyInviteEnd.Accepted));
        Assert.True(b.SameParty(A, B));
        Assert.Equal(new ushort[] { A, B }, b.MembersOf(B).ToArray());
        Assert.True(b.TakeRosterChanged()); Assert.False(b.TakeRosterChanged());
    }

    [Fact]
    public void AJoinerIsAnnouncedToTheMembersBesideTheInviter()
    {
        PartyBook b = Book();
        Join(b, A, B);
        Do(b, A, PartyOp.Invite, C); b.Notes.Clear();
        Do(b, C, PartyOp.Accept, A);
        Assert.True(Has(b, B, PartyNoticeKind.MemberJoined));
        Assert.False(Has(b, A, PartyNoticeKind.MemberJoined)); // told by its own invite closing
        Assert.Equal(new ushort[] { A, B, C }, b.MembersOf(A).ToArray());
    }

    [Fact]
    public void DeclineClosesItWithoutAParty()
    {
        PartyBook b = Book();
        Do(b, A, PartyOp.Invite, B); b.Notes.Clear();
        Assert.Equal(PartyFail.None, Do(b, B, PartyOp.Decline, A));
        Assert.True(Has(b, A, PartyNoticeKind.OutClosed, (byte)PartyInviteEnd.Declined));
        Assert.True(Has(b, B, PartyNoticeKind.InClosed, (byte)PartyInviteEnd.Declined));
        Assert.False(b.HasOpenInvite(A)); Assert.False(b.HasOpenInvite(B)); Assert.Equal(0, b.PartyIdOf(A));
    }

    [Fact]
    public void CancelClosesTheInviteesWindowAtOnce()
    {
        PartyBook b = Book();
        Do(b, A, PartyOp.Invite, B); b.Notes.Clear();
        Assert.Equal(PartyFail.None, Do(b, A, PartyOp.CancelInvite));
        Assert.True(Has(b, B, PartyNoticeKind.InClosed, (byte)PartyInviteEnd.Cancelled));
        Assert.True(Has(b, A, PartyNoticeKind.OutClosed, (byte)PartyInviteEnd.Cancelled));
        Assert.False(b.HasOpenInvite(B));
    }

    [Fact]
    public void AnAnswerCrossingACancelFindsNothingAndChangesNothing()
    {
        PartyBook b = Book();
        Do(b, A, PartyOp.Invite, B);
        Do(b, A, PartyOp.CancelInvite); b.Notes.Clear();
        Assert.Equal(PartyFail.NoInvite, Do(b, B, PartyOp.Accept, A));
        Assert.True(Has(b, B, PartyNoticeKind.Failed, (byte)PartyFail.NoInvite));
        Assert.Equal(0, b.PartyIdOf(B));
    }

    [Fact]
    public void AnAnswerToAnOldInviterDoesNotLandOnTheNewInvite()
    {
        PartyBook b = Book();
        Do(b, A, PartyOp.Invite, B);
        Do(b, A, PartyOp.CancelInvite);
        Do(b, C, PartyOp.Invite, B);
        Assert.Equal(PartyFail.NoInvite, Do(b, B, PartyOp.Accept, A));
        Assert.True(b.HasOpenInvite(B)); Assert.Equal(0, b.PartyIdOf(B));
        Assert.Equal(PartyFail.None, Do(b, B, PartyOp.Accept, C));
    }

    [Fact]
    public void OnlyTheInviterCancelsAndOnlyTheInviteeAnswers()
    {
        PartyBook b = Book();
        Do(b, A, PartyOp.Invite, B);
        Assert.Equal(PartyFail.NoInvite, Do(b, B, PartyOp.CancelInvite));
        Assert.Equal(PartyFail.NoInvite, Do(b, A, PartyOp.Accept, A));
        Assert.Equal(PartyFail.NoInvite, Do(b, C, PartyOp.Accept, A));
        Assert.Equal(PartyFail.NoInvite, Do(b, C, PartyOp.Decline, A));
        Assert.True(b.HasOpenInvite(A));
    }

    [Fact]
    public void AnInviteExpiresAfterThirtySecondsForBothSides()
    {
        PartyBook b = Book();
        Do(b, A, PartyOp.Invite, B, 1000); b.Notes.Clear();
        b.Tick(30999);
        Assert.Empty(b.Notes); Assert.True(b.HasOpenInvite(A));
        b.Tick(31000);
        Assert.True(Has(b, A, PartyNoticeKind.OutClosed, (byte)PartyInviteEnd.Expired));
        Assert.True(Has(b, B, PartyNoticeKind.InClosed, (byte)PartyInviteEnd.Expired));
        Assert.False(b.HasOpenInvite(A));
        Assert.Equal(PartyFail.NoInvite, Do(b, B, PartyOp.Accept, A, 31001));
    }

    [Fact]
    public void DoubleInviteAndOneOpenInviteAtATime()
    {
        PartyBook b = Book();
        Do(b, A, PartyOp.Invite, B); b.Notes.Clear();
        Assert.Equal(PartyFail.AlreadyInvited, Do(b, A, PartyOp.Invite, B));
        Assert.True(Has(b, A, PartyNoticeKind.Failed, (byte)PartyFail.AlreadyInvited));
        Assert.Equal(PartyFail.AlreadyPending, Do(b, A, PartyOp.Invite, C));
        Assert.Equal(PartyFail.TargetBusy, Do(b, C, PartyOp.Invite, B)); // B is answering A
        Assert.Equal(PartyFail.TargetBusy, Do(b, C, PartyOp.Invite, A)); // A is waiting on B
        Assert.Equal(PartyFail.AnswerFirst, Do(b, B, PartyOp.Invite, C));
    }

    [Fact]
    public void InvitesThatCannotBe()
    {
        PartyBook b = Book();
        Assert.Equal(PartyFail.Self, Do(b, A, PartyOp.Invite, A));
        Assert.Equal(PartyFail.NoSuchPlayer, Do(b, A, PartyOp.Invite, 999));
        Join(b, A, B);
        Assert.Equal(PartyFail.TargetInParty, Do(b, C, PartyOp.Invite, A));
        Assert.Equal(PartyFail.TargetInParty, Do(b, A, PartyOp.Invite, B));
        Assert.Equal(PartyFail.NotLeader, Do(b, B, PartyOp.Invite, C));
    }

    [Fact]
    public void AFullPartyTakesNobodyMore()
    {
        PartyBook b = Book();
        Join(b, A, B); Join(b, A, C); Join(b, A, D); Join(b, A, E);
        Assert.Equal(PartyBook.MaxMembers, b.MembersOf(A).Count);
        Assert.Equal(PartyFail.PartyFull, Do(b, A, PartyOp.Invite, F));
        Assert.True(Has(b, A, PartyNoticeKind.Failed, (byte)PartyFail.PartyFull));
        Assert.False(b.HasOpenInvite(F));
    }

    [Fact]
    public void LeavingTellsTheRestAndTheLeaverIsFree()
    {
        PartyBook b = Book();
        Join(b, A, B); Join(b, A, C);
        Assert.Equal(PartyFail.None, Do(b, C, PartyOp.Leave));
        Assert.True(Has(b, C, PartyNoticeKind.YouLeft));
        Assert.True(Has(b, A, PartyNoticeKind.MemberLeft)); Assert.True(Has(b, B, PartyNoticeKind.MemberLeft));
        Assert.Equal(0, b.PartyIdOf(C));
        Assert.Equal(new ushort[] { A, B }, b.MembersOf(A).ToArray());
        Assert.Equal(PartyFail.NotInParty, Do(b, C, PartyOp.Leave));
    }

    [Fact]
    public void TheLeaderKicksAndNobodyElseDoes()
    {
        PartyBook b = Book();
        Join(b, A, B); Join(b, A, C);
        Assert.Equal(PartyFail.NotLeader, Do(b, B, PartyOp.Kick, C));
        Assert.Equal(PartyFail.Self, Do(b, A, PartyOp.Kick, A));
        Assert.Equal(PartyFail.NotMember, Do(b, A, PartyOp.Kick, D));
        Assert.Equal(PartyFail.NotInParty, Do(b, D, PartyOp.Kick, A));
        b.Notes.Clear();
        Assert.Equal(PartyFail.None, Do(b, A, PartyOp.Kick, C));
        Assert.True(Has(b, C, PartyNoticeKind.YouWereKicked));
        Assert.True(Has(b, B, PartyNoticeKind.MemberKicked)); Assert.True(Has(b, A, PartyNoticeKind.MemberKicked));
        Assert.False(b.SameParty(A, C));
    }

    [Fact]
    public void TheLeadPassesToTheLongestServingMemberWhenTheLeaderLeaves()
    {
        PartyBook b = Book();
        Join(b, A, B); Join(b, A, C);
        Assert.Equal(PartyFail.None, Do(b, A, PartyOp.Leave));
        Assert.Equal(new ushort[] { B, C }, b.MembersOf(B).ToArray());
        Assert.Equal(B, Take(b, C).Single(m => m.Kind == PartyNoticeKind.LeaderChanged).Other);
        Assert.Contains(Take(b, B), m => m.Kind == PartyNoticeKind.LeaderChanged);
        Assert.Equal(PartyFail.None, Do(b, B, PartyOp.Kick, C)); // the new leader's powers work
    }

    [Fact]
    public void ADepartingLeaderTakesItsOpenInviteAlong()
    {
        PartyBook b = Book();
        Join(b, A, B); Join(b, A, C);
        Do(b, A, PartyOp.Invite, D); b.Notes.Clear();
        Do(b, A, PartyOp.Leave);
        Assert.True(Has(b, D, PartyNoticeKind.InClosed, (byte)PartyInviteEnd.Cancelled));
        Assert.False(b.HasOpenInvite(D));
    }

    [Fact]
    public void DisconnectRemovesTheMemberAndHandsTheLeadOn()
    {
        PartyBook b = Book();
        Join(b, A, B); Join(b, A, C);
        b.RemovePlayer(A);
        Assert.Equal(new ushort[] { B, C }, b.MembersOf(C).ToArray());
        Assert.True(Has(b, B, PartyNoticeKind.MemberLeft)); Assert.True(Has(b, C, PartyNoticeKind.LeaderChanged));
        Assert.Equal(PartyFail.NotMember, Do(b, B, PartyOp.Kick, A));
        Assert.Equal(PartyFail.NoSuchPlayer, Do(b, D, PartyOp.Invite, A));
    }

    [Fact]
    public void DisconnectCancelsPendingInvitesBothWays()
    {
        PartyBook b = Book();
        Do(b, A, PartyOp.Invite, B); Do(b, C, PartyOp.Invite, D); b.Notes.Clear();
        b.RemovePlayer(A); // inviter gone
        Assert.True(Has(b, B, PartyNoticeKind.InClosed, (byte)PartyInviteEnd.Offline));
        Assert.False(b.HasOpenInvite(B));
        b.Notes.Clear();
        b.RemovePlayer(D); // invitee gone
        Assert.True(Has(b, C, PartyNoticeKind.OutClosed, (byte)PartyInviteEnd.Offline));
        Assert.False(b.HasOpenInvite(C));
    }

    [Fact]
    public void ThePartyEndsWhenOneMemberRemains()
    {
        foreach (System.Action<PartyBook> lose in new System.Action<PartyBook>[] { x => Do(x, B, PartyOp.Leave), x => Do(x, A, PartyOp.Kick, B), x => x.RemovePlayer(B), x => Do(x, A, PartyOp.Leave) })
        {
            PartyBook b = Book();
            Join(b, A, B);
            lose(b);
            Assert.Equal(0, b.PartyIdOf(A)); Assert.Equal(0, b.PartyIdOf(B)); Assert.False(b.SameParty(A, B));
            Assert.Empty(b.MembersOf(A));
            Assert.Contains(b.Notes, n => n.Msg.Kind == PartyNoticeKind.Disbanded);
            Assert.True(b.TakeRosterChanged());
        }
    }

    [Fact]
    public void ALeaderWhosePartyEndedCanStillBeAcceptedByItsInvitee()
    {
        PartyBook b = Book();
        Join(b, A, B);
        Do(b, A, PartyOp.Invite, C);
        Do(b, B, PartyOp.Leave); // party over, A is alone, the invite stands
        Assert.Equal(PartyFail.None, Do(b, C, PartyOp.Accept, A));
        Assert.Equal(new ushort[] { A, C }, b.MembersOf(C).ToArray());
    }

    [Fact]
    public void PlayersAreOnlyAlliesOfTheirOwnParty()
    {
        PartyBook b = Book();
        Join(b, A, B); Join(b, C, D);
        Assert.True(b.SameParty(A, B)); Assert.True(b.SameParty(C, D));
        Assert.False(b.SameParty(A, C)); Assert.False(b.SameParty(A, E)); Assert.False(b.SameParty(E, F));
        Assert.False(b.SameParty(0, 0)); // a dev host's own hero has no party
        Assert.NotEqual(b.PartyIdOf(A), b.PartyIdOf(C));
    }

    [Fact]
    public void EveryNoticeHasAToastExceptTheInviteWindow()
    {
        var kinds = new[] { PartyNoticeKind.InviteOut, PartyNoticeKind.MemberJoined, PartyNoticeKind.MemberLeft, PartyNoticeKind.MemberKicked, PartyNoticeKind.YouLeft, PartyNoticeKind.YouWereKicked, PartyNoticeKind.LeaderChanged, PartyNoticeKind.Disbanded };
        foreach (PartyNoticeKind k in kinds) Assert.NotEqual("", PartyText.Of(new PartyNoticeMsg { Kind = k, Name = "Tay" }));
        Assert.Equal("", PartyText.Of(new PartyNoticeMsg { Kind = PartyNoticeKind.InviteIn, Name = "Tay" }));
        for (byte e = 0; e <= (byte)PartyInviteEnd.Offline; e++)
        {
            Assert.Contains("Tay", PartyText.Of(new PartyNoticeMsg { Kind = PartyNoticeKind.OutClosed, Detail = e, Name = "Tay" }));
            Assert.Contains("Tay", PartyText.Of(new PartyNoticeMsg { Kind = PartyNoticeKind.InClosed, Detail = e, Name = "Tay" }));
        }
        for (byte f = 1; f <= (byte)PartyFail.NotMember; f++) Assert.NotEqual("", PartyText.Of(new PartyNoticeMsg { Kind = PartyNoticeKind.Failed, Detail = f, Name = "Tay" }));
        Assert.True(PartyText.IsBad(new PartyNoticeMsg { Kind = PartyNoticeKind.OutClosed, Detail = (byte)PartyInviteEnd.Declined }));
        Assert.False(PartyText.IsBad(new PartyNoticeMsg { Kind = PartyNoticeKind.OutClosed, Detail = (byte)PartyInviteEnd.Accepted }));
    }
}

public class PartyExpTests
{
    [Theory]
    [InlineData(100, 1, 100)]  // alone: no bonus
    [InlineData(100, 2, 120)]
    [InlineData(100, 3, 130)]
    [InlineData(100, 5, 150)]
    [InlineData(7, 2, 8)]      // rounded down
    [InlineData(0, 3, 0)]
    public void ThePotGrowsTenPercentPerMember(int exp, int members, int pot)
    {
        Assert.Equal(pot, PartyExp.Pot(exp, members));
    }

    [Fact]
    public void AnEvenSplitAddsUpToThePotWithTheRestOnTheFirst()
    {
        Assert.Equal(new[] { 60, 60 }, PartyExp.Split(100, 2));
        Assert.Equal(new[] { 44, 43, 43 }, PartyExp.Split(100, 3));
        Assert.Equal(new[] { 30, 30, 30, 30, 30 }, PartyExp.Split(100, 5));
        Assert.Equal(new[] { 100 }, PartyExp.Split(100, 1));
        Assert.Equal(new[] { 0, 0 }, PartyExp.Split(0, 2));
        Assert.Empty(PartyExp.Split(100, 0));
        for (int exp = 1; exp < 300; exp++)
            for (int n = 1; n <= PartyBook.MaxMembers; n++)
                Assert.Equal(PartyExp.Pot(exp, n), PartyExp.Split(exp, n).Sum());
    }

    [Fact]
    public void AHugeRewardDoesNotOverflow()
    {
        Assert.Equal(1500000000, PartyExp.Pot(1000000000, 5));
    }

    [Fact]
    public void RangeIsFourHundredUnits()
    {
        Assert.True(PartyExp.InRange(400f, 0f)); Assert.True(PartyExp.InRange(240f, 320f));
        Assert.False(PartyExp.InRange(400.5f, 0f)); Assert.False(PartyExp.InRange(300f, 300f));
    }
}

public class PartyClientStateTests
{
    [Fact]
    public void AnInviteOpensAndClosesOnTheServersWordOnly()
    {
        var s = new PartyClientState { MyId = 1 };
        s.Apply(new PartyNoticeMsg { Kind = PartyNoticeKind.InviteIn, Other = 2, Name = "Tay", MsLeft = 30000 }, 1000.0);
        Assert.True(s.Incoming.HasValue); Assert.Equal(2, s.Incoming.Value.Other); Assert.Equal(31000.0, s.Incoming.Value.ExpiresAtMs);
        s.Apply(new PartyNoticeMsg { Kind = PartyNoticeKind.Failed, Detail = (byte)PartyFail.NoInvite }, 2000.0);
        Assert.True(s.Incoming.HasValue);
        s.Apply(new PartyNoticeMsg { Kind = PartyNoticeKind.InClosed, Detail = (byte)PartyInviteEnd.Cancelled }, 2000.0);
        Assert.False(s.Incoming.HasValue);
        s.Apply(new PartyNoticeMsg { Kind = PartyNoticeKind.InviteOut, Other = 3, Name = "Ly", MsLeft = 30000 }, 0.0);
        Assert.Equal("Ly", s.Outgoing.Value.Name);
        s.Apply(new PartyNoticeMsg { Kind = PartyNoticeKind.OutClosed, Detail = (byte)PartyInviteEnd.Expired }, 0.0);
        Assert.False(s.Outgoing.HasValue);
    }

    [Fact]
    public void TheRosterDecidesWhoIsAnAllyAndWhoLeads()
    {
        var s = new PartyClientState { MyId = 2 };
        Assert.False(s.InParty); Assert.False(s.IsLeader); Assert.False(s.SameParty(2, 2));
        int v = s.Version;
        s.Apply(new PartyRosterMsg { Members = new[] { new PartyMemberInfo { PlayerId = 1 }, new PartyMemberInfo { PlayerId = 2 } } });
        Assert.True(s.Version > v);
        Assert.True(s.InParty); Assert.False(s.IsLeader);
        Assert.True(s.SameParty(1, 2)); Assert.False(s.SameParty(1, 3)); Assert.False(s.SameParty(3, 4));
        s.Apply(new PartyRosterMsg { Members = new[] { new PartyMemberInfo { PlayerId = 2 }, new PartyMemberInfo { PlayerId = 3 } } });
        Assert.True(s.IsLeader);
        s.Apply(new PartyRosterMsg { Members = new PartyMemberInfo[0] });
        Assert.False(s.InParty); Assert.False(s.Contains(2));
    }

    [Fact]
    public void ClearForgetsEverything()
    {
        var s = new PartyClientState { MyId = 2 };
        s.Apply(new PartyNoticeMsg { Kind = PartyNoticeKind.InviteIn, Other = 9, Name = "x", MsLeft = 1 }, 0.0);
        s.Apply(new PartyRosterMsg { Members = new[] { new PartyMemberInfo { PlayerId = 2 } } });
        s.Clear();
        Assert.False(s.InParty); Assert.False(s.Incoming.HasValue); Assert.Equal(0, s.MyId);
    }
}
