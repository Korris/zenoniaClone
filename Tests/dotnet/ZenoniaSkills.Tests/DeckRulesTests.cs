using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

// Phase 05: deck limits (8 cards, 1 boss, 2 SR/SSR without the boss), ownership and locks.
public class DeckRulesTests
{
    static CardCollection Owns(params string[] ids)
    {
        var c = new CardCollection();
        foreach (string id in ids) c.Add(id, 2);
        return c;
    }

    static readonly string[] Eight = { "meadowSlime", "hornRabbit", "thornBoar", "meadowMantis", "forestWolf", "brambleTreant", "thornSprite", "caveMantis" };

    [Fact]
    public void ValidEightCardDeckPasses()
    {
        Assert.Equal(DeckError.None, DeckRules.Validate(Eight, Owns(Eight), true));
    }

    [Fact]
    public void FullDeckNeedsEightButASmallerOneCanBeSaved()
    {
        var seven = new List<string>(Eight); seven.RemoveAt(0);
        Assert.Equal(DeckError.NotFull, DeckRules.Validate(seven, Owns(Eight), true));
        Assert.Equal(DeckError.None, DeckRules.Validate(seven, Owns(Eight), false));
    }

    [Fact]
    public void NineCardsAreTooMany()
    {
        var nine = new List<string>(Eight) { "meadowSlime" };
        Assert.Equal(DeckError.TooMany, DeckRules.Validate(nine, Owns(Eight), false));
    }

    [Fact]
    public void UnknownIdIsRefused()
    {
        Assert.Equal(DeckError.Unknown, DeckRules.Validate(new[] { "notACard" }, null, false));
    }

    [Fact]
    public void CannotUseMoreCopiesThanOwned()
    {
        var c = new CardCollection(); c.Add("meadowSlime", 2);
        Assert.Equal(DeckError.None, DeckRules.Validate(new[] { "meadowSlime", "meadowSlime" }, c, false));
        Assert.Equal(DeckError.NotEnough, DeckRules.Validate(new[] { "meadowSlime", "meadowSlime", "meadowSlime" }, c, false));
        Assert.Equal(DeckError.NotEnough, DeckRules.Validate(new[] { "hornRabbit" }, c, false)); // not owned at all
    }

    [Fact]
    public void LockedCardsStillGoInTheDeckUpToTheCopiesOwned()
    {
        var c = new CardCollection(); c.Add("meadowSlime", 2);
        Assert.True(c.SetLocked("meadowSlime", true));
        Assert.Equal(DeckError.None, DeckRules.Validate(new[] { "meadowSlime", "meadowSlime" }, c, false));
        Assert.Equal(DeckError.NotEnough, DeckRules.Validate(new[] { "meadowSlime", "meadowSlime", "meadowSlime" }, c, false));
    }

    [Fact]
    public void OnlyOneBossAllowed()
    {
        var c = Owns("frostfangKing", "thornfangAlpha", "meadowSlime");
        Assert.Equal(DeckError.None, DeckRules.Validate(new[] { "frostfangKing", "meadowSlime" }, c, false));
        Assert.Equal(DeckError.TooManyBoss, DeckRules.Validate(new[] { "frostfangKing", "thornfangAlpha" }, c, false));
    }

    [Fact]
    public void TwoSrPlusAllowedAndABossDoesNotUseOne()
    {
        var c = Owns("duneScarab", "sandRaider", "amberWisp", "frostfangKing");
        Assert.Equal(DeckError.None, DeckRules.Validate(new[] { "duneScarab", "sandRaider" }, c, false));
        Assert.Equal(DeckError.TooManySrPlus, DeckRules.Validate(new[] { "duneScarab", "sandRaider", "amberWisp" }, c, false));
        // 1 boss + 2 SR is the biggest legal mix: the boss is not one of the 2
        Assert.Equal(DeckError.None, DeckRules.Validate(new[] { "frostfangKing", "duneScarab", "sandRaider" }, c, false));
        Assert.Equal(2, DeckRules.SrPlus(new[] { "frostfangKing", "duneScarab", "sandRaider" }));
        Assert.Equal(1, DeckRules.Bosses(new[] { "frostfangKing", "duneScarab", "sandRaider" }));
    }

    [Fact]
    public void PowerIsTheSumOfEveryEdge()
    {
        Assert.Equal(CardTable.Get("meadowSlime").EdgeSum + CardTable.Get("hornRabbit").EdgeSum, DeckRules.Power(new[] { "meadowSlime", "hornRabbit" }));
    }

    [Fact]
    public void WhyNotAddExplainsTheFirstBrokenRule()
    {
        var c = Owns("duneScarab", "sandRaider", "amberWisp");
        Assert.Equal("", DeckRules.WhyNotAdd(new List<string> { "duneScarab" }, "sandRaider", c));
        Assert.Contains("SR", DeckRules.WhyNotAdd(new List<string> { "duneScarab", "sandRaider" }, "amberWisp", c));
        Assert.Contains("full", DeckRules.WhyNotAdd(new List<string>(Eight), "meadowSlime", Owns(Eight)));
    }

    [Fact]
    public void EveryNpcDeckFollowsTheRules()
    {
        for (int i = 0; i < DuelNpcTable.Count; i++)
            Assert.Equal(DeckError.None, DeckRules.Validate(DuelNpcTable.Get(i).Deck, null, true));
    }

    [Fact]
    public void CardsInTheDeckAreNotSpareSoTheyCannotBeSoldOrConverted()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Cards.Add("meadowSlime", 2);
        p.Cards.Deck.Add("meadowSlime");
        Assert.Equal(1, p.Cards.Spare("meadowSlime"));
        Assert.False(CardOps.Sell(p, "meadowSlime", 2).Ok);
        Assert.True(CardOps.Sell(p, "meadowSlime", 1).Ok);
        Assert.False(CardOps.Sell(p, "meadowSlime", 1).Ok); // the last copy is the deck's
        p.Gold = 100000;
        Assert.False(CardOps.Convert(p, "meadowSlime", GemKind.Power).Ok);
    }
}
