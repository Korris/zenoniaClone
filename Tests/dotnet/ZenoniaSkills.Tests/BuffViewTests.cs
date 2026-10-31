using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>What the HUD's buff row reads: which skill a buff came from and how long it has left, whatever the buff's kind.</summary>
public class BuffViewTests
{
    [Fact]
    public void SourcePacksEverySkillOfEveryClassAndUnpacksToTheSameSkill()
    {
        foreach (string cls in RpgClasses.Ids)
            foreach (SkillDef d in SkillTable.ForClass(cls))
            {
                byte source = BuffSource.Pack(cls, d.Index);
                Assert.NotEqual(BuffSource.None, source);
                Assert.Same(d, BuffSource.Def(source));
            }
    }

    [Fact]
    public void UnknownSourcesHaveNoSkill()
    {
        Assert.Equal(BuffSource.None, BuffSource.Pack("nobody", 0));
        Assert.Equal(BuffSource.None, BuffSource.Pack("cleric", SkillDef.PerClass));
        Assert.Null(BuffSource.Def(BuffSource.None));
        Assert.Null(BuffSource.Def(200));
    }

    [Fact]
    public void ViewsListEachBuffWithItsSourceAndTimeUntilItEnds()
    {
        var b = new BuffState();
        var views = new List<BuffView>();
        byte source = BuffSource.Pack("warrior", 7);
        b.Add(BuffKind.AtkPct, 25f, 12000, 0, source);
        b.Add(BuffKind.AtkSpeedPct, 15f, 12000, 0, source);
        b.Tick(2000f);
        b.CopyViews(views);
        Assert.Equal(2, views.Count);
        Assert.Equal(source, views[0].Source);
        Assert.Equal(10000f, views[0].LeftMs);
        Assert.Equal(12000f, views[0].TotalMs);
        b.Tick(10000f);
        b.CopyViews(views);
        Assert.Empty(views);
    }

    [Fact]
    public void ARefreshRestartsTheFullTime()
    {
        var b = new BuffState();
        var views = new List<BuffView>();
        b.Add(BuffKind.AtkPct, 15f, 12000, 0, BuffSource.Pack("cleric", 1));
        b.Tick(11000f);
        b.Add(BuffKind.AtkPct, 15f, 12000, 0, BuffSource.Pack("cleric", 1));
        b.CopyViews(views);
        Assert.Single(views);
        Assert.Equal(12000f, views[0].LeftMs);
        Assert.Equal(12000f, views[0].TotalMs);
    }
}
