using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

// Phase 04: the option of a socketed gem lands in the hero stats, flat, never multiplied by enhance.
public class GemStatTests
{
    static CardDef Card(CardCategory cat, CardRarity r, bool mp = false) { return CardTable.All.First(c => c.Category == cat && c.Rarity == r && !c.IsBoss && (cat != CardCategory.Utility || c.UtilityMp == mp)); }

    static PlayerState Hero()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Gold = 100000;
        return p;
    }

    [Fact]
    public void APowerAtkGemRaisesAtkByExactlyItsValueAndRemovingLowersItAgain()
    {
        PlayerState p = Hero();
        CardDef card = Card(CardCategory.Atk, CardRarity.N);
        int baseAtk = p.Atk;
        p.Cards.AddGem(card.Id, GemKind.Power, false);
        Assert.True(CardOps.Socket(p, p.GetEquipped(Slots.Weapon).uid, card.Id, GemKind.Power).Ok);
        Assert.Equal(baseAtk + 6, p.Atk);
        Assert.True(CardOps.Remove(p, p.GetEquipped(Slots.Weapon).uid, 0).Ok);
        Assert.Equal(baseAtk, p.Atk);
    }

    [Fact]
    public void GemOptionIsNotMultipliedByEnhance()
    {
        PlayerState p = Hero();
        Item w = p.GetEquipped(Slots.Weapon);
        w.enhance = 10;
        p.RecomputeStats();
        int enhanced = p.Atk;
        CardDef card = Card(CardCategory.Atk, CardRarity.N);
        p.Cards.AddGem(card.Id, GemKind.Power, false);
        ActionResult r = CardOps.Socket(p, w.uid, card.Id, GemKind.Power);
        Assert.True(r.Ok, r.Text);
        Assert.Equal(enhanced + 6, p.Atk);
    }

    [Fact]
    public void GuardGemRaisesDefAndAHpCardRaisesMaxHp()
    {
        PlayerState p = Hero();
        CardDef atk = Card(CardCategory.Atk, CardRarity.R);
        int def = p.Def, hp = p.MaxHp;
        Item armor = ItemFactory.Generate(() => 0.4, 1, Slots.Armor, "rare", "warrior");
        armor.uid = "ar1"; p.AddToInventory(armor); p.Equip(armor);
        int defBase = p.Def, hpBase = p.MaxHp;
        p.Cards.AddGem(atk.Id, GemKind.Guard, false);
        CardOps.Socket(p, "ar1", atk.Id, GemKind.Guard);
        Assert.Equal(defBase + 8, p.Def);                                 // R guard = Def 8

        CardDef hpCard = Card(CardCategory.Hp, CardRarity.N);
        p.Cards.AddGem(hpCard.Id, GemKind.Power, false);
        CardOps.Socket(p, "ar1", hpCard.Id, GemKind.Power);
        Assert.Equal(hpBase + 40, p.MaxHp);                               // N hp power = +40
    }

    [Fact]
    public void UtilityGemsRaiseCritOrMaxMp()
    {
        PlayerState p = Hero();
        Item armor = ItemFactory.Generate(() => 0.4, 1, Slots.Armor, "rare", "warrior");
        armor.uid = "ar1"; p.AddToInventory(armor); p.Equip(armor);
        float crit = p.CritBonus; int mp = p.MaxMp;
        CardDef critCard = Card(CardCategory.Utility, CardRarity.R, false);
        p.Cards.AddGem(critCard.Id, GemKind.Power, false);
        CardOps.Socket(p, "ar1", critCard.Id, GemKind.Power);
        Assert.Equal(crit + 0.02f, p.CritBonus, 4);   // R crit gem = +2 %
        CardDef mpCard = Card(CardCategory.Utility, CardRarity.R, true);
        p.Cards.AddGem(mpCard.Id, GemKind.Power, false);
        CardOps.Socket(p, "ar1", mpCard.Id, GemKind.Power);
        Assert.Equal(mp + 30, p.MaxMp);                 // R max-MP gem = +30
    }

    [Fact]
    public void AGemInABagItemChangesNothingUntilItIsWorn()
    {
        PlayerState p = Hero();
        Item spare = ItemFactory.Generate(() => 0.4, 1, Slots.Weapon, "rare", "warrior");
        spare.uid = "w2"; p.AddToInventory(spare);
        int atk = p.Atk;
        CardDef card = Card(CardCategory.Atk, CardRarity.N);
        p.Cards.AddGem(card.Id, GemKind.Power, false);
        CardOps.Socket(p, "w2", card.Id, GemKind.Power);
        Assert.Equal(atk, p.Atk);
        p.Equip(spare);
        Assert.True(p.Atk >= atk + 6);
    }

    [Fact]
    public void SocketsPastTheSlotLimitAreIgnoredByTheStats()
    {
        Item glove = ItemFactory.Generate(() => 0.4, 1, Slots.Gloves, "rare", "warrior");
        glove.sockets.Add(Card(CardCategory.Atk, CardRarity.SR).Id + ":p");
        Assert.Equal(0, GemSockets.Sum(glove).atk);
    }

    [Fact]
    public void DescribeListsEveryGemWithItsOption()
    {
        Item w = ItemFactory.Generate(() => 0.4, 1, Slots.Weapon, "rare", "warrior");
        CardDef atk = Card(CardCategory.Atk, CardRarity.N);
        w.sockets.Add(atk.Id + ":p");
        string line = GemSockets.Describe(w).Single();
        Assert.Contains("ATK +6", line);
        Assert.Contains(atk.Name, line);
    }

    [Theory]
    [InlineData("warrior")] [InlineData("wizard")] [InlineData("mechanic")] [InlineData("assassin")] [InlineData("cleric")]
    public void GemOptionIsFlatEvenWithASecondWeaponPercentBonus(string cls)
    {
        PlayerState p = PlayerState.NewGame(cls);
        Item off = ItemFactory.Generate(() => 0.4, 1, Slots.Offhand, "rare", cls);
        off.uid = "off1"; off.enhance = 15;
        p.AddToInventory(off); p.Equip(off);
        Item armor = ItemFactory.Generate(() => 0.4, 1, Slots.Armor, "rare", cls);
        armor.uid = "ar9"; p.AddToInventory(armor); p.Equip(armor);
        Assert.True(p.Second.HpPct > 0 || p.Second.DefPct > 0 || p.Second.MpPct > 0 || cls != "warrior", "the second weapon should add a percent bonus");
        int hp = p.MaxHp, def = p.Def, atk = p.Atk; float dmg = p.DamageAtk;
        CardDef hpCard = Card(CardCategory.Hp, CardRarity.SR);
        p.Cards.AddGem(hpCard.Id, GemKind.Power, false);
        Assert.True(CardOps.Socket(p, "ar9", hpCard.Id, GemKind.Power).Ok);
        Assert.Equal(hp + 140, p.MaxHp);                       // SR HP gem = +140 exactly, not 140 x (1 + HpPct)
        CardDef guard = Card(CardCategory.Atk, CardRarity.SR);
        p.Cards.AddGem(guard.Id, GemKind.Guard, false);
        Assert.True(CardOps.Socket(p, "ar9", guard.Id, GemKind.Guard).Ok);
        Assert.Equal(def + 14, p.Def);                         // SR guard = Def 14 exactly
        Assert.Equal(atk, p.Atk);
        Assert.Equal(dmg, p.DamageAtk);
    }

    [Fact]
    public void AtkGemAddsExactlyToDamageAtk()
    {
        PlayerState p = Hero();
        float before = p.DamageAtk;
        CardDef card = Card(CardCategory.Atk, CardRarity.SR);
        p.Cards.AddGem(card.Id, GemKind.Power, false);
        Assert.True(CardOps.Socket(p, p.GetEquipped(Slots.Weapon).uid, card.Id, GemKind.Power).Ok);
        Assert.Equal(before + 20f, p.DamageAtk, 3);
    }
}
