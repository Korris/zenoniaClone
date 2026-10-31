using Xunit;
using ZenoniaProto.Rpg;

public class PotionTests
{
    static PlayerState Hurt()
    {
        var p = new PlayerState("warrior");
        p.Hp = 1f; p.Mp = 1f;
        return p;
    }

    [Theory]
    [InlineData(PotionCatalog.Small, 30)]
    [InlineData(PotionCatalog.Medium, 50)]
    [InlineData(PotionCatalog.Large, 80)]
    public void EachSizeRestoresItsShareOfTheMaximum(int size, int percent)
    {
        PlayerState p = Hurt();
        p.Gold = 1000;
        Assert.True(p.BuyPotion(true, size).Ok);
        Assert.True(p.UsePotion(true, size).Ok);
        Assert.Equal(1f + RpgMath.Round(p.MaxHp * percent / 100.0), p.Hp);
        Assert.Equal(0, p.PotionCount(true, size));
    }

    [Fact]
    public void QuickButtonDrinksThePickThenFallsToTheNextOwnedSize()
    {
        PlayerState p = Hurt();
        p.HpPotions = 1; p.HpPotionsLarge = 1;
        Assert.Equal(PotionCatalog.Small, p.QuickPotionSize(true));
        p.SetPotionPick(true, PotionCatalog.Medium);
        Assert.Equal(PotionCatalog.Large, p.QuickPotionSize(true)); // no Medium: the next size up
        Assert.True(p.UsePotion(true).Ok);
        Assert.Equal(0, p.HpPotionsLarge);
        Assert.Equal(PotionCatalog.Small, p.QuickPotionSize(true)); // only a smaller one is left
        p.Hp = 1f;
        Assert.True(p.UsePotion(true).Ok);
        Assert.Equal(PotionCatalog.Medium, p.QuickPotionSize(true)); // none left: back to the pick
        Assert.False(p.UsePotion(true).Ok);
    }

    [Fact]
    public void UsingFromTheBagSetsTheQuickSizeEvenAtFullHealth()
    {
        var p = new PlayerState("warrior");
        p.MpPotionsMedium = 2;
        Assert.False(p.UsePotion(false, PotionCatalog.Medium).Ok);
        Assert.Equal(PotionCatalog.Medium, p.PotionPick(false));
        Assert.Equal(2, p.MpPotionsMedium);
        Assert.Equal(0, p.PotionPick(true)); // the other button is untouched
    }

    [Fact]
    public void BuyingChargesGoldAndRefusesWithoutEnough()
    {
        var p = new PlayerState("wizard");
        p.Gold = PotionCatalog.Price(PotionCatalog.Medium);
        Assert.False(p.BuyPotion(false, PotionCatalog.Large).Ok);
        Assert.Equal(PotionCatalog.Price(PotionCatalog.Medium), p.Gold);
        Assert.True(p.BuyPotion(false, PotionCatalog.Medium).Ok);
        Assert.Equal(0, p.Gold);
        Assert.Equal(1, p.MpPotionsMedium);
        Assert.False(p.BuyPotion(false, PotionCatalog.Small).Ok);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(126)]
    public void ABadSizeNeverChargesOrAdds(int size)
    {
        var p = new PlayerState("wizard") { Gold = 5000 };
        Assert.False(p.BuyPotion(true, size).Ok);
        Assert.False(p.UsePotion(true, size).Ok);
        Assert.Equal(5000, p.Gold);
    }

    [Fact]
    public void AFullStackRefusesTheSale()
    {
        var p = new PlayerState("cleric") { Gold = 5000, HpPotions = PotionCatalog.MaxStack };
        Assert.False(p.BuyPotion(true, PotionCatalog.Small).Ok);
        Assert.Equal(5000, p.Gold);
    }

    [Fact]
    public void SlotAndEventBytesRoundTrip()
    {
        foreach (bool hp in new[] { true, false })
            for (int size = 0; size < PotionCatalog.Sizes; size++)
            {
                byte slot = PotionCatalog.PackSlot(hp, size);
                Assert.Equal(hp, PotionCatalog.SlotIsHp(slot));
                Assert.Equal(size, PotionCatalog.SlotSize(slot));
                byte step = PotionCatalog.PackEvent(hp, size);
                Assert.Equal(hp, PotionCatalog.EventIsHp(step));
                Assert.Equal(size, PotionCatalog.EventSize(step));
            }
        Assert.True(PotionCatalog.SlotIsHp(0)); Assert.Equal(-1, PotionCatalog.SlotSize(0)); // the old "drink HP" byte still means the quick button
        Assert.False(PotionCatalog.SlotIsHp(1)); Assert.Equal(-1, PotionCatalog.SlotSize(1));
    }

    [Fact]
    public void OakhavenSellsTwoSizesAndSunspireThree()
    {
        Assert.True(PotionShops.Sells("healerNina", PotionCatalog.Medium));
        Assert.False(PotionShops.Sells("healerNina", PotionCatalog.Large));
        Assert.False(PotionShops.Sells("merchantDale", PotionCatalog.Large));
        Assert.True(PotionShops.Sells("apothecaryNara", PotionCatalog.Large));
        Assert.False(PotionShops.IsSeller("guardRook"));
    }

    [Fact]
    public void AServerRefusesAWrongMapOrAFarAwayBuyer()
    {
        Assert.Null(PotionShops.Refusal("apothecaryNara", PotionShops.Sunspire, PotionCatalog.Large, 20f));
        Assert.NotNull(PotionShops.Refusal("apothecaryNara", PotionShops.Oakhaven, PotionCatalog.Small, 20f)); // that villager is not on this map
        Assert.NotNull(PotionShops.Refusal("healerNina", PotionShops.Oakhaven, PotionCatalog.Large, 20f));
        Assert.NotNull(PotionShops.Refusal("healerNina", PotionShops.Oakhaven, PotionCatalog.Small, PotionShops.ReachUnits + 1f));
        Assert.NotNull(PotionShops.Refusal("guardRook", PotionShops.Oakhaven, PotionCatalog.Small, 1f));
        Assert.NotNull(PotionShops.Refusal("healerNina", PotionShops.Oakhaven, PotionCatalog.SlotSize(0), 1f)); // a buy with no size byte
    }
}
