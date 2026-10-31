using Xunit;
using ZenoniaProto.Rpg;

public class ClericBuffTests
{
    [Fact]
    public void DivineAegisReplacesHolyRayInSlotTwoAndReachesAlliesInRange()
    {
        SkillDef d = SkillTable.Get("cleric", 2);
        Assert.Equal("Divine Aegis", d.Name);
        Assert.Equal("cleric.divine-aegis", d.Id);
        Assert.Equal(15, d.UnlockLevel);
        SkillEffect e = d.Effects[0];
        Assert.Equal(EffectKind.Buff, e.Kind);
        Assert.Equal(BuffKind.DefPct, e.Buff.Kind);
        Assert.True(e.Ally);
        Assert.Equal(4f, e.Radius);
        Assert.DoesNotContain(SkillTable.ForClass("cleric"), s => s.Name == "Holy Ray");
    }

    [Fact]
    public void SpiritBlessingStillBuffsAttackForAlliesInRange()
    {
        SkillEffect e = SkillTable.Get("cleric", 1).Effects[0];
        Assert.Equal(BuffKind.AtkPct, e.Buff.Kind);
        Assert.True(e.Ally);
        Assert.Equal(4f, e.Radius);
    }

    [Fact]
    public void DefenseBuffRaisesDefenseAndEndsWithItsTime()
    {
        var s = new BuffState();
        Assert.Equal(1f, s.DefMul);
        s.Add(BuffKind.DefPct, 15f, 12000);
        Assert.Equal(1.15f, s.DefMul, 3);
        s.Tick(12001f);
        Assert.Equal(1f, s.DefMul);
    }

    [Fact]
    public void RecastingADefenseBuffRefreshesInsteadOfStacking()
    {
        var s = new BuffState();
        s.Add(BuffKind.DefPct, 15f, 12000);
        s.Tick(6000f);
        s.Add(BuffKind.DefPct, 15f, 12000);
        Assert.Equal(1.15f, s.DefMul, 3);
        Assert.Equal(12000f, s.LeftMs(BuffKind.DefPct));
    }

    [Fact]
    public void AttackAndDefenseBuffsLayer()
    {
        var s = new BuffState();
        s.Add(BuffKind.AtkPct, 15f, 12000);
        s.Add(BuffKind.DefPct, 15f, 12000);
        Assert.Equal(1.15f, s.AtkMul, 3);
        Assert.Equal(1.15f, s.DefMul, 3);
    }
}
