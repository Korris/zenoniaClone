using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

public class MonsterStatusRulesTests
{
    static StatusSpec S(StatusKind k, float v, int ms, int charges = 0) { return new StatusSpec(k, v, ms, charges); }

    [Fact]
    public void BossTurnsFreezeAndStunIntoHalfSlow()
    {
        var b = new MonsterStatus();
        Assert.True(b.Apply(S(StatusKind.Freeze, 0, 2000), null, true, 10));
        Assert.False(b.Disabled);
        Assert.Equal(0.5f, b.SpeedFactor, 3);
        Assert.Equal(StatusKind.Slow, b.Dominant);
        var b2 = new MonsterStatus();
        b2.Apply(S(StatusKind.Stun, 0, 1500), null, true, 10);
        Assert.False(b2.Disabled);
        Assert.Equal(0.5f, b2.SpeedFactor, 3);
    }

    [Fact]
    public void BossHalvesSlowAndChillButTakesDotAndMark()
    {
        var b = new MonsterStatus();
        b.Apply(S(StatusKind.Slow, 40, 1000), null, true, 10);
        Assert.Equal(0.8f, b.SpeedFactor, 3);
        Assert.True(b.Apply(S(StatusKind.Burn, 15, 3000), null, true, 10));
        Assert.True(b.Apply(S(StatusKind.Mark, 50, 5000), null, true, 10));
        Assert.Equal(1.5f, b.IncomingMul, 3);
    }

    [Fact]
    public void KnockbackAndLaunchAreNeverStored()
    {
        var m = new MonsterStatus();
        Assert.False(m.Apply(S(StatusKind.Knockback, 2, 500), null, false, 10));
        Assert.False(m.Apply(S(StatusKind.Launch, 0, 1500), null, false, 10));
        Assert.False(m.Apply(S(StatusKind.Freeze, 0, 0), null, false, 10)); // zero duration = nothing
        Assert.Equal(StatusKind.None, m.Dominant);
        Assert.Equal(1f, m.SpeedFactor);
        Assert.False(StatusRules.ForBoss(S(StatusKind.Launch, 0, 1)).Kind != StatusKind.None);
    }

    [Fact]
    public void FreezeAndStunDisableUntilExpiry()
    {
        var m = new MonsterStatus();
        m.Apply(S(StatusKind.Freeze, 0, 1000), null, false, 10);
        Assert.True(m.Disabled);
        Assert.Equal(0f, m.SpeedFactor);
        m.Tick(999, null);
        Assert.True(m.Disabled);
        m.Tick(2, null);
        Assert.False(m.Disabled);
        Assert.Equal(1f, m.SpeedFactor);
    }

    [Fact]
    public void RefreshKeepsLongerDurationAndStrongerValue()
    {
        var m = new MonsterStatus();
        m.Apply(S(StatusKind.Slow, 30, 2000), null, false, 10);
        m.Apply(S(StatusKind.Slow, 20, 5000), null, false, 10);   // weaker but longer
        Assert.Equal(0.7f, m.SpeedFactor, 3);
        m.Tick(4000, null);
        Assert.True(m.Has(StatusKind.Slow));                       // the 5 s duration won
        m.Apply(S(StatusKind.Slow, 60, 100), null, false, 10);     // stronger but shorter
        Assert.Equal(0.4f, m.SpeedFactor, 3);
        m.Tick(1100, null);
        Assert.False(m.Has(StatusKind.Slow));
    }

    [Fact]
    public void SlowAndChillTakeTheStrongerAndClampAtNinetyPercent()
    {
        var m = new MonsterStatus();
        m.Apply(S(StatusKind.Chill, 50, 1000), null, false, 10);
        m.Apply(S(StatusKind.Slow, 30, 1000), null, false, 10);
        Assert.Equal(0.5f, m.SpeedFactor, 3);
        m.Apply(S(StatusKind.Slow, 200, 1000), null, false, 10);
        Assert.Equal(0.1f, m.SpeedFactor, 3);
    }

    [Fact]
    public void DotTicksTwiceASecondForTheCasterOfTheDot()
    {
        var m = new MonsterStatus();
        var hero = new object();
        m.Apply(S(StatusKind.Burn, 20, 3000), hero, false, 100f); // 20 % of 100 per second = 10 per tick of 500 ms
        var log = new List<(object, float)>();
        m.Tick(499, (s, d) => log.Add((s, d)));
        Assert.Empty(log);
        m.Tick(1, (s, d) => log.Add((s, d)));
        Assert.Single(log);
        Assert.Same(hero, log[0].Item1);
        Assert.Equal(10f, log[0].Item2, 3);
        m.Tick(2500, (s, d) => log.Add((s, d)));
        Assert.Equal(6, log.Count);                                // 3 s of burn = 6 ticks, the last one lands as it expires
        m.Tick(1000, (s, d) => log.Add((s, d)));
        Assert.Equal(6, log.Count);
        Assert.False(m.Has(StatusKind.Burn));
    }

    [Fact]
    public void BurnAndPoisonOfOneCasterShareOneHitPerTick()
    {
        var m = new MonsterStatus();
        var hero = new object();
        m.Apply(S(StatusKind.Burn, 20, 5000), hero, false, 100f);
        m.Apply(S(StatusKind.Poison, 10, 5000), hero, false, 100f);
        var log = new List<float>();
        m.Tick(500, (s, d) => log.Add(d));
        Assert.Single(log);
        Assert.Equal(15f, log[0], 3);
        Assert.Equal(StatusKind.Burn, m.Dominant);
    }

    [Fact]
    public void DotsStackPerSourceUpToThreeAndWeakerOnesAreRejected()
    {
        var m = new MonsterStatus();
        object a = new object(), b = new object(), c = new object(), d = new object();
        Assert.True(m.Apply(S(StatusKind.Poison, 10, 4000), a, false, 100f));
        Assert.True(m.Apply(S(StatusKind.Poison, 10, 4000), b, false, 100f));
        Assert.True(m.Apply(S(StatusKind.Poison, 10, 4000), c, false, 100f));
        Assert.False(m.Apply(S(StatusKind.Poison, 10, 4000), d, false, 100f)); // not stronger than the weakest
        Assert.Equal(3, m.DotSources);
        Assert.True(m.Apply(S(StatusKind.Poison, 30, 4000), d, false, 100f));  // stronger: displaces one
        Assert.Equal(3, m.DotSources);
        var sources = new HashSet<object>();
        m.Tick(500, (s, dmg) => sources.Add(s));
        Assert.Equal(3, sources.Count);
        Assert.Contains(d, sources);
    }

    [Fact]
    public void SameSourceRefreshDoesNotAddASecondStack()
    {
        var m = new MonsterStatus();
        var hero = new object();
        m.Apply(S(StatusKind.Burn, 10, 1000), hero, false, 50f);
        m.Apply(S(StatusKind.Burn, 10, 3000), hero, false, 50f);
        Assert.Equal(1, m.DotSources);
    }

    [Fact]
    public void MarkAmplifiesDamageAndEndsWhenChargesAreSpent()
    {
        var m = new MonsterStatus();
        m.Apply(S(StatusKind.Mark, 50, 10000, 3), new object(), false, 10);
        Assert.Equal(1.5f, m.IncomingMul, 3);
        m.ConsumeMarkCharge(); m.ConsumeMarkCharge();
        Assert.True(m.Has(StatusKind.Mark));
        m.ConsumeMarkCharge();
        Assert.False(m.Has(StatusKind.Mark));
        Assert.Equal(1f, m.IncomingMul);
    }

    [Fact]
    public void DominantFollowsTheTintPriority()
    {
        var m = new MonsterStatus();
        m.Apply(S(StatusKind.Chill, 50, 5000), null, false, 10);
        Assert.Equal(StatusKind.Chill, m.Dominant);
        m.Apply(S(StatusKind.Poison, 10, 5000), null, false, 10);
        Assert.Equal(StatusKind.Poison, m.Dominant);
        m.Apply(S(StatusKind.Burn, 10, 5000), null, false, 10);
        Assert.Equal(StatusKind.Burn, m.Dominant);
        m.Apply(S(StatusKind.Freeze, 0, 5000), null, false, 10);
        Assert.Equal(StatusKind.Freeze, m.Dominant);
        Assert.NotEqual(0, m.Mask & SkillEffect.StatusBit(StatusKind.Burn));
        m.Clear();
        Assert.Equal(StatusKind.None, m.Dominant);
        Assert.Equal(0, m.Mask);
    }

    [Fact]
    public void ConditionalBonusUsesStatusMaskAndBehind()
    {
        var e = new SkillEffect { BonusVsMask = SkillEffect.StatusBit(StatusKind.Burn) | SkillEffect.StatusBit(StatusKind.Freeze), BonusPct = 25f, BehindBonusPct = 30f };
        int burning = SkillEffect.StatusBit(StatusKind.Burn);
        Assert.Equal(1.25f, StatusRules.ConditionalMultiplier(e, burning, false), 3);
        Assert.Equal(1.55f, StatusRules.ConditionalMultiplier(e, burning, true), 3);
        Assert.Equal(1f, StatusRules.ConditionalMultiplier(e, SkillEffect.StatusBit(StatusKind.Poison), false), 3);
        Assert.Equal(1.3f, StatusRules.ConditionalMultiplier(e, 0, true), 3);
        Assert.Equal(1f, StatusRules.ConditionalMultiplier(new SkillEffect(), burning, false));
    }
}
