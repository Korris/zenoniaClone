using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Ground-targeted skills (ChatGPT "Smart Ground Targeting", 26/11/2026): which skills, their cast ranges, the clamp and the auto pick.</summary>
public class GroundTargetRulesTests
{
    const float U = GroundTargetRules.TileUnits;

    [Theory]
    [InlineData("warrior", "Earth Break", 3f)]
    [InlineData("warrior", "Inferno Crash", 4f)]
    [InlineData("wizard", "Ice Storm", 5.5f)]
    [InlineData("wizard", "Elemental Burst", 5.5f)]
    [InlineData("wizard", "Astral Cataclysm", 6.5f)]
    [InlineData("mechanic", "Deploy Turret", 4f)]
    [InlineData("mechanic", "Orbital Cannon", 7f)]
    [InlineData("cleric", "Divine Light", 5f)]
    [InlineData("cleric", "Sanctuary", 5f)]
    [InlineData("archer", "Arrow Rain", 7f)]
    [InlineData("archer", "Starfall Volley", 7.5f)]
    [InlineData("fighter", "Dragon Fist", 4f)]
    public void TheTwelveChosenSkillsAreGroundTargeted(string cls, string name, float range)
    {
        SkillDef d = SkillTable.ForClass(cls).Single(s => s.Name == name);
        Assert.True(GroundTargetRules.IsGround(d));
        Assert.Equal(range, d.CastRange);
        Assert.True(GroundTargetRules.AreaRadiusTiles(d) > 0f);
    }

    [Fact]
    public void OnlyThoseTwelveAreGround()
    {
        Assert.Equal(12, SkillTable.All.Count(GroundTargetRules.IsGround));
        foreach (SkillDef d in SkillTable.All) if (!GroundTargetRules.IsGround(d)) Assert.Equal(0f, d.CastRange);
    }

    [Fact]
    public void LeapsNeverReachPastTheirJump()
    {
        foreach (SkillDef d in SkillTable.All.Where(GroundTargetRules.IsGround))
        {
            if (GroundTargetRules.IndicatorOf(d) != GroundIndicator.Leap) continue;
            float jump = d.Effects.First(e => e.Kind == EffectKind.Motion).MoveTiles;
            Assert.True(d.CastRange <= jump, d.Name);
        }
    }

    [Fact]
    public void IndicatorKinds()
    {
        Assert.Equal(GroundIndicator.Placement, GroundTargetRules.IndicatorOf(SkillTable.ForClass("mechanic").Single(s => s.Name == "Deploy Turret")));
        Assert.Equal(GroundIndicator.Leap, GroundTargetRules.IndicatorOf(SkillTable.ForClass("fighter").Single(s => s.Name == "Dragon Fist")));
        Assert.Equal(GroundIndicator.Area, GroundTargetRules.IndicatorOf(SkillTable.ForClass("archer").Single(s => s.Name == "Arrow Rain")));
        SkillDef rain = SkillTable.ForClass("archer").Single(s => s.Name == "Arrow Rain");
        Assert.Equal(2.5f, GroundTargetRules.AreaRadiusTiles(rain));
        Assert.Equal(2.5f + 1.2f, GroundTargetRules.OuterRadiusTiles(rain), 3);
        Assert.Equal(LegacySkillNumbers.IceStormRadius / U, GroundTargetRules.AreaRadiusTiles(SkillTable.ForClass("wizard").Single(s => s.Name == "Ice Storm")));
    }

    [Fact]
    public void ClampPullsAFarPointOntoTheRangeCircle()
    {
        float x, y;
        GroundTargetRules.ClampToRange(100f, 100f, 100f + 300f, 100f, 7f * U, out x, out y);
        Assert.Equal(100f + 7f * U, x, 3); Assert.Equal(100f, y, 3);
        GroundTargetRules.ClampToRange(0f, 0f, 30f, 40f, 112f, out x, out y); // inside: unchanged
        Assert.Equal(30f, x); Assert.Equal(40f, y);
        GroundTargetRules.ClampToRange(0f, 0f, 300f, 400f, 100f, out x, out y);
        Assert.Equal(60f, x, 3); Assert.Equal(80f, y, 3);
        GroundTargetRules.ClampToRange(5f, 5f, float.NaN, 0f, 100f, out x, out y);
        Assert.Equal(5f, x); Assert.Equal(5f, y);
    }

    [Fact]
    public void RangeCheckHasAnOptionalSlack()
    {
        Assert.True(GroundTargetRules.InRange(0f, 0f, 112f, 0f, 112f));
        Assert.False(GroundTargetRules.InRange(0f, 0f, 115f, 0f, 112f));
        Assert.True(GroundTargetRules.InRange(0f, 0f, 115f, 0f, 112f, 4f));
    }

    [Fact]
    public void AutoPickCoversTheDensestGroupAndPrefersTheTarget()
    {
        // a pack of 3 to the right, a lone monster to the left (the preferred target only wins a tie)
        float[] xs = { 60f, 66f, 62f, -60f }, ys = { 0f, 6f, -5f, 0f };
        float bx, by;
        Assert.True(GroundTargetRules.BestCentre(0f, 0f, 7f * U, 2.5f * U, xs, ys, 4, -1, out bx, out by));
        Assert.True(bx > 50f);
        Assert.True(GroundTargetRules.BestCentre(0f, 0f, 7f * U, 2.5f * U, xs, ys, 4, 3, out bx, out by));
        Assert.True(bx > 50f); // 3 monsters beat one target counted double
        float[] two = { 60f, -60f }, zero = { 0f, 0f };
        Assert.True(GroundTargetRules.BestCentre(0f, 0f, 7f * U, 2.5f * U, two, zero, 2, 1, out bx, out by));
        Assert.Equal(-60f, bx); // tie: the target wins
    }

    [Fact]
    public void AutoPickClampsAFarGroupIntoRangeAndFailsWithNothingInReach()
    {
        float[] xs = { 150f }, ys = { 0f };
        float bx, by;
        Assert.True(GroundTargetRules.BestCentre(0f, 0f, 112f, 40f, xs, ys, 1, -1, out bx, out by));
        Assert.Equal(112f, bx, 3);
        float[] far = { 400f };
        Assert.False(GroundTargetRules.BestCentre(0f, 0f, 112f, 40f, far, ys, 1, -1, out bx, out by));
    }

    [Fact]
    public void DescriptionSaysWhereItCanBeAimed()
    {
        SkillDef d = SkillTable.ForClass("archer").Single(s => s.Name == "Arrow Rain");
        Assert.Contains("within 7 tiles", SkillDescribe.Description(d, 1));
        Assert.Contains("on a spot within 5.5 tiles", SkillDescribe.Description(SkillTable.ForClass("wizard").Single(s => s.Name == "Ice Storm"), 1));
    }
}
