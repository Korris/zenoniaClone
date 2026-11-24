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
    public void TheLockFlagSurvivesSaveAndLoad()
    {
        var p = Hero();
        p.Cards.Add("meadowSlime", 5);
        Assert.True(p.Cards.SetLocked("meadowSlime", true));
        PlayerState q = RoundTrip(p);
        Assert.True(q.Cards.IsLocked("meadowSlime"));
        Assert.Equal(0, q.Cards.Spare("meadowSlime"));
    }

    [Fact]
    public void ALockedKindCannotBeTakenOutAndNewCopiesAreLockedToo()
    {
        var c = Hero().Cards;
        c.Add("thornBoar", 3); c.SetLocked("thornBoar", true);
        Assert.False(c.Remove("thornBoar", 1));
        c.Add("thornBoar", 2); // a copy that drops later is locked at once: the flag belongs to the kind
        Assert.Equal(5, c.Count("thornBoar"));
        Assert.Equal(0, c.Spare("thornBoar"));
        Assert.False(c.Remove("thornBoar", 1));
        Assert.True(c.SetLocked("thornBoar", false));
        Assert.Equal(5, c.Spare("thornBoar"));
        Assert.True(c.Remove("thornBoar", 5));
    }

    [Fact]
    public void LockNeedsAnOwnedCardAndTheDeckDoesNotBlockIt()
    {
        var c = Hero().Cards;
        c.Add("snowFox", 2);
        c.Deck.Add("snowFox");
        Assert.True(c.SetLocked("snowFox", true)); // a card in the deck can be locked too
        Assert.Equal(0, c.Spare("snowFox"));
        Assert.False(c.SetLocked("ghost", true));
        Assert.False(c.SetLocked("hornRabbit", true)); // not owned
    }

    [Fact]
    public void ALockedCardMayStillBeInTheDeck()
    {
        var p = Hero();
        p.Cards.Add("meadowSlime", 2);
        p.Cards.SetLocked("meadowSlime", true);
        Assert.Equal(DeckError.None, DeckRules.Save(p, new[] { "meadowSlime", "meadowSlime" }));
        Assert.Equal(2, p.Cards.InDeck("meadowSlime"));
        Assert.False(CardOps.Sell(p, "meadowSlime", 1).Ok); // locked: not for sale, deck or not
    }

    [Fact]
    public void AnyPositiveLockValueInASaveMeansLockedAndNegativeMeansOpen()
    {
        SaveData d = SaveSerializer.Capture(Hero(), false, 0f, 0f, false, 1);
        d.cards = new List<CardEntry> { new CardEntry { id = "meadowSlime", n = 2, locked = 50 }, new CardEntry { id = "snowFox", n = 4, locked = -3 } };
        PlayerState p = SaveSerializer.Restore(SaveSerializer.Parse(SaveSerializer.ToJson(d)));
        Assert.True(p.Cards.IsLocked("meadowSlime"));
        Assert.False(p.Cards.IsLocked("snowFox"));
    }

    [Fact]
    public void ALockedCardCanFillTheDeckOnLoadUpToTheCopiesOwned()
    {
        SaveData d = SaveSerializer.Capture(Hero(), false, 0f, 0f, false, 1);
        d.cards = new List<CardEntry> { new CardEntry { id = "meadowSlime", n = 2, locked = 1 } };
        d.cardDeck = new List<string> { "meadowSlime", "meadowSlime", "meadowSlime" };
        PlayerState p = SaveSerializer.Restore(SaveSerializer.Parse(SaveSerializer.ToJson(d)));
        Assert.Equal(2, p.Cards.Deck.Count);
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
        for (int i = 0; i < 3; i++) p.AddCard(CardTable.All[CardTable.LiveStart + i].Id);
        p.Gold = PlayerState.GoldCap - 100;
        Assert.False(AlbumRules.Claim(p, 0).Ok); // refused, like Sell: the reward is not cut and the milestone stays claimable
        Assert.Equal(PlayerState.GoldCap - 100, p.Gold);
        Assert.Equal(0, p.Cards.AlbumClaimed);
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
        Assert.NotNull(LootTable.For("tauCloverSlime"));
    }

    [Fact]
    public void FullStackWithGoldAtTheCapSkipsTheCardInsteadOfSellingItForNothing()
    {
        var p = Hero();
        p.Cards.Add("thornBoar", CardTable.MaxStack);
        p.Gold = PlayerState.GoldCap - 10;
        CardGain g = CardOps.Receive(p, "thornBoar");
        Assert.False(g.Stored); Assert.True(g.GoldFull); Assert.Equal(0, g.Gold);
        Assert.Equal(PlayerState.GoldCap - 10, p.Gold);
        Assert.Equal(CardTable.MaxStack, p.Cards.Count("thornBoar"));
        p.Gold = 0;
        g = CardOps.Receive(p, "thornBoar");
        Assert.False(g.GoldFull); Assert.Equal(CardFees.Sell(CardTable.Get("thornBoar")), g.Gold);
    }
}
