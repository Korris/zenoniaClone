using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Designer 08/11: the "helmet" slot id (saves, server) is shown as a stat-only Necklace.</summary>
public class NecklaceSlotTests
{
    [Fact]
    public void HelmetSlotIdIsKeptButShownAsNecklace()
    {
        Assert.Equal("helmet", Slots.Helmet);
        Assert.Equal("Necklace", ItemInfo.SlotLabel(Slots.Helmet));
    }

    [Fact]
    public void GeneratedNecklacesUseTheTierNames()
    {
        Assert.Equal("Cord Necklace", ItemFactory.Generate(() => 0.0, 1, Slots.Helmet, "common").name);
        Assert.Equal("Legendary Dragon Amulet", ItemFactory.Generate(() => 0.0, 95, Slots.Helmet, "epic").name);
        foreach (string n in ItemDefs.ArmorTierNames[Slots.Helmet]) Assert.DoesNotContain("Helm", n);
    }

    [Fact]
    public void OldSavedHelmetNamesAreRenamedOnLoad()
    {
        var raw = new Item { uid = "ironHelm-x", templateId = "ironHelm", slot = Slots.Helmet, name = "Fine Steel Helm", rarity = "uncommon", itemLevel = 12, stats = new ItemStats { def = 4, maxHp = 15 } };
        Item s = ItemFactory.Sanitize(raw);
        Assert.Equal("Fine Steel Pendant", s.name);
        Assert.Equal("ironHelm", s.templateId);
        Assert.Equal(Slots.Helmet, s.slot);
    }

    [Fact]
    public void NecklacesAreSharedByEveryClass()
    {
        Item made = ItemFactory.Generate(() => 0.0, 45, Slots.Helmet, "rare", "wizard");
        Assert.Null(made.classId);
        Item tagged = ItemFactory.Sanitize(new Item { templateId = "knightHelm", slot = Slots.Helmet, rarity = "common", itemLevel = 45, classId = "wizard", stats = new ItemStats { def = 6 } });
        Assert.Null(tagged.classId); // a class tag from an untrusted save never locks it
        Assert.Null(ItemInfo.ClassIconName(tagged, "wizard"));
        Assert.Equal("helmet-4", ItemInfo.IconName(tagged));
        var p = new PlayerState("cleric");
        Assert.True(p.CanEquip(made));
    }

    [Fact]
    public void OnlyNecklaceGlovesAndBootsLeaveTheLookAlone()
    {
        Assert.False(ItemInfo.ChangesAppearance(Slots.Helmet));
        Assert.False(ItemInfo.ChangesAppearance(Slots.Gloves));
        Assert.False(ItemInfo.ChangesAppearance(Slots.Boots));
        Assert.True(ItemInfo.ChangesAppearance(Slots.Weapon));
        Assert.True(ItemInfo.ChangesAppearance(Slots.Offhand));
        Assert.True(ItemInfo.ChangesAppearance(Slots.Armor));
        Assert.True(ItemInfo.ChangesAppearance(Slots.Pet));
    }
}
