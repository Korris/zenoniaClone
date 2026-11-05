using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Weapon / off-hand names (WeaponNames) and the equip level by tier (EquipLevel). PlayerState.Equip is the rule the zone server runs on every Equip request.</summary>
public class EquipLevelTests
{
    static Item Gear(string cls, string slot, int itemLevel, string rarity = "common", string uid = null)
    {
        Item i = ItemFactory.Generate(() => 0.0, itemLevel, slot, rarity, cls);
        if (uid != null) i.uid = uid;
        return i;
    }

    [Fact]
    public void EveryClassSlotAndTierHasItsOwnName()
    {
        foreach (string cls in RpgClasses.Ids)
            foreach (string slot in new[] { Slots.Weapon, Slots.Offhand })
            {
                var seen = new HashSet<string>();
                for (int tier = 0; tier < WeaponNames.Tiers; tier++)
                {
                    string name = WeaponNames.BaseName(cls, slot, tier);
                    Assert.False(string.IsNullOrWhiteSpace(name), cls + " " + slot + " " + tier);
                    Assert.True(seen.Add(name), "duplicate name " + name + " (" + cls + " " + slot + ")");
                    Assert.True(("Legendary " + name).Length <= 40, name + " too long for the 40-char item name");
                }
            }
    }

    [Fact]
    public void GeneratedGearIsNamedByTierWithRarityOnlyAsPrefix()
    {
        Item epic = Gear("mechanic", Slots.Weapon, 95, "epic");
        Assert.Equal("Legendary Dragonfire Twin", epic.name);
        Assert.Equal("Scrap Repeater", Gear("mechanic", Slots.Weapon, 1).name);
        Assert.Equal("Dragon Maw Cannon", Gear("mechanic", Slots.Offhand, 90).name);
        Assert.Equal("Fine Skull Sickle", Gear("assassin", Slots.Weapon, 70, "uncommon").name);
        Assert.NotEqual(Gear("mechanic", Slots.Weapon, 30, "epic").name, Gear("mechanic", Slots.Weapon, 50, "epic").name);
    }

    [Fact]
    public void StarterGearUsesTheTierZeroName()
    {
        PlayerState p = PlayerState.NewGame("wizard");
        Assert.Equal("Apprentice Staff", p.GetEquipped(Slots.Weapon).name);
        Assert.Equal("Apprentice Tome", p.GetEquipped(Slots.Offhand).name);
    }

    [Fact]
    public void SanitizeRenamesOldGenericNames()
    {
        Item old = Gear("mechanic", Slots.Weapon, 55, "epic");
        old.name = "Legendary Gun";
                Item clean = ItemFactory.Sanitize(old);
        Assert.Equal("Legendary Frost Shardgun", clean.name);
    }

    [Theory]
    [InlineData(1, 0, 1)]
    [InlineData(9, 0, 1)]
    [InlineData(10, 1, 5)]
    [InlineData(20, 2, 10)]
    [InlineData(30, 3, 15)]
    [InlineData(40, 4, 20)]
    [InlineData(50, 5, 30)]
    [InlineData(60, 6, 35)]
    [InlineData(70, 7, 40)]
    [InlineData(80, 8, 50)]
    [InlineData(90, 9, 60)]
    [InlineData(95, 9, 60)]
    public void RequiredLevelFollowsTheArtTier(int itemLevel, int tier, int required)
    {
        foreach (string slot in new[] { Slots.Weapon, Slots.Offhand })
        {
            Item i = Gear("warrior", slot, itemLevel);
            Assert.Equal(tier, ItemInfo.ArtTier(i));
            Assert.Equal(required, EquipLevel.Required(i));
        }
        Assert.Equal(60, Leveling.MaxLevel);
    }

    [Fact]
    public void EnhancementAndArmorDoNotChangeTheRequirement()
    {
        Item w = Gear("warrior", Slots.Weapon, 50);
        w.enhance = 12;
        Assert.Equal(30, EquipLevel.Required(w));
        Item helm = ItemFactory.Generate(() => 0.0, 90, Slots.Helmet, "common");
        Assert.Equal(1, EquipLevel.Required(helm));
        Assert.Equal("", EquipLevel.Label(helm));
        Assert.Equal("Req. Lv 30", EquipLevel.Label(w));
    }

    [Fact]
    public void EquipIsRefusedBelowTheLevelAndAllowedAtIt()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        Item sword = Gear("warrior", Slots.Weapon, 50, "common", "w50");
        p.Inventory.Add(sword);
        p.Level = 29;
        ActionResult low = p.Equip(sword);
        Assert.False(low.Ok);
        Assert.Contains("30", low.Text);
        Assert.Contains(sword, p.Inventory);
        Assert.NotEqual("w50", p.GetEquipped(Slots.Weapon).uid);
        p.Level = 30;
        Assert.True(p.Equip(sword).Ok);
        Assert.Equal("w50", p.GetEquipped(Slots.Weapon).uid);
    }

    [Fact]
    public void WornGearAboveTheLevelStaysAndOffhandIsGatedToo()
    {
        PlayerState p = PlayerState.NewGame("mechanic");
        Item cannon = Gear("mechanic", Slots.Offhand, 90, "epic", "o90");
        p.Inventory.Add(cannon);
        Assert.False(p.Equip(cannon).Ok);
        p.SetEquippedRaw(Slots.Offhand, cannon); // an old save wore it already
        p.Inventory.Remove(cannon);
        p.Level = 3;
        Assert.Equal("o90", p.GetEquipped(Slots.Offhand).uid);
        Assert.True(p.Unequip(Slots.Offhand).Ok); // taking it off is allowed, putting it back is not
        Assert.False(p.Equip(cannon).Ok);
    }

    [Fact]
    public void NewHeroWearsItsStarterAtLevelOne()
    {
        foreach (string cls in RpgClasses.Ids)
        {
            PlayerState p = PlayerState.NewGame(cls);
            Assert.Equal(1, p.Level);
            Assert.True(p.MeetsLevel(p.GetEquipped(Slots.Weapon)));
            Assert.True(p.MeetsLevel(p.GetEquipped(Slots.Offhand)));
        }
    }
}
