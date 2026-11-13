using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

public class GearShopTests
{
    // ---- catalog shape ----

    [Fact]
    public void EachSellerStocksItsOwnTierRangeOnly()
    {
        Assert.True(GearShops.IsSeller("smithBram"));
        Assert.True(GearShops.IsSeller("smithHolt"));
        Assert.True(GearShops.IsSeller("blacksmithKhaled"));
        Assert.False(GearShops.IsSeller("healerNina")); // potions only
        Assert.False(GearShops.IsSeller("guardRook"));

        Assert.True(GearShops.Sells("smithBram", GearShops.Encode(0, 0)));
        Assert.True(GearShops.Sells("smithBram", GearShops.Encode(1, 5)));
        Assert.False(GearShops.Sells("smithBram", GearShops.Encode(2, 0))); // Thatchwick's tier

        Assert.True(GearShops.Sells("smithHolt", GearShops.Encode(2, 0)));
        Assert.True(GearShops.Sells("smithHolt", GearShops.Encode(3, 5)));
        Assert.False(GearShops.Sells("smithHolt", GearShops.Encode(1, 0)));
        Assert.False(GearShops.Sells("smithHolt", GearShops.Encode(4, 0)));

        Assert.True(GearShops.Sells("blacksmithKhaled", GearShops.Encode(4, 0)));
        Assert.True(GearShops.Sells("blacksmithKhaled", GearShops.Encode(5, 5)));
        Assert.False(GearShops.Sells("blacksmithKhaled", GearShops.Encode(3, 0)));
    }

    [Fact]
    public void EncodeAndDecodeRoundTrip()
    {
        for (int tier = 0; tier < GearShops.Tiers; tier++)
            for (int slotIndex = 0; slotIndex < Slots.Generated.Length; slotIndex++)
            {
                int entry = GearShops.Encode(tier, slotIndex);
                int outTier, outSlot;
                Assert.True(GearShops.Decode(entry, out outTier, out outSlot));
                Assert.Equal(tier, outTier);
                Assert.Equal(slotIndex, outSlot);
            }
        Assert.False(GearShops.Decode(-1, out _, out _));
        Assert.False(GearShops.Decode(GearShops.EntryCount, out _, out _));
    }

    [Theory]
    [InlineData("smithBram", PotionShops.Oakhaven, 0, 10f, null)]
    [InlineData("smithHolt", PotionShops.Thatchwick, 2, 10f, null)]
    [InlineData("blacksmithKhaled", PotionShops.Sunspire, 4, 10f, null)]
    [InlineData("smithBram", PotionShops.Thatchwick, 0, 10f, "Nobody sells that here")]
    [InlineData("smithBram", PotionShops.Oakhaven, 0, 500f, "Too far from the shop")]
    [InlineData("hacker", PotionShops.Oakhaven, 0, 0f, "Nobody sells that here")]
    public void TheSellerMustBeRightHereAndInReach(string npc, string map, int tier, float distance, string refusal)
    {
        Assert.Equal(refusal, GearShops.Refusal(npc, map, GearShops.Encode(tier, 0), distance));
    }

    [Fact]
    public void ASellerRefusesAnEntryOutsideItsOwnTiers()
    {
        Assert.Equal("Not sold here", GearShops.Refusal("smithBram", PotionShops.Oakhaven, GearShops.Encode(2, 0), 0f));
    }

    // ---- price ----

    [Fact]
    public void PriceAlwaysGrowsWithTierForEverySlot()
    {
        for (int slotIndex = 0; slotIndex < Slots.Generated.Length; slotIndex++)
            for (int tier = 0; tier < GearShops.Tiers - 1; tier++)
                Assert.True(GearCatalog.Price(tier, slotIndex) < GearCatalog.Price(tier + 1, slotIndex));
    }

    [Fact]
    public void AWeaponOrOffhandCostsMoreThanArmourOfTheSameTier()
    {
        int weapon = GearCatalog.Price(1, System.Array.IndexOf(Slots.Generated, Slots.Weapon));
        int offhand = GearCatalog.Price(1, System.Array.IndexOf(Slots.Generated, Slots.Offhand));
        int helmet = GearCatalog.Price(1, System.Array.IndexOf(Slots.Generated, Slots.Helmet));
        int boots = GearCatalog.Price(1, System.Array.IndexOf(Slots.Generated, Slots.Boots));
        Assert.True(weapon > helmet);
        Assert.True(weapon > boots);
        Assert.True(offhand > helmet);
    }

    // ---- the item itself ----

    [Fact]
    public void EveryShopPieceIsBasic_PlusZeroCommonAtTheTierFloor()
    {
        for (int tier = 0; tier < 6; tier++)
            for (int slotIndex = 0; slotIndex < Slots.Generated.Length; slotIndex++)
            {
                Item item = GearCatalog.Preview(tier, slotIndex, "warrior");
                Assert.Equal("common", item.rarity);
                Assert.Equal(0, item.enhance);
                Assert.Equal(GearCatalog.FloorItemLevel(tier), item.itemLevel);
                Assert.Equal(ItemDefs.EquipmentTier(item.itemLevel), tier);
            }
    }

    [Fact]
    public void WeaponAndOffhandCarryTheBuyersClass_ArmourCarriesNone()
    {
        Item weapon = GearCatalog.Preview(1, System.Array.IndexOf(Slots.Generated, Slots.Weapon), "wizard");
        Item offhand = GearCatalog.Preview(1, System.Array.IndexOf(Slots.Generated, Slots.Offhand), "wizard");
        Item armor = GearCatalog.Preview(1, System.Array.IndexOf(Slots.Generated, Slots.Armor), "wizard");
        Assert.Equal("wizard", weapon.classId);
        Assert.Equal("wizard", offhand.classId);
        Assert.Equal("spellbook", offhand.templateId); // the wizard's own offhand type, not any class's
        Assert.True(string.IsNullOrEmpty(armor.classId));
    }

    [Fact]
    public void SameTierAndSlotAlwaysPreviewsTheSamePiece()
    {
        Item a = GearCatalog.Preview(3, 0, "assassin");
        Item b = GearCatalog.Preview(3, 0, "assassin");
        Assert.Equal(a.templateId, b.templateId);
        Assert.Equal(a.name, b.name);
        Assert.Equal(a.stats.atk, b.stats.atk);
    }

    [Fact]
    public void BuyRollsAFreshUidButTheSamePiece()
    {
        double[] rolls = { 0.1, 0.9 };
        int next = 0;
        Rand r = () => rolls[next++];
        Item a = GearCatalog.Buy(0, 0, "warrior", r);
        Item b = GearCatalog.Buy(0, 0, "warrior", r);
        Assert.NotEqual(a.uid, b.uid);
        Assert.Equal(a.templateId, b.templateId);
    }

    // ---- PlayerState.BuyGear ----

    [Fact]
    public void BuyingPaysOnceAndPutsItInTheBag()
    {
        var p = new PlayerState("warrior");
        int slotIndex = System.Array.IndexOf(Slots.Generated, Slots.Armor);
        int price = GearCatalog.Price(0, slotIndex);
        p.Gold = price + 1;
        Assert.True(p.BuyGear(0, slotIndex).Ok);
        Assert.Equal(1, p.Gold);
        Item bought = p.Inventory.Last();
        Assert.Equal(Slots.Armor, bought.slot);
        Assert.Equal("common", bought.rarity);
    }

    [Fact]
    public void BuyingRefusesWithoutGoldOrRoomOrABadEntry()
    {
        var p = new PlayerState("warrior");
        int slotIndex = System.Array.IndexOf(Slots.Generated, Slots.Weapon);
        int price = GearCatalog.Price(0, slotIndex);
        p.Gold = price - 1;
        Assert.False(p.BuyGear(0, slotIndex).Ok);
        Assert.Equal(price - 1, p.Gold);
        Assert.Empty(p.Inventory);

        p.Gold = price * 100;
        while (p.Inventory.Count < PlayerState.InventorySize) p.Inventory.Add(GearCatalog.Preview(0, 0, "warrior"));
        int before = p.Gold;
        Assert.False(p.BuyGear(0, slotIndex).Ok);
        Assert.Equal(before, p.Gold);

        var q = new PlayerState("warrior") { Gold = 100000 };
        Assert.False(q.BuyGear(-1, 0).Ok);
        Assert.False(q.BuyGear(GearShops.Tiers, 0).Ok);
        Assert.False(q.BuyGear(0, -1).Ok);
        Assert.False(q.BuyGear(0, Slots.Generated.Length).Ok);
    }

    [Fact]
    public void TwoPurchasesNeverShareAUid()
    {
        var p = new PlayerState("warrior");
        int slotIndex = System.Array.IndexOf(Slots.Generated, Slots.Boots);
        p.Gold = GearCatalog.Price(0, slotIndex) * 2;
        double[] rolls = { 0.5, 0.25 };
        int next = 0;
        Rand r = () => rolls[next++];
        p.BuyGear(0, slotIndex, r);
        p.BuyGear(0, slotIndex, r);
        Assert.Equal(2, p.Inventory.Select(i => i.uid).Distinct().Count());
    }

    // ---- save round trip (the shop item is just a normal item) ----

    [Fact]
    public void ABoughtPieceSurvivesASave()
    {
        var p = new PlayerState("cleric");
        int slotIndex = System.Array.IndexOf(Slots.Generated, Slots.Offhand);
        p.Gold = GearCatalog.Price(1, slotIndex);
        Assert.True(p.BuyGear(1, slotIndex).Ok);
        string uid = p.Inventory.Last().uid;
        string json = SaveSerializer.ToJson(SaveSerializer.Capture(p, true, 1f, 2f, false));
        PlayerState back = SaveSerializer.Restore(SaveSerializer.Parse(json));
        Assert.Contains(back.Inventory, i => i.uid == uid && i.slot == Slots.Offhand);
    }
}
