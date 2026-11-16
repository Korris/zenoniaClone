using Xunit;
using ZenoniaProto.Rpg;

public class LevelingTests
{
    [Fact]
    public void FirstLevelTakesEighteenFrostSlimes()
    {
        // World Map 2.0 step 1 (13/11/2026) moved Frost Slime to Lv11 (Frost Hollow's own band) and its EXP with it, so this leveling-curve check no longer reads it from LootTable:
        // 14 is simply the old Lv1 Frost Slime EXP this test was built around (Leveling.ExpToNext(1) == 250, so 18 kills of a Lv1-ish 14-EXP monster is the first level-up).
        int reward = 14;
        var before = Leveling.ApplyExpGain(1, 0, 17 * reward);
        Assert.Equal(1, before.Level);
        var after = Leveling.ApplyExpGain(before.Level, before.Exp, reward);
        Assert.Equal(2, after.Level);
        Assert.Equal(2, after.Exp);
    }

    [Fact]
    public void LargeRewardCarriesRemainderAcrossSeveralLevels()
    {
        int amount = Leveling.ExpToNext(1) + Leveling.ExpToNext(2) + 19;
        var result = Leveling.ApplyExpGain(1, 0, amount);
        Assert.Equal(3, result.Level);
        Assert.Equal(2, result.LevelsGained);
        Assert.Equal(19, result.Exp);
    }

    [Fact]
    public void LevelCapAndProgressStayBounded()
    {
        var result = Leveling.ApplyExpGain(89, 0, int.MaxValue);
        Assert.Equal(90, Leveling.MaxLevel);
        Assert.Equal(Leveling.MaxLevel, result.Level);
        Assert.Equal(Leveling.ExpToNext(Leveling.MaxLevel) - 1, result.Exp);
        Assert.InRange(Leveling.ProgressRatio(result.Level, result.Exp), 0f, 1f);
    }

    [Fact]
    public void RequirementsRiseThroughoutTheLevelRange()
    {
        for (int level = 1; level < Leveling.MaxLevel; level++)
            Assert.True(Leveling.ExpToNext(level + 1) > Leveling.ExpToNext(level));
    }

    [Fact]
    public void LevelsAboveSixtyContinueTheSameCurveWithoutAJump()
    {
        // Lv 61-90 (16/11/2026) use the one ExpToNext formula: each step past Lv 60 grows by under 3 %, and the whole road fits an int.
        Assert.Equal(116185, Leveling.ExpToNext(60));
        Assert.Equal(119105, Leveling.ExpToNext(61));
        Assert.Equal(209905, Leveling.ExpToNext(89));
        for (int level = 60; level < Leveling.MaxLevel; level++)
            Assert.InRange(Leveling.ExpToNext(level + 1) / (double)Leveling.ExpToNext(level), 1.0, 1.03);
        long total = 0;
        for (int level = 1; level < Leveling.MaxLevel; level++) total += Leveling.ExpToNext(level);
        Assert.True(total < int.MaxValue);
        var r = Leveling.ApplyExpGain(60, 0, Leveling.ExpToNext(60) + 5);
        Assert.Equal(61, r.Level);
        Assert.Equal(5, r.Exp);
    }
}
