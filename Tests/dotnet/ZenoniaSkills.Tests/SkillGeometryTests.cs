using Xunit;
using ZenoniaProto.Rpg;

public class SkillGeometryTests
{
    // A 10x10 box whose bottom-left corner is at (x, y).
    static bool Cone(float ox, float oy, float dx, float dy, float r, float ang, float x, float y, float s = 10f)
    {
        return SkillGeometry.InSector(ox, oy, dx, dy, r, ang, x, y, x + s, y + s);
    }

    [Fact]
    public void CircleTouchesBoxByNearestPoint()
    {
        Assert.True(SkillGeometry.InCircle(0, 0, 5, 4, -2, 14, 2));   // nearest point (4,0) at distance 4
        Assert.False(SkillGeometry.InCircle(0, 0, 3.9f, 4, -2, 14, 2));
        Assert.True(SkillGeometry.InCircle(7, 0, 0.1f, 4, -2, 14, 2)); // centre inside the box
    }

    [Fact]
    public void ConeKeepsOnlyTheFacingSide()
    {
        Assert.True(Cone(0, 0, 1, 0, 40, 90, 10, -5));    // straight ahead
        Assert.False(Cone(0, 0, 1, 0, 40, 90, -30, -5));  // behind
        Assert.False(Cone(0, 0, 1, 0, 40, 90, -5, 20));   // beside, outside the 45 degree half angle
        Assert.False(Cone(0, 0, 1, 0, 40, 90, 60, -5));   // ahead but beyond the radius
    }

    [Fact]
    public void ConeEdgeClipsBoxThatStraddlesTheRay()
    {
        // 90 degree cone along +x: the upper edge is y = x. A box centred above the edge still overlaps the wedge.
        Assert.True(Cone(0, 0, 1, 0, 50, 90, 10, 8));
        Assert.False(Cone(0, 0, 1, 0, 50, 90, 0, 12));   // x in 0..10, y in 12..22: above y = x everywhere
    }

    [Fact]
    public void ConeRadiusUsesNearestPointOfTheClippedBox()
    {
        // Box corner (20,0) is the closest wedge point: distance 20.
        Assert.True(Cone(0, 0, 1, 0, 21, 60, 20, -5));
        Assert.False(Cone(0, 0, 1, 0, 19, 60, 20, -5));
    }

    [Fact]
    public void ApexInsideBoxAlwaysHits()
    {
        Assert.True(Cone(0, 0, 0, 1, 10, 30, -5, -5));
        Assert.True(SkillGeometry.InLine(0, 0, 1, 0, 5, 2, -1, -1, 1, 1));
    }

    [Fact]
    public void WideArcBeyondHalfPlaneCoversBehindTheSides()
    {
        Assert.True(Cone(0, 0, 1, 0, 40, 200, -10, 10));    // 100 degrees to each side reaches slightly behind the apex line
        Assert.False(Cone(0, 0, 1, 0, 40, 200, -40, -5));   // directly behind
        Assert.True(Cone(0, 0, 1, 0, 40, 360, -30, -5));    // full circle
    }

    [Fact]
    public void LineIsAnOrientedRectangle()
    {
        Assert.True(SkillGeometry.InLine(0, 0, 1, 0, 100, 20, 50, -3, 60, 3));
        Assert.False(SkillGeometry.InLine(0, 0, 1, 0, 100, 20, 50, 12, 60, 20));  // outside half width 10
        Assert.False(SkillGeometry.InLine(0, 0, 1, 0, 100, 20, 101, -3, 110, 3)); // past the end
        Assert.False(SkillGeometry.InLine(0, 0, 1, 0, 100, 20, -20, -3, -5, 3));  // behind the start
        // Diagonal line: a box on the axis hits, one far off the axis misses.
        float d = 0.70710678f;
        Assert.True(SkillGeometry.InLine(0, 0, d, d, 100, 10, 40, 40, 44, 44));
        Assert.False(SkillGeometry.InLine(0, 0, d, d, 100, 10, 60, 20, 64, 24));
    }

    [Fact]
    public void HitsDispatchesByShape()
    {
        Assert.True(SkillGeometry.Hits(AreaShape.Single, 0, 0, 1, 0, 5, 0, 0, 0, 3, -1, 6, 1));
        Assert.True(SkillGeometry.Hits(AreaShape.Circle, 0, 0, 1, 0, 5, 0, 0, 0, 3, -1, 6, 1));
        Assert.False(SkillGeometry.Hits(AreaShape.Arc, 0, 0, -1, 0, 20, 90, 0, 0, 3, -1, 6, 1));
        Assert.True(SkillGeometry.Hits(AreaShape.Cone, 0, 0, 1, 0, 20, 90, 0, 0, 3, -1, 6, 1));
        Assert.True(SkillGeometry.Hits(AreaShape.Line, 0, 0, 1, 0, 0, 0, 10, 4, 3, -1, 6, 1));
    }

    [Theory]
    [InlineData(0, 0f, 1f, true)]     // victim faces down, attacker above (behind)
    [InlineData(0, 0f, -1f, false)]   // attacker in front
    [InlineData(3, -1f, 0f, true)]    // faces right, attacker on the left
    [InlineData(3, 1f, 0f, false)]
    [InlineData(2, 1f, 0.1f, true)]   // faces left, attacker right and slightly up
    [InlineData(1, 1f, 0f, false)]    // flank: not behind
    public void BehindIsTheRearCone(int facing, float ax, float ay, bool expected)
    {
        Assert.Equal(expected, SkillGeometry.IsBehind(facing, ax, ay));
    }
}
