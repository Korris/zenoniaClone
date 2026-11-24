using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Monster leash rules (2026-11-24: a monster beyond its leash froze in a Chase / Return flip and dropped the hero's hits).</summary>
public class MonsterLeashRulesTests
{
    [Fact]
    public void WandererTurnsHomeBeforeTheLeash()
    {
        Assert.False(MonsterLeashRules.WanderTowardHome(10f, 72f));
        Assert.False(MonsterLeashRules.WanderTowardHome(43f, 72f));
        Assert.True(MonsterLeashRules.WanderTowardHome(44f, 72f));
        Assert.True(MonsterLeashRules.WanderTowardHome(100f, 72f));
    }

    [Fact]
    public void ResetOnReturnMonsterNeverTurnsBackOnTheWayHome()
    {
        Assert.False(MonsterLeashRules.ReaggroOnReturn(true, 1f, 72f)); // hero right beside it: it still walks home (no flip at the leash edge)
    }

    [Fact]
    public void OldMonstersStillReaggroInsideSixtyPercentOfAggro()
    {
        Assert.True(MonsterLeashRules.ReaggroOnReturn(false, 10f, 72f));
        Assert.False(MonsterLeashRules.ReaggroOnReturn(false, 50f, 72f));
    }
}
