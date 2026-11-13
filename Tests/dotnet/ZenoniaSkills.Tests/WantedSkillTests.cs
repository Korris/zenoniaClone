using Xunit;
using ZenoniaProto.Rpg;

public class WantedSkillTests
{
    const float U = WantedSkillArea.TileUnits; // 16

    [Fact]
    public void EveryBossHasFourSkillsEndingWithOneEnrageSkill()
    {
        foreach (WantedSkillDef[] table in new[] { WantedSkillTable.RimeclawTyrant, WantedSkillTable.GlacialColossus, WantedSkillTable.MireHydraQueen })
        {
            Assert.Equal(4, table.Length);
            for (int i = 0; i < table.Length; i++) Assert.Equal(i, table[i].Id);
            Assert.True(table[3].Enrage);
            for (int i = 0; i < 3; i++) Assert.False(table[i].Enrage);
        }
    }

    [Fact]
    public void RimeclawMatchesTheDesignDoc()
    {
        WantedSkillDef sweep = WantedSkillTable.RimeclawTyrant[0], slam = WantedSkillTable.RimeclawTyrant[1];
        WantedSkillDef stomp = WantedSkillTable.RimeclawTyrant[2], blizzard = WantedSkillTable.RimeclawTyrant[3];
        Assert.Equal(WantedAreaShape.Cone, sweep.Shape); Assert.Equal(120f, sweep.AngleDeg); Assert.Equal(4f, sweep.RadiusTiles);
        Assert.Equal(1200, sweep.TelegraphMs); Assert.Equal(9000, sweep.CooldownMs); Assert.Equal(1.5f, sweep.DamageMult); Assert.Equal(1f, sweep.KnockbackTiles);
        Assert.True(slam.OnPlayers); Assert.Equal(2f, slam.RadiusTiles); Assert.Equal(1500, slam.TelegraphMs); Assert.Equal(13000, slam.CooldownMs);
        Assert.Equal(StatusKind.Slow, slam.StatusKind); Assert.Equal(30f, slam.StatusValue); Assert.Equal(2000, slam.StatusMs);
        Assert.Equal(4f, stomp.RadiusTiles); Assert.Equal(1600, stomp.TelegraphMs); Assert.Equal(17000, stomp.CooldownMs); Assert.Equal(1.8f, stomp.DamageMult);
        Assert.Equal(StatusKind.Freeze, stomp.StatusKind); Assert.Equal(3000, stomp.StatusMs);
        Assert.Equal(6f, blizzard.RadiusTiles); Assert.Equal(90f, blizzard.SafeArcDeg); Assert.Equal(1, blizzard.SafeArcCount);
        Assert.Equal(2200, blizzard.TelegraphMs); Assert.Equal(28000, blizzard.CooldownMs); Assert.Equal(2.2f, blizzard.DamageMult);
    }

    [Fact]
    public void CircleHitsInsideAndMissesOutsideTheEdgeMargin()
    {
        var skill = new WantedSkillDef { Shape = WantedAreaShape.Circle, RadiusTiles = 4f }; // radius 64 units, margin 4 units -> danger radius 60
        var shape = new WantedShapeInstance { OriginX = 100f, OriginY = 100f };
        Assert.True(WantedSkillArea.Hits(skill, shape, 100f + 59f, 100f));   // just inside the shrunk radius
        Assert.False(WantedSkillArea.Hits(skill, shape, 100f + 61f, 100f)); // in the margin band: forgiven
        Assert.False(WantedSkillArea.Hits(skill, shape, 100f + 90f, 100f)); // well outside
    }

    [Fact]
    public void ConeOnlyHitsWithinItsFacingAndAngle()
    {
        var skill = new WantedSkillDef { Shape = WantedAreaShape.Cone, RadiusTiles = 4f, AngleDeg = 120f };
        var facingRight = new WantedShapeInstance { OriginX = 0f, OriginY = 0f, DirDeg = 0f };
        Assert.True(WantedSkillArea.Hits(skill, facingRight, 30f, 0f));    // straight ahead
        Assert.True(WantedSkillArea.Hits(skill, facingRight, 20f, 20f));  // inside the 120 deg wedge
        Assert.False(WantedSkillArea.Hits(skill, facingRight, -30f, 0f)); // behind the boss
        Assert.False(WantedSkillArea.Hits(skill, facingRight, 0f, 40f));  // outside the radius
    }

    [Fact]
    public void LineOnlyHitsTheRectangleAheadOfTheOrigin()
    {
        var skill = new WantedSkillDef { Shape = WantedAreaShape.Line, LengthTiles = 9f, WidthTiles = 2f };
        var shape = new WantedShapeInstance { OriginX = 0f, OriginY = 0f, DirDeg = 0f }; // facing +X
        Assert.True(WantedSkillArea.Hits(skill, shape, 50f, 0f));   // on the centre line, within length
        Assert.False(WantedSkillArea.Hits(skill, shape, -10f, 0f)); // behind the origin
        Assert.False(WantedSkillArea.Hits(skill, shape, 50f, 20f)); // too far off to the side
    }

    [Fact]
    public void SafeLaneExcludesItsArcButNotTheRestOfTheCircle()
    {
        var skill = new WantedSkillDef { Shape = WantedAreaShape.Circle, RadiusTiles = 6f, SafeArcDeg = 90f, SafeArcCount = 1 };
        var shape = new WantedShapeInstance { OriginX = 0f, OriginY = 0f, DirDeg = 0f }; // safe lane centred on +X
        Assert.False(WantedSkillArea.Hits(skill, shape, 50f, 0f));  // dead centre of the safe lane
        Assert.True(WantedSkillArea.Hits(skill, shape, 0f, 50f));   // 90 deg away: still dangerous
        Assert.True(WantedSkillArea.Hits(skill, shape, -50f, 0f));  // opposite side: dangerous
    }

    [Fact]
    public void TwoOppositeSafeLanesBothExcludeTheirArcs()
    {
        var skill = new WantedSkillDef { Shape = WantedAreaShape.Circle, RadiusTiles = 7f, SafeArcDeg = 60f, SafeArcCount = 2 };
        var shape = new WantedShapeInstance { OriginX = 0f, OriginY = 0f, DirDeg = 90f }; // lanes at 90 and 270 degrees
        Assert.False(WantedSkillArea.Hits(skill, shape, 0f, 50f));  // +Y lane
        Assert.False(WantedSkillArea.Hits(skill, shape, 0f, -50f)); // -Y lane (opposite)
        Assert.True(WantedSkillArea.Hits(skill, shape, 50f, 0f));   // +X: dangerous
    }

    [Fact]
    public void ClockBlocksPicksDuringTheSharedGapAndAfterUse()
    {
        var clock = new WantedSkillClock();
        Assert.Equal(0, WantedSkillPicker.Pick(WantedSkillTable.RimeclawTyrant, clock, 1f, 0f));
        clock.OnSkillUsed(WantedSkillTable.RimeclawTyrant[0]);
        Assert.Equal(-1, WantedSkillPicker.Pick(WantedSkillTable.RimeclawTyrant, clock, 1f, 0f)); // shared gap still running
        clock.Tick(WantedSkillClock.SkillGapMs);
        int again = WantedSkillPicker.Pick(WantedSkillTable.RimeclawTyrant, clock, 1f, 0f);
        Assert.NotEqual(0, again); // skill 0's own cooldown (9 s) is still running
        Assert.True(again >= 1 && again <= 2); // only the two other plain skills are ready (enrage excluded above 30 % hp)
    }

    [Fact]
    public void EnrageSkillOnlyJoinsThePoolBelowThirtyPercentHp()
    {
        var clock = new WantedSkillClock();
        Assert.NotEqual(3, WantedSkillPicker.Pick(WantedSkillTable.RimeclawTyrant, clock, 0.31f, 0.999f));
        Assert.Equal(3, WantedSkillPicker.Pick(WantedSkillTable.RimeclawTyrant, clock, 0.29f, 0.999f)); // roll near 1 -> the last ready skill
    }

    [Fact]
    public void RollSelectsAmongReadySkillsDeterministically()
    {
        var clock = new WantedSkillClock();
        Assert.Equal(0, WantedSkillPicker.Pick(WantedSkillTable.RimeclawTyrant, clock, 1f, 0f));
        Assert.Equal(1, WantedSkillPicker.Pick(WantedSkillTable.RimeclawTyrant, clock, 1f, 0.34f));
        Assert.Equal(2, WantedSkillPicker.Pick(WantedSkillTable.RimeclawTyrant, clock, 1f, 0.99f));
    }
}
