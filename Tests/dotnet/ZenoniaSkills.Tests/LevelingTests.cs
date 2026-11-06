using Xunit;
using ZenoniaProto.Rpg;

public class LevelingTests
{
    [Fact]
    public void FirstLevelTakesEighteenFrostSlimes()
    {
        int reward = LootTable.For("frostSlime").Exp;
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
