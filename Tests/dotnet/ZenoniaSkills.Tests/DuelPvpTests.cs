using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

// Phase 06 (+ the phase 05 review): the player-vs-player match with its 60 s reconnect window, PvP gold and its daily limit, and a placement stamped with a stale turn or hand card.
public class DuelPvpTests
{
    static readonly string[] DeckA = DuelNpcTable.Get(0).Deck, DeckB = DuelNpcTable.Get(1).Deck;

    static DuelPvpMatch New(long now = 0, ulong seed = 7) { return DuelPvpMatch.Start(seed, DeckA, DeckB, "Ann", "Bob", now); }

    static void PlayFirstFree(DuelPvpMatch m, long now)
    {
        int cell = Enumerable.Range(0, 16).First(c => m.Match.Board.IsEmpty(c));
        Assert.True(m.Place(m.Match.ToMove, 0, cell, now).Ok);
    }

    [Fact]
    public void SidesAreTheInviterAndTheReceiverAndEachSeesTheOtherByName()
    {
        DuelPvpMatch m = New();
        DuelView a = m.View(0, 0), b = m.View(1, 0);
        Assert.Equal("Bob", a.OppName); Assert.Equal("Ann", b.OppName);
        Assert.True(a.IsPvp); Assert.Equal(5, a.Hand.Count); Assert.Equal(5, b.Hand.Count);
        Assert.NotEqual(a.YourTurn, b.YourTurn);
    }

    [Fact]
    public void ADroppedPlayerHasSixtySecondsToComeBack()
    {
        DuelPvpMatch m = New(0);
        m.SetAway(1, 1000);
        Assert.True(m.IsAway(1));
        Assert.Equal(59999, m.AwayMs(1, 60999) );
        m.Tick(60999 - 40000); // well inside the window (the turn clock may place a card, the match goes on)
        Assert.False(m.Finished);
        m.SetBack(1);
        Assert.False(m.IsAway(1));
        m.Tick(200000);
        Assert.False(m.Finished || m.Match.Forfeited >= 0);
    }

    [Fact]
    public void ThePlayerWhoDoesNotComeBackForfeitsAtSixtySecondsAndTheOtherWins()
    {
        DuelPvpMatch m = New(0);
        m.SetAway(1, 0);
        for (long t = 1000; t < DuelPvpMatch.ReconnectMs; t += 1000) { m.Tick(t); Assert.True(m.Match.Forfeited < 0); }
        Assert.False(m.Finished);
        Assert.True(m.Tick(DuelPvpMatch.ReconnectMs));
        Assert.True(m.Finished);
        Assert.Equal(1, m.Match.Forfeited);
        Assert.Equal("a", m.WagerResult); // the inviter wins and takes both cards
        DuelView win = m.View(0, DuelPvpMatch.ReconnectMs), lose = m.View(1, DuelPvpMatch.ReconnectMs);
        Assert.Equal(DuelResult.Win, win.Result); Assert.Equal(DuelResult.Lose, lose.Result);
        Assert.Equal(2, win.EndReason); Assert.Equal(1, lose.EndReason);
    }

    [Fact]
    public void TheTurnClockKeepsRunningForADroppedPlayer()
    {
        DuelPvpMatch m = New(0);
        int mover = m.Match.ToMove;
        m.SetAway(mover, 0);
        Assert.True(m.Tick(DuelMatch.TurnMs)); // 20 s: the match places for them
        Assert.Single(m.Match.Moves);
        Assert.True(m.Match.Moves[0].Auto);
        Assert.Equal(1 - mover, m.Match.ToMove);
    }

    [Fact]
    public void WhenBothAreAwayTheOneWhoLeftFirstForfeitsFirst()
    {
        DuelPvpMatch m = New(0);
        m.SetAway(0, 5000); m.SetAway(1, 9000);
        m.SetAway(0, 20000); // a second drop does not restart the clock
        for (long t = 10000; t <= 65000 && !m.Finished; t += 1000) m.Tick(t);
        Assert.True(m.Finished);
        Assert.Equal(0, m.Match.Forfeited);
        Assert.Equal("b", m.WagerResult);
    }

    [Fact]
    public void LeavingOrGivingUpLosesAtOnceAndNothingIsLeftToTick()
    {
        DuelPvpMatch m = New(0);
        m.Forfeit(1);
        Assert.Equal("a", m.WagerResult);
        Assert.False(m.Tick(100000));
    }

    [Fact]
    public void APlayedOutMatchGivesTheWagerOutcomeAWinnerOrADraw()
    {
        DuelPvpMatch m = New(0);
        long t = 0;
        while (!m.Finished) PlayFirstFree(m, t += 100);
        string r = m.WagerResult;
        Assert.Contains(r, new[] { "a", "b", "draw" });
        Assert.Equal(m.Match.Outcome == DuelOutcome.Draw, r == "draw");
        Assert.Equal(-1, m.Match.Forfeited);
    }

    [Fact]
    public void AViewKeepsTheWagerCardsAndTheAwayTime()
    {
        DuelPvpMatch m = New(0);
        m.SetAway(1, 1000);
        DuelView v = m.View(0, 4000, 5, 9, DuelView.WagerHeld);
        Assert.True(v.HasWager); Assert.Equal(5, v.WagerMine); Assert.Equal(9, v.WagerTheirs); Assert.Equal(DuelView.WagerHeld, v.WagerState); Assert.Equal(3000, v.OppAwayMs);
        Assert.False(m.View(0, 0).HasWager);
    }

    // ---- PvP gold ----

    static PlayerState Hero() { return PlayerState.NewGame("warrior"); }

    [Fact]
    public void WinLoseDrawPayThreeHundredOneHundredOneHundredFifty()
    {
        PlayerState p = Hero(); int gold = p.Gold;
        Assert.Equal(300, DuelRewards.ApplyPvpResult(p, DuelResult.Win, false, 50).DailyGold);
        Assert.Equal(100, DuelRewards.ApplyPvpResult(p, DuelResult.Lose, false, 50).DailyGold);
        Assert.Equal(150, DuelRewards.ApplyPvpResult(p, DuelResult.Draw, false, 50).DailyGold);
        Assert.Equal(gold + 550, p.Gold);
        Assert.Equal(3, p.Cards.Daily.pvpRewards);
    }

    [Fact]
    public void OnlyFiveMatchesADayPayAndTheNextDayStartsOver()
    {
        PlayerState p = Hero();
        for (int i = 0; i < 5; i++) Assert.Equal(300, DuelRewards.ApplyPvpResult(p, DuelResult.Win, false, 7).DailyGold);
        int gold = p.Gold;
        Assert.Equal(0, DuelRewards.ApplyPvpResult(p, DuelResult.Win, false, 7).DailyGold);
        Assert.Equal(gold, p.Gold);
        Assert.Equal(300, DuelRewards.ApplyPvpResult(p, DuelResult.Win, false, 8).DailyGold);
    }

    [Fact]
    public void ASideThatLeftOrADuelStillPlayingPaysNothingAndUsesNoPaidMatch()
    {
        PlayerState p = Hero(); int gold = p.Gold;
        Assert.Equal(0, DuelRewards.ApplyPvpResult(p, DuelResult.Lose, true, 7).DailyGold);
        Assert.Equal(0, DuelRewards.ApplyPvpResult(p, DuelResult.Playing, false, 7).DailyGold);
        Assert.Equal(gold, p.Gold);
        Assert.Equal(0, p.Cards.Daily.pvpRewards);
    }

    [Fact]
    public void PvpGoldStopsAtTheGoldCap()
    {
        PlayerState p = Hero(); p.Gold = PlayerState.GoldCap - 100;
        DuelRewards.ApplyPvpResult(p, DuelResult.Win, false, 7);
        Assert.Equal(PlayerState.GoldCap, p.Gold);
    }

    [Fact]
    public void TheFirstNpcWinIsHeldNearTheGoldCapAndPaidByALaterWin()
    {
        PlayerState p = Hero(); p.Gold = PlayerState.GoldCap - 10;
        DuelReward r = DuelRewards.ApplyNpcResult(p, 0, DuelResult.Win, 1);
        Assert.Equal(0, r.FirstWinGold);
        Assert.Equal(0, p.Cards.NpcFirstWins & DuelNpcTable.FirstWinBit(0)); // the bit is not set: nothing is lost
        p.Gold = 0;
        Assert.Equal(1000, DuelRewards.ApplyNpcResult(p, 0, DuelResult.Win, 1).FirstWinGold);
        Assert.Equal(1000, p.Gold);
    }

    // ---- the placement stamp (phase 05 review, medium) ----

    [Fact]
    public void APlacementStampedWithAStaleTurnIsRefusedAndNothingChanges()
    {
        DuelMatch m = DuelMatch.Create(3, DeckA, DeckB, 0);
        int side = m.ToMove, card = m.Hand(side)[0];
        DuelPlay p = m.Place(side, 0, 0, 0, 5, card); // the client thinks it is turn 5, it is turn 1
        Assert.False(p.Ok); Assert.Equal(DuelPlayError.Stale, p.Code);
        Assert.Empty(m.Moves); Assert.Equal(side, m.ToMove);
        Assert.True(m.Place(side, 0, 0, 0, 1, card).Ok);
    }

    [Fact]
    public void APlacementWhoseHandCardChangedUnderTheTapIsRefused()
    {
        DuelMatch m = DuelMatch.Create(3, DeckA, DeckB, 0);
        int side = m.ToMove, shown = m.Hand(side)[0], other = m.Hand(side)[1];
        DuelPlay p = m.Place(side, 0, 0, 0, 1, other); // the player tapped slot 0 while looking at a different card there
        Assert.False(p.Ok); Assert.Equal(DuelPlayError.Stale, p.Code);
        Assert.Equal(shown, m.Hand(side)[0]);
        Assert.Empty(m.Moves);
    }

    [Fact]
    public void AnUnstampedPlacementStillWorksAndOtherRefusalsKeepTheirOwnCode()
    {
        DuelMatch m = DuelMatch.Create(3, DeckA, DeckB, 0);
        int side = m.ToMove;
        Assert.Equal(DuelPlayError.NotYourTurn, m.Place(1 - side, 0, 0, 0, 1, m.Hand(1 - side)[0]).Code);
        Assert.Equal(DuelPlayError.NoSuchCard, m.Place(side, 9, 0, 0).Code);
        Assert.Equal(DuelPlayError.CellTaken, m.Place(side, 0, 99, 0).Code);
        Assert.True(m.Place(side, 0, 0, 0).Ok);
        Assert.Equal(DuelPlayError.CellTaken, m.Place(1 - side, 0, 0, 0).Code);
    }

    [Fact]
    public void TheNpcSessionPassesTheStampThrough()
    {
        DuelNpcSession s = DuelNpcSession.Start(11, DeckA, 0, 0);
        if (s.Match.ToMove != DuelNpcSession.Player) { Assert.True(s.Tick(5000)); }
        int turn = s.Match.Moves.Count + 1, card = s.Match.Hand(DuelNpcSession.Player)[0];
        Assert.Equal(DuelPlayError.Stale, s.PlayerPlace(0, 0, 5000, turn + 1, card).Code);
        Assert.True(s.PlayerPlace(0, 0, 5000, turn, card).Ok);
    }
}
