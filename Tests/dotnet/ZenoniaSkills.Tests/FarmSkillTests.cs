using Xunit;
using ZenoniaProto.Rpg;

public class FarmSkillTests
{
    [Fact]
    public void WarriorFarmSkillRecoversBeforeAreaAndDefensiveSkills()
    {
        Assert.Equal(3000, SkillTable.Get("warrior", 0).CooldownMs);
        int[] expected = { 3000, 8000, 12000, 10000, 12000, 12000, 22000, 50000 };
        for (int i = 0; i < expected.Length; i++) Assert.Equal(expected[i], SkillTable.Get("warrior", i).CooldownMs);
        Assert.True(FarmSkillRules.IsRepeatable("warrior", 0));
        for (int i = 1; i < SkillDef.PerClass; i++) Assert.False(FarmSkillRules.IsRepeatable("warrior", i));
        Assert.False(FarmSkillRules.IsRepeatable("cleric", 0)); // healing is not a repeatable offensive farm skill
    }

    [Fact]
    public void FarmCooldownMustExpireAndDoesNotResetOtherSkills()
    {
        var cds = new SkillCooldowns();
        cds.Start(0, SkillTable.Get("warrior", 0).CooldownMs);
        cds.Start(1, SkillTable.Get("warrior", 1).CooldownMs);
        cds.Tick(2999);
        Assert.False(cds.Ready(0));
        cds.Tick(1);
        Assert.True(cds.Ready(0));
        Assert.False(cds.Ready(1));
        Assert.Equal(5000f, cds.LeftMs(1));
    }

    [Fact]
    public void PrototypePreservesOtherClassCooldowns()
    {
        foreach (var def in SkillTable.All)
            if (def.Class != "warrior") Assert.Equal((int)(def.BaseCooldownMs * SkillTable.CooldownScale), def.CooldownMs);
    }
}
