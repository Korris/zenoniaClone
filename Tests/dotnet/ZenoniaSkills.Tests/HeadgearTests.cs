using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Cosmetic hats (HeadgearCatalog): catalog, slot rules, drops, shop, newbie quest gift, save round trip, headgear.json mirror.</summary>
public class HeadgearTests
{
    static Rand Fixed(double v) { return () => v; }

    static Item Hat(int id) { return HeadgearCatalog.CreateItem(HeadgearCatalog.Get(id), Fixed(0.25)); }

    [Fact]
    public void IdsAreStableOneToNineteenWithTheDecidedSources()
    {
        Assert.Equal(19, HeadgearCatalog.Count);
        for (int i = 0; i < HeadgearCatalog.All.Length; i++) Assert.Equal(i + 1, HeadgearCatalog.All[i].Id);
        Assert.Equal(5, HeadgearCatalog.BySource(HeadgearSource.Starter).Count);
        Assert.Equal(4, HeadgearCatalog.BySource(HeadgearSource.Shop).Count);
        Assert.Equal(6, HeadgearCatalog.BySource(HeadgearSource.Monster).Count);
        Assert.Equal(4, HeadgearCatalog.BySource(HeadgearSource.Wanted).Count);
        foreach (HeadgearDef d in HeadgearCatalog.All)
        {
            Assert.NotNull(ItemDefs.Template(d.TemplateId));
            Assert.NotNull(ItemDefs.Rarity(d.Rarity));
            if (d.Source == HeadgearSource.Monster) { Assert.InRange(d.Rate, 0.01, 0.05); Assert.NotNull(LootTable.For(d.DropFrom)); }
            if (d.Source == HeadgearSource.Shop) Assert.InRange(d.Price, 100, 300); // about 15-20 minutes of early gold
        }
    }

    [Theory]
    [InlineData("warrior", "iron-helmet")]
    [InlineData("wizard", "wizard-hat")]
    [InlineData("mechanic", "copper-goggles")]
    [InlineData("assassin", "ninja-headband")]
    [InlineData("cleric", "nun-veil")]
    [InlineData("fighter", "cloth-headband")]
    public void EveryClassHasAStarterHat(string cls, string key) { Assert.Equal(key, HeadgearCatalog.StarterFor(cls).Key); }

    [Fact]
    public void HatsHaveNoStatsNoLevelAndFitEveryClass()
    {
        PlayerState p = PlayerState.NewGame("cleric");
        int atk = p.Atk, def = p.Def, hp = p.MaxHp;
        Item hat = Hat(1); // the warrior's starter, worn by a cleric
        p.AddToInventory(hat);
        Assert.True(p.Equip(hat).Ok);
        Assert.Equal(new[] { atk, def, hp }, new[] { p.Atk, p.Def, p.MaxHp });
        Assert.False(EquipLevel.Applies(hat));
        Assert.Equal(1, p.Appearance.equippedHeadgearId);
        Assert.Same(hat, p.GetEquipped(Slots.Headgear));
        Assert.Null(p.GetEquipped(Slots.Helmet)); // the necklace slot is untouched
        Assert.False(EnhanceStats.IsEnhanceable(hat));
    }

    [Fact]
    public void EquipSwapsUnequipBaresTheHead()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        Item a = Hat(6), b = Hat(10);
        p.AddToInventory(a); p.AddToInventory(b);
        p.Equip(a);
        Assert.True(p.Equip(b).Ok);
        Assert.Equal(10, p.Appearance.equippedHeadgearId);
        Assert.Contains(a, p.Inventory);
        Assert.True(p.Unequip(Slots.Headgear).Ok);
        Assert.Equal(0, p.Appearance.equippedHeadgearId);
        Assert.Contains(b, p.Inventory);
    }

    [Fact]
    public void ANewHeroStartsBareHeaded()
    {
        PlayerState p = PlayerState.NewGame("wizard");
        Assert.Null(p.GetEquipped(Slots.Headgear));
        Assert.Equal(0, p.Appearance.equippedHeadgearId);
    }

    [Fact]
    public void WornHatSurvivesASaveAndSanitizeTrustsOnlyTheCatalog()
    {
        PlayerState p = PlayerState.NewGame("mechanic");
        Item worn = Hat(16), bag = Hat(8);
        p.AddToInventory(worn); p.AddToInventory(bag); p.Equip(worn);
        string json = SaveSerializer.ToJson(SaveSerializer.Capture(p, false, 0f, 0f, false, 1));
        json = json.Replace("\"Pompom Beanie\"", "\"Godly Beanie\"");
        PlayerState back = SaveSerializer.Restore(SaveSerializer.Parse(json));
        Assert.Equal(16, back.Appearance.equippedHeadgearId);
        Assert.Equal(worn.uid, back.GetEquipped(Slots.Headgear).uid);
        Item beanie = back.Inventory.Find(i => i.uid == bag.uid);
        Assert.Equal("Pompom Beanie", beanie.name);
        Assert.Equal(1, beanie.itemLevel);
        Assert.True(beanie.stats.IsZero);
    }

    [Fact]
    public void SanitizeDropsAHatInAnotherSlotAndRewritesStats()
    {
        Item raw = Hat(10);
        raw.slot = Slots.Helmet;
        Assert.Null(ItemFactory.Sanitize(raw));
        raw = Hat(10);
        raw.stats.atk = 500; raw.rarity = "epic"; raw.itemLevel = 90;
        Item s = ItemFactory.Sanitize(raw);
        Assert.Equal(0, s.stats.atk);
        Assert.Equal("uncommon", s.rarity);
        Assert.Equal(1, s.itemLevel);
    }

    [Fact]
    public void MonsterDropsOnlyItsOwnHatAtItsRate()
    {
        Assert.Equal(10, HeadgearCatalog.IdOf(HeadgearCatalog.RollMonsterDrop("hornRabbit", Fixed(0.039))));
        Assert.Null(HeadgearCatalog.RollMonsterDrop("hornRabbit", Fixed(0.04)));
        Assert.Equal(14, HeadgearCatalog.IdOf(HeadgearCatalog.RollMonsterDrop("mireToad", Fixed(0.0))));
        Assert.Null(HeadgearCatalog.RollMonsterDrop("thornBoar", Fixed(0.0))); // no signature hat
        int draws = 0;
        HeadgearCatalog.RollMonsterDrop("thornBoar", () => { draws++; return 0.0; });
        Assert.Equal(0, draws); // other species draw nothing: their seeded loot keeps its order
    }

    [Fact]
    public void LootRollAllAddsTheHatLast()
    {
        List<LootEntry> drops = LootTable.RollAll(LootTable.For("snowFox"), Fixed(0.0));
        LootEntry last = drops[drops.Count - 1];
        Assert.Equal(LootKind.Equipment, last.Kind);
        Assert.Equal(12, HeadgearCatalog.IdOf(last.Item));
        drops = LootTable.RollAll(LootTable.For("frostSlime"), Fixed(0.5));
        Assert.DoesNotContain(drops, e => e.Item != null && e.Item.slot == Slots.Headgear);
    }

    [Fact]
    public void LongRunMonsterRateMatches()
    {
        var rng = new System.Random(7);
        int hits = 0, n = 200000;
        for (int i = 0; i < n; i++) if (HeadgearCatalog.RollMonsterDrop("iceImp", () => rng.NextDouble()) != null) hits++;
        Assert.InRange(hits / (double)n, 0.018, 0.022);
    }

    [Fact]
    public void WantedBossesDropTheirCrownAndTheUltimateHat()
    {
        List<Item> all = HeadgearCatalog.RollWantedDrops("glacialColossus", Fixed(0.0));
        Assert.Equal(new[] { 17, 19 }, all.ConvertAll(HeadgearCatalog.IdOf).ToArray());
        List<Item> crownOnly = HeadgearCatalog.RollWantedDrops("rimeclawTyrant", Fixed(0.03));
        Assert.Equal(new[] { 16 }, crownOnly.ConvertAll(HeadgearCatalog.IdOf).ToArray());
        Assert.Empty(HeadgearCatalog.RollWantedDrops("mireHydraQueen", Fixed(0.5)));
        Assert.Empty(HeadgearCatalog.RollWantedDrops("frostfangKing", Fixed(0.0))); // a map boss is not a Wanted Boss
        Assert.Empty(HeadgearCatalog.RollWantedDrops("hornRabbit", Fixed(0.0)));
    }

    [Fact]
    public void ShopSellsOnlyShopHatsForGold()
    {
        PlayerState p = PlayerState.NewGame("assassin");
        Assert.False(p.BuyHeadgear(6).Ok); // no gold
        p.Gold = 1000;
        Assert.True(p.BuyHeadgear(6).Ok);
        Assert.Equal(850, p.Gold);
        Assert.Equal(6, HeadgearCatalog.IdOf(p.Inventory[p.Inventory.Count - 1]));
        Assert.False(p.BuyHeadgear(10).Ok); // a monster hat is not for sale
        Assert.False(p.BuyHeadgear(1).Ok);  // nor a quest gift
        Assert.Equal(850, p.Gold);
    }

    [Fact]
    public void ShopEntriesEncodeOnTheGearBuyByte()
    {
        HeadgearDef hat;
        Assert.True(HeadgearCatalog.TryShopHat(HeadgearCatalog.ShopEntry(9), out hat));
        Assert.Equal(9, hat.Id);
        Assert.True(HeadgearCatalog.ShopEntry(HeadgearCatalog.Count) <= 255);
        Assert.False(HeadgearCatalog.TryShopHat(HeadgearCatalog.ShopEntry(12), out hat));
        Assert.False(HeadgearCatalog.TryShopHat(GearShops.EntryCount - 1, out hat));
        Assert.True(HeadgearCatalog.ShopEntryBase >= GearShops.EntryCount);
        Assert.Null(GearShops.Refusal("smithBram", "oakhavenVillage", HeadgearCatalog.ShopEntry(6), 10f));
        Assert.NotNull(GearShops.Refusal("smithBram", "oakhavenVillage", HeadgearCatalog.ShopEntry(17), 10f));
        Assert.NotNull(GearShops.Refusal("merchantDale", "oakhavenVillage", HeadgearCatalog.ShopEntry(6), 10f));
    }

    [Fact]
    public void HeadgearJsonMirrorsTheCatalog()
    {
        string path = Path.Combine("..", "..", "..", "..", "..", "..", "Assets", "Resources", "Data", "hero-layered", "headgear.json");
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement hats = doc.RootElement.GetProperty("hats");
        Assert.Equal(HeadgearCatalog.Count, hats.GetArrayLength());
        int i = 0;
        foreach (JsonElement h in hats.EnumerateArray())
        {
            HeadgearDef d = HeadgearCatalog.All[i++];
            Assert.Equal(d.Id, h.GetProperty("id").GetInt32());
            Assert.Equal(d.Key, h.GetProperty("key").GetString());
            Assert.Equal(d.Name, h.GetProperty("name").GetString());
            Assert.Equal(d.Source, h.GetProperty("source").GetString());
            Assert.Equal(d.DropFrom ?? "", h.GetProperty("dropFrom").GetString());
            Assert.Equal(d.Rate, h.GetProperty("rate").GetDouble(), 6);
            Assert.Equal(d.Price, h.GetProperty("price").GetInt32());
            Assert.Equal(d.ClassId ?? "", h.GetProperty("classId").GetString());
            Assert.Equal(d.Rarity, h.GetProperty("rarity").GetString());
            Assert.Equal("Sprites/hero-layered/headgear/" + d.Key, h.GetProperty("sheet").GetString());
            Assert.Equal("headgear-" + d.Key, h.GetProperty("icon").GetString());
        }
    }
}
