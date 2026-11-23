using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

// Review fixes of phase 01: Lock Card in the save, full stacks turned into gold, gold cap, every card has a loot table.
public class CardLockAndOverflowTests
{
    static PlayerState Hero() { return PlayerState.NewGame("warrior"); }

    static PlayerState RoundTrip(PlayerState p)
    {
        SaveData d = SaveSerializer.Parse(SaveSerializer.ToJson(SaveSerializer.Capture(p, false, 0f, 0f, false, 1)));
        Assert.NotNull(d);
        return SaveSerializer.Restore(d);
    }

    [Fact]
    public void LockedCopiesSurviveSaveAndLoad()
    {
        var p = Hero();
        p.Cards.Add("meadowSlime", 5);
        Assert.True(p.Cards.SetLocked("meadowSlime", 3));
        PlayerState q = RoundTrip(p);
        Assert.Equal(3, q.Cards.Locked("meadowSlime"));
        Assert.Equal(2, q.Cards.Spare("meadowSlime"));
    }

    [Fact]
    public void LockedCopiesCannotBeTakenOut()
    {
        var c = Hero().Cards;
        c.Add("thornBoar", 3); c.SetLocked("thornBoar", 2);
        Assert.False(c.Remove("thornBoar", 2));   // only 1 spare
        Assert.Equal(3, c.Count("thornBoar"));
        Assert.True(c.Remove("thornBoar", 1));
        Assert.False(c.Remove("thornBoar", 1));
        Assert.Equal(2, c.Count("thornBoar"));
        Assert.True(c.SetLocked("thornBoar", 0));
        Assert.True(c.Remove("thornBoar", 2));
    }

    [Fact]
    public void LockIsCappedByWhatIsOwnedAndNotInTheDeck()
    {
        var c = Hero().Cards;
        c.Add("snowFox", 2);
        Assert.False(c.SetLocked("snowFox", 3));
        Assert.False(c.SetLocked("snowFox", -1));
        c.Deck.Add("snowFox");
        Assert.False(c.SetLocked("snowFox", 2));  // one copy is in the deck
        Assert.True(c.SetLocked("snowFox", 1));
        Assert.Equal(0, c.Spare("snowFox"));
        Assert.False(c.SetLocked("ghost", 1));
    }

    [Fact]
    public void AForgedLockIsClampedOnLoad()
    {
        SaveData d = SaveSerializer.Capture(Hero(), false, 0f, 0f, false, 1);
        d.cards = new List<CardEntry> { new CardEntry { id = "meadowSlime", n = 2, locked = 50 }, new CardEntry { id = "snowFox", n = 4, locked = -3 } };
        PlayerState p = SaveSerializer.Restore(SaveSerializer.Parse(SaveSerializer.ToJson(d)));
        Assert.Equal(2, p.Cards.Locked("meadowSlime"));
        Assert.Equal(0, p.Cards.Locked("snowFox"));
    }

    [Fact]
    public void ALockedCopyCannotFillTheDeckOnLoad()
    {
        SaveData d = SaveSerializer.Capture(Hero(), false, 0f, 0f, false, 1);
        d.cards = new List<CardEntry> { new CardEntry { id = "meadowSlime", n = 2, locked = 1 } };
        d.cardDeck = new List<string> { "meadowSlime", "meadowSlime" };
        PlayerState p = SaveSerializer.Restore(SaveSerializer.Parse(SaveSerializer.ToJson(d)));
        Assert.Single(p.Cards.Deck);
    }

    [Fact]
    public void AFullStackTurnsTheNextCopyIntoGoldAtTheSellPrice()
    {
        var p = Hero();
        p.Cards.Add("meadowSlime", 999);
        p.Gold = 100;
        CardGain g = CardOps.Receive(p, "meadowSlime");
        Assert.False(g.Stored);
        Assert.Equal(CardFees.Sell(CardTable.Get("meadowSlime")), g.Gold);
        Assert.Equal(100 + g.Gold, p.Gold);
        Assert.Equal(999, p.Cards.Count("meadowSlime"));
    }

    [Fact]
    public void ANormalDropIsStoredAndReportsFirstTime()
    {
        var p = Hero();
        CardGain g = CardOps.Receive(p, "snowFox");
        Assert.True(g.Stored); Assert.True(g.First); Assert.Equal(0, g.Gold);
        Assert.False(CardOps.Receive(p, "snowFox").First);
        Assert.False(CardOps.Receive(p, "ghost").Stored);
    }

    [Fact]
    public void SellPriceGrowsWithRarity()
    {
        int last = 0;
        foreach (CardRarity r in new[] { CardRarity.N, CardRarity.R, CardRarity.SR, CardRarity.SSR })
        {
            int price = CardFees.Sell(CardTable.All.First(c => c.Rarity == r));
            Assert.True(price > last, r.ToString());
            last = price;
        }
    }

    [Fact]
    public void TheAlbumGoldRewardNeverPassesTheGoldCap()
    {
        var p = Hero();
        for (int i = 0; i < 10; i++) p.AddCard(CardTable.All[i].Id);
        p.Gold = PlayerState.GoldCap - 100;
        Assert.True(AlbumRules.Claim(p, 0).Ok);
        Assert.Equal(PlayerState.GoldCap, p.Gold);
    }

    [Fact]
    public void AddGoldCappedNeverGoesNegativeOrPastTheCap()
    {
        var p = Hero();
        p.Gold = int.MaxValue - 5;
        p.AddGoldCapped(int.MaxValue);
        Assert.Equal(PlayerState.GoldCap, p.Gold);
        p.Gold = 10; p.AddGoldCapped(-50);
        Assert.Equal(0, p.Gold);
    }

    [Fact]
    public void EveryCardHasALootTableSoItCanDrop()
    {
        // KillRewards.Resolve returns early for a monster without a loot def: its card would never fall
        foreach (CardDef card in CardTable.All) Assert.True(LootTable.For(card.MonsterId) != null, card.Id + " has no LootTable entry");
    }
}
