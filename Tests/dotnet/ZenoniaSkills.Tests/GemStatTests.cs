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
}
