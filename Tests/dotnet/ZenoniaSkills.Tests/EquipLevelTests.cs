using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Weapon / off-hand names (WeaponNames) and the equip level by tier (EquipLevel: every gear slot, T(n) = Lv n*10 since 16/11/2026). PlayerState.Equip is the rule the zone server runs on every Equip request.</summary>
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
    [InlineData(10, 1, 10)]
    [InlineData(20, 2, 20)]
    [InlineData(30, 3, 30)]
    [InlineData(39, 3, 30)]
    [InlineData(40, 4, 40)]
    [InlineData(50, 5, 50)]
    [InlineData(60, 6, 60)]
    [InlineData(70, 7, 70)]
    [InlineData(80, 8, 80)]
    [InlineData(90, 9, 90)]
    [InlineData(95, 9, 90)]
    public void RequiredLevelIsTenTimesTheArtTierOnEveryGearSlot(int itemLevel, int tier, int required)
    {
        foreach (string slot in Slots.Generated)
        {
            Item i = Gear("warrior", slot, itemLevel);
            Assert.Equal(tier, ItemInfo.ArtTier(i));
            Assert.Equal(required, EquipLevel.Required(i));
        }
        Assert.Equal(90, Leveling.MaxLevel);
        Assert.Equal(Leveling.MaxLevel, EquipLevel.ByTier[EquipLevel.ByTier.Length - 1]); // T9 is reachable exactly at the cap
    }

    [Fact]
    public void EnhancementDoesNotChangeTheRequirementAndPetsHaveNone()
    {
        Item w = Gear("warrior", Slots.Weapon, 50);
        w.enhance = 12;
        Assert.Equal(50, EquipLevel.Required(w));
        Assert.Equal("Req. Lv 50", EquipLevel.Label(w));
        Item helm = ItemFactory.Generate(() => 0.0, 30, Slots.Helmet, "common");
        Assert.Equal(30, EquipLevel.Required(helm));
        Assert.Equal("Req. Lv 30", EquipLevel.Label(helm));
        Item pet = new Item { uid = "p", slot = Slots.Pet, templateId = "pet-spirit", itemLevel = 90 };
        Assert.Equal(1, EquipLevel.Required(pet));
        Assert.Equal("", EquipLevel.Label(pet));
    }

    [Fact]
    public void EquipIsRefusedBelowTheLevelAndAllowedAtIt()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        Item sword = Gear("warrior", Slots.Weapon, 35, "common", "w35");
        p.Inventory.Add(sword);
        p.Level = 29;
        ActionResult low = p.Equip(sword);
        Assert.False(low.Ok);
        Assert.Contains("30", low.Text);
        Assert.Contains(sword, p.Inventory);
        Assert.NotEqual("w35", p.GetEquipped(Slots.Weapon).uid);
        p.Level = 30;
        Assert.True(p.Equip(sword).Ok);
        Assert.Equal("w35", p.GetEquipped(Slots.Weapon).uid);

        Item boots = Gear("warrior", Slots.Boots, 20, "common", "b20");
        p.Inventory.Add(boots);
        p.Level = 19;
        Assert.False(p.Equip(boots).Ok); // armor is gated too since 16/11/2026
        p.Level = 20;
        Assert.True(p.Equip(boots).Ok);
    }

    [Fact]
    public void UnequipIsAlwaysAllowedEvenAboveTheLevel()
    {
        PlayerState p = PlayerState.NewGame("mechanic");
        Item cannon = Gear("mechanic", Slots.Offhand, 90, "epic", "o90");
        p.Inventory.Add(cannon);
        Assert.False(p.Equip(cannon).Ok);
        p.SetEquippedRaw(Slots.Offhand, cannon);
        p.Inventory.Remove(cannon);
        p.Level = 3;
        Assert.True(p.Unequip(Slots.Offhand).Ok); // taking it off is allowed, putting it back is not
        Assert.False(p.Equip(cannon).Ok);
    }

    static PlayerState Reload(PlayerState p) { return SaveSerializer.Restore(SaveSerializer.Parse(SaveSerializer.ToJson(SaveSerializer.Capture(p, true, 1f, 2f, false)))); }

    [Fact]
    public void OldSaveWearingGearAboveTheLevelLoadsWithItInTheBagAndTheStarterBack()
    {
        // Lv 25 hero of the old table: a T4 weapon (was Lv 20, now Lv 40) and T3 armor (no gate before, now Lv 30) no longer fit; a T2 helmet (Lv 20) still does.
        PlayerState p = PlayerState.NewGame("warrior");
        p.Level = 25;
        p.SetEquippedRaw(Slots.Weapon, Gear("warrior", Slots.Weapon, 45, "rare", "w45"));
        p.SetEquippedRaw(Slots.Armor, Gear("warrior", Slots.Armor, 30, "common", "a30"));
        p.SetEquippedRaw(Slots.Helmet, Gear("warrior", Slots.Helmet, 25, "common", "h25"));
        PlayerState back = Reload(p);
        Assert.Equal("starter-weapon", back.GetEquipped(Slots.Weapon).uid);
        Assert.Null(back.GetEquipped(Slots.Armor));
        Assert.Equal("h25", back.GetEquipped(Slots.Helmet).uid);
        Assert.Contains(back.Inventory, i => i.uid == "w45" && i.rarity == "rare");
        Assert.Contains(back.Inventory, i => i.uid == "a30");
        Assert.DoesNotContain(back.Inventory, i => i.uid == "starter-weapon");
    }

    [Fact]
    public void WithAFullBagGearAboveTheLevelStaysWornRatherThanBeingLost()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Level = 5;
        p.SetEquippedRaw(Slots.Weapon, Gear("warrior", Slots.Weapon, 45, "epic", "w45"));
        while (p.Inventory.Count < PlayerState.InventorySize) p.Inventory.Add(Gear("warrior", Slots.Gloves, 1, "common", "g" + p.Inventory.Count));
        PlayerState back = Reload(p);
        Assert.Equal("w45", back.GetEquipped(Slots.Weapon).uid);
        Assert.Equal(PlayerState.InventorySize, back.Inventory.Count);
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
