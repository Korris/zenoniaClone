using System;
using Xunit;
using ZenoniaProto.Rpg;

public class MonsterRangedRulesTests
{
    static readonly MonsterRangedProfile P = new MonsterRangedProfile { Range = 100f, ProjectileSpeed = 130f, MaxTravel = 150f, HitSize = 6f };
    static readonly Func<float, float, bool> Open = (x, y) => false;
    /// <summary>A wall filling the tile column x 48..64.</summary>
    static readonly Func<float, float, bool> Wall = (x, y) => x >= 48f && x < 64f;

    [Fact]
    public void CliffHarpyProfileIsADodgeableSixTileShot()
    {
        MonsterRangedProfile f = MonsterRangedProfile.CliffHarpyFeather;
        Assert.InRange(f.Range, 90f, 110f);
        Assert.True(f.MaxTravel > f.Range);            // walking straight back does not outrun it
        Assert.InRange(f.ProjectileSpeed, 100f, 180f); // slow enough to sidestep at 6 tiles, faster than a walking hero
    }

    [Fact]
    public void ShootsOnlyWithinRange()
    {
        Assert.True(MonsterRangedRules.ShouldShoot(P, 0f, 0f, 100f, 0f, Open));
        Assert.True(MonsterRangedRules.ShouldShoot(P, 0f, 0f, 5f, 0f, Open));   // point blank: still shoots (no kiting AI)
        Assert.False(MonsterRangedRules.ShouldShoot(P, 0f, 0f, 101f, 0f, Open));
        Assert.False(MonsterRangedRules.ShouldShoot(P, 0f, 0f, 80f, 80f, Open)); // 113 units on the diagonal
    }

    [Fact]
    public void AWallInBetweenKeepsItWalkingInsteadOfShooting()
    {
        Assert.False(MonsterRangedRules.ShouldShoot(P, 0f, 0f, 90f, 0f, Wall));
        Assert.True(MonsterRangedRules.ShouldShoot(P, 0f, 0f, 40f, 0f, Wall));   // target on the near side of the wall
        Assert.True(MonsterRangedRules.ClearLine(70f, 0f, 120f, 30f, Wall));    // both beyond it
    }

    [Fact]
    public void ProjectileFliesStraightAndExpiresAtMaxTravel()
    {
        float x = 0f, y = 10f, travelled = 0f;
        Assert.Equal(MonsterShotEnd.Flying, MonsterRangedRules.Advance(ref x, ref y, 1f, 0f, 13f, ref travelled, P.MaxTravel, Open, null));
        Assert.Equal(13f, x, 3); Assert.Equal(10f, y, 3); Assert.Equal(13f, travelled, 3);
        MonsterShotEnd end = MonsterShotEnd.Flying;
        for (int i = 0; i < 20 && end == MonsterShotEnd.Flying; i++) end = MonsterRangedRules.Advance(ref x, ref y, 1f, 0f, 13f, ref travelled, P.MaxTravel, Open, null);
        Assert.Equal(MonsterShotEnd.Expired, end);
        Assert.InRange(x, 150f, 150f + MonsterRangedRules.StepUnits);
    }

    [Fact]
    public void ProjectileIsBlockedByASolidTileEvenInOneBigStep()
    {
        float x = 0f, y = 0f, travelled = 0f;
        Assert.Equal(MonsterShotEnd.Wall, MonsterRangedRules.Advance(ref x, ref y, 1f, 0f, 120f, ref travelled, P.MaxTravel, Wall, null)); // a 120-unit tick must not tunnel through 16 units of wall
        Assert.InRange(x, 48f, 50f);
    }

    [Fact]
    public void ProjectileHitsAHeroItTouchesAndMissesOneWhoSidestepped()
    {
        // Hero body box 10 x 8, feet at (60, 0): bottom-left (55, 0). The shot flies along y = 4 (the middle of the box).
        Func<float, float, bool> heroInPath = (px, py) => MonsterRangedRules.Touches(px, py, P.HitSize, 55f, 0f, 10f, 8f);
        float x = 0f, y = 4f, travelled = 0f;
        Assert.Equal(MonsterShotEnd.Hit, MonsterRangedRules.Advance(ref x, ref y, 1f, 0f, 100f, ref travelled, P.MaxTravel, Open, heroInPath));
        Assert.InRange(x, 50f, 56f); // stops at the near edge of the box, not past the hero

        Func<float, float, bool> heroSidestepped = (px, py) => MonsterRangedRules.Touches(px, py, P.HitSize, 55f, 14f, 10f, 8f); // 14 units up: clear of the 6-unit shot
        x = 0f; y = 4f; travelled = 0f;
        Assert.Equal(MonsterShotEnd.Expired, MonsterRangedRules.Advance(ref x, ref y, 1f, 0f, 400f, ref travelled, P.MaxTravel, Open, heroSidestepped));
    }

    [Fact]
    public void AWallInFrontOfTheHeroTakesTheShot()
    {
        Func<float, float, bool> heroBehindWall = (px, py) => MonsterRangedRules.Touches(px, py, P.HitSize, 75f, 0f, 10f, 8f);
        float x = 0f, y = 4f, travelled = 0f;
        Assert.Equal(MonsterShotEnd.Wall, MonsterRangedRules.Advance(ref x, ref y, 1f, 0f, 140f, ref travelled, P.MaxTravel, Wall, heroBehindWall));
    }

    [Fact]
    public void TouchesIsAnOverlapOfTheShotSquareAndTheBox()
    {
        Assert.True(MonsterRangedRules.Touches(53f, 4f, 6f, 55f, 0f, 10f, 8f));   // right edge of the shot (56) inside the box
        Assert.False(MonsterRangedRules.Touches(51.9f, 4f, 6f, 55f, 0f, 10f, 8f));
        Assert.True(MonsterRangedRules.Touches(60f, 10.5f, 6f, 55f, 0f, 10f, 8f)); // grazes the top
        Assert.False(MonsterRangedRules.Touches(60f, 11.1f, 6f, 55f, 0f, 10f, 8f));
    }
}
