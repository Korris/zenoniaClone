using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenoniaProto.Net;

/// <summary>The zone's write queue against a scripted API: order, retries, conflicts, nothing ever dropped silently, and that a trade waits for a definite answer.</summary>
public class CharacterSaveQueueTests
{
    sealed class Call
    {
        public string Kind, CharacterId, Other, State, Id;
        public int Rev, RevB;
        public Action<ZoneApiReply> Done;
    }

    sealed class FakeApi : IZoneApi
    {
        public readonly List<Call> Calls = new List<Call>();
        public Call Last => Calls[Calls.Count - 1];
        public void Save(string c, string s, int rev, string json, Action<ZoneApiReply> done) { Calls.Add(new Call { Kind = "save", CharacterId = c, Rev = rev, State = json, Done = done }); }
        public void CommitTrade(string id, TradeCommitSide a, TradeCommitSide b, Action<ZoneApiReply> done) { Calls.Add(new Call { Kind = "trade", Id = id, CharacterId = a.CharacterId, Other = b.CharacterId, Rev = a.Rev, RevB = b.Rev, State = a.StateJson + "|" + b.StateJson, Done = done }); }
        public void Enhance(string c, string s, int rev, string reqId, string uid, Action<ZoneApiReply> done) { Calls.Add(new Call { Kind = "enhance", Id = reqId, CharacterId = c, Rev = rev, Done = done }); }
        public void EndSession(string c, string s, Action<ZoneApiReply> done) { Calls.Add(new Call { Kind = "end", CharacterId = c, Done = done }); }
    }

    const long Period = 30000;
    readonly FakeApi api = new FakeApi();
    readonly CharacterSaveQueue q;
    readonly List<string> lost = new List<string>(), rejected = new List<string>();
    string stateA = "A0", stateB = "B0";
    long now = 1000;

    public CharacterSaveQueueTests()
    {
        q = new CharacterSaveQueue(api, Period, "test");
        q.Lost += (c, code, state) => lost.Add(c.Id + ":" + code + ":" + state);
        q.Rejected += (c, code, msg, state) => rejected.Add(c.Id + ":" + code + ":" + state);
        q.Tick(now);
    }

    SavedCharacter A() { return q.Register("a", "sa", 5, () => stateA); }
    SavedCharacter B() { return q.Register("b", "sb", 7, () => stateB); }
    void Tick(long ms = 0) { now += ms; q.Tick(now); }
    static ZoneApiReply Saved(int rev) { return new ZoneApiReply { Status = 200, Rev = rev, RevB = -1, CurrentRev = -1 }; }
    static ZoneApiReply NoAnswer() { return new ZoneApiReply { Status = 0, Rev = -1, RevB = -1, CurrentRev = -1 }; }
    static ZoneApiReply Refused(int status, string code, int currentRev = -1) { return new ZoneApiReply { Status = status, Code = code, CurrentRev = currentRev, Rev = -1, RevB = -1 }; }

    [Fact]
    public void ASaveCarriesTheCurrentRevAndCapturesTheStateWhenItLeaves()
    {
        SavedCharacter a = A();
        q.RequestSave(a);
        stateA = "A1"; // changed after the request, before the send
        Tick();
        Assert.Equal("A1", api.Last.State);
        Assert.Equal(5, api.Last.Rev);
        api.Last.Done(Saved(6));
        Assert.Equal(6, a.Rev);
    }

    [Fact]
    public void OneWriteAtATimePerCharacterAndRequestsCoalesce()
    {
        SavedCharacter a = A();
        q.RequestSave(a); q.RequestSave(a); q.RequestSave(a);
        Tick(); Tick();
        Assert.Single(api.Calls);
        q.RequestSave(a); // the first is out: this is one more
        q.RequestSave(a);
        Tick();
        Assert.Single(api.Calls);
        api.Last.Done(Saved(6));
        Tick();
        Assert.Equal(2, api.Calls.Count);
        Assert.Equal(6, api.Last.Rev);
    }

    [Fact]
    public void SavesPeriodicallyAndThatIsTheHeartbeatEvenWithNoChange()
    {
        A();
        Tick(Period - 1);
        Assert.Empty(api.Calls);
        Tick(1);
        Assert.Single(api.Calls);
        api.Last.Done(Saved(6));
        Tick(Period);
        Assert.Equal(2, api.Calls.Count);
    }

    [Fact]
    public void NoAnswerRetriesWithBackoffAndNeverDropsTheJob()
    {
        SavedCharacter a = A();
        q.RequestSave(a);
        Tick();
        for (int i = 0; i < 20; i++)
        {
            api.Last.Done(NoAnswer());
            Assert.False(q.ApiHealthy);
            int calls = api.Calls.Count;
            Tick(100);
            Assert.Equal(calls, api.Calls.Count); // not at once
            Tick(30000);
            Assert.Equal(calls + 1, api.Calls.Count);
        }
        api.Last.Done(Saved(6));
        Assert.True(q.ApiHealthy);
        Assert.Equal(6, a.Rev);
        Assert.Empty(lost);
        Assert.Empty(rejected);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(503)]
    [InlineData(429)]
    [InlineData(401)]
    [InlineData(403)]
    public void ServerTroubleAndBadConfigAreRetriedNotDropped(int status)
    {
        SavedCharacter a = A();
        q.RequestSave(a);
        Tick();
        api.Last.Done(Refused(status, "whatever"));
        Tick(60000);
        Assert.Equal(2, api.Calls.Count);
        Assert.Empty(lost);
        Assert.Empty(rejected);
    }

    [Fact]
    public void ALostAnswerWhoseWriteLandedAdoptsTheRevAndWritesAgain()
    {
        SavedCharacter a = A();
        q.RequestSave(a);
        Tick();
        api.Last.Done(NoAnswer());
        Tick(60000);
        Assert.Equal(5, api.Last.Rev);
        api.Last.Done(Refused(409, "rev_conflict", 6)); // the first one did land
        Assert.Empty(lost);
        Tick(1);
        Assert.Equal(3, api.Calls.Count);
        Assert.Equal(6, api.Last.Rev);
        api.Last.Done(Saved(7));
        Assert.Equal(7, a.Rev);
    }

    [Fact]
    public void ARevConflictWithNothingUnknownMeansSomebodyElseWroteAndTheCharacterIsLost()
    {
        SavedCharacter a = A();
        q.RequestSave(a);
        Tick();
        api.Last.Done(Refused(409, "rev_conflict", 9));
        Assert.Equal(new[] { "a:rev_conflict:A0" }, lost);
        Assert.Equal(0, q.Characters);
        Tick(60000);
        Assert.Single(api.Calls); // never writes again
    }

    [Fact]
    public void ALostSessionStopsWritingAndHandsTheStateOver()
    {
        SavedCharacter a = A();
        q.RequestSave(a);
        Tick();
        api.Last.Done(Refused(409, "session_mismatch"));
        Assert.Equal(new[] { "a:session_mismatch:A0" }, lost);
        Tick(60000);
        Assert.Single(api.Calls);
    }

    [Fact]
    public void ARefusedStateIsReportedWithTheStateAndTheCharacterStaysSaved()
    {
        SavedCharacter a = A();
        q.RequestSave(a);
        Tick();
        api.Last.Done(Refused(422, "enhance_tamper"));
        Assert.Equal(new[] { "a:enhance_tamper:A0" }, rejected);
        Assert.Equal(1, q.Characters);
        Tick(Period);
        Assert.Equal(2, api.Calls.Count); // the next one tries again with what is true then
    }

    [Fact]
    public void LeavingWritesThenReleasesInThatOrderEvenThroughAnOutage()
    {
        SavedCharacter a = A();
        SavedCharacter gone = null;
        q.Finished += c => gone = c;
        q.Leave(a);
        stateA = "A-final";
        Tick();
        Assert.Equal("save", api.Last.Kind);
        api.Last.Done(NoAnswer());
        Tick(60000);
        Assert.Equal("save", api.Last.Kind);
        Assert.Equal("A-final", api.Last.State);
        api.Last.Done(Saved(6));
        Tick();
        Assert.Equal("end", api.Last.Kind);
        api.Last.Done(NoAnswer());
        Assert.Null(gone);
        Assert.Equal(1, q.Characters); // not idle: a shutdown keeps waiting
        Tick(60000);
        api.Last.Done(new ZoneApiReply { Status = 200 });
        Assert.Same(a, gone);
        Assert.Equal(0, q.Characters);
    }

    [Fact]
    public void AfterLeavingNoMoreSavesAreQueued()
    {
        SavedCharacter a = A();
        q.Leave(a);
        q.RequestSave(a);
        Tick();
        api.Last.Done(Saved(6));
        Tick();
        Assert.Equal(new[] { "save", "end" }, api.Calls.Select(c => c.Kind));
    }

    [Fact]
    public void ATradeWaitsUntilFirstInLineForBothAndUsesBothCurrentRevs()
    {
        SavedCharacter a = A(), b = B();
        q.RequestSave(b);
        Tick();
        Assert.Equal("save", api.Last.Kind);
        bool? result = null;
        q.CommitTrade(a, b, new[] { "x" }, 10, new string[0], 0, ok => result = ok);
        Tick();
        Assert.Single(api.Calls); // b is still writing
        api.Last.Done(Saved(8));
        Tick();
        Assert.Equal("trade", api.Last.Kind);
        Assert.Equal(5, api.Last.Rev);
        Assert.Equal(8, api.Last.RevB);
        Assert.Null(result);
        api.Last.Done(new ZoneApiReply { Status = 200, Rev = 6, RevB = 9, CurrentRev = -1 });
        Assert.True(result);
        Assert.Equal(6, a.Rev);
        Assert.Equal(9, b.Rev);
    }

    [Fact]
    public void ATradeWithALostAnswerIsRetriedUnderTheSameIdAndHasNoAnswerUntilThen()
    {
        SavedCharacter a = A(), b = B();
        bool? result = null;
        q.CommitTrade(a, b, new[] { "x" }, 0, new string[0], 0, ok => result = ok);
        Tick();
        string id = api.Last.Id;
        api.Last.Done(NoAnswer());
        Assert.Null(result);
        Tick(60000);
        Assert.Equal(id, api.Last.Id);
        api.Last.Done(new ZoneApiReply { Status = 200, Rev = 6, RevB = 8, CurrentRev = -1 });
        Assert.True(result);
    }

    [Fact]
    public void ARefusedTradeMovesNoRevAndSaysNo()
    {
        SavedCharacter a = A(), b = B();
        bool? result = null;
        q.CommitTrade(a, b, new[] { "x" }, 0, new string[0], 0, ok => result = ok);
        Tick();
        api.Last.Done(Refused(422, "trade_invalid"));
        Assert.False(result);
        Assert.Equal(5, a.Rev);
        Assert.Equal(7, b.Rev);
    }

    [Fact]
    public void LeavingDuringATradeWritesTheLastStateAfterItsAnswer()
    {
        SavedCharacter a = A(), b = B();
        q.CommitTrade(a, b, new[] { "x" }, 0, new string[0], 0, ok => { stateA = "A-after-trade"; });
        Tick();
        q.Leave(a); // the player's connection dropped with the commit out
        Tick();
        Assert.Single(api.Calls);
        api.Last.Done(new ZoneApiReply { Status = 200, Rev = 6, RevB = 8, CurrentRev = -1 });
        Tick();
        Assert.Equal("save", api.Last.Kind);
        Assert.Equal("A-after-trade", api.Last.State); // includes the trade, never the state before it
        Assert.Equal(6, api.Last.Rev);
    }

    [Fact]
    public void ACharacterLostWithATradeInLineCancelsTheTradeForTheOtherSide()
    {
        SavedCharacter a = A(), b = B();
        q.RequestSave(a);
        Tick();
        bool? result = null;
        q.CommitTrade(a, b, new[] { "x" }, 0, new string[0], 0, ok => result = ok);
        api.Calls[0].Done(Refused(409, "session_mismatch"));
        Assert.False(result);
        q.RequestSave(b);
        Tick();
        Assert.Equal("save", api.Last.Kind); // b is not stuck behind it
        Assert.Equal("b", api.Last.CharacterId);
    }

    [Fact]
    public void AnEnhanceWritesFirstThenAsksAndAdoptsTheRev()
    {
        SavedCharacter a = A();
        ZoneApiReply? answer = null;
        q.Enhance(a, "w", r => answer = r);
        Tick();
        Assert.Equal("save", api.Last.Kind);
        api.Last.Done(Saved(6));
        Tick();
        Assert.Equal("enhance", api.Last.Kind);
        Assert.Equal(6, api.Last.Rev);
        string id = api.Last.Id;
        api.Last.Done(NoAnswer());
        Assert.Null(answer);
        Tick(60000);
        Assert.Equal(id, api.Last.Id); // same request id: the API can never roll it twice
        api.Last.Done(new ZoneApiReply { Status = 200, Rev = 7, Attempted = true, Success = true, Level = 4, CurrentRev = -1 });
        Assert.Equal(4, answer.Value.Level);
        Assert.Equal(7, a.Rev);
    }

    [Fact]
    public void NeverMoreThanTheCapInFlight()
    {
        for (int i = 0; i < CharacterSaveQueue.MaxInFlight + 10; i++) q.RequestSave(q.Register("c" + i, "s", 0, () => "x"));
        Tick();
        Assert.Equal(CharacterSaveQueue.MaxInFlight, api.Calls.Count);
        foreach (Call c in api.Calls.ToArray()) c.Done(Saved(1));
        Tick();
        Assert.Equal(CharacterSaveQueue.MaxInFlight + 10, api.Calls.Count);
    }

    [Theory]
    [InlineData("AbCd_-0123456789abcdefghijklmnopqrstuvwxyz0", true)]
    [InlineData("short", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("with space with space with space", false)]
    [InlineData("quote\"quote\"quote\"quote\"quote", false)]
    [InlineData("k", false)] // the dev key is no ticket
    public void TicketShape(string token, bool ok) { Assert.Equal(ok, ZoneTicket.LooksValid(token)); }

    [Fact]
    public void ATicketLongerThanTheProtocolAllowsIsNoTicket() { Assert.False(ZoneTicket.LooksValid(new string('a', NetProtocol.MaxTokenLength + 1))); }
}
