using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Armor / gloves / boots icons are drawn per class tier set ("armor-wizard-4"); necklaces (slot id "helmet") and the fallback use the shared name.</summary>
public class ArmorClassIconTests
{
    static Item Piece(string slot, int itemLevel, string classId = null)
    {
        return new Item { slot = slot, itemLevel = itemLevel, classId = classId };
    }

    [Fact]
    public void ArmourUsesTheViewersClassAndTier()
    {
        Assert.Equal("armor-wizard-4", ItemInfo.ClassIconName(Piece(Slots.Armor, 45), "wizard"));
        Assert.Null(ItemInfo.ClassIconName(Piece(Slots.Helmet, 1), "warrior")); // the necklace (id "helmet") is shared by every class
        Assert.Equal("helmet-0", ItemInfo.IconName(Piece(Slots.Helmet, 1)));
        Assert.Equal("gloves-cleric-9", ItemInfo.ClassIconName(Piece(Slots.Gloves, 95), "cleric"));
        Assert.Equal("boots-assassin-2", ItemInfo.ClassIconName(Piece(Slots.Boots, 20), "assassin"));
    }

    [Fact]
    public void ItemClassTagWinsOverTheViewer()
    {
        Assert.Equal("boots-mechanic-3", ItemInfo.ClassIconName(Piece(Slots.Boots, 30, "mechanic"), "warrior"));
    }

    [Fact]
    public void NoClassOrNotArmourGivesNullSoCallersUseTheSharedIcon()
    {
        Assert.Null(ItemInfo.ClassIconName(Piece(Slots.Armor, 45), null));
        Assert.Null(ItemInfo.ClassIconName(Piece(Slots.Armor, 45), "nobody"));
        Assert.Null(ItemInfo.ClassIconName(Piece(Slots.Weapon, 45, "wizard"), "wizard"));
        Assert.Null(ItemInfo.ClassIconName(null, "wizard"));
        Assert.Null(ItemInfo.ClassIconName(Piece(Slots.Helmet, 45), "wizard")); // the helmet slot is a shared necklace set
        Assert.Equal("helmet-4", ItemInfo.IconName(Piece(Slots.Helmet, 45)));
        Assert.Equal("armor-4", ItemInfo.IconName(Piece(Slots.Armor, 45)));
    }
}
