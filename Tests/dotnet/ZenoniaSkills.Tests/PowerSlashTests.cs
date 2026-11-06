using Xunit;
using ZenoniaProto.Rpg;

public class PowerSlashTests
{
    static bool Hits(float x, float y)
    {
        SkillEffect e = SkillTable.Get("warrior", 0).Effects[0];
        return SkillGeometry.Hits(e.Shape, 0, 0, 1, 0, e.Radius, e.AngleDeg, 0, 0, x - 0.02f, y - 0.02f, x + 0.02f, y + 0.02f);
    }

    [Fact]
    public void WideSweepCatchesFlanksButNotTargetsBehindOrBeyondReach()
    {
        Assert.True(Hits(0.65f, 2.5f));
        Assert.True(Hits(0.65f, -2.5f));
        Assert.True(Hits(2.9f, 0));
        Assert.False(Hits(-1f, 0));
        Assert.False(Hits(3.2f, 0));
    }

    [Fact]
    public void GatherIsShortAndDoesNotIntroduceExtraDamageTicks()
    {
        var def = SkillTable.Get("warrior", 0);
        Assert.Equal(3000, def.CooldownMs);
        Assert.Single(def.Effects);
        Assert.Equal(1, def.Effects[0].Hits);
        Assert.Equal(170f, def.Effects[0].Pct);
        Assert.Equal(0.75f, def.Effects[0].PullTiles);
        Assert.Contains("GATHER", SkillDescribe.StatusLines(def));
        Assert.Contains("immune to pull", SkillDescribe.StatusLines(def));
        Assert.Equal(0f, SkillTable.Get("warrior", 1).Effects[0].PullTiles);
    }
}
