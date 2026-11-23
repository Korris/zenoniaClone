using Xunit;
using ZenoniaProto.Rpg;

// Card drop chances: per kill of the monster, N 1 %, R 0.5 %, SR 0.2 %, boss 3 %.
public class CardDropTests
{
    /// <summary>Evenly spread values in [0, 1): over n calls exactly chance * n of them fall below the chance.</summary>
    static Rand Even(int n) { int i = 0; return () => ((i++ % n) + 0.5) / n; }

    static int Drops(string monster, int rolls, Rand rand)
    {
        int hits = 0;
        for (int i = 0; i < rolls; i++) if (CardDrops.Roll(monster, rand) != null) hits++;
        return hits;
    }

    [Theory]
    [InlineData("meadowSlime", 100)]     // N 1 %
    [InlineData("frostSlime", 50)]       // R 0.5 %
    [InlineData("duneGolem", 20)]        // SR 0.2 %
    [InlineData("frostfangKing", 300)]   // boss 3 %
    public void TenThousandRollsHitTheTargetRate(string monster, int expected)
    {
        Assert.Equal(expected, Drops(monster, 10000, Even(10000)));
    }

    [Fact]
    public void SeededRngStaysWithinTenPercentOfTheNormalRate()
    {
        var rng = new SeededRng(12345);
        int hits = Drops("meadowSlime", 200000, rng.Next);
        Assert.InRange(hits, 1800, 2200); // 1 % of 200000 = 2000, +-10 %
    }

    [Fact]
    public void RollReturnsTheCardIdOfThatMonster()
    {
        Assert.Equal("thornBoar", CardDrops.Roll("thornBoar", () => 0.0));
    }

    [Fact]
    public void NoCardForAMonsterWithoutOne()
    {
        Assert.Null(CardDrops.Roll("glacierWarden", () => 0.0)); // a map boss outside the 40
        Assert.Null(CardDrops.Roll("nope", () => 0.0));
        Assert.Null(CardDrops.Roll(null, () => 0.0));
        Assert.Null(CardDrops.Roll("meadowSlime", null));
    }

    [Fact]
    public void RollAboveTheChanceDropsNothing()
    {
        Assert.Null(CardDrops.Roll("meadowSlime", () => 0.0100001));
        Assert.Null(CardDrops.Roll("frostfangKing", () => 0.03));
    }

    [Fact]
    public void DisabledSwitchStopsEveryDrop()
    {
        CardDrops.Enabled = false;
        try { Assert.Null(CardDrops.Roll("meadowSlime", () => 0.0)); }
        finally { CardDrops.Enabled = true; }
    }
}
