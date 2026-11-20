using System.Collections.Generic;
using Xunit;
using ZenoniaProto;
using ZenoniaProto.Rpg;

/// <summary>Warrior lock-target slash (2026-11-20): which monster one slash locks, the facing it turns to, and the one-step chain that repeats.</summary>
public class LockTargetRulesTests
{
    [Fact]
    public void PicksTheNearestLiveMonsterInRange()
    {
        var d = new List<float> { 30f * 30f, 10f * 10f, 5f * 5f };
        var alive = new List<bool> { true, true, false };
        Assert.Equal(1, LockTargetRules.Pick(-1, d, alive, 40f)); // the dead nearer one is skipped
        Assert.Equal(-1, LockTargetRules.Pick(-1, new List<float> { 50f * 50f }, new List<bool> { true }, 40f)); // nothing in reach: the slash whiffs
    }

    [Fact]
    public void KeepsTheLockedMonsterWhileValidEvenWhenAnotherIsNearer()
    {
        var d = new List<float> { 30f * 30f, 4f * 4f };
        var alive = new List<bool> { true, true };
        Assert.Equal(0, LockTargetRules.Pick(0, d, alive, 40f));
        alive[0] = false; // locked one died: re-lock on the nearest
        Assert.Equal(1, LockTargetRules.Pick(0, d, alive, 40f));
        alive[0] = true; d[0] = 45f * 45f; // locked one walked out of reach
        Assert.Equal(1, LockTargetRules.Pick(0, d, alive, 40f));
    }

    [Fact]
    public void BoxDistanceIsZeroInsideAndToTheNearestEdgeOutside()
    {
        Assert.Equal(0f, LockTargetRules.BoxDistSq(5f, 5f, 0f, 0f, 10f, 10f));
        Assert.Equal(9f, LockTargetRules.BoxDistSq(13f, 5f, 0f, 0f, 10f, 10f));
    }

    [Theory]
    [InlineData(10f, 2f, 0, 3)]
    [InlineData(-10f, 2f, 0, 2)]
    [InlineData(1f, -9f, 3, 0)]
    [InlineData(0f, 9f, 2, 1)]
    [InlineData(5f, 5f, 3, 3)]  // exact diagonal keeps the current axis
    [InlineData(5f, 5f, 0, 1)]
    [InlineData(0.2f, 0.1f, 2, 2)] // on top of the hero: keep the facing
    public void FacesTheTarget(float dx, float dy, int current, int expected)
    {
        Assert.Equal(expected, LockTargetRules.Facing4(dx, dy, current));
    }

    [Fact]
    public void OneStepChainRepeatsTheSlashWhenPressedDuringIt()
    {
        var combo = new ComboSystem(ComboConfigs.WarriorHits, 400f);
        var ev = new List<ComboEvent>();
        Assert.True(combo.PressAttack());
        combo.Update(150f, ev);
        Assert.Contains(ev, e => e.Type == ComboEventType.Hit);
        Assert.False(combo.PressAttack()); // buffered
        ev.Clear();
        combo.Update(260f, ev); // past the 400 ms swing
        Assert.Contains(ev, e => e.Type == ComboEventType.End);
        Assert.True(combo.IsAttacking); // the buffered press started the next slash
        Assert.Equal(1, combo.Step);
        ev.Clear();
        combo.Update(400f, ev);
        Assert.False(combo.IsAttacking); // no press: it stops
    }

    [Fact]
    public void ChaseWalksStraightInEightWaysWithoutZigZag()
    {
        int sx, sy;
        LockTargetRules.ChaseDir(40f, 20f, 3f, out sx, out sy); Assert.Equal((1, 1), (sx, sy));
        LockTargetRules.ChaseDir(-40f, 2f, 3f, out sx, out sy); Assert.Equal((-1, 0), (sx, sy)); // nearly level: only left
        LockTargetRules.ChaseDir(1f, -2f, 3f, out sx, out sy); Assert.Equal((0, -1), (sx, sy));  // inside the dead zone: still steps the longer axis
    }

    [Fact]
    public void ChaseStopsWhenTooFarTooLongOrStuck()
    {
        Assert.True(LockTargetRules.KeepChasing(100f, 2f, 0.2f));
        Assert.False(LockTargetRules.KeepChasing(LockTargetRules.ChaseLeash + 1f, 2f, 0.2f));
        Assert.False(LockTargetRules.KeepChasing(100f, LockTargetRules.ChaseMaxSeconds + 0.1f, 0.2f));
        Assert.False(LockTargetRules.KeepChasing(100f, 2f, LockTargetRules.ChaseStuckSeconds + 0.1f));
    }
}
