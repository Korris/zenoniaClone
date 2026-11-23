using Xunit;
using ZenoniaProto.Rpg;

// Phase 07 review fixes: offline results parked until login (H1), same-account block + early forfeit (H2), hidden MMR (M1), season == 0 / MMR floor (M2), pair limit constant (M3), skipped-season prevBest (L5).
public class RankedReviewFixesTests
{
    static CardRankData Rank(int mmr = 1000, int lp = 100, int season = 1) { return new CardRankData { season = season, mmr = mmr, lp = lp, best = lp }; }
    static long Day(int n) { return RankedRules.SeasonEpochMs + n * 86400000L; }

    // ---- H1: the offline player who ends up winning ----

    [Fact]
    public void AnOfflinePlayerWhoWinsKeepsTheWinNotTheProvisionalLoss()
    {
        CardRankData before = Rank();
        int lpLoss, lpWin;
        CardRankData loss = RankedRules.Apply(before, DuelResult.Lose, 1000, true, out lpLoss);   // saved when the connection dropped
        CardRankData win = RankedRules.Apply(before, DuelResult.Win, 1000, true, out lpWin);      // the opponent forfeits: the real result
        Assert.Equal(80, loss.lp); Assert.Equal(125, win.lp);
        var pending = new RankedPending();
        pending.Put("charA", win, 1000);
        CardRankData back;
        Assert.True(pending.TryTake("charA", 5000, out back));                        // logs in again: this replaces the saved loss
        Assert.Equal(125, back.lp); Assert.Equal(1, back.wins); Assert.Equal(0, back.losses); Assert.Equal(1016, back.mmr);
        Assert.False(pending.TryTake("charA", 5000, out back));                       // applied once
    }

    [Fact]
    public void BothOfflineEachGetsTheRealResultWhenTheyReturn()
    {
        int lp;
        var pending = new RankedPending();
        pending.Put("A", RankedRules.Apply(Rank(), DuelResult.Draw, 1000, true, out lp), 0);
        pending.Put("B", RankedRules.Apply(Rank(), DuelResult.Draw, 1000, true, out lp), 0);
        CardRankData ra, rb;
        Assert.True(pending.TryTake("B", 10, out rb)); Assert.True(pending.TryTake("A", 20, out ra));
        Assert.Equal(100, ra.lp); Assert.Equal(1, ra.draws); Assert.Equal(0, ra.losses);
        Assert.Equal(100, rb.lp); Assert.Equal(1, rb.draws);
    }

    [Fact]
    public void AParkedResultIsACopyAndExpiresAfterADay()
    {
        CardRankData r = Rank();
        var pending = new RankedPending();
        pending.Put("A", r, 0); r.lp = 9999;
        CardRankData back;
        Assert.True(pending.TryTake("A", 1000, out back)); Assert.Equal(100, back.lp);
        pending.Put("A", r, 0);
        Assert.False(pending.TryTake("A", RankedPending.MaxAgeMs + 1, out back));
        Assert.Equal(0, pending.Count);
    }

    [Fact]
    public void ParkingManyCharactersPurgesTheOldOnes()
    {
        var pending = new RankedPending();
        for (int i = 0; i < 300; i++) pending.Put("c" + i, Rank(), 0);
        pending.Put("fresh", Rank(), RankedPending.MaxAgeMs + 5);
        Assert.Equal(1, pending.Count);
    }

    // ---- H2: same account, early forfeit ----

    [Fact]
    public void TwoCharactersOfOneAccountNeverMatchButAStrangerDoes()
    {
        var q = new RankedQueue();
        q.Add(1, "c1", 1000, 0, 0, "acct-1"); q.Add(2, "c2", 1000, 0, 0, "acct-1");
        Assert.Empty(q.Match(0));
        Assert.Empty(q.Match(300000));                       // the window growing does not matter
        q.Add(3, "c3", 1100, 0, 0, "acct-2");
        var made = q.Match(300000);
        Assert.Single(made);
        Assert.NotEqual(made[0].A.Account, made[0].B.Account);
    }

    [Fact]
    public void TheRematchCooldownIsPerAccountPairNotPerCharacter()
    {
        var q = new RankedQueue();
        q.Add(1, "c1", 1000, 0, 0, "acct-1"); q.Add(2, "c2", 1000, 0, 0, "acct-2");
        Assert.Single(q.Match(0));
        q.Add(1, "c1b", 1000, 0, 10, "acct-1"); q.Add(2, "c2b", 1000, 0, 10, "acct-2"); // other characters, same two accounts
        Assert.Empty(q.Match(20));
        Assert.Equal(2, q.Count);
    }

    [Fact]
    public void WithoutAnAccountTheKeyIsTheAccount()
    {
        var q = new RankedQueue();
        q.Add(1, "c1", 1000, 0, 0); q.Add(2, "c2", 1000, 0, 0);
        Assert.Equal("c1", q.Find(1).Account);
        Assert.Single(q.Match(0));
    }

    [Fact]
    public void AForfeitBeforeThreeMovesByHandCountsForNobody()
    {
        Assert.True(RankedRules.EarlyForfeit(0, 0)); Assert.True(RankedRules.EarlyForfeit(1, 2));
        Assert.False(RankedRules.EarlyForfeit(1, 3));
        Assert.False(RankedRules.EarlyForfeit(-1, 0));       // played out: always counts
        CardRankData r = Rank();
        int lp;
        CardRankData same = RankedRules.Apply(r, DuelResult.Win, 1000, false, out lp);  // what the zone plays when it does not count
        Assert.Equal(0, lp); Assert.Equal(r.lp, same.lp); Assert.Equal(r.mmr, same.mmr); Assert.Equal(0, same.wins);
    }

    // ---- M1: the rating is not sent ----

    [Fact]
    public void TheSaveSentToAClientHasNoRatingAndTheServersOwnStateKeepsIt()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Cards.Rank = Rank(1234, 400);
        SaveData sent = SaveSerializer.Capture(p, false, 0f, 0f, false);
        RankedRules.HideRating(sent);
        Assert.Equal(0, sent.cardRank.mmr); Assert.Equal(400, sent.cardRank.lp);
        Assert.Equal(1234, p.Cards.Rank.mmr);                 // the server keeps its own
        Assert.Equal(1234, SaveSerializer.Capture(p, false, 0f, 0f, false).cardRank.mmr);
    }

    [Fact]
    public void ARankWithAHiddenRatingKeepsItsLpWhenTheClientRollsASeasonForDisplay()
    {
        var r = Rank(0, 500);                                  // what a client mirror holds
        RankedRules.EnsureSeason(r, Day(3));
        Assert.Equal(500, r.lp); Assert.Equal(1, r.mmr);
    }

    // ---- M2: season == 0 is the only "never played"; MMR floor ----

    [Fact]
    public void AnMmrOfZeroIsRaisedToOneNotResetToANewCharacter()
    {
        var r = new CardRankData { season = 1, mmr = 0, lp = 250, wins = 4, best = 300 };
        Assert.True(RankedRules.EnsureSeason(r, Day(2)));
        Assert.Equal(1, r.mmr); Assert.Equal(250, r.lp); Assert.Equal(300, r.best); Assert.Equal(4, r.wins);
    }

    [Fact]
    public void MmrNeverFallsBelowOne()
    {
        int lp;
        Assert.Equal(1, RankedRules.Apply(Rank(1, 100), DuelResult.Lose, 3000, true, out lp).mmr);
        Assert.Equal(1, RankedRules.Apply(Rank(10, 100), DuelResult.Lose, 10, true, out lp).mmr);
    }

    [Fact]
    public void OnlySeasonZeroGetsTheStartRating()
    {
        var fresh = new CardRankData();
        RankedRules.EnsureSeason(fresh, Day(1));
        Assert.Equal(1000, fresh.mmr); Assert.Equal(1, fresh.season);
    }

    // ---- M3: one constant for the Ranked pair limit ----

    [Fact]
    public void TheRankedLedgerUsesTheRankedLimit()
    {
        var ledger = new WagerRules.PairLedger(RankedRules.MaxMatchesPerPairPerDay);
        for (int i = 0; i < RankedRules.MaxMatchesPerPairPerDay; i++) { Assert.True(ledger.Allowed("a", "b", 5)); ledger.Note("a", "b", 5); }
        Assert.False(ledger.Allowed("b", "a", 5));
        var three = new WagerRules.PairLedger(3);
        for (int i = 0; i < 2; i++) three.Note("a", "b", 5);
        Assert.True(three.Allowed("a", "b", 5));
    }

    // ---- L5: prevBest is the season that just ended ----

    [Fact]
    public void PrevBestIsTheSeasonThatJustEndedAndZeroWhenSeasonsWereSkipped()
    {
        var next = new CardRankData { season = 1, mmr = 1100, lp = 400, wins = 3, best = 450 };
        RankedRules.EnsureSeason(next, Day(31));
        Assert.Equal(450, next.prevBest);
        var skipped = new CardRankData { season = 1, mmr = 1100, lp = 400, wins = 3, best = 450 };
        RankedRules.EnsureSeason(skipped, Day(65));            // season 3: season 2 was never played
        Assert.Equal(0, skipped.prevBest);
        Assert.NotEqual(0, skipped.rewards & (1 << (int)RankTier.Silver)); // the season 1 reward flag is still earned
    }
}
