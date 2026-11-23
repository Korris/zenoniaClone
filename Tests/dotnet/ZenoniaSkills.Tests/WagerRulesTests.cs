using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

// Phase 06: the zone-side Card Wager checks (they only hide / grey the UI and refuse early; server-api checks again).
public class WagerRulesTests
{
    static CardDef First(CardRarity r, bool boss = false) { return CardTable.All.First(c => c.Rarity == r && c.IsBoss == boss); }

    static PlayerState Hero(int level = 30)
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Level = level;
        return p;
    }

    [Fact]
    public void NeedsLevelThirtyAndTheSwitchOn()
    {
        Assert.Equal(WagerError.Level, WagerRules.CanEnter(Hero(29), 10));
        Assert.Equal(WagerError.None, WagerRules.CanEnter(Hero(30), 10));
        WagerRules.Enabled = false;
        try { Assert.Equal(WagerError.Disabled, WagerRules.CanEnter(Hero(60), 10)); }
        finally { WagerRules.Enabled = true; }
    }

    [Fact]
    public void ThreeWagersADayIsTheLimitAndANewDayResetsIt()
    {
        PlayerState p = Hero();
        p.Cards.Daily = new CardDailyData { day = 10, wagers = 2 };
        Assert.Equal(WagerError.None, WagerRules.CanEnter(p, 10));
        p.Cards.Daily.wagers = 3;
        Assert.Equal(WagerError.DailyLimit, WagerRules.CanEnter(p, 10));
        Assert.Equal(WagerError.None, WagerRules.CanEnter(p, 11));
    }

    [Fact]
    public void OnlyASpareNormalOrRareNonBossCardCanBePut()
    {
        PlayerState p = Hero();
        CardDef n = First(CardRarity.N), r = First(CardRarity.R), sr = First(CardRarity.SR), boss = First(CardRarity.SSR, true);
        foreach (CardDef c in new[] { n, r, sr, boss }) p.Cards.Add(c.Id);
        Assert.Equal(WagerError.None, WagerRules.CheckCard(p, n.Id));
        Assert.Equal(WagerError.None, WagerRules.CheckCard(p, r.Id));
        Assert.Equal(WagerError.Rarity, WagerRules.CheckCard(p, sr.Id));
        Assert.Equal(WagerError.BossCard, WagerRules.CheckCard(p, boss.Id));
        Assert.Equal(WagerError.UnknownCard, WagerRules.CheckCard(p, "noSuchCard"));
        Assert.Equal(WagerError.UnknownCard, WagerRules.CheckCard(p, null));
    }

    [Fact]
    public void ALockedCardOrACardTheDeckNeedsIsNotSpare()
    {
        PlayerState p = Hero();
        CardDef n = First(CardRarity.N);
        Assert.Equal(WagerError.NotSpare, WagerRules.CheckCard(p, n.Id)); // not owned
        p.Cards.Add(n.Id);
        p.Cards.Deck.Add(n.Id); // the only copy is in the deck
        Assert.Equal(WagerError.NotSpare, WagerRules.CheckCard(p, n.Id));
        p.Cards.Add(n.Id); // a second copy is spare
        Assert.Equal(WagerError.None, WagerRules.CheckCard(p, n.Id));
        p.Cards.SetLocked(n.Id, true);
        Assert.Equal(WagerError.NotSpare, WagerRules.CheckCard(p, n.Id));
    }

    [Fact]
    public void TheTwoCardsMustShareARarityAndNeitherMayBeABoss()
    {
        CardDef n1 = First(CardRarity.N), n2 = CardTable.All.Where(c => c.Rarity == CardRarity.N && !c.IsBoss).Skip(1).First(), r = First(CardRarity.R), boss = First(CardRarity.SSR, true);
        Assert.True(WagerRules.Matches(n1, n2));
        Assert.False(WagerRules.Matches(n1, r));
        Assert.False(WagerRules.Matches(boss, boss));
        Assert.False(WagerRules.Matches(n1, null));
    }

    [Fact]
    public void ASamePairWagersTwiceADayThenWaitsForTheNextDayAndTheOrderOfTheNamesDoesNotMatter()
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
}
