using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>The V2 rules (docs/pet-system-design.md 3, 5, 6): the five-slot team with Bond and caps, levels / stars / resets, catching monsters.</summary>
public class PetTeamGrowthCaptureTests
{
    static AccountState Owning(params string[] ids) { var a = new AccountState(); foreach (string id in ids) AccountPets.Add(a, id); return a; }
    static readonly long Noon = new System.DateTimeOffset(2026, 11, 16, 12, 0, 0, System.TimeSpan.FromHours(7)).ToUnixTimeMilliseconds();

    // ---- team ----

    [Fact]
    public void SlotsOpenByLevel_OneSpeciesOneSlot_OnlyOwnedPets()
    {
        AccountState a = Owning("imp", "frostSlime", "iceGoblin");
        string[] team = PetTeam.Empty();
        Assert.Equal("Opens at level 5", PetTeam.Refusal(team, 0, "imp", 4, a));
        Assert.Null(PetTeam.Refusal(team, 0, "imp", 5, a));
        Assert.Equal("Opens at level 20", PetTeam.Refusal(team, 1, "imp", 19, a));
        Assert.Equal("You do not own Thorn Boar", PetTeam.Refusal(team, 0, "thornBoar", 50, a));
        PetTeam.Set(team, 0, "imp");
        Assert.Equal("Imp is already in the team", PetTeam.Refusal(team, 3, "imp", 50, a));
        Assert.Null(PetTeam.Refusal(team, 3, "", 50, a)); // clearing is always allowed
    }

    [Fact]
    public void CombatGivesTheWholePassive_StatSlotsAQuarter_SupportSlotsNone_ScaledByStars()
    {
        AccountState a = Owning("imp", "frostfangKing", "thornBoar");
        a.Pet("frostfangKing").stars = 5; // 140 %
        string[] team = { "imp", "thornBoar", "", "frostfangKing", "" };
        PetBonus b = PetTeam.Bonus(team, a);
        Assert.Equal(0.03 + 0.05 * 1.4 * 0.25, b.Damage, 9); // Imp combat + King in a stat slot
        Assert.Equal(0.0, b.DamageTaken); // Thorn Boar in a support slot gives no passive
    }

    [Fact]
    public void BondsNeedBothSpecies_AndTheSumIsCapped()
    {
        AccountState a = Owning("frostSlime", "iceGoblin", "crystalGolem", "frostfangKing", "thornBoar");
        string[] team = { "iceGoblin", "", "", "frostSlime", "thornBoar" };
        PetBonus b = PetTeam.Bonus(team, a);
        Assert.Single(PetTeam.ActiveBonds(team));
        Assert.Equal(0.03 * 0.25 + 0.02, b.DamageTaken, 9); // Thorn Boar -3 % x 25 % + Frost Pack 2 %
        foreach (OwnedPet p in a.pets) p.stars = 5;
        string[] cap = { "frostfangKing", "", "", "crystalGolem", "iceGoblin" };
        PetBonus c = PetTeam.Bonus(cap, a);
        Assert.True(c.Damage <= PetRules.Cap(PetPassive.Damage) + 1e-9);
        Assert.True(c.BossDamage <= 0.02 + 1e-9);
    }

    [Fact]
    public void TheHeroStateFieldsATeam_AndDropsWhatTheAccountDoesNotOwn()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Level = 50;
        p.AttachAccount(Owning("imp", "frostSlime"));
        int hp = p.MaxHp, atk = p.Atk;
        Assert.True(p.SetTeamSlot(0, "imp").Ok);
        Assert.True(p.SetTeamSlot(3, "frostSlime").Ok);
        Assert.False(p.SetTeamSlot(4, "frostSlime").Ok);
        Assert.Equal((float)(atk * 1.03), p.DamageAtk, 3);
        Assert.Equal(RpgMath.Round(hp * (1 + 0.05 * 0.25)), p.MaxHp);
        Assert.Equal("imp", p.PetId);
        p.Team[1] = "frostfangKing"; // a hand-edited save
        p.AttachAccount(p.Account);
        Assert.Equal("", p.Team[1]);
    }

    [Fact]
    public void TheTeamSurvivesASave_AndAV4SaveLoadsItsPetIntoTheCombatSlot()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.Level = 50;
        p.AttachAccount(Owning("imp", "frostSlime", "iceGoblin"));
        p.SetTeamSlot(0, "imp"); p.SetTeamSlot(2, "iceGoblin"); p.SetTeamSlot(4, "frostSlime");
        SaveData d = SaveSerializer.Parse(SaveSerializer.ToJson(SaveSerializer.Capture(p, true, 1f, 2f, false)));
        Assert.Equal(5, d.version);
        Assert.Equal(new[] { "imp", "", "iceGoblin", "", "frostSlime" }, SaveSerializer.Restore(d).Team);
        const string v4 = "{\"version\":4,\"classId\":\"warrior\",\"level\":5,\"petId\":\"imp\",\"inventory\":[],\"equipment\":[]}";
        Assert.Equal(new[] { "imp", "", "", "", "" }, SaveSerializer.Restore(SaveSerializer.Parse(v4)).Team);
    }

    // ---- growth ----

    [Fact]
    public void LevelsCostFood_StopAtTheHeroesTop_AndRaiseThePetsOwnDamage()
    {
        AccountState a = Owning("imp");
        a.food = 3;
        Assert.True(PetGrowth.LevelUp(a, "imp", 60).Result.Ok); // 2 food at level 1
        Assert.Equal(1, a.food);
        Assert.False(PetGrowth.LevelUp(a, "imp", 60).Result.Ok);
        a.food = 1000;
        Assert.False(PetGrowth.LevelUp(a, "imp", 2).Result.Ok); // the hero's best is level 2
        for (int i = 0; i < 58; i++) PetGrowth.LevelUp(a, "imp", 60);
        Assert.Equal(60, a.LevelOf("imp"));
        Assert.False(PetGrowth.LevelUp(a, "imp", 90).Result.Ok);
        Assert.Equal(1.295, PetRules.LevelDamageMul(60), 9);
        Assert.Equal(1.0, PetRules.LevelDamageMul(1), 9);
        Assert.Equal(5, PetRules.FoodForLevel(30));
    }

    [Fact]
    public void StarsCostShardsAndGoldUpToFive_ThenResetGivesEverythingBack()
    {
        AccountState a = Owning("imp");
        a.Pet("imp").shards = 100;
        int[] gold = { 5000, 10000, 20000, 30000 };
        for (int i = 0; i < 4; i++) { AccountPets.Outcome o = PetGrowth.StarUp(a, "imp", 99999); Assert.True(o.Result.Ok); Assert.Equal(-gold[i], o.GoldDelta); }
        Assert.Equal(5, a.StarsOf("imp"));
        Assert.Equal(0, a.Pet("imp").shards);
        Assert.False(PetGrowth.StarUp(a, "imp", 99999).Result.Ok);
        Assert.Equal(0.03 * 1.4, PetConfig.PassiveAt(PetConfig.Get("imp"), 5), 9);
        AccountPets.Outcome r = PetGrowth.ResetStars(a, "imp");
        Assert.True(r.Result.Ok);
        Assert.Equal(65000, r.GoldDelta);
        Assert.Equal(100, a.Pet("imp").shards);
        Assert.Equal(1, a.StarsOf("imp"));
        AccountPets.Outcome again = PetGrowth.ResetStars(a, "imp"); // ChatGPT Lần 9: a second reset (a replayed request) refunds nothing
        Assert.False(again.Result.Ok); Assert.Equal(0, again.GoldDelta); Assert.Equal(100, a.Pet("imp").shards);
        a.food = 10; PetGrowth.LevelUp(a, "imp", 60); PetGrowth.LevelUp(a, "imp", 60);
        Assert.True(PetGrowth.ResetLevel(a, "imp").Result.Ok);
        Assert.Equal(10, a.food);
        Assert.Equal(1, a.LevelOf("imp"));
        Assert.False(PetGrowth.ResetLevel(a, "imp").Result.Ok); Assert.Equal(10, a.food);
    }

    [Fact]
    public void SharedShardsConvertOneToOne()
    {
        AccountState a = Owning("imp");
        a.sharedShards = 7;
        Assert.False(PetGrowth.Convert(a, "imp", 8).Result.Ok);
        Assert.True(PetGrowth.Convert(a, "imp", 7).Result.Ok);
        Assert.Equal(7, a.Pet("imp").shards);
        Assert.Equal(0, a.sharedShards);
    }

    // ---- capture ----

    static CaptureTarget Live(string id, double hp) { return new CaptureTarget { Pet = PetConfig.Get(id), Alive = true, HpShare = hp, HitRecently = true, InRange = true }; }

    [Fact]
    public void BallsCostFiveHundred_FiveADay()
    {
        var a = new AccountState();
        Assert.False(PetCapture.BuyBalls(a, 6, 99999, Noon).Result.Ok);
        AccountPets.Outcome o = PetCapture.BuyBalls(a, 5, 2500, Noon);
        Assert.True(o.Result.Ok);
        Assert.Equal(-2500, o.GoldDelta);
        Assert.False(PetCapture.BuyBalls(a, 1, 99999, Noon).Result.Ok);
        Assert.True(PetCapture.BuyBalls(a, 1, 99999, Noon + 86_400_000L).Result.Ok);
    }

    [Fact]
    public void ALivingMonsterNeedsHalfHp_ARecentHit_Range_ABall_AndTheCollectionLevel()
    {
        AccountState a = Owning("imp", "frostSlime"); // 2 species: collection level 1
        a.balls = 2;
        Assert.Equal("Weaken it to half its HP first", PetCapture.Refusal(a, Live("thornBoar", 0.6), false));
        CaptureTarget t = Live("thornBoar", 0.4);
        Assert.Null(PetCapture.Refusal(a, t, false));
        t.HitRecently = false; Assert.Equal("Hit it first", PetCapture.Refusal(a, t, false));
        t.HitRecently = true; t.InRange = false; Assert.Equal("Too far away", PetCapture.Refusal(a, t, false));
        Assert.StartsWith("Your pet collection must reach level 2", PetCapture.Refusal(a, Live("spectralWolf", 0.1), false)); // SR needs level 2 = 3 species
        AccountPets.Add(a, "iceGoblin");
        Assert.Null(PetCapture.Refusal(a, Live("spectralWolf", 0.1), false)); // 3 species: level 2
        Assert.StartsWith("Your pet collection must reach level 4", PetCapture.Refusal(a, Live("frostfangKing", 0.1), false));
        a.balls = 0;
        Assert.Equal("No Pet Ball", PetCapture.Refusal(a, t, false));
    }

    [Fact]
    public void OddsFollowTheTable_AMissSpendsTheBall_ACatchAddsThePet()
    {
        Assert.Equal(0.40, PetRules.LiveCaptureChance(PetRarity.N, 0.5));
        Assert.Equal(0.60, PetRules.LiveCaptureChance(PetRarity.N, 0.2));
        Assert.Equal(0.04, PetRules.LiveCaptureChance(PetRarity.SSR, 0.3));
        Assert.Equal(0.0, PetRules.LiveCaptureChance(PetRarity.UR, 0.1));
        Assert.Equal(0.05, PetRules.BossCaptureChance(PetRarity.SSR, false));
        Assert.Equal(0.005, PetRules.BossCaptureChance(PetRarity.UR, true));
        AccountState a = Owning("imp");
        a.balls = 3;
        CaptureTarget t = Live("thornBoar", 0.3);
        Assert.Equal(CaptureOutcome.Missed, PetCapture.Throw(a, t, false, 0.5, Noon)); // 40 % for an R at 26-50 %
        Assert.Equal(2, a.balls);
        Assert.Equal(CaptureOutcome.Caught, PetCapture.Throw(a, t, false, 0.1, Noon));
        Assert.True(a.OwnsPet("thornBoar"));
        Assert.Equal(CaptureOutcome.Caught, PetCapture.Throw(a, t, false, 0.1, Noon)); // again: shards
        Assert.Equal(10, a.Pet("thornBoar").shards);
        Assert.Equal(0, a.balls);
        Assert.Equal(CaptureOutcome.Refused, PetCapture.Throw(a, t, false, 0.1, Noon));
    }

    [Fact]
    public void AFallenBossIsOneTryADay_ARareBallAlwaysCatches()
    {
        AccountState a = Owning("imp", "frostSlime", "hornRabbit", "iceGoblin", "thornBoar", "spectralWolf", "marshStalker"); // 7 species: level 4
        a.balls = 2; a.rareBalls = 1;
        var boss = new CaptureTarget { Pet = PetConfig.Get("frostfangKing"), Boss = true, HitRecently = true, InRange = true };
        Assert.Null(PetCapture.Refusal(a, boss, false));
        Assert.Equal(CaptureOutcome.Missed, PetCapture.Throw(a, boss, false, 0.5, Noon)); // 1 %
        Assert.Equal(CaptureOutcome.Caught, PetCapture.Throw(a, boss, true, 0.99, Noon));
        Assert.True(a.OwnsPet("frostfangKing"));
        Assert.True(a.dayBossCaught);
        Assert.Equal("One boss pet a day", PetCapture.Refusal(a, boss, false));
        var nothing = new CaptureTarget { Pet = PetConfig.Get("spirit"), Alive = true, HpShare = 0.1, HitRecently = true, InRange = true };
        Assert.Equal("That cannot become a pet", PetCapture.Refusal(a, nothing, true));
    }
}
