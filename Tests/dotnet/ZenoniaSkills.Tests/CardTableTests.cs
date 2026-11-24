using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

// The 52 monster cards (40 pre-Taurus + 12 Taurus): ids are real monsters (the Unity side re-checks them against MonsterDef.All at start-up, CardIdsCheck), numbers fit the rarity bands.
public class CardTableTests
{
    // Hard-coded on purpose: a renamed or dropped card must be a conscious change.
    static readonly string[] ExpectedIds =
    {
        "meadowSlime", "hornRabbit", "thornBoar", "meadowMantis", "forestWolf", "brambleTreant", "thornSprite", "thornfangAlpha", "caveMantis", "gloomWolf", "crystalSprite", "hollowTreant", "ambercapWarden",
        "frostSlime", "snowFox", "iceImp", "iceWraith", "frostBear", "frostbiteYeti", "crystalGolem", "frostfangKing", "rimeclawTyrant", "iceGoblin", "spectralWolf",
        "duneScarab", "sandRaider", "amberWisp", "bogSlime", "mireToad", "marshStalker", "ruinWisp", "mirefangHydra",
        "highlandIbex", "dustscaleLizard", "cliffHarpy", "stonebackGuardian", "giantScorpion", "sandWraith", "rockBasilisk", "duneGolem",
        // Taurus V4: appended (saves store the index), 8 N + 3 R + 1 SR boss
        "tauCloverSlime", "tauHornRabbit", "tauReedFrog", "tauMossMushroom", "tauBrambleBoar", "tauLeafMantis", "tauRootling", "tauAmberSporeling",
        "tauThornSprite", "tauAncientBarkling", "tauRuneBarkGuardian", "tauElderRootGuardian",
        "tauBramblebackMatriarch", // Field Boss card, outside the Album
    };

    [Fact]
    public void HasFiftyThreeUniqueCardsWithTheExpectedIdsAndTaurusThenTheFieldBossLast()
    {
        Assert.Equal(CardTable.Count, CardTable.All.Length);
        Assert.Equal(53, CardTable.All.Select(c => c.Id).Distinct().Count());
        Assert.Equal(CardTable.LiveCount, CardTable.All.Count(CardTable.IsLive));
        Assert.All(CardTable.All.Skip(CardTable.LiveStart).Take(CardTable.LiveCount), c => Assert.True(CardTable.IsLive(c)));
        Assert.Equal("tauBramblebackMatriarch", CardTable.All[52].Id); Assert.False(CardTable.IsLive(CardTable.All[52])); // the Field Boss card: appended, outside the Album // append-only: the 40 old rows keep their indexes, the Taurus rows follow
        Assert.All(CardTable.All.Take(40), c => Assert.False(CardTable.IsLive(c)));
        Assert.Equal(ExpectedIds.OrderBy(x => x), CardTable.All.Select(c => c.Id).OrderBy(x => x));
    }

    [Fact]
    public void FiveOldBossesAreTheSsrCardsAndTheTaurusBossIsSr()
    {
        var bosses = CardTable.All.Where(c => c.IsBoss).Select(c => c.Id).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "ambercapWarden", "frostfangKing", "mirefangHydra", "rimeclawTyrant", "tauBramblebackMatriarch", "tauElderRootGuardian", "thornfangAlpha" }, bosses);
        Assert.Equal(CardRarity.R, CardTable.Get("tauBramblebackMatriarch").Rarity); // ChatGPT: Rare, never SSR
        foreach (CardDef c in CardTable.All.Take(40)) Assert.Equal(c.IsBoss, c.Rarity == CardRarity.SSR);
        Assert.Equal(CardRarity.SR, CardTable.Get("tauElderRootGuardian").Rarity); // BIBLE X.2: no SSR / UR at Taurus launch
        Assert.Equal(CardRarity.N, CardTable.Get("tauCloverSlime").Rarity);
        Assert.Equal(8, CardTable.All.Count(c => CardTable.IsLive(c) && c.Rarity == CardRarity.N));
        Assert.Equal(3, CardTable.All.Count(c => CardTable.IsLive(c) && c.Rarity == CardRarity.R));
        Assert.Equal(35, CardTable.All.Take(40).Count(c => !c.IsBoss));
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
        foreach (CardDef c in CardTable.All) Assert.True(System.Array.IndexOf(CardTable.Regions, c.Region) >= 0 || c.Region == CardTable.FieldBossRegion || System.Array.IndexOf(CardTable.LegacyRegions, c.Region) >= 0, c.Id);
        foreach (string r in CardTable.Regions) Assert.Contains(CardTable.All, c => c.Region == r);
        foreach (string r in CardTable.LegacyRegions) Assert.Contains(CardTable.All, c => c.Region == r);
    }

    [Fact]
    public void LookupByIdAndIndexAgree()
    {
        for (int i = 0; i < CardTable.Count; i++) { Assert.Equal(i, CardTable.Index(CardTable.All[i].Id)); Assert.Same(CardTable.All[i], CardTable.ByIndex(i)); }
        Assert.Null(CardTable.Get("nope")); Assert.Null(CardTable.Get(null)); Assert.Equal(-1, CardTable.Index("")); Assert.Null(CardTable.ByIndex(53)); Assert.Null(CardTable.ByIndex(-1));
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
