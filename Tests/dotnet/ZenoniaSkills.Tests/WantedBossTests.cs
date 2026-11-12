using System;
using Xunit;
using ZenoniaProto.Rpg;

public class WantedBossTests
{
    static long Utc(int y, int mo, int d, int h, int mi, int s, int ms = 0)
    {
        return new DateTimeOffset(y, mo, d, h, mi, s, ms, TimeSpan.Zero).ToUnixTimeMilliseconds();
    }

    static PlayerState Hero() { return PlayerState.NewGame("warrior"); }

    [Fact]
    public void DayTurnsAt2200UtcWhichIs0500Vietnam()
    {
        int before = WantedBoss.DayKey(Utc(2026, 11, 12, 21, 59, 59, 999));
        int after = WantedBoss.DayKey(Utc(2026, 11, 12, 22, 0, 0));
        Assert.Equal(before + 1, after);
        Assert.Equal(after, WantedBoss.DayKey(Utc(2026, 11, 13, 21, 59, 59, 999)));
        Assert.True(before > 0);
    }

    [Fact]
    public void NextResetIsTheFirstInstantOfTheNextKey()
    {
        long now = Utc(2026, 11, 12, 10, 0, 0);
        long next = WantedBoss.NextResetUtcMs(now);
        Assert.Equal(Utc(2026, 11, 12, 22, 0, 0), next);
        Assert.Equal(WantedBoss.DayKey(now) + 1, WantedBoss.DayKey(next));
        Assert.Equal(WantedBoss.DayKey(now), WantedBoss.DayKey(next - 1));
    }

    [Fact]
    public void RotationCoversAllBossesOverThreeDaysAndMatchesTheTable()
    {
        long t = Utc(2026, 11, 12, 12, 0, 0);
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < 3; i++) seen.Add(WantedBoss.Today(t + i * WantedBoss.DayMs).BossType);
        Assert.Equal(3, seen.Count);
        Assert.Equal(WantedBoss.Today(t).BossType, WantedBoss.Today(t + 3 * WantedBoss.DayMs).BossType);
        foreach (WantedBossDef d in WantedBoss.Rotation) Assert.Same(d, WantedBoss.Find(d.BossType));
        Assert.Null(WantedBoss.Find("goblin"));
    }

    [Fact]
    public void OverrideWinsOverTheRotation()
    {
        long t = Utc(2026, 11, 12, 12, 0, 0);
        try
        {
            WantedBoss.Override = "mireHydraQueen";
            Assert.Equal("mireHydraQueen", WantedBoss.Today(t).BossType);
            Assert.Equal("mireHydraQueen", WantedBoss.Today(t + WantedBoss.DayMs).BossType);
            WantedBoss.Override = "notABoss"; // unknown type: ignored, the rotation answers
            Assert.Equal(WantedBoss.For(WantedBoss.DayKey(t)).BossType, WantedBoss.Today(t).BossType);
        }
        finally { WantedBoss.Override = null; }
    }

    [Fact]
    public void ClaimAddsTheExactRewardOnce()
    {
        PlayerState p = Hero();
        WantedBossDef d = WantedBoss.Find("glacialColossus");
        int gold = p.Gold, stones = p.UpgradeStone, hpS = p.HpPotions, mpS = p.MpPotions, hpM = p.HpPotionsMedium, mpM = p.MpPotionsMedium;
        ActionResult r = p.ClaimWanted(20500, d);
        Assert.True(r.Ok);
        Assert.Equal("+600 G, +5 Upgrade Stones, +3 HP / +3 MP Medium Potions", r.Text);
        Assert.Equal(gold + 600, p.Gold);
        Assert.Equal(stones + 5, p.UpgradeStone);
        Assert.Equal(hpM + 3, p.HpPotionsMedium);
        Assert.Equal(mpM + 3, p.MpPotionsMedium);
        Assert.Equal(hpS, p.HpPotions);
        Assert.Equal(mpS, p.MpPotions);
        Assert.Equal(20500, p.WantedDay);

        ActionResult again = p.ClaimWanted(20500, d);
        Assert.False(again.Ok);
        Assert.Equal(gold + 600, p.Gold);
        Assert.Equal(stones + 5, p.UpgradeStone);
        Assert.Equal(hpM + 3, p.HpPotionsMedium);
        Assert.Equal(20500, p.WantedDay);

        Assert.True(p.ClaimWanted(20501, d).Ok); // next day pays again
        Assert.Equal(gold + 1200, p.Gold);
        Assert.False(p.ClaimWanted(20400, d).Ok); // an older key never reopens a claim
    }

    [Fact]
    public void EveryBossPaysItsOwnPotionSize()
    {
        PlayerState p = Hero();
        Assert.True(p.ClaimWanted(1, WantedBoss.Find("rimeclawTyrant")).Ok);
        Assert.True(p.ClaimWanted(2, WantedBoss.Find("mireHydraQueen")).Ok);
        Assert.Equal(3, p.HpPotionsLarge);
        Assert.Equal(3, p.MpPotionsLarge);
        Assert.Equal(3, p.HpPotions - Hero().HpPotions);
    }

    [Fact]
    public void ClaimRejectsNoBossOrNoDay()
    {
        PlayerState p = Hero();
        Assert.False(p.ClaimWanted(20500, null).Ok);
        Assert.False(p.ClaimWanted(0, WantedBoss.Rotation[0]).Ok);
        Assert.Equal(0, p.WantedDay);
    }

    [Fact]
    public void StonesStayCappedAtTheMaximum()
    {
        PlayerState p = Hero();
        p.UpgradeStone = PlayerState.MaxStones - 2;
        Assert.True(p.ClaimWanted(20500, WantedBoss.Find("mireHydraQueen")).Ok);
        Assert.Equal(PlayerState.MaxStones, p.UpgradeStone);
    }

    [Fact]
    public void SaveKeepsTheClaimedDayAndOldSavesReadZero()
    {
        PlayerState p = Hero();
        p.ClaimWanted(20500, WantedBoss.Rotation[1]);
        string json = SaveSerializer.ToJson(SaveSerializer.Capture(p, false, 0f, 0f, false));
        SaveData d = SaveSerializer.Parse(json);
        Assert.NotNull(d);
        Assert.Equal(20500, d.wantedDay);
        Assert.Equal(20500, SaveSerializer.Restore(d).WantedDay);

        string old = SaveSerializer.ToJson(SaveSerializer.Capture(Hero(), false, 0f, 0f, false));
        old = System.Text.RegularExpressions.Regex.Replace(old, ",\\s*\"wantedDay\":\\s*\\d+", ""); // the field is last, so the comma before it goes too
        Assert.DoesNotContain("wantedDay", old);
        Assert.Equal(0, SaveSerializer.Parse(old).wantedDay);
    }

    [Fact]
    public void ParseClampsAnAbsurdDay()
    {
        PlayerState p = Hero();
        SaveData raw = SaveSerializer.Capture(p, false, 0f, 0f, false);
        raw.wantedDay = int.MaxValue;
        Assert.Equal(1_000_000, SaveSerializer.Parse(SaveSerializer.ToJson(raw)).wantedDay);
        raw.wantedDay = -5;
        Assert.Equal(0, SaveSerializer.Parse(SaveSerializer.ToJson(raw)).wantedDay);
    }

    [Fact]
    public void EventBossesAreTheThreeNewTypesWithFixedTilesAndTheOldTiers()
    {
        string[] types = { "rimeclawTyrant", "glacialColossus", "mireHydraQueen" };
        int[] levels = { 10, 25, 40 }, gold = { 150, 600, 1000 }, stones = { 3, 5, 8 };
        for (int i = 0; i < 3; i++)
        {
            WantedBossDef d = WantedBoss.Rotation[i];
            Assert.Equal(types[i], d.BossType);
            Assert.Equal(levels[i], d.BossLevel);
            Assert.Equal(gold[i], d.Gold);
            Assert.Equal(stones[i], d.Stones);
            Assert.True(d.SpawnTx > 0 && d.SpawnTy > 0);
        }
        Assert.Equal("+150 G, +3 stones, +3/+3 potions", WantedBoss.ShortText(WantedBoss.Rotation[0]));
        Assert.Equal("+1000 G, +8 stones, +3/+3 potions", WantedBoss.ShortText(WantedBoss.Rotation[2]));
    }
}
