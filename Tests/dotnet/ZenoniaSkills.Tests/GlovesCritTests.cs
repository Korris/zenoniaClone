using UnityEngine;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>
/// Gloves give a fixed crit chance per equipment tier (16/11/2026): no scaling with item level inside a tier or with rarity. The off-hand dagger keeps its flat 2%.
/// Before, a Lv60 epic pair carried +84.1% and nearly every hit was a critical hit.
/// </summary>
public class GlovesCritTests
{
    [Theory]
    [InlineData(1, 0.02f)] [InlineData(9, 0.02f)] [InlineData(10, 0.03f)] [InlineData(35, 0.05f)] [InlineData(60, 0.08f)] [InlineData(89, 0.10f)] [InlineData(90, 0.12f)] [InlineData(95, 0.12f)]
    public void GeneratedGlovesUseTheTierTableWhateverTheRarity(int itemLevel, float crit)
    {
        Assert.Equal(crit, ItemDefs.GlovesCrit(itemLevel), 3);
        foreach (string rarity in ItemDefs.RarityOrder)
            Assert.Equal(crit, ItemFactory.Generate(RpgRandom.Default, itemLevel, Slots.Gloves, rarity, "warrior").stats.critChance, 3);
    }

    [Fact]
    public void RarityStillRaisesTheGlovesAtk()
    {
        Item common = ItemFactory.Generate(() => 0.0, 60, Slots.Gloves, "common", "warrior"), epic = ItemFactory.Generate(() => 0.0, 60, Slots.Gloves, "epic", "warrior");
        Assert.True(epic.stats.atk > common.stats.atk);
    }

    [Fact]
    public void OldScaledGlovesAreRewrittenOnLoad()
    {
        Item old = ItemFactory.Generate(RpgRandom.Default, 60, Slots.Gloves, "epic", "warrior");
        old.stats.critChance = 0.841f; // what a Lv60 epic carried before the change
        Assert.Equal(0.08f, ItemFactory.Sanitize(old).stats.critChance, 3);
    }

    [Fact]
    public void OldScaledGlovesAreRewrittenThroughASaveRoundTrip()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        Item old = ItemFactory.Generate(RpgRandom.Default, 60, Slots.Gloves, "epic", "warrior");
        old.stats.critChance = 0.841f;
        Assert.True(p.AddToInventory(old));
        PlayerState loaded = SaveSerializer.Restore(SaveSerializer.Parse(JsonUtility.ToJson(SaveSerializer.Capture(p, false, 0f, 0f, false))));
        Item back = loaded.Inventory.Find(i => i.uid == old.uid);
        Assert.NotNull(back);
        Assert.Equal(0.08f, back.stats.critChance, 3);
    }

    [Theory]
    [InlineData(1, "common")] [InlineData(60, "epic")] [InlineData(90, "epic")]
    public void OffhandDaggerCritIsAFlatTwoPercent(int itemLevel, string rarity)
    {
        Item dagger = ItemFactory.Generate(RpgRandom.Default, itemLevel, Slots.Offhand, rarity, "assassin");
        Assert.Equal("dagger", dagger.templateId);
        Assert.Equal(0.02f, dagger.stats.critChance, 3);
        dagger.stats.critChance = 0.24f; // what a Lv60 epic dagger carried before the change
        Assert.Equal(0.02f, ItemFactory.Sanitize(dagger).stats.critChance, 3);
    }

    [Fact]
    public void ItemsWithoutCritStayAtZero()
    {
        Item boots = ItemFactory.Generate(RpgRandom.Default, 60, Slots.Boots, "epic", "warrior");
        boots.stats.critChance = 0.5f; // a hand-edited item
        Assert.Equal(0f, ItemFactory.Sanitize(boots).stats.critChance);
        Assert.Equal(0f, ItemFactory.Generate(RpgRandom.Default, 60, Slots.Offhand, "epic", "warrior").stats.critChance);
    }

    [Fact]
    public void WornGlovesAddTheirTierCritToTheHero()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        float before = p.CritBonus;
        p.SetEquippedRaw(Slots.Gloves, ItemFactory.Generate(RpgRandom.Default, 60, Slots.Gloves, "epic", "warrior"));
        p.RecomputeStats();
        Assert.Equal(before + 0.08f, p.CritBonus, 3);
    }
}
