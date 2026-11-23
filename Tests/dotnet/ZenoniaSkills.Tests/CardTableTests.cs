using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

// The 40 monster cards: ids are real monsters (the Unity side re-checks them against MonsterDef.All at start-up, CardIdsCheck), numbers fit the rarity bands.
public class CardTableTests
{
    // Hard-coded on purpose: a renamed or dropped card must be a conscious change.
    static readonly string[] ExpectedIds =
    {
        "meadowSlime", "hornRabbit", "thornBoar", "meadowMantis", "forestWolf", "brambleTreant", "thornSprite", "thornfangAlpha", "caveMantis", "gloomWolf", "crystalSprite", "hollowTreant", "ambercapWarden",
        "frostSlime", "snowFox", "iceImp", "iceWraith", "frostBear", "frostbiteYeti", "crystalGolem", "frostfangKing", "rimeclawTyrant", "iceGoblin", "spectralWolf",
        "duneScarab", "sandRaider", "amberWisp", "bogSlime", "mireToad", "marshStalker", "ruinWisp", "mirefangHydra",
        "highlandIbex", "dustscaleLizard", "cliffHarpy", "stonebackGuardian", "giantScorpion", "sandWraith", "rockBasilisk", "duneGolem",
    };

    [Fact]
    public void HasFortyUniqueCardsWithTheExpectedIds()
    {
        Assert.Equal(CardTable.Count, CardTable.All.Length);
        Assert.Equal(40, CardTable.All.Select(c => c.Id).Distinct().Count());
        Assert.Equal(ExpectedIds.OrderBy(x => x), CardTable.All.Select(c => c.Id).OrderBy(x => x));
    }

    [Fact]
    public void FiveBossesAreTheSsrCardsAndNothingElseIsSsr()
    {
        var bosses = CardTable.All.Where(c => c.IsBoss).Select(c => c.Id).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "ambercapWarden", "frostfangKing", "mirefangHydra", "rimeclawTyrant", "thornfangAlpha" }, bosses);
        foreach (CardDef c in CardTable.All) Assert.Equal(c.IsBoss, c.Rarity == CardRarity.SSR);
        Assert.Equal(35, CardTable.All.Count(c => !c.IsBoss));
    }

    [Fact]
    public void EdgesAreOneToNineWithTheRaritySumAndOneClearlyStrongestEdge()
    {
        foreach (CardDef c in CardTable.All)
        {
            int[] e = { c.Top, c.Right, c.Bottom, c.Left };
            Assert.All(e, v => Assert.InRange(v, 1, 9));
            Assert.InRange(c.EdgeSum, CardTable.EdgeSumMin[(int)c.Rarity], CardTable.EdgeSumMax[(int)c.Rarity]);
            Assert.True(e.Count(v => v == c.MaxEdge) == 1, c.Id + " needs one strongest edge");
        }
    }

    [Fact]
    public void RegionsAreKnownAndEveryRegionHasCards()
    {
        foreach (CardDef c in CardTable.All) Assert.Contains(c.Region, CardTable.Regions);
        foreach (string r in CardTable.Regions) Assert.Contains(CardTable.All, c => c.Region == r);
    }

    [Fact]
    public void LookupByIdAndIndexAgree()
    {
        for (int i = 0; i < CardTable.Count; i++) { Assert.Equal(i, CardTable.Index(CardTable.All[i].Id)); Assert.Same(CardTable.All[i], CardTable.ByIndex(i)); }
        Assert.Null(CardTable.Get("nope")); Assert.Null(CardTable.Get(null)); Assert.Equal(-1, CardTable.Index("")); Assert.Null(CardTable.ByIndex(40));
    }

    [Fact]
    public void EveryCardHasNameDescriptionAndElement()
    {
        foreach (CardDef c in CardTable.All) { Assert.False(string.IsNullOrWhiteSpace(c.Name)); Assert.False(string.IsNullOrWhiteSpace(c.Description)); Assert.False(string.IsNullOrWhiteSpace(c.Element)); }
    }

    [Theory]
    [InlineData("meadowSlime", GemKind.Power, GemStat.MaxHp, 40)]    // HP card, N
    [InlineData("meadowSlime", GemKind.Guard, GemStat.Def, 4)]
    [InlineData("hornRabbit", GemKind.Power, GemStat.Atk, 6)]
    [InlineData("thornBoar", GemKind.Guard, GemStat.MaxHp, 40)]      // a Def card guards with HP
    [InlineData("snowFox", GemKind.Power, GemStat.Crit, 2)]          // Utility crit, R
    [InlineData("amberWisp", GemKind.Power, GemStat.MaxMp, 50)]      // Utility MP, SR
    [InlineData("frostfangKing", GemKind.Power, GemStat.Atk, 32)]    // boss SSR
    public void GemOptionsFollowCategoryAndRarity(string id, GemKind kind, GemStat stat, int value)
    {
        GemOption o = CardTable.Get(id).Option(kind);
        Assert.Equal(stat, o.Stat); Assert.Equal(value, o.Value);
    }

    [Fact]
    public void GemValuesGrowWithRarity()
    {
        foreach (GemStat s in new[] { GemStat.Atk, GemStat.Def, GemStat.MaxHp, GemStat.MaxMp, GemStat.Crit })
            for (int r = 1; r < 4; r++) Assert.True(GemTable.Value(s, (CardRarity)r) > GemTable.Value(s, (CardRarity)(r - 1)));
    }
}
