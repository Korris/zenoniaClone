using System;
using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

public class PetTests
{
    static readonly PetDef Spirit = PetConfig.Get("spirit"), Imp = PetConfig.Get("imp");

    static Item Pet(PetDef d, string uid = null)
    {
        Item i = ItemFactory.CreatePet(d, () => 0.5);
        if (uid != null) i.uid = uid;
        return i;
    }

    static PlayerState Hero(string cls = "warrior", int level = 20)
    {
        PlayerState p = PlayerState.NewGame(cls);
        p.Level = level; p.RecomputeStats(); p.Hp = p.MaxHp;
        return p;
    }

    // ---- the numbers ----

    [Fact]
    public void TheStartingNumbersAreWhatWasAsked()
    {
        Assert.Equal(0.15, Spirit.MaxHpPct);
        Assert.Equal(0.0, Spirit.DamagePct);
        Assert.Equal(0.10, Imp.DamagePct);
        Assert.Equal(0.0, Imp.MaxHpPct);
        Assert.True(Spirit.Price > 0 && Imp.Price > 0);
    }

    [Theory]
    [InlineData("warrior")]
    [InlineData("wizard")]
    [InlineData("mechanic")]
    [InlineData("cleric")]
    [InlineData("assassin")]
    public void SpiritRaisesMaxHpByFifteenPercentOnEveryClass(string cls)
    {
        PlayerState p = Hero(cls);
        int before = p.MaxHp, atk = p.Atk;
        Assert.True(p.Equip(Pet(Spirit)).Ok);
        Assert.Equal(RpgMath.Round(before * 1.15), p.MaxHp);
        Assert.Equal(atk, p.Atk);
        Assert.Equal(atk, p.DamageAtk);
        Assert.True(p.Unequip(Slots.Pet).Ok);
        Assert.Equal(before, p.MaxHp);
    }

    [Fact]
    public void SpiritMultipliesTheSecondWeaponHpBonus()
    {
        PlayerState p = Hero("warrior");
        p.GetEquipped(Slots.Offhand).enhance = 15;
        p.RecomputeStats();
        BaseStats b = Leveling.StatsForLevel(20, "warrior");
        int gear = EnhanceStats.Effective(p.GetEquipped(Slots.Weapon)).maxHp + EnhanceStats.Effective(p.GetEquipped(Slots.Offhand)).maxHp;
        Assert.Equal(RpgMath.Round((b.MaxHp + gear) * (1 + p.Second.HpPct)), p.MaxHp);
        p.Equip(Pet(Spirit));
        Assert.Equal(RpgMath.Round((b.MaxHp + gear) * (1 + p.Second.HpPct) * 1.15), p.MaxHp);
    }

    [Fact]
    public void ImpRaisesWhatHitsAreRolledFromButNotTheHealingStatOrMaxHp()
    {
        PlayerState p = Hero("cleric");
        int atk = p.Atk, hp = p.MaxHp;
        Assert.Equal(atk, p.DamageAtk);
        p.Equip(Pet(Imp));
        Assert.Equal((float)(atk * 1.1), p.DamageAtk);
        Assert.Equal(atk, p.Atk);
        Assert.Equal(hp, p.MaxHp);
        p.Unequip(Slots.Pet);
        Assert.Equal(atk, p.DamageAtk);
    }

    [Fact]
    public void AnEmptySlotChangesNothing()
    {
        PlayerState p = Hero();
        Assert.Null(p.GetEquipped(Slots.Pet));
        Assert.Equal(p.Atk, p.DamageAtk);
    }

    // ---- equip rules ----

    [Theory]
    [InlineData("warrior")]
    [InlineData("wizard")]
    [InlineData("mechanic")]
    [InlineData("cleric")]
    [InlineData("assassin")]
    public void EveryClassCanWearAPet(string cls)
    {
        PlayerState p = Hero(cls);
        Item pet = Pet(Spirit);
        Assert.True(p.CanEquip(pet));
        p.Inventory.Add(pet);
        Assert.True(p.Equip(pet).Ok);
        Assert.Same(pet, p.GetEquipped(Slots.Pet));
        Assert.DoesNotContain(p.Inventory, i => i.slot == Slots.Pet);
    }

    [Fact]
    public void OnePetAtATime_TheOldOneGoesBackToTheBag_AndItsBonusStops()
    {
        PlayerState p = Hero();
        int hp = p.MaxHp;
        Item a = Pet(Spirit, "a"), b = Pet(Imp, "b");
        p.Inventory.Add(a); p.Inventory.Add(b);
        p.Equip(a);
        p.Equip(b);
        Assert.Same(b, p.GetEquipped(Slots.Pet));
        Assert.Contains(a, p.Inventory);
        Assert.Equal(hp, p.MaxHp);
    }

    [Fact]
    public void SwappingPetsWorksInAFullBagButTakingOneOffDoesNot()
    {
        PlayerState p = Hero();
        Item a = Pet(Spirit, "a");
        p.Equip(a);
        while (p.Inventory.Count < PlayerState.InventorySize) p.Inventory.Add(Pet(Imp, "x" + p.Inventory.Count));
        Assert.False(p.Unequip(Slots.Pet).Ok);
        Assert.True(p.Equip(p.Inventory[0]).Ok);
        Assert.Contains(a, p.Inventory);
    }

    [Fact]
    public void HpShrinksToTheNewMaximumWhenTheSpiritComesOff()
    {
        PlayerState p = Hero();
        p.Equip(Pet(Spirit));
        p.Hp = p.MaxHp;
        p.Unequip(Slots.Pet);
        Assert.Equal(p.MaxHp, p.Hp);
    }

    [Fact]
    public void ThePetSlotIsSeparateFromTheOffhand_AndNewHeroesHaveNone()
    {
        PlayerState p = PlayerState.NewGame("cleric");
        Assert.Null(p.GetEquipped(Slots.Pet));
        Item off = p.GetEquipped(Slots.Offhand);
        p.Equip(Pet(Spirit));
        Assert.Same(off, p.GetEquipped(Slots.Offhand));
    }

    [Fact]
    public void MonstersNeverDropPets()
    {
        var r = new Random(7);
        for (int i = 0; i < 2000; i++) Assert.NotEqual(Slots.Pet, ItemFactory.RollEquipmentDrop(20, true, () => r.NextDouble()).slot);
    }

    // ---- enhance ----

    [Fact]
    public void EnhanceRefusesAPet_WornOrInTheBag_AndTakesNoStone()
    {
        PlayerState p = Hero();
        p.UpgradeStone = 5;
        Item worn = Pet(Spirit, "w"), bag = Pet(Imp, "b");
        p.Equip(worn); p.Inventory.Add(bag);
        foreach (Item i in new[] { worn, bag })
        {
            Assert.False(EnhanceStats.IsEnhanceable(i));
            EnhanceResult r = EnhanceSystem.Attempt(p, i);
            Assert.False(r.Ok);
            Assert.Equal("not-weapon", r.Reason);
            Assert.Equal(0, i.enhance);
            Assert.Equal("", ItemInfo.EnhancePreview(i));
        }
        Assert.Equal(5, p.UpgradeStone);
    }

    // ---- shop ----

    [Fact]
    public void BuyingPaysOnceAndPutsItInTheBag()
    {
        PlayerState p = Hero();
        p.Gold = Imp.Price + 1;
        Assert.True(p.BuyPet(Imp).Ok);
        Assert.Equal(1, p.Gold);
        Item bought = p.Inventory.Last();
        Assert.Equal(Slots.Pet, bought.slot);
        Assert.Equal("pet-imp", bought.templateId);
        Assert.Equal(1, p.PetCount(Imp));
        Assert.Equal(0, p.PetCount(Spirit));
        p.Equip(bought);
        Assert.Equal(1, p.PetCount(Imp));
    }

    [Fact]
    public void BuyingRefusesWithoutGoldOrRoom_AndTakesNothing()
    {
        PlayerState p = Hero();
        p.Gold = Spirit.Price - 1;
        Assert.False(p.BuyPet(Spirit).Ok);
        Assert.Equal(Spirit.Price - 1, p.Gold);
        Assert.Empty(p.Inventory);
        p.Gold = Spirit.Price;
        while (p.Inventory.Count < PlayerState.InventorySize) p.Inventory.Add(Pet(Imp, "x" + p.Inventory.Count));
        Assert.False(p.BuyPet(Spirit).Ok);
        Assert.Equal(Spirit.Price, p.Gold);
        Assert.False(p.BuyPet(null).Ok);
    }

    [Fact]
    public void TwoPurchasesNeverShareAUid()
    {
        PlayerState p = Hero();
        p.Gold = Spirit.Price * 2;
        double[] rolls = { 0.5, 0.5, 0.25 };
        int next = 0;
        Rand r = () => rolls[next++];
        p.BuyPet(Spirit, r);
        p.BuyPet(Spirit, r);
        Assert.Equal(2, p.Inventory.Select(i => i.uid).Distinct().Count());
    }

    [Theory]
    [InlineData("merchantDale", PotionShops.Oakhaven, 10f, null)]
    [InlineData("apothecaryNara", PotionShops.Sunspire, 10f, null)]
    [InlineData("healerNina", PotionShops.Oakhaven, 10f, "Nobody sells that here")]
    [InlineData("merchantDale", PotionShops.Sunspire, 10f, "Nobody sells that here")]
    [InlineData("merchantDale", PotionShops.Oakhaven, 500f, "Too far from the shop")]
    [InlineData("hacker", PotionShops.Oakhaven, 0f, "Nobody sells that here")]
    public void TheSellerMustBeRightHereAndInReach(string npc, string map, float distance, string refusal)
    {
        Assert.Equal(refusal, PetConfig.Refusal(npc, map, 0, distance));
    }

    [Fact]
    public void AnUnknownPetIndexIsNotSold()
    {
        Assert.Equal("Not sold here", PetConfig.Refusal("merchantDale", PotionShops.Oakhaven, PetConfig.All.Length, 0f));
    }

    // ---- save ----

    static PlayerState RoundTrip(PlayerState p)
    {
        string json = SaveSerializer.ToJson(SaveSerializer.Capture(p, true, 1f, 2f, false));
        return SaveSerializer.Restore(SaveSerializer.Parse(json));
    }

    [Fact]
    public void WornAndBaggedPetsSurviveASaveAndKeepTheirBonus()
    {
        PlayerState p = Hero();
        Item worn = Pet(Spirit, "pet-spirit-a"), bag = Pet(Imp, "pet-imp-b");
        p.Equip(worn); p.Inventory.Add(bag); p.Hp = p.MaxHp;
        PlayerState back = RoundTrip(p);
        Assert.Equal("pet-spirit-a", back.GetEquipped(Slots.Pet).uid);
        Assert.Equal("pet-spirit", back.GetEquipped(Slots.Pet).templateId);
        Assert.Contains(back.Inventory, i => i.uid == "pet-imp-b" && i.slot == Slots.Pet);
        Assert.Equal(p.MaxHp, back.MaxHp);
        Assert.Equal(p.Hp, back.Hp); // the part of the HP above the base maximum is not lost on load
        Assert.Equal(p.DamageAtk, back.DamageAtk);
    }

    [Fact]
    public void AnOldSaveHasAnEmptySlot()
    {
        const string old = "{\"version\":1,\"classId\":\"warrior\",\"level\":5,\"mapId\":\"frostHollow\",\"inventory\":[],\"equipment\":[{\"uid\":\"starter-weapon\",\"templateId\":\"sword\",\"slot\":\"weapon\",\"name\":\"Sword\",\"rarity\":\"common\",\"classId\":\"warrior\",\"itemLevel\":1,\"stats\":{\"atk\":4}}]}";
        PlayerState p = SaveSerializer.Restore(SaveSerializer.Parse(old));
        Assert.Null(p.GetEquipped(Slots.Pet));
        Assert.Equal(p.Atk, p.DamageAtk);
        Assert.NotNull(p.GetEquipped(Slots.Weapon));
    }

    [Fact]
    public void ASaveCannotGiveAPetStatsALevelOrAClass()
    {
        const string hacked = "{\"version\":1,\"classId\":\"warrior\",\"level\":5,\"inventory\":[],\"equipment\":[{\"uid\":\"p\",\"templateId\":\"pet-imp\",\"slot\":\"pet\",\"name\":\"Imp\",\"rarity\":\"rare\",\"classId\":\"wizard\",\"itemLevel\":1,\"enhance\":9,\"stats\":{\"atk\":500,\"maxHp\":500}}]}";
        PlayerState p = SaveSerializer.Restore(SaveSerializer.Parse(hacked));
        Item pet = p.GetEquipped(Slots.Pet);
        Assert.NotNull(pet); // the class tag is dropped, so it stays on
        Assert.Equal(0, pet.enhance);
        Assert.True(pet.stats.IsZero);
    }

    [Fact]
    public void TheOffhandCalledSpiritIsNotThePet()
    {
        const string json = "{\"version\":1,\"classId\":\"warrior\",\"level\":5,\"inventory\":[{\"uid\":\"p\",\"templateId\":\"spirit\",\"slot\":\"pet\",\"name\":\"x\",\"rarity\":\"rare\"},{\"uid\":\"q\",\"templateId\":\"pet-spirit\",\"slot\":\"offhand\",\"name\":\"x\",\"rarity\":\"rare\"}],\"equipment\":[]}";
        Assert.Empty(SaveSerializer.Parse(json).inventory);
    }

    // ---- durability ----

    [Fact]
    public void ANewPetStartsAtTheMaximum()
    {
        Assert.Equal(PetConfig.MaxDurability, Pet(Spirit).durability);
        Assert.Equal(1000, PetConfig.MaxDurability);
    }

    [Fact]
    public void TheSpiritLosesOnePerHitTaken_AndNothingForAttacks()
    {
        PlayerState p = Hero();
        p.Equip(Pet(Spirit));
        Assert.Equal(PetWear.None, p.PetHitTaken());
        Assert.Equal(999, p.GetEquipped(Slots.Pet).durability);
        Assert.Equal(PetWear.None, p.PetAttackLanded(1000));
        Assert.Equal(999, p.GetEquipped(Slots.Pet).durability);
    }

    [Fact]
    public void TheImpLosesOnePerAttackEvent_NotPerMonster_AndNothingForHitsTaken()
    {
        PlayerState p = Hero();
        p.Equip(Pet(Imp));
        p.PetHitTaken();
        Assert.Equal(1000, p.GetEquipped(Slots.Pet).durability);
        for (int monster = 0; monster < 12; monster++) p.PetAttackLanded(5000 + monster); // one area skill: twelve monsters in the same moment
        Assert.Equal(999, p.GetEquipped(Slots.Pet).durability);
        p.PetAttackLanded(5000 + PetConfig.AttackWindowMs - 1);
        Assert.Equal(999, p.GetEquipped(Slots.Pet).durability);
        p.PetAttackLanded(5000 + PetConfig.AttackWindowMs); // the next attack
        Assert.Equal(998, p.GetEquipped(Slots.Pet).durability);
    }

    [Fact]
    public void APetInTheBagAndAnEmptySlotDrainNothing()
    {
        PlayerState p = Hero();
        Item bag = Pet(Spirit);
        p.Inventory.Add(bag);
        Assert.Equal(PetWear.None, p.PetHitTaken());
        Assert.Equal(PetWear.None, p.PetAttackLanded(1000));
        Assert.Equal(1000, bag.durability);
    }

    [Fact]
    public void AtZeroThePetIsGone_TheBonusStops_AndHpComesDown()
    {
        PlayerState p = Hero();
        int baseHp = p.MaxHp;
        Item pet = Pet(Spirit);
        p.Equip(pet);
        pet.durability = 1;
        p.Hp = p.MaxHp;
        Assert.True(p.Hp > baseHp);
        Assert.Equal(PetWear.WornOut, p.PetHitTaken());
        Assert.Null(p.GetEquipped(Slots.Pet));
        Assert.DoesNotContain(p.Inventory, i => i.slot == Slots.Pet);
        Assert.Equal(baseHp, p.MaxHp);
        Assert.Equal(baseHp, p.Hp);
        Assert.Equal(PetWear.None, p.PetHitTaken()); // cannot be lost twice
    }

    [Fact]
    public void AWornOutImpTakesItsDamageWithIt()
    {
        PlayerState p = Hero();
        Item pet = Pet(Imp);
        p.Equip(pet);
        pet.durability = 1;
        Assert.Equal(PetWear.WornOut, p.PetAttackLanded(1000));
        Assert.Equal(p.Atk, p.DamageAtk);
    }

    [Fact]
    public void TheWarningComesOnceWhenTheLineIsCrossed()
    {
        PlayerState p = Hero();
        Item pet = Pet(Spirit);
        p.Equip(pet);
        pet.durability = 102;
        Assert.Equal(PetWear.None, p.PetHitTaken());
        Assert.Equal(PetWear.Low, p.PetHitTaken());
        Assert.Equal(100, pet.durability);
        Assert.Equal(PetWear.None, p.PetHitTaken());
        Assert.False(PetConfig.IsLow(101));
        Assert.True(PetConfig.IsLow(100));
    }

    [Fact]
    public void DurabilitySurvivesASaveInTheSlotAndInTheBag()
    {
        PlayerState p = Hero();
        Item worn = Pet(Spirit, "pet-spirit-a"), bag = Pet(Imp, "pet-imp-b");
        worn.durability = 437; bag.durability = 12;
        p.Equip(worn); p.Inventory.Add(bag);
        PlayerState back = RoundTrip(p);
        Assert.Equal(437, back.GetEquipped(Slots.Pet).durability);
        Assert.Equal(12, back.Inventory.Find(i => i.uid == "pet-imp-b").durability);
    }

    [Fact]
    public void AnOldPetWithoutTheFieldLoadsAtTheMaximum()
    {
        const string old = "{\"version\":1,\"classId\":\"warrior\",\"level\":5,\"inventory\":[{\"uid\":\"b\",\"templateId\":\"pet-imp\",\"slot\":\"pet\",\"name\":\"Imp\",\"rarity\":\"rare\"}],\"equipment\":[{\"uid\":\"w\",\"templateId\":\"pet-spirit\",\"slot\":\"pet\",\"name\":\"Spirit\",\"rarity\":\"rare\"}]}";
        PlayerState p = SaveSerializer.Restore(SaveSerializer.Parse(old));
        Assert.Equal(PetConfig.MaxDurability, p.GetEquipped(Slots.Pet).durability);
        Assert.Equal(PetConfig.MaxDurability, p.Inventory[0].durability);
    }

    [Fact]
    public void InTheNewFormatAPetAtZeroIsGone_AndNoValueGoesOverTheMaximum()
    {
        const string zero = "{\"version\":2,\"classId\":\"warrior\",\"level\":5,\"inventory\":[],\"equipment\":[{\"uid\":\"w\",\"templateId\":\"pet-spirit\",\"slot\":\"pet\",\"name\":\"Spirit\",\"rarity\":\"rare\",\"durability\":0}]}";
        Assert.Null(SaveSerializer.Restore(SaveSerializer.Parse(zero)).GetEquipped(Slots.Pet));
        Assert.Equal(PetConfig.MaxDurability, ItemFactory.Sanitize(new Item { uid = "x", templateId = "pet-imp", slot = Slots.Pet, rarity = "rare", durability = 99999 }).durability);
    }

    [Fact]
    public void OtherGearHasNoDurability()
    {
        Item sword = ItemFactory.CreateStarterItem(Slots.Weapon, "warrior");
        sword.durability = 50;
        Assert.Equal(0, ItemFactory.Sanitize(sword).durability);
        Assert.Equal("", ItemInfo.DurabilityLine(sword));
    }

    [Fact]
    public void DurabilityText()
    {
        Item pet = Pet(Spirit);
        pet.durability = 950;
        Assert.Equal("Durability 950 / 1000", ItemInfo.DurabilityLine(pet));
        Assert.Equal("95%", ItemInfo.DurabilityPercent(pet));
        pet.durability = 1;
        Assert.Equal("1%", ItemInfo.DurabilityPercent(pet));
    }

    // ---- what the panels and the wire see ----

    [Fact]
    public void IconsAndTooltipLines()
    {
        Assert.Equal("pet-spirit", ItemInfo.IconName(Pet(Spirit)));
        Assert.Equal("pet-imp", ItemInfo.IconName(Pet(Imp)));
        Assert.Null(ItemInfo.IconName(new Item { slot = Slots.Pet }));
        Assert.Equal("Pet", ItemInfo.SlotLabel(Slots.Pet));
        Assert.Equal(new[] { "Max HP +15%" }, ItemInfo.Lines(Pet(Spirit)).Select(ItemInfo.Format));
        Assert.Equal(new[] { "Damage +10%" }, ItemInfo.Lines(Pet(Imp)).Select(ItemInfo.Format));
        var against = ItemInfo.Lines(Pet(Imp), Pet(Spirit));
        Assert.Equal(new[] { "Max HP", "Damage" }, against.Select(l => l.Label));
        Assert.Equal(-0.15, against[0].Delta, 6);
        Assert.Equal(0.10, against[1].Delta, 6);
    }

    [Fact]
    public void WireIdsRoundTrip()
    {
        Assert.Equal(0, PetConfig.WireId(null));
        Assert.Null(PetConfig.IdOfWire(0));
        Assert.Null(PetConfig.IdOfWire(200));
        foreach (PetDef d in PetConfig.All) Assert.Equal(d.Id, PetConfig.IdOfWire(PetConfig.WireId(Pet(d))));
        Assert.Equal(0, PetConfig.WireId(new Item { slot = Slots.Offhand, templateId = "pet-imp" }));
    }
}
