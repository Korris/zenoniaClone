using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Pet gacha with Ragnarok: Pocket Saga's table (docs/pet-system-design.md 4): rates + shard result, pity 20 / 50, coins from crystals, duplicates, UR shards, history.</summary>
public class PetGachaTests
{
    static Rand Fixed(params double[] rolls) { int i = 0; return () => rolls[Math.Min(i++, rolls.Length - 1)]; }

    [Fact]
    public void RatesAreRagnaroksAndEveryPetHasItsShare()
    {
        Assert.Equal(new[] { 0.12, 0.24, 0.36, 0.0625, 0.025 }, PetGacha.Rates);
        Assert.Equal(1.0, PetGacha.Rates.Sum() + PetRules.GachaShardRate, 9);
        Assert.Equal(PetGacha.Rates.Sum(), PetConfig.All.Sum(d => PetGacha.PetRate(d)), 9);
        Assert.Equal(0.06, PetGacha.PetRate(PetConfig.Get("frostSlime")), 9); // 12 % over two N pets
        Assert.Equal(0.025, PetGacha.PetRate(PetConfig.Get("frostfangKing")), 9);
        Assert.Equal(0.0, PetGacha.PetRate(PetConfig.Get("spirit"))); // Legacy: never pulled
    }

    [Fact]
    public void ANormalRollFollowsTheRates_TheRestIsShards()
    {
        bool p;
        Assert.Equal(PetRarity.UR, PetGacha.RollRarity(0, 0, 0.0, out p));
        Assert.Equal(PetRarity.SSR, PetGacha.RollRarity(0, 0, 0.03, out p));
        Assert.Equal(PetRarity.SR, PetGacha.RollRarity(0, 0, 0.10, out p));
        Assert.Equal(PetRarity.R, PetGacha.RollRarity(0, 0, 0.50, out p));
        Assert.Equal(PetRarity.N, PetGacha.RollRarity(0, 0, 0.75, out p));
        Assert.Null(PetGacha.RollRarity(0, 0, 0.90, out p)); // the 19.25 % shard band
        Assert.False(p);
    }

    [Fact]
    public void ManyPullsLandNearThePublishedRates()
    {
        var rng = new SeededRng(12345);
        var counts = new int[6];
        var a = new AccountState();
        const int n = 200000;
        for (int i = 0; i < n; i++) { a.pitySSR = a.pityUR = 0; PullResult r = PetGacha.PullOne(a, rng.AsRand(), 0); counts[r.Pet == null ? 5 : (int)r.Pet.Rarity]++; }
        for (int r = 0; r < 5; r++) Assert.InRange(counts[r] / (double)n, PetGacha.Rates[r] - 0.005, PetGacha.Rates[r] + 0.005);
        Assert.InRange(counts[5] / (double)n, PetRules.GachaShardRate - 0.005, PetRules.GachaShardRate + 0.005);
    }

    [Fact]
    public void TheTwentiethWithoutSsrIsSsrOrBetter_TheFiftiethWithoutUrIsUr()
    {
        bool p;
        Assert.Equal(PetRarity.SSR, PetGacha.RollRarity(19, 19, 0.99, out p));
        Assert.True(p);
        Assert.Equal(PetRarity.UR, PetGacha.RollRarity(19, 19, 0.10, out p)); // 28.57 % of the forced band
        Assert.Equal(PetRarity.UR, PetGacha.RollRarity(0, 49, 0.99, out p));
        var a = new AccountState { tickets = 50 };
        var results = new List<PullResult>();
        for (int i = 0; i < 5; i++) PetGacha.Pull(a, 10, Fixed(0.99), 0, results); // every roll is the commonest outcome: shards
        Assert.Equal(PetRarity.SSR, results[19].Pet.Rarity);
        Assert.Equal(PetRarity.SSR, results[39].Pet.Rarity);
        Assert.Equal(PetRarity.UR, results[49].Pet.Rarity);
        Assert.Equal(0, a.pitySSR + a.pityUR);
        Assert.Equal(50, a.totalPulls);
        Assert.Equal(47 * PetRules.SharedShardsPerPull, a.sharedShards);
    }

    [Fact]
    public void ResetsFollowTheRarity()
    {
        var a = new AccountState { pitySSR = 7, pityUR = 8 };
        PetGacha.PullOne(a, Fixed(0.03, 0), 0); // SSR
        Assert.Equal(new[] { 0, 9 }, new[] { a.pitySSR, a.pityUR });
        PetGacha.PullOne(a, Fixed(0.9, 0), 0); // shards
        Assert.Equal(new[] { 1, 10 }, new[] { a.pitySSR, a.pityUR });
        PetGacha.PullOne(a, Fixed(0.01, 0), 0); // UR
        Assert.Equal(new[] { 0, 0 }, new[] { a.pitySSR, a.pityUR });
    }

    [Fact]
    public void NewIsOneStarLevelOne_DuplicateIsTenShardsOfItsKind_NotUrAddsAUrShard()
    {
        var a = new AccountState();
        PullResult first = PetGacha.PullOne(a, Fixed(0.75, 0), 0);
        Assert.False(first.Duplicate);
        Assert.Equal(1, a.StarsOf(first.Pet.Id));
        Assert.Equal(1, a.LevelOf(first.Pet.Id));
        PullResult again = PetGacha.PullOne(a, Fixed(0.75, 0), 0);
        Assert.True(again.Duplicate);
        Assert.Equal(10, again.Shards);
        Assert.Equal(10, a.Pet(first.Pet.Id).shards);
        Assert.Equal(2, a.urShards);
        PetGacha.PullOne(a, Fixed(0.0, 0), 0); // UR: no UR shard
        Assert.Equal(2, a.urShards);
    }

    [Fact]
    public void CoinsCostOneHundredFiftyCrystalsEach_AndATenPullNeedsTenCoins()
    {
        var a = new AccountState { crystals = 1499 };
        Assert.False(PetGacha.BuyCoins(a, 10).Ok);
        Assert.True(PetGacha.BuyCoins(a, 9).Ok);
        Assert.Equal(149, a.crystals);
        Assert.Equal(9, a.tickets);
        var results = new List<PullResult>();
        Assert.False(PetGacha.Pull(a, 10, Fixed(0.5), 0, results).Ok);
        Assert.False(PetGacha.Pull(a, 3, Fixed(0.5), 0, results).Ok);
        Assert.Empty(results);
        Assert.True(PetGacha.Pull(a, 1, Fixed(0.5, 0), 0, results).Ok);
        Assert.Equal(8, a.tickets);
    }

    [Fact]
    public void HistoryKeepsTheLast200WithPityAfterEachPull()
    {
        var a = new AccountState { tickets = 250 };
        var results = new List<PullResult>();
        for (int i = 0; i < 25; i++) PetGacha.Pull(a, 10, Fixed(0.9), i, results);
        Assert.Equal(200, a.history.Count);
        Assert.Equal(24, a.history.Last().at);
        Assert.Equal("frostfangKing", a.history.Last().id); // pull 250 is a UR guarantee (every 50th without one)
        Assert.Contains(a.history, h => h.id == ""); // shard pulls in between
        Assert.Equal(a.pitySSR, a.history.Last().pitySSR);
    }

    [Fact]
    public void OneHundredTwentyUrShardsBuyFrostfangKingOnce()
    {
        var a = new AccountState { urShards = 119 };
        Assert.False(PetGacha.ExchangeUr(a).Ok);
        a.urShards = 125;
        Assert.True(PetGacha.ExchangeUr(a).Ok);
        Assert.Equal(5, a.urShards);
        Assert.True(a.OwnsPet("frostfangKing"));
        a.urShards = 500;
        Assert.False(PetGacha.ExchangeUr(a).Ok);
    }

    [Fact]
    public void SummaryFitsTheWire()
    {
        var a = new AccountState { tickets = 10 };
        var results = new List<PullResult>();
        string text = PetGacha.Pull(a, 10, new SeededRng(3).AsRand(), 0, results).Text;
        Assert.StartsWith("10 pulls:", text);
        Assert.True(text.Length < 120);
    }

    [Fact]
    public void AnOldAccountKeepsItsPityClamped()
    {
        AccountState a = AccountState.Sanitize(new AccountState { version = 1, pitySSR = 29, pityUR = 59, tickets = 4 });
        Assert.Equal(19, a.pitySSR); // the next pull is the SSR+ guarantee
        Assert.Equal(49, a.pityUR);
        Assert.Equal(4, a.tickets);
    }

    [Fact]
    public void SecureRandomStaysInRange()
    {
        for (int i = 0; i < 1000; i++) { double v = RpgRandom.Secure(); Assert.InRange(v, 0.0, 0.9999999999); }
    }
}
