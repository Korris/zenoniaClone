using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

public class RangedAimRulesTests
{
    // A 16x16 monster hit box whose bottom-left corner is at (x, y); the centre sits 6 units above the feet like Monster.Center.
    static AimCandidate Mon(float x, float y, ushort id, bool dying = false)
    {
        return new AimCandidate(x + 8f, y + 6f, x, y, x + 16f, y + 16f, id, dying);
    }

    // Hero at (100, 100) facing right: the bolt leaves the muzzle 12 ahead and 4 up, like FireBolt for every class but the Mechanic (RangedAimRules.BoltOrigin).
    const float Hx = 100f, Hy = 100f, Fx = 1f, Fy = 0f, Ox = 112f, Oy = 104f, Range = 120f;

    [Fact]
    public void NothingInRangeKeepsTheStraightShot()
    {
        var all = new List<AimCandidate> { Mon(300f, 100f, 1), Mon(100f, 300f, 2) };
        Assert.Equal(-1, RangedAimRules.Pick(Hx, Hy, Fx, Fy, Ox, Oy, Range, all));
        Assert.Equal(-1, RangedAimRules.Pick(Hx, Hy, Fx, Fy, Ox, Oy, Range, new List<AimCandidate>()));
    }

    [Fact]
    public void DiagonalMonsterInRangeIsPicked()
    {
        var all = new List<AimCandidate> { Mon(160f, 150f, 7) }; // ahead and up: off the facing axis
        Assert.Equal(0, RangedAimRules.Pick(Hx, Hy, Fx, Fy, Ox, Oy, Range, all));
        float dx, dy, a;
        Assert.True(RangedAimRules.Direction(Ox, Oy, all[0].Cx, all[0].Cy, out dx, out dy, out a));
        Assert.InRange(dx * dx + dy * dy, 0.999f, 1.001f);
        Assert.True(dx > 0f && dy > 0f);
        Assert.InRange(a, 30f, 60f);
    }

    [Fact]
    public void MonstersBehindTheHeroAreIgnoredEvenWhenClose()
    {
        var all = new List<AimCandidate> { Mon(60f, 100f, 1), Mon(70f, 150f, 2) }; // both have their centre behind a right-facing hero
        Assert.Equal(-1, RangedAimRules.Pick(Hx, Hy, Fx, Fy, Ox, Oy, Range, all));
        var beside = new List<AimCandidate> { Mon(92f, 140f, 3) }; // centre x = 100: on the half-plane edge, still a target
        Assert.Equal(0, RangedAimRules.Pick(Hx, Hy, Fx, Fy, Ox, Oy, Range, beside));
    }

    [Fact]
    public void ClosestLiveMonsterWinsAndDyingOnesAreSkipped()
    {
        var all = new List<AimCandidate>
        {
            Mon(200f, 100f, 1),        // 88 from the muzzle
            Mon(140f, 120f, 2, true),  // closer but dying
            Mon(150f, 60f, 3),         // 47 from the muzzle: the pick
            Mon(230f, 100f, 4),        // in range but farther
        };
        Assert.Equal(2, RangedAimRules.Pick(Hx, Hy, Fx, Fy, Ox, Oy, Range, all));
    }

    [Fact]
    public void RangeIsMeasuredFromTheMuzzleToTheHitBox()
    {
        var all = new List<AimCandidate> { Mon(Ox + Range - 0.5f, 96f, 1) }; // box edge just inside the range
        Assert.Equal(0, RangedAimRules.Pick(Hx, Hy, Fx, Fy, Ox, Oy, Range, all));
        all[0] = Mon(Ox + Range + 0.5f, 96f, 1);
        Assert.Equal(-1, RangedAimRules.Pick(Hx, Hy, Fx, Fy, Ox, Oy, Range, all));
    }

    [Fact]
    public void EqualDistanceTiesGoToTheLowerIdWhateverTheListOrder()
    {
        var up = Mon(150f, 130f, 9);
        var down = Mon(150f, 62f, 4); // mirror image below the muzzle line: same distance
        Assert.Equal(1, RangedAimRules.Pick(Hx, Hy, Fx, Fy, Ox, Oy, Range, new List<AimCandidate> { up, down }));
        Assert.Equal(0, RangedAimRules.Pick(Hx, Hy, Fx, Fy, Ox, Oy, Range, new List<AimCandidate> { down, up }));
    }

    [Fact]
    public void OtherFacingsUseTheirOwnHalfPlane()
    {
        var all = new List<AimCandidate> { Mon(92f, 140f, 1), Mon(92f, 40f, 2) };
        Assert.Equal(0, RangedAimRules.Pick(Hx, Hy, 0f, 1f, 100f, 116f, Range, all));  // facing up: the upper one
        Assert.Equal(1, RangedAimRules.Pick(Hx, Hy, 0f, -1f, 100f, 92f, Range, all));  // facing down: the lower one
    }

    [Fact]
    public void DirectionOnTheMuzzleKeepsTheStraightShot()
    {
        float dx, dy, a;
        Assert.False(RangedAimRules.Direction(Ox, Oy, Ox, Oy, out dx, out dy, out a));
        Assert.True(RangedAimRules.Direction(0f, 0f, -5f, 0f, out dx, out dy, out a));
        Assert.Equal(-1f, dx, 3); Assert.Equal(0f, dy, 3); Assert.Equal(180f, a, 3);
    }
}
