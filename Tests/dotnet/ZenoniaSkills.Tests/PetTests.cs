using System;
using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>
/// Pet Companion (16/11/2026): pets are account data, a hero wears one by id; passives by stars. The old durability-pet tests (items in a Pet slot, drain, worn out,
/// buying into the bag) were replaced by these when that design was dropped (docs/reference/gacha-discussion-chatgpt.md Lần 6); the old ITEM format is still read for migration.
/// </summary>
public class PetTests
{
    static AccountState Owning(params string[] ids)
    {
        var a = new AccountState();
        foreach (string id in ids) a.pets.Add(new OwnedPet { id = id, stars = 1 });
        return a;
    }

    static PlayerState Hero(string cls = "warrior", int level = 20, AccountState account = null)
    {
        PlayerState p = PlayerState.NewGame(cls);
        p.Level = level;
        p.AttachAccount(account ?? Owning(PetConfig.All.Select(d => d.Id).ToArray()));
        p.Hp = p.MaxHp;
        return p;
    }

    [Fact]
    public void TheTableIsWhatChatGptSettled()
    {
        // Lần 7: the game's own monsters / bosses in the gacha, Spirit and Imp kept as Legacy outside it.
        Assert.Equal(new[] { "spirit", "imp", "frostSlime", "hornRabbit", "iceGoblin", "thornBoar", "spectralWolf", "marshStalker", "crystalGolem", "frostfangKing" }, PetConfig.All.Select(d => d.Id).ToArray());
        Assert.Equal(new[] { true, true, false, false, false, false, false, false, false, false }, PetConfig.All.Select(d => d.Legacy).ToArray());
        PetDef[] pool = PetConfig.All.Where(d => !d.Legacy).ToArray();
        Assert.Equal(new[] { PetRarity.N, PetRarity.N, PetRarity.R, PetRarity.R, PetRarity.SR, PetRarity.SR, PetRarity.SSR, PetRarity.UR }, pool.Select(d => d.Rarity).ToArray());
        Assert.Equal(new[] { 0.05, 0.03, 0.03, 0.03, 0.03, 0.05, 0.04, 0.05 }, pool.Select(d => d.PassivePct).ToArray());
        Assert.Equal(new[] { 0.05, 0.40, 0.45, 0.45, 0.55, 0.55, 0.07, 0.75 }, pool.Select(d => d.SkillPct).ToArray());
        Assert.All(pool, d => Assert.False(string.IsNullOrEmpty(d.MonsterType))); // every gacha pet is one of the game's monsters
        Assert.Equal(20000, PetConfig.SkillCooldownMs);
        Assert.Equal(1500, PetConfig.BasicAttackMs); // ChatGPT 16/11 06:41: first hit at once, then every 1.5 s
        Assert.Equal(0.25, PetConfig.BasicPct); // 25 % of the owner's hit after defence (10 % before defence gave 1-2 damage)
        Assert.Equal(3000, PetConfig.Get("imp").Price);
        Assert.Equal(5, PetConfig.Get("spirit").UnlockLevel);
        Assert.All(PetConfig.All, d => Assert.True(d.Price == 0 || d.Id == "imp"));
        Assert.Equal("Legacy", PetConfig.RarityLabel(PetConfig.Get("spirit")));
        Assert.Equal("UR", PetConfig.RarityLabel(PetConfig.Get("frostfangKing")));
        Assert.True(PetConfig.Get("frostfangKing").HeightUnits > PetConfig.Get("crystalGolem").HeightUnits);
        Assert.Equal(5, PetConfig.MaxStars);
        Assert.True(PetConfig.Get("crystalGolem").HeightUnits > PetConfig.Get("hornRabbit").HeightUnits);
    }

    [Theory]
    [InlineData(1, 0.05)]
    [InlineData(2, 0.055)]
    [InlineData(5, 0.07)]
    public void StarsScaleThePassive(int stars, double hp)
    {
        Assert.Equal(hp, PetConfig.PassiveAt(PetConfig.Get("spirit"), stars), 6);
        Assert.Equal(0.0, PetConfig.PassiveAt(PetConfig.Get("spirit"), 0));
    }

    [Theory]
    [InlineData("warrior")]
    [InlineData("wizard")]
    [InlineData("cleric")]
    public void SpiritRaisesMaxHpAndOnlyMaxHp(string cls)
    {
        PlayerState p = Hero(cls);
        int hp = p.MaxHp, atk = p.Atk;
        Assert.True(p.TakePet("spirit").Ok);
        Assert.Equal(RpgMath.Round(hp * 1.05), p.MaxHp);
        Assert.Equal(atk, p.DamageAtk);
        p.Account.Pet("spirit").stars = 5;
        p.RecomputeStats();
        Assert.Equal(RpgMath.Round(hp * 1.07), p.MaxHp);
        Assert.True(p.TakePet("").Ok);
        Assert.Equal(hp, p.MaxHp);
    }

    [Fact]
    public void DamagePetsRaiseWhatHitsOnMonstersAreRolledFromButNotAtk()
    {
        PlayerState p = Hero();
        int atk = p.Atk;
        p.TakePet("imp");
        Assert.Equal((float)(atk * 1.03), p.DamageAtk, 4);
        p.TakePet("frostfangKing");
        Assert.Equal((float)(atk * 1.05), p.DamageAtk, 4);
        Assert.Equal(atk, p.Atk);
    }

    [Fact]
    public void HornRabbitSpeedStaysUnderTheCap()
    {
        PlayerState p = Hero();
        float before = p.Speed;
        p.TakePet("hornRabbit");
        Assert.True(p.Speed > before);
        Assert.True(p.Speed <= Leveling.StatsForLevel(p.Level, p.ClassId).Speed * (1 + ItemDefs.MaxSpeedBonus) + 0.001f);
    }

    [Fact]
    public void MarshStalkerHealsMoreFromHpPotionsOnly()
    {
        PlayerState p = Hero();
        p.HpPotions = 2;
        p.Hp = 1;
        int plain = int.Parse(p.UsePotion(true).Text.Replace("HP +", ""));
        p.TakePet("marshStalker");
        p.Hp = 1;
        int fairy = int.Parse(p.UsePotion(true).Text.Replace("HP +", ""));
        Assert.Equal(RpgMath.Round(p.MaxHp * PotionCatalog.Fraction(PotionCatalog.Small) * 1.05), fairy);
        Assert.True(fairy > plain);
    }

    [Fact]
    public void OnlyAnOwnedPetCanComeAlong()
    {
        PlayerState p = Hero(account: Owning("imp"));
        Assert.False(p.TakePet("spirit").Ok);
        Assert.False(p.TakePet("nope").Ok);
        Assert.True(p.TakePet("imp").Ok);
        Assert.False(p.TakePet("imp").Ok);
        PlayerState bare = PlayerState.NewGame("warrior");
        Assert.False(bare.TakePet("imp").Ok); // no account attached
    }

    [Fact]
    public void AttachingAnAccountPutsAwayAPetItDoesNotOwn()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Level = 10; // the combat slot opens at level 5
        p.PetId = "phoenix";
        p.AttachAccount(Owning("imp"));
        Assert.Equal("", p.PetId);
        p.PetId = "imp";
        p.AttachAccount(Owning("imp"));
        Assert.Equal("imp", p.PetId);
    }

    [Fact]
    public void ThePetIdSurvivesASave_AndAGarbageIdIsDropped()
    {
        PlayerState p = Hero();
        p.TakePet("spectralWolf");
        string json = SaveSerializer.ToJson(SaveSerializer.Capture(p, true, 1f, 2f, false));
        SaveData d = SaveSerializer.Parse(json);
        Assert.Equal(5, d.version);
        Assert.Equal("spectralWolf", SaveSerializer.Restore(d).PetId);
        Assert.Equal("", SaveSerializer.Parse(json.Replace("spectralWolf", "godPet")).petId);
    }

    [Fact]
    public void WireIdsRoundTrip()
    {
        foreach (PetDef d in PetConfig.All) Assert.Equal(d.Id, PetConfig.IdOfWire(PetConfig.WireId(d.Id)));
        Assert.Equal(0, PetConfig.WireId(""));
        Assert.Null(PetConfig.IdOfWire(0));
        Assert.Null(PetConfig.IdOfWire(200));
    }

    [Fact]
    public void Texts()
    {
        Assert.Equal("+5.5% Max HP", PetConfig.PassiveText(PetConfig.Get("spirit"), 2));
        Assert.Equal("-3.6% damage taken from monsters", PetConfig.PassiveText(PetConfig.Get("thornBoar"), 3));
        Assert.Contains("75%", PetConfig.SkillText(PetConfig.Get("frostfangKing")));
        Assert.Contains("rushes", PetConfig.SkillText(PetConfig.Get("spectralWolf")));
    }

    // ---- shop ----

    [Theory]
    [InlineData("merchantDale", PotionShops.Oakhaven, 10f, null)]
    [InlineData("apothecaryNara", PotionShops.Sunspire, 10f, null)]
    [InlineData("healerNina", PotionShops.Oakhaven, 10f, "Nobody sells that here")]
    [InlineData("merchantDale", PotionShops.Sunspire, 10f, "Nobody sells that here")]
    [InlineData("merchantDale", PotionShops.Oakhaven, 500f, "Too far from the shop")]
    public void TheSellerMustBeRightHereAndInReach(string npc, string map, float distance, string refusal)
    {
        Assert.Equal(refusal, PetConfig.Refusal(npc, map, PetConfig.IndexOf(PetConfig.Get("imp")), distance));
    }

    [Fact]
    public void OnlyPricedPetsAreSold()
    {
        Assert.Equal("Not sold here", PetConfig.Refusal("merchantDale", PotionShops.Oakhaven, 0, 0f)); // Spirit: level 5
        Assert.Equal("Not sold here", PetConfig.Refusal("merchantDale", PotionShops.Oakhaven, PetConfig.All.Length, 0f));
    }

    // ---- the old item format (read only to migrate) ----

    [Fact]
    public void MonstersNeverDropPetItems()
    {
        var r = new Random(7);
        for (int i = 0; i < 2000; i++) Assert.NotEqual(Slots.Pet, ItemFactory.RollEquipmentDrop(20, true, () => r.NextDouble()).slot);
    }

    [Fact]
    public void AnOldPetItemCannotBeWornOrEnhanced()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        Item old = ItemFactory.CreateLegacyPet(PetConfig.Get("imp"), () => 0.5);
        p.Inventory.Add(old);
        Assert.False(p.Equip(old).Ok);
        Assert.False(EnhanceStats.IsEnhanceable(old));
    }

    [Fact]
    public void AnOldSaveStillLoadsItsPetItemsCleanly_ForTheMigration()
    {
        const string hacked = "{\"version\":3,\"classId\":\"warrior\",\"level\":5,\"inventory\":[{\"uid\":\"b\",\"templateId\":\"pet-spirit\",\"slot\":\"pet\",\"name\":\"Spirit\",\"rarity\":\"rare\",\"durability\":40}],\"equipment\":[{\"uid\":\"p\",\"templateId\":\"pet-imp\",\"slot\":\"pet\",\"name\":\"Imp\",\"rarity\":\"rare\",\"classId\":\"wizard\",\"itemLevel\":1,\"enhance\":9,\"durability\":900,\"stats\":{\"atk\":500}}]}";
        PlayerState p = SaveSerializer.Restore(SaveSerializer.Parse(hacked));
        Assert.Equal(2, p.LegacyPets().Count);
        Assert.Equal("imp", p.LegacyWornPet.Id);
        Assert.True(p.GetEquipped(Slots.Pet).stats.IsZero);
        Assert.Equal(0, p.GetEquipped(Slots.Pet).enhance);
        Assert.Equal(p.Atk, p.DamageAtk); // an old item gives nothing until it is migrated
        p.RemoveLegacyPets();
        Assert.Empty(p.LegacyPets());
    }

    [Fact]
    public void AWornOutOldPetWasGone_AndTheOffhandCalledSpiritIsNotAPet()
    {
        const string json = "{\"version\":3,\"classId\":\"warrior\",\"level\":5,\"inventory\":[{\"uid\":\"x\",\"templateId\":\"pet-imp\",\"slot\":\"pet\",\"name\":\"x\",\"rarity\":\"rare\",\"durability\":0},{\"uid\":\"p\",\"templateId\":\"spirit\",\"slot\":\"pet\",\"name\":\"x\",\"rarity\":\"rare\"},{\"uid\":\"q\",\"templateId\":\"pet-spirit\",\"slot\":\"offhand\",\"name\":\"x\",\"rarity\":\"rare\"}],\"equipment\":[]}";
        Assert.Empty(SaveSerializer.Parse(json).inventory);
    }
}
