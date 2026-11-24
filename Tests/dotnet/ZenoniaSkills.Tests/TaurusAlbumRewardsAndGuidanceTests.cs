using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

// Phase 03b: Taurus Album milestone rewards (150 G + 2 / 300 G + 3 / 500 G + 5 / 1000 G + 5 + title flag) and Orin's Guidance (2 duels not won in a row -> offer, accepted flag saved).
public class TaurusAlbumRewardsAndGuidanceTests
{
    static PlayerState Hero() { return PlayerState.NewGame("warrior"); }
    static void Fill(PlayerState p, int n) { for (int i = 0; i < n; i++) p.AddCard(CardTable.All[CardTable.LiveStart + i].Id); }

    [Fact]
    public void EachMilestonePaysItsGoldAndStonesOnce()
    {
        PlayerState p = Hero();
        Fill(p, CardTable.LiveCount);
        int[] gold = { 150, 300, 500, 1000 }, stones = { 2, 3, 5, 5 };
        for (int m = 0; m < 4; m++)
        {
            int g = p.Gold, s = p.UpgradeStone;
            Assert.True(AlbumRules.Claim(p, m).Ok);
            Assert.Equal(g + gold[m], p.Gold);
            Assert.Equal(s + stones[m], p.UpgradeStone);
            Assert.False(AlbumRules.Claim(p, m).Ok);        // once per character
            Assert.Equal(g + gold[m], p.Gold);
            Assert.Equal(s + stones[m], p.UpgradeStone);
        }
        Assert.True(AlbumRules.Claimed(p.Cards, AlbumRules.CollectorTitle)); // the title flag (no title system yet)
        Assert.Equal(1950, gold.Sum());
        Assert.Equal(15, stones.Sum());
    }

    [Fact]
    public void MilestonesNeedTheirSpeciesCountAndTheTitleNeedsAll12()
    {
        PlayerState p = Hero();
        Assert.Equal(12, CardTable.LiveCount);
        Fill(p, 5);
        Assert.True(AlbumRules.Claim(p, 0).Ok);
        Assert.False(AlbumRules.Claim(p, 1).Ok);            // 6 needs 6
        Fill(p, 11);
        Assert.False(AlbumRules.Claim(p, 3).Ok);            // 12 needs 12
        Assert.False(AlbumRules.Claimed(p.Cards, AlbumRules.CollectorTitle));
    }

    [Fact]
    public void ClaimsCountSpeciesEverRegisteredEvenAfterTheCardsAreGone()
    {
        PlayerState p = Hero();
        Fill(p, 6);
        foreach (CardDef c in CardTable.All.Skip(CardTable.LiveStart).Take(6)) p.Cards.Remove(c.Id); // sold / traded: the Album keeps them
        Assert.Equal(6, p.Cards.AlbumCount);
        Assert.True(AlbumRules.Claim(p, 1).Ok);
        Fill(p, 6);                                          // collecting the same cards again pays nothing more
        Assert.False(AlbumRules.Claim(p, 1).Ok);
    }

    [Fact]
    public void ACappedStoneBagRefusesTheClaimAndKeepsTheBitFree()
    {
        PlayerState p = Hero();
        Fill(p, 3);
        p.UpgradeStone = PlayerState.MaxStones - 1;
        int gold = p.Gold;
        Assert.False(AlbumRules.Claim(p, 0).Ok);
        Assert.Equal(0, p.Cards.AlbumClaimed);
        Assert.Equal(gold, p.Gold);
        p.UpgradeStone = 0;
        Assert.True(AlbumRules.Claim(p, 0).Ok);
    }

    [Fact]
    public void GuidanceIsOfferedAfterTwoNotWonDuelsInARow()
    {
        CardCollection c = Hero().Cards;
        Assert.False(OrinGuidance.ShouldOffer(c));
        OrinGuidance.Record(c, DuelResult.Lose);
        Assert.False(OrinGuidance.ShouldOffer(c));           // 1st loss: a normal rematch
        OrinGuidance.Record(c, DuelResult.Draw);             // a draw counts, like a give-up (Forfeit finishes as a loss)
        Assert.True(OrinGuidance.ShouldOffer(c));
        OrinGuidance.Record(c, DuelResult.Playing);          // a running view is no result
        Assert.Equal(2, c.OrinLosses);
    }

    [Fact]
    public void AWinClearsTheStreakAndDecliningStartsItAgain()
    {
        CardCollection c = Hero().Cards;
        OrinGuidance.Record(c, DuelResult.Lose);
        OrinGuidance.Record(c, DuelResult.Win);
        OrinGuidance.Record(c, DuelResult.Lose);
        Assert.False(OrinGuidance.ShouldOffer(c));
        OrinGuidance.Record(c, DuelResult.Lose);
        Assert.True(OrinGuidance.ShouldOffer(c));
        OrinGuidance.Decline(c);                             // "Try alone"
        Assert.False(OrinGuidance.ShouldOffer(c));
        OrinGuidance.Record(c, DuelResult.Lose); OrinGuidance.Record(c, DuelResult.Lose);
        Assert.True(OrinGuidance.ShouldOffer(c));            // asks again after 2 more
    }

    [Fact]
    public void AcceptedGuidanceIsSavedStaysAndStopsTheOffer()
    {
        PlayerState p = Hero();
        OrinGuidance.Record(p.Cards, DuelResult.Lose); OrinGuidance.Record(p.Cards, DuelResult.Lose);
        Assert.True(CardOps.MarkHelp(p, CardCollection.HelpGuidance).Ok); // InvOp.CardHelp on the zone / offline
        Assert.True(OrinGuidance.Accepted(p.Cards));
        Assert.False(OrinGuidance.ShouldOffer(p.Cards));
        SaveData d = SaveSerializer.Parse(SaveSerializer.ToJson(SaveSerializer.Capture(p, false, 0f, 0f, false, 1)));
        PlayerState q = SaveSerializer.Restore(d);
        Assert.True(OrinGuidance.Accepted(q.Cards));         // persists with the character
        Assert.Equal(0, q.Cards.OrinLosses);                 // the streak itself is per session
        OrinGuidance.Record(q.Cards, DuelResult.Lose); OrinGuidance.Record(q.Cards, DuelResult.Lose);
        Assert.True(OrinGuidance.Accepted(q.Cards));         // more losses keep it
    }
}
