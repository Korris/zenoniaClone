using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Facing hysteresis of monsters (the Mire Hydra Queen "keeps spinning" report, 2026-11-19).</summary>
public class MonsterFacingRuleTests
{
    const int Down = 0, Up = 1, Left = 2, Right = 3;

    [Fact]
    public void PlainRuleIsTheOldAxisDominance()
    {
        Assert.Equal(Right, MonsterFacingRule.Plain(5f, 1f));
        Assert.Equal(Left, MonsterFacingRule.Plain(-5f, 1f));
        Assert.Equal(Up, MonsterFacingRule.Plain(1f, 5f));
        Assert.Equal(Down, MonsterFacingRule.Plain(1f, -5f));
    }

    [Fact]
    public void JitterAroundTheDiagonalKeepsTheCurrentFacing()
    {
        // A target wobbling across the 45 degree line between right and up: the old rule flipped Right/Up on every wobble.
        int facing = Right;
        int flips = 0;
        for (int i = 0; i < 100; i++)
        {
            float dy = 40f + (i % 2 == 0 ? 3f : -3f);
            int next = MonsterFacingRule.Sticky(facing, 40f, dy);
            if (next != facing) flips++;
            facing = next;
        }
        Assert.Equal(0, flips);
        Assert.Equal(Right, facing);
    }

    [Fact]
    public void TinyVectorsKeepTheFacing()
    {
        Assert.Equal(Left, MonsterFacingRule.Sticky(Left, 2f, 1f));   // a hero standing on the boss's feet
        Assert.Equal(Left, MonsterFacingRule.Sticky(Left, 0f, -3.9f));
        Assert.Equal(Down, MonsterFacingRule.Sticky(Left, 0f, -40f)); // a real direction still turns it
    }

    [Fact]
    public void TurnsOnlyPastTheMargin()
    {
        float m = MonsterFacingRule.SwitchMarginDeg;
        // Current Right: a direction (45 + m - 3) degrees up stays Right, (45 + m + 3) turns Up.
        Assert.Equal(Right, MonsterFacingRule.Sticky(Right, Dir(45f + m - 3f).x, Dir(45f + m - 3f).y));
        Assert.Equal(Up, MonsterFacingRule.Sticky(Right, Dir(45f + m + 3f).x, Dir(45f + m + 3f).y));
        Assert.Equal(Left, MonsterFacingRule.Sticky(Right, -50f, 0f));   // straight behind: turns at once
        Assert.Equal(Down, MonsterFacingRule.Sticky(Up, 0f, -50f));
    }

    static (float x, float y) Dir(float deg)
    {
        double r = deg * System.Math.PI / 180.0;
        return ((float)System.Math.Cos(r) * 50f, (float)System.Math.Sin(r) * 50f);
    }
}
