using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Cleric Heal (LowestAlly): the lowest hurt ally is the target, the caster is always healed too, and the skill never refuses (user 2026-11-20).</summary>
public class LowestAllyHealRuleTests
{
    [Fact]
    public void NobodyHurtTargetsTheCasterInsteadOfRefusing()
    {
        Assert.Equal(1, LowestAllyHealRule.PickTarget(new List<float> { 1f, 1f, 1f }, 1));
        Assert.Equal(0, LowestAllyHealRule.PickTarget(new List<float> { 1f }, 0));
        Assert.Equal(0, LowestAllyHealRule.PickTarget(new List<float>(), 0)); // empty pool: still the caster
        Assert.False(LowestAllyHealRule.AlsoHealsCaster(1, 1));
    }

    [Fact]
    public void LowestHurtAllyIsTheTargetAndTheCasterIsHealedToo()
    {
        int target = LowestAllyHealRule.PickTarget(new List<float> { 0.9f, 0.4f, 0.7f }, 0);
        Assert.Equal(1, target);
        Assert.True(LowestAllyHealRule.AlsoHealsCaster(target, 0));
    }

    [Fact]
    public void HurtCasterLowestIsHealedOnce()
    {
        int target = LowestAllyHealRule.PickTarget(new List<float> { 0.3f, 0.8f, 1f }, 0);
        Assert.Equal(0, target);
        Assert.False(LowestAllyHealRule.AlsoHealsCaster(target, 0));
    }

    [Fact]
    public void FullHpAlliesAreNeverPickedOverTheCaster()
    {
        Assert.Equal(2, LowestAllyHealRule.PickTarget(new List<float> { 0.9995f, 1f, 1f }, 2)); // 99.95 % counts as full
        Assert.Equal(0, LowestAllyHealRule.PickTarget(new List<float> { 0.5f, 0.5f }, 1));      // tie: the first one
    }

    [Fact]
    public void HealSkillTextSaysItHealsTheCasterToo()
    {
        SkillDef heal = SkillTable.Get("cleric", 0);
        Assert.Equal(SkillTargeting.LowestAlly, heal.Targeting);
        Assert.StartsWith("Heal yourself and the lowest-HP ally within", SkillDescribe.Description(heal, 1));
    }
}
