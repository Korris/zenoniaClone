using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Boots give a fixed move speed per equipment tier (design answer 13/11/2026): no scaling with item level inside a tier, rarity or enhance; the worn total is capped at +25%.</summary>
public class BootsSpeedTests
{
    [Theory]
    [InlineData(1, 0.03f)] [InlineData(9, 0.03f)] [InlineData(10, 0.04f)] [InlineData(35, 0.06f)] [InlineData(60, 0.10f)] [InlineData(90, 0.15f)]
    public void GeneratedBootsUseTheTierTableWhateverTheRarity(int itemLevel, float speed)
    {
        foreach (string rarity in new[] { "common", "rare", "epic" })
            Assert.Equal(speed, ItemFactory.Generate(RpgRandom.Default, itemLevel, Slots.Boots, rarity, "warrior").stats.speedMult, 3);
    }

    [Fact]
    public void OldScaledBootsAreRewrittenOnLoad()
    {
        Item old = ItemFactory.Generate(RpgRandom.Default, 60, Slots.Boots, "epic", "warrior");
        old.stats.speedMult = 0.36f; // what a Lv60 epic carried before the change
        Assert.Equal(0.10f, ItemFactory.Sanitize(old).stats.speedMult, 3);
    }

    [Fact]
    public void WornSpeedIsCappedAtTwentyFivePercent()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        float baseSpeed = p.Speed;
        Item boots = ItemFactory.Generate(RpgRandom.Default, 1, Slots.Boots, "epic", "warrior");
        Assert.True(p.Equip(boots).Ok);
        Assert.Equal(baseSpeed * 1.03f, p.Speed, 2);
        boots.stats.speedMult = 0.9f; // a hand-edited item
        p.RecomputeStats();
        Assert.Equal(baseSpeed * 1.25f, p.Speed, 2);
    }
}
