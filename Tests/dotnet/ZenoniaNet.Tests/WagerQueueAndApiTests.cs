using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenoniaProto.Net;

/// <summary>
/// Phase 06, zone to server-api: the wager calls as JSON (the contract in the phase file), the answers read back (success, refusals, a settle with payouts), and the write queue's handling of them: escrow only
/// when both characters are idle and online, a lost answer is retried under the same matchId, a settle waits out receiver_online and applies a payout once, and a failed escrow after a lost answer says so.
/// The HTTP layer is a canned `post` delegate: the same code ZoneApiHttp runs, with no network.
/// </summary>
public class WagerQueueAndApiTests
{
    // ---- the request / answer bodies ----

    static WagerSide Side(string id, int rev, string card = null) { return new WagerSide { CharacterId = id, SessionId = new string('a', 32), Rev = rev, StateJson = "{\"v\":1}", CardId = card }; }

    [Fact]
    public void EscrowRequestHasTheContractFields()
    {
        string json = WagerApiClient.EscrowJson("duel-1", Side("c1", 4, "meadowSlime"), Side("c2", 9, "hornRabbit"));
        Assert.Equal("{\"matchId\":\"duel-1\",\"a\":{\"characterId\":\"c1\",\"sessionId\":\"" + new string('a', 32) + "\",\"rev\":4,\"state\":{\"v\":1},\"cardId\":\"meadowSlime\"},\"b\":{\"characterId\":\"c2\",\"sessionId\":\"" + new string('a', 32) + "\",\"rev\":9,\"state\":{\"v\":1},\"cardId\":\"hornRabbit\"}}", json);
    }

    [Fact]
    public void SettleRequestSendsOnlyTheReceiversThatAreOnline()
    {
        Assert.Equal("{\"result\":\"draw\"}", WagerApiClient.SettleJson("draw", null, null));
        string one = WagerApiClient.SettleJson("a", Side("c1", 4), null);
        Assert.Contains("\"result\":\"a\"", one); Assert.Contains("\"a\":{\"sessionId\"", one); Assert.DoesNotContain("\"b\":", one); Assert.DoesNotContain("characterId", one); Assert.DoesNotContain("cardId", one);
    }

    [Fact]
    public void ARequestIdWithOddCharactersIsEscapedInTheUrlAndTheJson()
    {
        string path = null, body = null;
        var client = new WagerApiClient((p, j, done) => { path = p; body = j; });
        client.Settle("a b/\"c", "b", null, Side("c2", 1), r => { });
        Assert.Equal("/internal/card-wagers/a%20b%2F%22c/settle", path);
        client.Escrow("x\"y", Side("c1", 1, "k"), Side("c2", 1, "k"), r => { });
        Assert.StartsWith("{\"matchId\":\"x\\\"y\"", body);
    }

    [Fact]
    public void EscrowAnswerGivesBothRevsAndReplayFlag()
    {
        ZoneApiReply r = WagerApiClient.EscrowReply(200, "{\"replayed\":true,\"status\":\"held\",\"a\":{\"rev\":5,\"state\":{\"a\":{\"x\":[1,2]},\"cards\":[]}},\"b\":{\"rev\":10,\"state\":{\"q\":\"}\"}}}");
        Assert.True(r.Ok); Assert.Equal(5, r.Rev); Assert.Equal(10, r.RevB); Assert.True(r.Replayed);
    }

    [Fact]
    public void RefusalsKeepTheirCodeAndAnUnreadable200IsNoAnswer()
    {
        ZoneApiReply r = WagerApiClient.EscrowReply(422, "{\"error\":{\"code\":\"wager_level\",\"message\":\"x\"}}");
        Assert.False(r.Ok); Assert.Equal("wager_level", r.Code); Assert.False(r.Transient);
        r = WagerApiClient.EscrowReply(409, "{\"code\":\"rev_conflict\",\"details\":{\"currentRev\":12}}");
        Assert.Equal("rev_conflict", r.Code); Assert.Equal(12, r.CurrentRev);
        r = WagerApiClient.EscrowReply(200, "{\"status\":\"held\"}");
        Assert.False(r.Ok); Assert.True(r.OutcomeUnknown); // a success we cannot act on is treated as a lost answer: the retry replays it
        Assert.True(WagerApiClient.EscrowReply(0, null).OutcomeUnknown);
        Assert.Equal("wager_disabled", WagerApiClient.EscrowReply(403, "{\"code\":\"wager_disabled\"}").Code);
    }

    [Fact]
    public void SettleAnswerListsThePayouts()
    {
        ZoneApiReply r = WagerApiClient.SettleReply(200, "{\"replayed\":false,\"status\":\"settled\",\"result\":\"a\",\"credited\":[{\"side\":\"a\",\"characterId\":\"c1\",\"cards\":[\"meadowSlime\",\"hornRabbit\"],\"rev\":7,\"state\":{\"k\":[\"}\"]}},{\"side\":\"b\",\"characterId\":\"c2\",\"cards\":[],\"rev\":3,\"state\":{}}]}");
        Assert.True(r.Ok); Assert.Equal(2, r.Credits.Length);
        Assert.Equal(0, r.Credits[0].Side); Assert.Equal("c1", r.Credits[0].CharacterId); Assert.Equal(new[] { "meadowSlime", "hornRabbit" }, r.Credits[0].Cards); Assert.Equal(7, r.Credits[0].Rev);
        Assert.Equal(1, r.Credits[1].Side); Assert.Empty(r.Credits[1].Cards);
        Assert.False(WagerApiClient.SettleReply(200, "{\"credited\":5}").Ok);
    }

    // ---- the queue ----

    sealed class Call { public string Kind, MatchId, Result; public WagerSide A, B; public bool HasA, HasB; public Action<ZoneApiReply> Done; }

    sealed class Api : IZoneApi
    {
        public readonly List<Call> Calls = new List<Call>();
        public Call Last { get { return Calls[Calls.Count - 1]; } }
        public void Save(string c, string s, int rev, string json, Action<ZoneApiReply> done) { }
        public void CommitTrade(string id, TradeCommitSide a, TradeCommitSide b, Action<ZoneApiReply> done) { }
        public void Enhance(string c, string s, int rev, string reqId, string uid, Action<ZoneApiReply> done) { }
        public void EndSession(string c, string s, Action<ZoneApiReply> done) { }
        public void EscrowWager(string id, WagerSide a, WagerSide b, Action<ZoneApiReply> done) { Calls.Add(new Call { Kind = "escrow", MatchId = id, A = a, B = b, HasA = true, HasB = true, Done = done }); }
        public void SettleWager(string id, string result, WagerSide? a, WagerSide? b, Action<ZoneApiReply> done)
        {
            Calls.Add(new Call { Kind = "settle", MatchId = id, Result = result, HasA = a.HasValue, HasB = b.HasValue, A = a ?? default(WagerSide), B = b ?? default(WagerSide), Done = done });
        }
    }

    readonly Api api = new Api();
    readonly CharacterSaveQueue q;
    long now = 1000;

    public WagerQueueAndApiTests() { q = new CharacterSaveQueue(api, 30000, "t"); q.Tick(now); }

    SavedCharacter Char(string id, int rev) { return q.Register(id, "s" + id, rev, () => "state-" + id); }
    void Tick(long ms = 0) { now += ms; q.Tick(now); }
    static ZoneApiReply Ok(int rev, int revB = -1) { return new ZoneApiReply { Status = 200, Rev = rev, RevB = revB, CurrentRev = -1 }; }
    static ZoneApiReply NoAnswer() { return new ZoneApiReply { Status = 0, Rev = -1, RevB = -1, CurrentRev = -1 }; }
    static ZoneApiReply Refused(int status, string code) { return new ZoneApiReply { Status = status, Code = code, Rev = -1, RevB = -1, CurrentRev = -1 }; }

    [Fact]
    public void EscrowGoesOutWithBothCurrentStatesAndAdoptsBothRevs()
    {
        SavedCharacter a = Char("a", 5), b = Char("b", 7);
        ZoneApiReply got = default(ZoneApiReply); int answers = 0;
        q.EscrowWager("m1", a, b, "meadowSlime", "hornRabbit", r => { got = r; answers++; });
        Tick();
        Assert.Equal("escrow", api.Last.Kind); Assert.Equal("m1", api.Last.MatchId);
        Assert.Equal(5, api.Last.A.Rev); Assert.Equal("state-a", api.Last.A.StateJson); Assert.Equal("meadowSlime", api.Last.A.CardId);
        Assert.Equal(7, api.Last.B.Rev); Assert.Equal("hornRabbit", api.Last.B.CardId);
        api.Last.Done(Ok(6, 8));
        Assert.Equal(1, answers); Assert.True(got.Ok);
        Assert.Equal(6, a.Rev); Assert.Equal(8, b.Rev);
        Assert.Equal(0, q.WagerJobCount);
    }

    [Fact]
    public void EscrowWaitsWhileACharacterIsBusyWritingAndNeverSendsTwice()
    {
        SavedCharacter a = Char("a", 5), b = Char("b", 7);
        q.RequestSave(a); Tick(); // a save of A is in flight
        int saves = 0;
        q.EscrowWager("m1", a, b, "k1", "k2", r => saves++);
        Tick(100);
        Assert.DoesNotContain(api.Calls, c => c.Kind == "escrow");
    }

    [Fact]
    public void ALostEscrowAnswerIsRetriedWithTheSameMatchIdAndABackoff()
    {
        SavedCharacter a = Char("a", 5), b = Char("b", 7);
        ZoneApiReply got = default(ZoneApiReply);
        q.EscrowWager("m1", a, b, "k1", "k2", r => got = r);
        Tick();
        api.Last.Done(NoAnswer());
        Tick(100);
        Assert.Single(api.Calls); // backing off
        Tick(600);
        Assert.Equal(2, api.Calls.Count);
        Assert.Equal("m1", api.Last.MatchId);
        api.Last.Done(Ok(6, 8)); // the API says replayed:true, same result
        Assert.True(got.Ok);
    }

    [Fact]
    public void ARefusedEscrowChangesNothingAndSaysWhy()
    {
        SavedCharacter a = Char("a", 5), b = Char("b", 7);
        ZoneApiReply got = default(ZoneApiReply);
        q.EscrowWager("m1", a, b, "k1", "k2", r => got = r);
        Tick();
        api.Last.Done(Refused(422, "wager_level"));
        Assert.Equal("wager_level", got.Code); Assert.False(got.MayHaveLanded);
        Assert.Equal(5, a.Rev); Assert.Equal(7, b.Rev);
        Assert.Equal(0, q.WagerJobCount);
    }

    [Fact]
    public void EscrowEndsAtOnceWhenAPlayerIsNoLongerHere()
    {
        SavedCharacter a = Char("a", 5), b = Char("b", 7);
        q.Leave(b); // B is leaving: its lock goes after the last write
        ZoneApiReply got = default(ZoneApiReply);
        q.EscrowWager("m1", a, b, "k1", "k2", r => got = r);
        Tick();
        Assert.Equal("not_online", got.Code);
        Assert.DoesNotContain(api.Calls, c => c.Kind == "escrow");
    }

    [Fact]
    public void AnEscrowThatNeverGetsAnAnswerGivesUpAndSaysItMayHaveLanded()
    {
        SavedCharacter a = Char("a", 5), b = Char("b", 7);
        ZoneApiReply got = default(ZoneApiReply);
        q.EscrowWager("m1", a, b, "k1", "k2", r => got = r);
        for (int i = 0; i < 40 && got.Code == null; i++)
        {
            Tick(5000);
            if (api.Calls.Count > 0 && api.Last.Done != null && q.WagerJobCount > 0) { var c = api.Last; if (c.Kind == "escrow" && !called.Contains(c)) { called.Add(c); c.Done(NoAnswer()); } }
        }
        Assert.Equal("timeout", got.Code);
        Assert.True(got.MayHaveLanded); // the owner settles it as a draw so no card is stuck
    }

    readonly HashSet<Call> called = new HashSet<Call>();

    [Fact]
    public void SettleSendsStateOnlyForReceiversStillOnlineAndMarksThePayoutToApply()
    {
        SavedCharacter a = Char("a", 6); // B left or never was here
        ZoneApiReply got = default(ZoneApiReply);
        q.SettleWager("m1", "a", "a", null, r => got = r);
        Tick();
        Assert.Equal("settle", api.Last.Kind); Assert.Equal("a", api.Last.Result);
        Assert.True(api.Last.HasA); Assert.False(api.Last.HasB);
        Assert.Equal(6, api.Last.A.Rev);
        var reply = new ZoneApiReply { Status = 200, Rev = -1, RevB = -1, CurrentRev = -1, Credits = new[] { new WagerCredit { Side = 0, CharacterId = "a", Cards = new[] { "k1", "k2" }, Rev = 7 } } };
        api.Last.Done(reply);
        Assert.True(got.Ok); Assert.True(got.Credits[0].Apply); Assert.Equal(7, a.Rev);
    }

    [Fact]
    public void AReplayedPayoutAtAnOlderOrEqualRevIsNotAppliedTwice()
    {
        SavedCharacter a = Char("a", 9);
        ZoneApiReply got = default(ZoneApiReply);
        q.SettleWager("m1", "draw", "a", null, r => got = r);
        Tick();
        api.Last.Done(new ZoneApiReply { Status = 200, Rev = -1, RevB = -1, CurrentRev = -1, Replayed = true, Credits = new[] { new WagerCredit { Side = 0, CharacterId = "a", Cards = new[] { "k1" }, Rev = 9 } } });
        Assert.False(got.Credits[0].Apply);
        Assert.Equal(9, a.Rev);
    }

    [Fact]
    public void ASettleWaitsOutReceiverOnlineAndLostAnswersUntilItIsDefinite()
    {
        SavedCharacter a = Char("a", 6);
        ZoneApiReply got = default(ZoneApiReply); int answers = 0;
        q.SettleWager("m1", "b", null, "b", r => { got = r; answers++; }); // the winner B is not on this zone yet: no state goes along
        Tick();
        Assert.False(api.Last.HasB);
        api.Last.Done(Refused(409, "receiver_online")); // their session is alive elsewhere
        Tick(100);
        Assert.Single(api.Calls);
        Tick(700);
        Assert.Equal(2, api.Calls.Count);
        api.Last.Done(NoAnswer());
        Tick(2000);
        Assert.Equal(3, api.Calls.Count);
        Assert.Equal(0, answers);
        api.Last.Done(new ZoneApiReply { Status = 200, Rev = -1, RevB = -1, CurrentRev = -1, Credits = new WagerCredit[0] });
        Assert.Equal(1, answers); Assert.True(got.Ok);
        Assert.Equal(0, q.WagerJobCount);
        Assert.Equal(6, a.Rev);
    }

    [Fact]
    public void ASettleThatIsAlreadySettledOrRefundedEndsWithoutRetry()
    {
        ZoneApiReply got = default(ZoneApiReply);
        q.SettleWager("m1", "a", "a", null, r => got = r);
        Tick();
        api.Last.Done(Refused(409, "wager_refunded"));
        Assert.Equal("wager_refunded", got.Code);
        Assert.Equal(0, q.WagerJobCount);
        Tick(60000);
        Assert.Single(api.Calls);
    }

    [Fact]
    public void ASettleReceiverThatBecameOnlineBetweenTriesIsSentWithItsState()
    {
        ZoneApiReply got = default(ZoneApiReply);
        q.SettleWager("m1", "a", "a", null, r => got = r);
        Tick();
        Assert.False(api.Last.HasA); // nobody online yet
        api.Last.Done(Refused(409, "receiver_online"));
        Char("a", 11); // the winner logs in on this zone
        Tick(5000);
        Assert.True(api.Last.HasA); Assert.Equal(11, api.Last.A.Rev);
    }

    [Fact]
    public void ASettleDoesNotRunWhileTheReceiverIsMidWrite()
    {
        SavedCharacter a = Char("a", 6);
        q.RequestSave(a); Tick(); // save in flight
        q.SettleWager("m1", "a", "a", null, r => { });
        Tick(100);
        Assert.DoesNotContain(api.Calls, c => c.Kind == "settle");
    }
}
