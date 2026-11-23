using Xunit;
using ZenoniaProto.Rpg;

// Phase 07: Ranked math (season, tiers, MMR / LP, the season reset, queue entry) and the matchmaking line. Pure, no engine.
public class RankedRulesTests
{
    static CardRankData Rank(int mmr = 1000, int lp = 0, int season = 1) { return new CardRankData { season = season, mmr = mmr, lp = lp }; }

    // ---- LP and MMR ----

    [Fact]
    public void EqualRatingsWinPlus25LpLosePlusMinus20AndDrawNothing()
    {
        Assert.Equal(25, RankedRules.ChangeFor(DuelResult.Win, 1000, 1000).Lp);
        Assert.Equal(-20, RankedRules.ChangeFor(DuelResult.Lose, 1000, 1000).Lp);
        Assert.Equal(0, RankedRules.ChangeFor(DuelResult.Draw, 1000, 1000).Lp);
        Assert.Equal(16, RankedRules.ChangeFor(DuelResult.Win, 1000, 1000).Mmr);
        Assert.Equal(-16, RankedRules.ChangeFor(DuelResult.Lose, 1000, 1000).Mmr);
        Assert.Equal(0, RankedRules.ChangeFor(DuelResult.Draw, 1000, 1000).Mmr);
    }

    [Theory]
    [InlineData(1000, 2000)] [InlineData(1000, 1000)] [InlineData(2000, 1000)] [InlineData(1000, 1150)] [InlineData(0, 3000)]
    public void LpStaysInsideTheBandsWhateverTheRatings(int mine, int theirs)
    {
        int win = RankedRules.ChangeFor(DuelResult.Win, mine, theirs).Lp, loss = RankedRules.ChangeFor(DuelResult.Lose, mine, theirs).Lp;
        Assert.InRange(win, 20, 30);
        Assert.InRange(loss, -25, -15);
        Assert.Equal(0, RankedRules.ChangeFor(DuelResult.Draw, mine, theirs).Lp);
    }

    [Fact]
    public void BeatingAStrongerPlayerPaysMoreAndLosingToAWeakerOneCostsMore()
    {
        Assert.True(RankedRules.ChangeFor(DuelResult.Win, 1000, 1400).Lp > RankedRules.ChangeFor(DuelResult.Win, 1000, 1000).Lp);
        Assert.True(RankedRules.ChangeFor(DuelResult.Win, 1000, 1400).Mmr > RankedRules.ChangeFor(DuelResult.Win, 1000, 1000).Mmr);
        Assert.True(RankedRules.ChangeFor(DuelResult.Lose, 1400, 1000).Lp < RankedRules.ChangeFor(DuelResult.Lose, 1000, 1000).Lp);
        Assert.Equal(-25, RankedRules.ChangeFor(DuelResult.Lose, 1600, 1000).Lp);
        Assert.Equal(30, RankedRules.ChangeFor(DuelResult.Win, 1000, 1600).Lp);
    }

    [Fact]
    public void ApplyMovesMmrLpRecordAndBestFromTheStartOfMatchRank()
    {
        int lp;
        CardRankData before = Rank(1000, 100);
        CardRankData won = RankedRules.Apply(before, DuelResult.Win, 1000, true, out lp);
        Assert.Equal(25, lp); Assert.Equal(125, won.lp); Assert.Equal(1016, won.mmr); Assert.Equal(1, won.wins); Assert.Equal(125, won.best);
        Assert.Equal(100, before.lp); Assert.Equal(0, before.wins); // the input is never changed
        CardRankData lost = RankedRules.Apply(won, DuelResult.Lose, 1000, true, out lp);
        Assert.Equal(-20, lp); Assert.Equal(105, lost.lp); Assert.Equal(1, lost.losses); Assert.Equal(125, lost.best); // best keeps the peak
        CardRankData drawn = RankedRules.Apply(lost, DuelResult.Draw, 1000, true, out lp);
        Assert.Equal(0, lp); Assert.Equal(105, drawn.lp); Assert.Equal(1, drawn.draws);
    }

    [Fact]
    public void LpNeverGoesBelowZeroAndTheReportedChangeIsWhatWasReallyLost()
    {
        int lp;
        CardRankData r = RankedRules.Apply(Rank(1000, 8), DuelResult.Lose, 1000, true, out lp);
        Assert.Equal(0, r.lp); Assert.Equal(-8, lp);
        r = RankedRules.Apply(Rank(1000, 0), DuelResult.Lose, 1000, true, out lp);
        Assert.Equal(0, r.lp); Assert.Equal(0, lp); Assert.Equal(1, r.losses);
    }

    [Fact]
    public void AMatchThatDoesNotCountChangesNothingAtAll()
    {
        int lp;
        CardRankData before = Rank(1234, 410); before.wins = 5;
        CardRankData r = RankedRules.Apply(before, DuelResult.Win, 1000, false, out lp);
        Assert.Equal(0, lp);
        Assert.Equal(1234, r.mmr); Assert.Equal(410, r.lp); Assert.Equal(5, r.wins);
    }

    [Fact]
    public void AnUnratedCharacterStartsAt1000()
    {
        int lp;
        CardRankData r = RankedRules.Apply(new CardRankData(), DuelResult.Win, 1000, true, out lp);
        Assert.Equal(1016, r.mmr);
    }

    // ---- tiers ----

    [Theory]
    [InlineData(0, RankTier.Bronze)] [InlineData(299, RankTier.Bronze)] [InlineData(300, RankTier.Silver)] [InlineData(699, RankTier.Silver)] [InlineData(700, RankTier.Gold)]
    [InlineData(1199, RankTier.Gold)] [InlineData(1200, RankTier.Platinum)] [InlineData(1799, RankTier.Platinum)] [InlineData(1800, RankTier.Diamond)] [InlineData(99999, RankTier.Diamond)]
    public void TiersStartAt0_300_700_1200_1800(int lp, RankTier tier) { Assert.Equal(tier, RankedRules.Tier(lp)); }

    [Fact]
    public void TierProgressFillsTheBarInsideTheTier()
    {
        Assert.Equal(0f, RankedRules.TierProgress(0));
        Assert.Equal(0.5f, RankedRules.TierProgress(150));
        Assert.Equal(0f, RankedRules.TierProgress(300));
        Assert.Equal(1f, RankedRules.TierProgress(5000)); // Diamond is always full
        Assert.Equal(300, RankedRules.NextTierLp(RankTier.Bronze)); Assert.Equal(-1, RankedRules.NextTierLp(RankTier.Diamond));
    }

    // ---- season ----

    static long Day(int n) { return RankedRules.SeasonEpochMs + n * 86400000L; }

    [Fact]
    public void ASeasonIs30DaysFromTheEpochAndTheCountdownReachesZero()
    {
        Assert.Equal(1, RankedRules.Season(Day(0))); Assert.Equal(1, RankedRules.Season(Day(30) - 1)); Assert.Equal(2, RankedRules.Season(Day(30)));
        Assert.Equal(1, RankedRules.Season(0)); // before the epoch counts as season 1
        Assert.Equal(30 * 24 * 60, RankedRules.MinutesLeft(Day(0)));
        Assert.Equal(0, RankedRules.MinutesLeft(Day(30) - 1));
        Assert.Equal(Day(30), RankedRules.SeasonEndMs(1));
    }

    [Fact]
    public void FirstUseGetsTheStartRatingInTheCurrentSeason()
    {
        var r = new CardRankData();
        Assert.True(RankedRules.EnsureSeason(r, Day(3)));
        Assert.Equal(1, r.season); Assert.Equal(1000, r.mmr); Assert.Equal(0, r.lp);
        Assert.False(RankedRules.EnsureSeason(r, Day(10))); // same season: nothing to do
    }

    [Fact]
    public void ANewSeasonHalvesTheDistanceFrom1000AndTheLpKeepsTheBestAndTheTierFlag()
    {
        var r = new CardRankData { season = 1, mmr = 1400, lp = 820, wins = 20, losses = 9, draws = 1, best = 910 };
        Assert.True(RankedRules.EnsureSeason(r, Day(31)));
        Assert.Equal(2, r.season);
        Assert.Equal(1200, r.mmr);          // 1000 + (1400 - 1000) / 2
        Assert.Equal(410, r.lp);            // 820 / 2
        Assert.Equal(910, r.prevBest);      // the finished season's best LP
        Assert.Equal(1 << (int)RankTier.Gold, r.rewards); // best 910 = Gold at the season's end
        Assert.Equal(0, r.wins + r.losses + r.draws); Assert.Equal(410, r.best);
        Assert.False(RankedRules.EnsureSeason(r, Day(40)));
    }

    [Fact]
    public void AWeakRatingRisesTowards1000OnReset()
    {
        var r = new CardRankData { season = 1, mmr = 800, lp = 41, wins = 1, best = 41 };
        RankedRules.EnsureSeason(r, Day(31));
        Assert.Equal(900, r.mmr); Assert.Equal(20, r.lp);
    }

    [Fact]
    public void ACharacterThatSkippedSeasonsIsHalvedOncePerSeasonAndAnInactiveSeasonEarnsNoFlag()
    {
        var r = new CardRankData { season = 1, mmr = 1400, lp = 800, wins = 1, best = 800 };
        RankedRules.EnsureSeason(r, Day(95)); // season 4: three resets
        Assert.Equal(4, r.season); Assert.Equal(1050, r.mmr); Assert.Equal(100, r.lp);
        var idle = new CardRankData { season = 1, mmr = 1100, lp = 400, best = 400 }; // never played that season: no record
        RankedRules.EnsureSeason(idle, Day(31));
        Assert.Equal(0, idle.rewards); Assert.Equal(0, idle.prevBest);
    }

    [Fact]
    public void AClockThatWentBackLeavesTheDataAlone()
    {
        var r = new CardRankData { season = 5, mmr = 1300, lp = 500 };
        Assert.False(RankedRules.EnsureSeason(r, Day(1)));
        Assert.Equal(5, r.season); Assert.Equal(500, r.lp);
    }

    // ---- entry ----

    [Fact]
    public void RankedNeedsLevelTwentyAndTheSwitchOn()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Level = 19; Assert.Equal(RankedError.Level, RankedRules.CanQueue(p));
        p.Level = 20; Assert.Equal(RankedError.None, RankedRules.CanQueue(p));
        RankedRules.Enabled = false;
        try { Assert.Equal(RankedError.Disabled, RankedRules.CanQueue(p)); }
        finally { RankedRules.Enabled = true; }
    }

    // ---- the line ----

    static RankedQueue.Waiter Wait(RankedQueue q, ushort id, int mmr, long since = 0, int party = 0) { q.Add(id, "c" + id, mmr, party, since); return q.Find(id); }

    [Fact]
    public void TheWindowIs150AndGrows100EveryThirtySeconds()
    {
        Assert.Equal(150, RankedQueue.Window(0)); Assert.Equal(150, RankedQueue.Window(29999));
        Assert.Equal(250, RankedQueue.Window(30000)); Assert.Equal(450, RankedQueue.Window(90000));
    }

    [Fact]
    public void FourHundredApartDoNotMatchInTheFirstThirtySecondsAndDoOnceTheWindowHasGrown()
    {
        var q = new RankedQueue();
        Wait(q, 1, 1000); Wait(q, 2, 1400);
        Assert.Empty(q.Match(0));
        Assert.Empty(q.Match(29999));
        Assert.Empty(q.Match(60000));   // window 350 < 400
        var made = q.Match(90000);      // window 450
        Assert.Single(made);
        Assert.Equal(0, q.Count);
    }

    [Fact]
    public void ThePlayerWhoWaitedLongerWidensTheWindowForTheNewcomer()
    {
        var q = new RankedQueue();
        Wait(q, 1, 1000, 0);
        Wait(q, 2, 1240, 55000); // 240 apart; the first has waited 55 s (window 250)
        Assert.Single(q.Match(55000));
    }

    [Fact]
    public void AnEdgeOf150MatchesAndTheClosestPartnerIsChosenForTheLongestWaiter()
    {
        var q = new RankedQueue();
        Wait(q, 1, 1000, 0); Wait(q, 2, 1150, 1); Wait(q, 3, 1040, 2);
        var made = q.Match(10);
        Assert.Single(made);
        Assert.Equal(1, made[0].A.Id); Assert.Equal(3, made[0].B.Id);
        Assert.True(q.Contains(2));
    }

    [Fact]
    public void TheSamePartyIsNeverMatchedAndAPlayerWhoIsNotReadyKeepsTheirPlace()
    {
        var q = new RankedQueue();
        Wait(q, 1, 1000, 0, party: 7); Wait(q, 2, 1000, 0, party: 7);
        Assert.Empty(q.Match(500000)); // however long they wait
        Assert.Equal(2, q.Count);
        var q2 = new RankedQueue();
        Wait(q2, 1, 1000); RankedQueue.Waiter busy = Wait(q2, 2, 1000); busy.Ready = false;
        Assert.Empty(q2.Match(0)); Assert.True(q2.Contains(2));
        busy.Ready = true;
        Assert.Single(q2.Match(1));
    }

    [Fact]
    public void ThePlayerJustMetIsNotMetAgainForTenMinutesButAnotherIs()
    {
        var q = new RankedQueue();
        Wait(q, 1, 1000); Wait(q, 2, 1000);
        Assert.Single(q.Match(0));
        q.Add(1, "c1", 1000, 0, 100000); q.Add(2, "c2", 1000, 0, 100000);
        Assert.Empty(q.Match(100000));           // 100 s later: still on the cooldown
        q.Add(3, "c3", 1010, 0, 100000);
        var made = q.Match(100001);              // 3 is new to both
        Assert.Single(made);
        q.Remove(1); q.Remove(2); q.Remove(3);
        q.Add(1, "c1", 1000, 0, 700000); q.Add(2, "c2", 1000, 0, 700000);
        Assert.Single(q.Match(700000));          // ten minutes passed
    }

    [Fact]
    public void ForgettingAPairLetsThemMeetAtOnce()
    {
        var q = new RankedQueue();
        RankedQueue.Waiter a = Wait(q, 1, 1000), b = Wait(q, 2, 1000);
        q.Match(0);
        q.ForgetPair(a, b);
        q.Add(1, "c1", 1000, 0, 5); q.Add(2, "c2", 1000, 0, 5);
        Assert.Single(q.Match(5));
    }

    [Fact]
    public void ManyWaitingPlayersAllGetMatchedInOnePassAndAnOddOneStays()
    {
        var q = new RankedQueue();
        for (ushort i = 1; i <= 5; i++) Wait(q, i, 1000 + i * 10, i);
        var made = q.Match(10);
        Assert.Equal(2, made.Count);
        Assert.Equal(1, q.Count);
    }

    [Fact]
    public void AddingTwiceOrRemovingAStrangerIsHarmless()
    {
        var q = new RankedQueue();
        Assert.True(q.Add(1, "c1", 1000, 0, 0)); Assert.False(q.Add(1, "c1", 1000, 0, 0));
        Assert.False(q.Remove(9)); Assert.True(q.Remove(1)); Assert.Equal(0, q.Count);
        Assert.Equal(0L, q.WaitedMs(1, 5000));
    }
}
