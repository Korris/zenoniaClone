using Xunit;
using ZenoniaProto;
using ZenoniaProto.Rpg;

/// <summary>
/// 2026-11-15 (docs/reference/combo-knockback-chatgpt.md): only the combo finisher pushes a monster. Hits 1-3 of every class carry no knockback,
/// so they never pause the monster's AI nor stretch its windup; the finisher's slide (knockback x 1.35 over 170 ms, Monster.TakeHit with FX on)
/// matches the distances chosen with ChatGPT, and a push during a windup costs the monster that strike plus a 300 ms recovery.
/// </summary>
public class ComboKnockbackTests
{
    const float SlideSeconds = 0.17f, FxKnockMultiplier = 1.35f; // FeelConfig.KnockbackMs, FeelConfig.KnockbackMultiplier

    public static TheoryData<string, float> Finishers()
    {
        return new TheoryData<string, float>
        {
            { "fighter", 22f }, { "assassin", 14f }, { "wizard", 18f }, { "mechanic", 20f }, { "cleric", 16f },
        };
    }

    static ComboHit[] HitsOf(string cls)
    {
        switch (cls)
        {
            case "fighter": return ComboConfigs.FighterHits;
            case "assassin": return ComboConfigs.AssassinHits;
            case "wizard": return ComboConfigs.WizardHits;
            case "mechanic": return ComboConfigs.MechanicHits;
            default: return ComboConfigs.ClericHits;
        }
    }

    [Theory]
    [MemberData(nameof(Finishers))]
    public void OnlyTheFinisherPushes(string cls, float finisherUnits)
    {
        ComboHit[] h = HitsOf(cls);
        Assert.Equal(4, h.Length);
        for (int i = 0; i < 3; i++) Assert.Equal(0f, h[i].Knockback);
        float slide = h[3].Knockback * FxKnockMultiplier * SlideSeconds;
        Assert.InRange(slide, finisherUnits * 0.9f, finisherUnits * 1.1f);
    }

    [Fact]
    public void AssassinFinisherPushesOnlyWithItsLastStab()
    {
        ComboHit fin = ComboConfigs.AssassinHits[3];
        Assert.Equal(2, fin.Stabs);
        Assert.Equal(0f, fin.KnockbackFor(false)); // first stab: keeps the monster in reach, no windup cancel
        Assert.Equal(fin.Knockback, fin.KnockbackFor(true));
        ComboHit single = ComboConfigs.FighterHits[3]; // one-stab steps push with their only hit
        Assert.Equal(single.Knockback, single.KnockbackFor(false));
    }

    [Fact]
    public void WarriorLockSlashIsOneStepThatNeverPushes()
    {
        // 2026-11-20: the Warrior's basic attack is one heavy lock-target slash (no combo, no push: the locked monster stays in reach)
        Assert.Single(ComboConfigs.WarriorHits);
        Assert.Equal(0f, ComboConfigs.WarriorHits[0].Knockback);
        Assert.Null(ComboConfigs.WarriorHits[0].Bolt);
    }

    [Fact]
    public void OnlyARealDisableDropsThePendingStab()
    {
        Assert.False(StatusRules.CancelsPendingStab(StatusKind.None, 0f)); // plain hit: the committed second stab still lands
        Assert.False(StatusRules.CancelsPendingStab(StatusKind.Slow, 0f));
        Assert.False(StatusRules.CancelsPendingStab(StatusKind.Poison, 0f));
        Assert.True(StatusRules.CancelsPendingStab(StatusKind.Freeze, 0f));
        Assert.True(StatusRules.CancelsPendingStab(StatusKind.Stun, 0f));
        Assert.True(StatusRules.CancelsPendingStab(StatusKind.None, 1f)); // shoved
    }

    [Fact]
    public void PushedWindupRecoversBriefly()
    {
        Assert.Equal(300f, StatusRules.KnockInterruptRecoverMs);
    }
}
