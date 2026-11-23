using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenoniaProto.Net;

/// <summary>
/// Phase 06 review fixes: the Card Wager stakes against a fake server-api (WagerFakeApi.cs) that loses requests, loses answers and holds them back. Every scenario ends with the same check: the cards of both
/// characters over the zone's copy, the API's stored copy and the escrow are exactly what they were plus what dropped, nothing made and nothing lost, and the API's own total (stored + held) never moved.
/// Players: A (cards a1..a8 staked + a spare) and B (b1..b8 + a spare). A wins by taking b3.
/// </summary>
public class WagerConservationTests
{
    sealed class Scenario
    {
        public readonly WagerWorld W;
        public readonly FakePlayer A, B;
        public readonly StakeMatch M;
        public int EscrowAnswers, SettleAnswers;
        public ZoneApiReply EscrowReply, SettleReply;

        public Scenario(long periodicSaveMs = 1000)
        {
            W = new WagerWorld(periodicSaveMs);
            A = W.Join("A", "a");
            B = W.Join("B", "b");
            M = new StakeMatch(W.Queue, "m1", A.Saved, B.Saved, "A", "B", WagerWorld.DeckOf("a").ToArray(), WagerWorld.DeckOf("b").ToArray(), side => W.Players[side == 0 ? "A" : "B"]);
        }

        public void Escrow() { M.Escrow(1, (m, r) => { EscrowAnswers++; EscrowReply = r; }); }
        public void Settle(string result = "a", string pick = "b3") { M.Settle(result, pick, (m, r) => { SettleAnswers++; SettleReply = r; }); }

        /// <summary>The API's total never moves except by the drops given.</summary>
        public void ApiTotalIs(int expected) { Assert.Equal(expected, W.Server.Total()); }

        public int Effective { get { return W.Effective("A") + W.Effective("B") + W.Server.Matches.Values.Where(x => x.Status == "held").Sum(x => x.Deck[0].Length + x.Deck[1].Length); } }

        /// <summary>The stored copy of a character, as a sorted "id:n" list.</summary>
        public string Stored(string id) { return string.Join(",", W.Server.Docs[id].Cards.Where(c => c.Value > 0).OrderBy(c => c.Key).Select(c => c.Key + ":" + c.Value)); }
        public string Copy(FakePlayer p) { return string.Join(",", p.Cards.Where(c => c.Value > 0).OrderBy(c => c.Key).Select(c => c.Key + ":" + c.Value)); }

        public static string Cards(params string[] ids) { return string.Join(",", ids.GroupBy(x => x).OrderBy(g => g.Key).Select(g => g.Key + ":" + g.Count())); }
        public static string[] Deck(string prefix, params int[] without) { return Enumerable.Range(1, 8).Where(i => !without.Contains(i)).Select(i => prefix + i).ToArray(); }
    }

    // ---- the plain game ----

    [Fact]
    public void AWinnerTakesOneCardAndDropsDuringTheMatchSurvive()
    {
        var s = new Scenario();
        s.Escrow(); s.W.Run(500);
        Assert.Equal(StakePhase.Held, s.M.Phase);
        Assert.Equal(1, s.A.Total); // only the spare is left
        s.ApiTotalIs(18);
        s.A.Drop("dropA");
        s.W.Run(5000); // periodic saves while the match runs
        s.Settle(); s.W.Run(1000);
        Assert.Equal(StakePhase.Paid, s.M.Phase);
        Assert.Equal(Scenario.Cards(Scenario.Deck("a").Concat(new[] { "b3", "aspare", "dropA" }).ToArray()), s.Copy(s.A));
        Assert.Equal(Scenario.Cards(Scenario.Deck("b", 3).Concat(new[] { "bspare" }).ToArray()), s.Copy(s.B));
        s.W.Leave("A"); s.W.Leave("B");
        Assert.Equal(s.Copy(s.A), s.Stored("A")); Assert.Equal(s.Copy(s.B), s.Stored("B"));
        s.ApiTotalIs(19); // 18 + the one that dropped
        Assert.Equal(1, s.A.Given); Assert.Equal(1, s.B.Given);
    }

    // ---- Critical 1 / High 1: a player who has gone still has his cards moved ----

    [Fact]
    public void ADisconnectWhileTheEscrowIsInFlightTakesTheDeckFromHisCopyBeforeHisLastSave()
    {
        var s = new Scenario();
        s.W.Http.Rule = (p, n) => p.EndsWith("/escrow") ? Fault.Hold : Fault.None;
        s.Escrow(); s.W.Run(300);
        Assert.Equal(0, s.EscrowAnswers); // the API took the decks, the answer is on its way
        s.W.Queue.Leave(s.A.Saved); // A closes the game
        s.W.Run(3000);
        Assert.Equal(0, s.W.Http.Count("/characters/A/save")); // nothing of A is written until the answer is known (the final save would still hold the deck)
        s.W.Http.ReleaseHeld(); s.W.Run(2000);
        Assert.Equal(1, s.EscrowAnswers); Assert.True(s.EscrowReply.Ok);
        Assert.False(s.W.IsHere("A")); // the final save went out after the answer, and the lock is gone
        Assert.Equal(1, s.A.Taken);
        Assert.Equal(Scenario.Cards("aspare"), s.Stored("A")); // the stored copy has the spare only: the final save did not put the deck back
        s.ApiTotalIs(18);
        s.Settle("draw", null); s.W.Run(2000); // A is not on the zone: the API writes it
        Assert.Equal(StakePhase.Paid, s.M.Phase);
        Assert.Equal(Scenario.Cards(Scenario.Deck("a").Concat(new[] { "aspare" }).ToArray()), s.Stored("A"));
        Assert.Equal(Scenario.Cards(Scenario.Deck("b").Concat(new[] { "bspare" }).ToArray()), s.Copy(s.B));
        Assert.Equal(18, s.Effective);
        Assert.Equal(0, s.A.Given); // the copy of a player who is gone is not touched again: the API wrote his stored one
    }

    [Fact]
    public void TheWinnerWhoClosesTheGameRightAfterTakeStillGetsThePayout()
    {
        var s = new Scenario();
        s.Escrow(); s.W.Run(500);
        s.Settle();
        s.W.Queue.Leave(s.A.Saved); // closes the game before the answer
        s.W.Run(3000);
        Assert.Equal(StakePhase.Paid, s.M.Phase);
        Assert.False(s.W.IsHere("A"));
        Assert.Equal(1, s.A.Given);
        Assert.Equal(Scenario.Cards(Scenario.Deck("a").Concat(new[] { "b3", "aspare" }).ToArray()), s.Stored("A")); // the final save came after the payout
        s.W.Leave("B");
        Assert.Equal(Scenario.Cards(Scenario.Deck("b", 3).Concat(new[] { "bspare" }).ToArray()), s.Stored("B"));
        s.ApiTotalIs(18);
    }

    // ---- Critical 2: a lost settle answer with a periodic save in between ----

    [Fact]
    public void ALostSettleAnswerWithPeriodicSavesDueIsReplayedAndPaidOnce()
    {
        var s = new Scenario(500);
        s.Escrow(); s.W.Run(500);
        s.W.Http.Rule = (p, n) => p.EndsWith("/settle") && s.W.Http.Count("/settle") == 1 ? Fault.LoseResponse : Fault.None; // the first settle lands, its answer is lost
        s.Settle();
        int savesBefore = s.W.Http.Count("/save");
        s.W.Run(20000);
        Assert.Equal(StakePhase.Paid, s.M.Phase);
        Assert.True(s.W.Http.Count("/settle") >= 2);
        // no save of A or B went out between the first settle call and the answer that ended it
        int first = s.W.Http.Log.FindIndex(x => x.EndsWith("/settle")), last = s.W.Http.Log.FindLastIndex(x => x.EndsWith("/settle"));
        Assert.DoesNotContain(s.W.Http.Log.Skip(first).Take(last - first + 1), x => x.EndsWith("/save"));
        Assert.Equal(1, s.A.Given); Assert.Equal(1, s.B.Given);
        s.W.Run(2000); // the saves go on afterwards, at the revs the API has
        s.W.Leave("A"); s.W.Leave("B");
        Assert.Equal(Scenario.Cards(Scenario.Deck("a").Concat(new[] { "b3", "aspare" }).ToArray()), s.Stored("A"));
        Assert.Equal(Scenario.Cards(Scenario.Deck("b", 3).Concat(new[] { "bspare" }).ToArray()), s.Stored("B"));
        s.ApiTotalIs(18);
        Assert.True(savesBefore >= 0);
    }

    [Fact]
    public void ALostSettleAnswerNeverGivesUpEvenAfterManyTriesAndAnApiOutage()
    {
        var s = new Scenario(500);
        s.Escrow(); s.W.Run(500);
        bool down = false;
        s.W.Http.Rule = (p, n) => down ? Fault.LoseRequest : p.EndsWith("/settle") && s.W.Http.Count("/settle") == 1 ? Fault.LoseResponse : Fault.None;
        s.Settle(); s.W.Run(300);
        down = true;
        s.W.Run(1200 * 1000); // 20 minutes of silence: far more than the 40 tries of a call that certainly changed nothing
        Assert.Equal(StakePhase.Settling, s.M.Phase); // still trying: the first one may have landed
        down = false;
        s.W.Run(60000);
        Assert.Equal(StakePhase.Paid, s.M.Phase);
        s.W.Leave("A"); s.W.Leave("B");
        Assert.Equal(Scenario.Cards(Scenario.Deck("a").Concat(new[] { "b3", "aspare" }).ToArray()), s.Stored("A"));
        Assert.Equal(Scenario.Cards(Scenario.Deck("b", 3).Concat(new[] { "bspare" }).ToArray()), s.Stored("B"));
        s.ApiTotalIs(18);
    }

    [Fact]
    public void AReplayedSettleForPlayersWhoAreGoneAppliesNothingToTheirOldCopies()
    {
        var s = new Scenario();
        s.Escrow(); s.W.Run(500);
        s.W.Leave("A"); s.W.Leave("B");
        s.W.Http.Rule = (p, n) => p.EndsWith("/settle") && s.W.Http.Count("/settle") == 1 ? Fault.LoseResponse : Fault.None;
        s.Settle(); s.W.Run(30000);
        Assert.Equal(StakePhase.Paid, s.M.Phase);
        Assert.Equal(0, s.A.Given); Assert.Equal(0, s.B.Given); // the API wrote both stored characters; a replay must not be added to a copy that is gone
        Assert.Equal(Scenario.Cards(Scenario.Deck("a").Concat(new[] { "b3", "aspare" }).ToArray()), s.Stored("A"));
        Assert.Equal(Scenario.Cards(Scenario.Deck("b", 3).Concat(new[] { "bspare" }).ToArray()), s.Stored("B"));
        s.ApiTotalIs(18);
    }

    // ---- Critical 3: escrow with a lost answer, then success or a long outage ----

    [Fact]
    public void ALostEscrowAnswerThenSuccessTakesTheDecksOnceAndSavesWaitForIt()
    {
        var s = new Scenario(500);
        s.W.Run(2000);
        s.W.Http.Rule = (p, n) => p.EndsWith("/escrow") && s.W.Http.Count("/escrow") == 1 ? Fault.LoseResponse : Fault.None;
        s.Escrow(); s.W.Run(15000);
        Assert.Equal(1, s.EscrowAnswers); Assert.True(s.EscrowReply.Ok); Assert.True(s.EscrowReply.Replayed);
        Assert.Equal(1, s.A.Taken); Assert.Equal(1, s.B.Taken);
        int first = s.W.Http.Log.FindIndex(x => x.EndsWith("/escrow")), last = s.W.Http.Log.FindLastIndex(x => x.EndsWith("/escrow"));
        Assert.DoesNotContain(s.W.Http.Log.Skip(first).Take(last - first + 1), x => x.EndsWith("/save"));
        s.W.Run(3000);
        Assert.Equal(Scenario.Cards("aspare"), s.Stored("A")); // a save at the new rev: the stored copy has no deck, the zone copy neither
        s.ApiTotalIs(18);
        s.Settle("draw", null); s.W.Run(2000);
        s.W.Leave("A"); s.W.Leave("B");
        Assert.Equal(Scenario.Cards(Scenario.Deck("a").Concat(new[] { "aspare" }).ToArray()), s.Stored("A"));
        Assert.Equal(18, s.Effective);
    }

    [Theory]
    [InlineData(false)] // the first escrow landed, its answer was lost
    [InlineData(true)]  // the first escrow never reached the API
    public void AnEscrowThatGetsNoAnswerForAWhileIsNeverGivenUpAndIfItLandedTheMatchIsSettledAsADraw(bool firstRequestLost)
    {
        var s = new Scenario(500);
        bool down = false;
        s.W.Http.Rule = (p, n) => down || (p.EndsWith("/escrow") && s.W.Http.Count("/escrow") == 1) ? (down || firstRequestLost ? Fault.LoseRequest : Fault.LoseResponse) : Fault.None;
        s.Escrow(); s.W.Run(300);
        down = true;
        s.W.Run(26000);
        s.M.Abandon(); // DuelService does this after EscrowPatienceMs: the players were told, nobody waits any more
        s.W.Run(60000);
        Assert.Equal(0, s.EscrowAnswers); // still not answered: the old code gave up here and settled as a draw on a state that still had the decks
        down = false;
        s.W.Run(60000);
        Assert.Equal(1, s.EscrowAnswers); Assert.True(s.EscrowReply.Ok);
        Assert.Equal(StakePhase.Paid, s.M.Phase); // taken, then settled as a draw by the match itself
        s.W.Leave("A"); s.W.Leave("B");
        Assert.Equal(Scenario.Cards(Scenario.Deck("a").Concat(new[] { "aspare" }).ToArray()), s.Stored("A"));
        Assert.Equal(Scenario.Cards(Scenario.Deck("b").Concat(new[] { "bspare" }).ToArray()), s.Stored("B"));
        s.ApiTotalIs(18);
        Assert.Equal(18, s.Effective);
    }

    [Fact]
    public void EveryWriteOfTheTwoCharactersWaitsWhileTheEscrowOutcomeIsUnknown()
    {
        var s = new Scenario(500);
        bool down = false;
        s.W.Http.Rule = (p, n) => down ? Fault.LoseRequest : p.EndsWith("/escrow") ? Fault.LoseResponse : Fault.None;
        s.Escrow(); s.W.Run(200);
        down = true;
        int before = s.W.Http.Log.Count;
        s.W.Queue.RequestSave(s.A.Saved);
        s.W.Queue.Leave(s.B.Saved);
        s.W.Run(120000);
        Assert.DoesNotContain(s.W.Http.Log.Skip(before), x => x.Contains("/save") || x.Contains("/end"));
        Assert.True(s.W.IsHere("A")); Assert.True(s.W.IsHere("B"));
    }

    [Fact]
    public void AReplayThatFindsTheMatchAlreadyRefundedTakesNothingAndAdoptsTheRevs()
    {
        var s = new Scenario(500);
        s.W.Http.Rule = (p, n) => p.EndsWith("/escrow") && s.W.Http.Count("/escrow") == 1 ? Fault.LoseResponse : Fault.None;
        s.Escrow(); s.W.Run(200);
        s.W.Server.Refund("m1", ignoreLive: true); // the API gave the decks back before the zone heard of the escrow
        s.W.Run(15000);
        Assert.Equal("wager_not_held", s.EscrowReply.Code);
        Assert.Equal(StakePhase.Refused, s.M.Phase);
        Assert.Equal(0, s.A.Taken); // nothing is taken out of a copy that never lost its deck
        s.W.Run(3000);
        s.W.Leave("A"); s.W.Leave("B");
        Assert.Equal(Scenario.Cards(Scenario.Deck("a").Concat(new[] { "aspare" }).ToArray()), s.Stored("A")); // saves went through at the adopted revs (no rev_conflict loop)
        Assert.Equal(18, s.Effective);
    }

    // ---- crash, expiry sweep, deleted characters ----

    [Fact]
    public void AZoneCrashMidMatchGivesBothDecksBackAndNoCardIsCreated()
    {
        var s = new Scenario();
        s.Escrow(); s.W.Run(500);
        s.A.Drop("dropA"); s.W.Run(3000); // saved: the API has it
        s.W.Queue.Abandon(); // the process dies
        Assert.Equal(1, s.W.Server.ResetZone()); // the restarted zone frees the locks and the API refunds its held matches
        Assert.Equal(Scenario.Cards(Scenario.Deck("a").Concat(new[] { "aspare", "dropA" }).ToArray()), s.Stored("A"));
        Assert.Equal(Scenario.Cards(Scenario.Deck("b").Concat(new[] { "bspare" }).ToArray()), s.Stored("B"));
        s.ApiTotalIs(19);
    }

    [Fact]
    public void ARefundSweepThatRunsBeforeALateSettleLeavesBothDecksWhole()
    {
        var s = new Scenario();
        s.Escrow(); s.W.Run(500);
        s.W.Leave("A"); s.W.Leave("B");
        bool down = true;
        s.W.Http.Rule = (p, n) => down ? Fault.LoseRequest : Fault.None;
        s.Settle(); s.W.Run(10000);
        s.W.Now += FakeApiServer.HoldMs + 1000; s.W.Server.Now = s.W.Now;
        Assert.Equal(1, s.W.Server.SweepExpired()); // the sweep refunds while the settle could not get through
        down = false;
        s.W.Run(60000);
        Assert.Equal(StakePhase.Failed, s.M.Phase); // wager_refunded: a definite no, nothing moves
        Assert.Equal("wager_refunded", s.SettleReply.Code);
        Assert.Equal(Scenario.Cards(Scenario.Deck("a").Concat(new[] { "aspare" }).ToArray()), s.Stored("A"));
        Assert.Equal(Scenario.Cards(Scenario.Deck("b").Concat(new[] { "bspare" }).ToArray()), s.Stored("B"));
        s.ApiTotalIs(18);
    }

    [Fact]
    public void ASettleThatLandedBeforeTheSweepIsNotRefundedAgain()
    {
        var s = new Scenario();
        s.Escrow(); s.W.Run(500);
        s.W.Http.Rule = (p, n) => p.EndsWith("/settle") && s.W.Http.Count("/settle") == 1 ? Fault.LoseResponse : Fault.None;
        s.Settle(); s.W.Run(600);
        s.W.Leave("A"); s.W.Leave("B");
        s.W.Now += FakeApiServer.HoldMs + 1000; s.W.Server.Now = s.W.Now;
        Assert.Equal(0, s.W.Server.SweepExpired()); // already settled
        s.W.Run(30000);
        Assert.Equal(StakePhase.Paid, s.M.Phase);
        Assert.Equal(Scenario.Cards(Scenario.Deck("a").Concat(new[] { "b3", "aspare" }).ToArray()), s.Stored("A"));
        Assert.Equal(Scenario.Cards(Scenario.Deck("b", 3).Concat(new[] { "bspare" }).ToArray()), s.Stored("B"));
        s.ApiTotalIs(18);
    }

    [Fact]
    public void ACharacterCannotBeDeletedWhileItsMatchIsHeldAndOnlyAfterTheRefund()
    {
        var s = new Scenario();
        s.Escrow(); s.W.Run(500);
        s.W.Leave("A");
        Assert.False(s.W.Server.Delete("A")); // 409 wager_held
        s.W.Leave("B");
        Assert.False(s.W.Server.Delete("A"));
        Assert.True(s.W.Server.Refund("m1"));
        Assert.True(s.W.Server.Delete("A"));
        Assert.Equal(Scenario.Cards(Scenario.Deck("b").Concat(new[] { "bspare" }).ToArray()), s.Stored("B"));
    }

    [Fact]
    public void ASettleWhoseOtherCharacterIsGoneStillPaysTheOneThatExists()
    {
        var s = new Scenario();
        s.Escrow(); s.W.Run(500);
        s.W.Leave("B");
        s.W.Server.Docs.Remove("B"); // gone behind the API's back (deletion is blocked now, an old one is not)
        s.Settle(); s.W.Run(3000);
        Assert.Equal(StakePhase.Paid, s.M.Phase);
        Assert.Equal(1, s.A.Given);
        Assert.Equal(Scenario.Cards(Scenario.Deck("a").Concat(new[] { "b3", "aspare" }).ToArray()), s.Copy(s.A));
        s.W.Leave("A");
        Assert.Equal(s.Copy(s.A), s.Stored("A"));
    }

    // ---- Medium: a lost session is not retried with state ----

    [Fact]
    public void ASettleMeetingAForeignSessionDropsThatStateAndWaitsForTheOtherSessionToEnd()
    {
        var s = new Scenario();
        s.Escrow(); s.W.Run(500);
        s.W.Server.Docs["A"].Session = "foreign"; // another zone locked A meanwhile
        s.Settle(); s.W.Run(20000);
        Assert.Contains("A:session_mismatch", s.W.Lost); // this zone no longer owns A
        Assert.NotEqual(StakePhase.Paid, s.M.Phase); // A is live elsewhere: wait (receiver_online), never write over it
        s.W.Server.Docs["A"].Session = null; // that session ended
        s.W.Run(60000);
        Assert.Equal(StakePhase.Paid, s.M.Phase);
        Assert.Equal(Scenario.Cards(Scenario.Deck("a").Concat(new[] { "b3", "aspare" }).ToArray()), s.Stored("A"));
        s.W.Leave("B");
        Assert.Equal(Scenario.Cards(Scenario.Deck("b", 3).Concat(new[] { "bspare" }).ToArray()), s.Stored("B"));
    }
}
