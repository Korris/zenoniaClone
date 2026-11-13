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
        var result = Leveling.ApplyExpGain(59, 0, int.MaxValue);
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
}
