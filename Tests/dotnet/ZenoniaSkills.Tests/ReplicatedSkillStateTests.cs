using Xunit;
using ZenoniaProto.Rpg;

/// <summary>State a predicting client mirrors from the server: the attack-speed buff (BuffState.Set) and the monster status bits (ShowReplicated).</summary>
public class ReplicatedSkillStateTests
{
    [Fact]
    public void BuffSetForcesExactValueAndTimeAndRemovesOnZero()
    {
        var b = new BuffState();
        b.Add(BuffKind.AtkSpeedPct, 15f, 12000);
        b.Set(BuffKind.AtkSpeedPct, 10f, 4000f);
        Assert.Equal(10f, b.ValueOf(BuffKind.AtkSpeedPct)); Assert.Equal(4000f, b.LeftMs(BuffKind.AtkSpeedPct)); Assert.Equal(1.1f, b.AtkSpeedMul, 3);
        b.Set(BuffKind.AtkSpeedPct, 10f, 0f);
        Assert.False(b.Has(BuffKind.AtkSpeedPct)); Assert.Equal(1f, b.AtkSpeedMul);
    }

    [Theory]
    [InlineData(StatusKind.Freeze)]
    [InlineData(StatusKind.Stun)]
    [InlineData(StatusKind.Burn)]
    [InlineData(StatusKind.Poison)]
    [InlineData(StatusKind.Chill)]
    public void ShowReplicatedReproducesTheDominantStatus(StatusKind k)
    {
        var s = new MonsterStatus();
        s.ShowReplicated(k, false);
        Assert.Equal(k, s.Dominant);
        Assert.False(s.Has(StatusKind.Mark));
        s.ShowReplicated(StatusKind.None, false);
        Assert.Equal(StatusKind.None, s.Dominant);
    }

    [Fact]
    public void ShowReplicatedMarkIsIndependentOfTheDominantStatus()
    {
        var s = new MonsterStatus();
        s.ShowReplicated(StatusKind.Freeze, true);
        Assert.Equal(StatusKind.Freeze, s.Dominant); Assert.True(s.Has(StatusKind.Mark)); Assert.True(s.Disabled);
        s.ShowReplicated(StatusKind.None, true);
        Assert.Equal(StatusKind.Mark, s.Dominant);
    }
}
