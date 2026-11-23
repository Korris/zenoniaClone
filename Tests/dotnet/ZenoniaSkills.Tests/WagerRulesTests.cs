using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

// Phase 06 v2: the zone-side card stakes checks (they only hide / grey the UI and refuse early; server-api checks again) and what staking a deck does to the collection.
public class WagerRulesTests
{
    static PlayerState Hero(int level = 30)
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Level = level;
        return p;
    }

    static PlayerState WithDeck(string[] deck, params string[] spare)
    {
        PlayerState p = Hero();
        foreach (string id in deck) p.Cards.Add(id);
        foreach (string id in spare) p.Cards.Add(id);
        Assert.Equal(DeckError.None, DeckRules.Save(p, deck));
        return p;
    }

    static int Total(PlayerState p) { int n = 0; foreach (var kv in p.Cards.Cards) n += kv.Value; return n; }

    [Fact]
    public void NeedsLevelThirtyAndTheLimitsApplyOnlyWhileTheSwitchIsOn()
    {
        Assert.Equal(WagerError.Level, WagerRules.CanEnter(Hero(29), 10));
        Assert.Equal(WagerError.None, WagerRules.CanEnter(Hero(30), 10));
        WagerRules.Enabled = false;
        try { Assert.Equal(WagerError.None, WagerRules.CanEnter(Hero(1), 10)); } // switched off: PvP plays, nothing is staked, no level needed
        finally { WagerRules.Enabled = true; }
    }

    [Fact]
    public void TenDuelsADayIsTheLimitAndANewDayResetsIt()
    {
        PlayerState p = Hero();
        p.Cards.Daily = new CardDailyData { day = 10, wagers = 9 };
        Assert.Equal(WagerError.None, WagerRules.CanEnter(p, 10));
        p.Cards.Daily.wagers = 10;
        Assert.Equal(WagerError.DailyLimit, WagerRules.CanEnter(p, 10));
        Assert.Equal(WagerError.None, WagerRules.CanEnter(p, 11));
    }

    [Fact]
    public void ASamePairDuelsTwiceADayThenWaitsForTheNextDayAndTheOrderOfTheNamesDoesNotMatter()
    {
        var ledger = new WagerRules.PairLedger();
        Assert.True(ledger.Allowed("a", "b", 5));
        ledger.Note("a", "b", 5);
        ledger.Note("b", "a", 5);
        Assert.False(ledger.Allowed("a", "b", 5));
        Assert.False(ledger.Allowed("b", "a", 5));
        Assert.True(ledger.Allowed("a", "c", 5)); // another partner is another pair
        Assert.True(ledger.Allowed("a", "b", 6));
    }

    [Fact]
    public void EveryErrorHasAMessageExceptNone()
    {
        foreach (WagerError e in System.Enum.GetValues(typeof(WagerError)))
            Assert.Equal(e == WagerError.None, WagerRules.Text(e).Length == 0);
    }

    [Fact]
    public void StakingTheDeckTakesEveryDeckCopyAndLeavesTheSpareOnes()
    {
        PlayerState p = WithDeck(DuelTestDecks.A, "meadowSlime", "frostBear");
        Assert.Equal(10, Total(p));
        p.Cards.EscrowDeck();
        Assert.Equal(2, Total(p));
        Assert.Equal(1, p.Cards.Count("meadowSlime")); Assert.Equal(1, p.Cards.Count("frostBear")); Assert.Equal(0, p.Cards.Count("hornRabbit"));
        Assert.Equal(8, p.Cards.Deck.Count); // the deck list stays so the cards can come back
        Assert.True(p.Cards.InAlbum("hornRabbit")); // the Album keeps the card
    }

    [Fact]
    public void ADrawGivesAllEightBackAndTheLockFlagOfAKindThatFullyLeftComesBack()
    {
        PlayerState p = WithDeck(DuelTestDecks.A);
        p.Cards.SetLocked("caveMantis", true); p.Cards.SetLocked("meadowSlime", true);
        List<string> lockedKinds = p.Cards.EscrowDeck();
        Assert.Contains("caveMantis", lockedKinds);
        Assert.False(p.Cards.IsLocked("caveMantis"));
        p.Cards.ReturnCards(DuelTestDecks.A, lockedKinds);
        Assert.Equal(8, Total(p)); Assert.True(p.Cards.IsLocked("caveMantis")); Assert.True(p.Cards.IsLocked("meadowSlime"));
        Assert.False(p.Cards.IsLocked("hornRabbit"));
        Assert.Equal(8, p.Cards.Deck.Count);
    }

    [Fact]
    public void ALockedKindStaysLockedWhenACopyDroppedWhileItsLastCopyWasStaked()
    {
        PlayerState p = WithDeck(DuelTestDecks.A);
        p.Cards.SetLocked("caveMantis", true);
        List<string> lockedKinds = p.Cards.EscrowDeck();
        p.Cards.Add("caveMantis"); // a new copy dropped during the match: it came without the flag
        Assert.False(p.Cards.IsLocked("caveMantis"));
        p.Cards.ReturnCards(DuelTestDecks.A, lockedKinds);
        Assert.Equal(2, p.Cards.Count("caveMantis"));
        Assert.True(p.Cards.IsLocked("caveMantis")); // the flag belongs to the kind (the API does the same: card-wager-move.js giveWagerCards)
    }

    [Fact]
    public void TheWinnerGetsItsEightPlusTheTakenCardAndTheLoserSevenWithADeckOfSeven()
    {
        PlayerState win = WithDeck(DuelTestDecks.A), lose = WithDeck(DuelTestDecks.B);
        List<string> wl = win.Cards.EscrowDeck(), ll = lose.Cards.EscrowDeck();
        string taken = "iceGoblin";
        var winCards = new List<string>(DuelTestDecks.A) { taken };
        var loseCards = new List<string>(DuelTestDecks.B); loseCards.Remove(taken);
        win.Cards.ReturnCards(winCards, wl); lose.Cards.ReturnCards(loseCards, ll);
        Assert.Equal(9, Total(win)); Assert.Equal(7, Total(lose));
        Assert.Equal(1, win.Cards.Count(taken)); Assert.Equal(0, lose.Cards.Count(taken));
        Assert.Equal(8, win.Cards.Deck.Count); Assert.Equal(7, lose.Cards.Deck.Count);
        Assert.DoesNotContain(taken, lose.Cards.Deck);
        Assert.True(win.Cards.InAlbum(taken));
    }

    [Fact]
    public void AReloggedPlayerWithoutADeckGetsItBackAndATwoCopyCardLeavesOneInTheDeck()
    {
        string[] dup = { "frostBear", "frostBear", "gloomWolf", "snowFox", "frostSlime", "iceGoblin", "spectralWolf", "hollowTreant" };
        PlayerState p = WithDeck(dup);
        p.Cards.EscrowDeck();
        p.Cards.Deck.Clear(); // what a character loaded while its cards were held looks like (the deck is cut to the copies owned)
        var back = new List<string>(dup); back.Remove("frostBear"); // the winner took one frostBear
        p.Cards.ReturnCards(back, null);
        p.Cards.Deck.Clear(); p.Cards.Deck.AddRange(dup); p.Cards.TrimDeck();
        Assert.Equal(7, p.Cards.Deck.Count);
        Assert.Equal(1, p.Cards.Count("frostBear")); Assert.Equal(1, p.Cards.InDeck("frostBear"));
    }
}
