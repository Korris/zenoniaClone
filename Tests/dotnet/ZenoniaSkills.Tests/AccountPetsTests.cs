using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Account side of the pet system (docs/pet-system-design.md): unlocks, buying, migration of old pets, rewards (crystals, food, balls), the account JSON limits.</summary>
public class AccountPetsTests
{
    static readonly long Monday10am = new System.DateTimeOffset(2026, 11, 16, 10, 0, 0, System.TimeSpan.FromHours(7)).ToUnixTimeMilliseconds();
    const long Day = 86_400_000L;

    [Fact]
    public void SpiritUnlocksAtLevelFive_Once()
    {
        var a = new AccountState();
        Assert.False(AccountPets.UnlockByLevel(a, 4).Result.Ok);
        Assert.True(AccountPets.UnlockByLevel(a, 5).Result.Ok);
        Assert.True(a.OwnsPet("spirit"));
        Assert.False(AccountPets.UnlockByLevel(a, 30).Result.Ok);
    }

    [Fact]
    public void ImpIsBoughtOncePerAccount()
    {
        var a = new AccountState();
        PetDef imp = PetConfig.Get("imp");
        Assert.False(AccountPets.Buy(a, imp, 2999).Result.Ok);
        AccountPets.Outcome o = AccountPets.Buy(a, imp, 3000);
        Assert.True(o.Result.Ok);
        Assert.Equal(-3000, o.GoldDelta);
        Assert.False(AccountPets.Buy(a, imp, 99999).Result.Ok);
        Assert.False(AccountPets.Buy(a, PetConfig.Get("spirit"), 99999).Result.Ok); // not for sale
        Assert.False(AccountPets.Buy(a, PetConfig.Get("frostfangKing"), 99999).Result.Ok);
    }

    static PlayerState OldHero(bool wornSpirit, int bagImps)
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Level = 12;
        if (wornSpirit) p.SetEquippedRaw(Slots.Pet, ItemFactory.CreateLegacyPet(PetConfig.Get("spirit"), () => 0.1));
        for (int i = 0; i < bagImps; i++) p.Inventory.Add(ItemFactory.CreateLegacyPet(PetConfig.Get("imp"), () => 0.2 + i * 0.1));
        return p;
    }

    [Fact]
    public void OldPetsBecomeAccountPetsWithCompensationOncePerKind()
    {
        var a = new AccountState();
        PlayerState p = OldHero(true, 2);
        AccountPets.Outcome o = AccountPets.Migrate(a, p.LegacyPets());
        Assert.True(o.Result.Ok);
        Assert.Equal(6000, o.GoldDelta); // two kinds, not three items
        Assert.Equal(1, a.StarsOf("spirit"));
        Assert.Equal(10, a.Pet("imp").shards);
        Assert.Equal(new[] { "spirit", "imp" }, a.compensated.ToArray());
        AccountPets.Outcome again = AccountPets.Migrate(a, OldHero(false, 1).LegacyPets()); // another hero of the account
        Assert.Equal(0, again.GoldDelta);
        Assert.Equal(10, a.Pet("imp").shards);
    }

    [Fact]
    public void JoiningMigratesGivesTheStarterAndLevelRewards_AndTheSecondJoinChangesNothing()
    {
        var a = new AccountState();
        PlayerState p = OldHero(true, 1);
        AccountSync.Plan plan = AccountSync.OnJoin(a, p, Monday10am);
        Assert.True(plan.Changed && plan.RemoveLegacy);
        Assert.Equal("spirit", plan.WearLegacy);
        Assert.Equal(6000, plan.GoldDelta);
        Assert.Equal(10, a.tickets); // starter coins
        Assert.Equal(300, a.crystals); // the level 10 milestone
        Assert.Equal(10, a.levelReward);
        p.RemoveLegacyPets();
        Assert.False(AccountSync.OnJoin(a, p, Monday10am).Changed);
    }

    [Fact]
    public void WantedBossPaysCrystalsOncePerDay_FourPerWeek_FoodEveryDay_AndARareBallEvery28()
    {
        var a = new AccountState();
        Assert.True(AccountRewards.OnWantedClaim(a, Monday10am).Ok);
        Assert.False(AccountRewards.OnWantedClaim(a, Monday10am + 3_600_000).Ok); // another hero, same day
        for (int d = 1; d < 7; d++) AccountRewards.OnWantedClaim(a, Monday10am + d * Day);
        Assert.Equal(4 * 150, a.crystals);
        Assert.Equal(7 * 10, a.food);
        Assert.Equal(7, a.wantedForRareBall);
        for (int d = 7; d < 28; d++) AccountRewards.OnWantedClaim(a, Monday10am + d * Day);
        Assert.Equal(1, a.rareBalls);
        Assert.Equal(0, a.wantedForRareBall);
    }

    [Fact]
    public void TheFifthValidMapBossOfTheWeekPaysCrystals_EveryOnePaysFood()
    {
        var a = new AccountState();
        for (int i = 0; i < 5; i++) Assert.True(AccountRewards.OnMapBossKill(a, Monday10am).Ok);
        Assert.Equal(150, a.crystals);
        Assert.Equal(15, a.food);
        AccountRewards.OnMapBossKill(a, Monday10am);
        Assert.Equal(150, a.crystals);
        Assert.True(AccountRewards.IsValidBossShare(6, 580f));
        Assert.False(AccountRewards.IsValidBossShare(5, 580f));
    }

    [Fact]
    public void LevelMilestonesPayCrystalsOncePerAccountUpToNinety()
    {
        var a = new AccountState();
        Assert.False(AccountRewards.OnLevel(a, 9).Ok);
        Assert.True(AccountRewards.OnLevel(a, 35).Ok);
        Assert.Equal(900, a.crystals);
        Assert.False(AccountRewards.OnLevel(a, 31).Ok); // an alt passing 30 again
        Assert.True(AccountRewards.OnLevel(a, 90).Ok);
        Assert.Equal(2700, a.crystals);
        Assert.Equal(90, a.levelReward);
    }

    [Fact]
    public void TheWeekTurnsOnMondayVietnamTime()
    {
        long sunday2359 = new System.DateTimeOffset(2026, 11, 15, 23, 59, 0, System.TimeSpan.FromHours(7)).ToUnixTimeMilliseconds();
        long monday0000 = new System.DateTimeOffset(2026, 11, 16, 0, 0, 0, System.TimeSpan.FromHours(7)).ToUnixTimeMilliseconds();
        Assert.Equal(AccountRewards.WeekKey(sunday2359) + 1, AccountRewards.WeekKey(monday0000));
        Assert.Equal(AccountRewards.WeekKey(monday0000), AccountRewards.WeekKey(monday0000 + 6 * Day + 3_600_000L));
    }

    [Fact]
    public void SanitizeKeepsTheServerLimits()
    {
        var raw = new AccountState { tickets = -5, pitySSR = 40, pityUR = 99, levelReward = 999, weekWantedClaims = 9, ballsBoughtToday = 50, wantedForRareBall = 99 };
        raw.pets.Add(new OwnedPet { id = "imp", stars = 9, shards = -1, level = 99 });
        raw.pets.Add(new OwnedPet { id = "imp", stars = 1 });
        raw.pets.Add(new OwnedPet { id = "dragonGod", stars = 1 });
        for (int i = 0; i < 230; i++) raw.history.Add(new PullRecord { id = i == 229 ? "nope" : i % 2 == 0 ? "imp" : "", at = i });
        AccountState a = AccountState.Sanitize(raw);
        Assert.Single(a.pets);
        Assert.Equal(5, a.StarsOf("imp"));
        Assert.Equal(60, a.LevelOf("imp"));
        Assert.Equal(0, a.Pet("imp").shards);
        Assert.Equal(new[] { 0, 19, 49 }, new[] { a.tickets, a.pitySSR, a.pityUR });
        Assert.Equal(90, a.levelReward);
        Assert.Equal(4, a.weekWantedClaims);
        Assert.Equal(5, a.ballsBoughtToday);
        Assert.Equal(27, a.wantedForRareBall);
        Assert.Equal(199, a.history.Count); // the last 200, minus the unknown one
        Assert.Equal(2, a.version);
    }

    [Fact]
    public void CloneIsDeep_AndCopyFromKeepsTheObject()
    {
        var a = new AccountState { tickets = 3 };
        a.pets.Add(new OwnedPet { id = "imp", foodSpent = 4 });
        AccountState c = a.Clone();
        c.pets[0].stars = 3; c.tickets = 7; c.pets[0].foodSpent = 9;
        Assert.Equal(1, a.StarsOf("imp"));
        Assert.Equal(4, a.pets[0].foodSpent);
        a.CopyFrom(c);
        Assert.Equal(3, a.StarsOf("imp"));
        Assert.Equal(7, a.tickets);
        c.pets[0].stars = 1;
        Assert.Equal(3, a.StarsOf("imp"));
    }

    [Fact]
    public void TheAccountJsonRoundTrips()
    {
        var a = new AccountState { tickets = 4, crystals = 450, pityUR = 12, starterGiven = true, food = 7, balls = 2, rareBalls = 1, sharedShards = 15 };
        a.pets.Add(new OwnedPet { id = "crystalGolem", stars = 2, shards = 30, level = 14, foodSpent = 40, shardsSpent = 10, goldSpent = 5000 });
        a.history.Add(new PullRecord { at = 5, id = "crystalGolem", pityUR = 0 });
        string json = UnityEngine.JsonUtility.ToJson(a);
        AccountState back = AccountState.Sanitize(UnityEngine.JsonUtility.FromJson<AccountState>(json));
        Assert.Equal(2, back.StarsOf("crystalGolem"));
        Assert.Equal(14, back.LevelOf("crystalGolem"));
        Assert.Equal(40, back.Pet("crystalGolem").foodSpent);
        Assert.Equal(new[] { 4, 450, 7, 2, 1, 15 }, new[] { back.tickets, back.crystals, back.food, back.balls, back.rareBalls, back.sharedShards });
        Assert.True(back.starterGiven);
        Assert.Single(back.history);
    }

    [Fact]
    public void CollectionLevelFollowsTheSpeciesOwned()
    {
        var a = new AccountState();
        Assert.Equal(1, a.CollectionLevel);
        foreach (string id in new[] { "spirit", "imp", "frostSlime" }) AccountPets.Add(a, id);
        Assert.Equal(2, a.CollectionLevel);
        foreach (string id in new[] { "hornRabbit", "iceGoblin" }) AccountPets.Add(a, id);
        Assert.Equal(3, a.CollectionLevel);
        foreach (string id in new[] { "thornBoar", "spectralWolf" }) AccountPets.Add(a, id);
        Assert.Equal(4, a.CollectionLevel);
        foreach (string id in new[] { "marshStalker", "crystalGolem", "frostfangKing" }) AccountPets.Add(a, id);
        Assert.Equal(5, a.CollectionLevel);
    }
}
