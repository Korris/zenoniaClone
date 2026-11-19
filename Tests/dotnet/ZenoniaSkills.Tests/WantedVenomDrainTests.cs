using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Mire Hydra Queen's Venom Drain (2026-11-19, ChatGPT V1 numbers): table entry, who it hits, how much the boss heals, FX timing.</summary>
public class WantedVenomDrainTests
{
    static WantedSkillDef Drain { get { return WantedSkillTable.MireHydraQueen[4]; } }

    [Fact]
    public void VenomDrainIsAPlainCircleRoundTheBossWithTheChatGptNumbers()
    {
        WantedSkillDef d = Drain;
        Assert.Equal("Venom Drain", d.Name);
        Assert.True(d.DrainPoisoned);
        Assert.False(d.Enrage);
        Assert.False(d.OnPlayers);
        Assert.Equal(WantedAreaShape.Circle, d.Shape);
        Assert.Equal(6f, d.RadiusTiles);
        Assert.Equal(2000, d.TelegraphMs);
        Assert.Equal(22000, d.CooldownMs);
        Assert.Equal(0.8f, d.DamageMult);
        Assert.Equal(0.5f, d.DrainHealShare);
        Assert.Equal(0.02f, d.DrainHealCap);
        Assert.Equal(StatusKind.None, d.StatusKind); // it removes poison, it never applies a status
        // Only Venom Drain drains: the other skills of every boss keep hitting anyone in their shape.
        foreach (WantedSkillDef[] t in new[] { WantedSkillTable.RimeclawTyrant, WantedSkillTable.GlacialColossus, WantedSkillTable.MireHydraQueen })
            foreach (WantedSkillDef s in t) Assert.Equal(s == d, s.DrainPoisoned);
    }

    [Fact]
    public void OnlyPoisonedHeroesInsideTheCircleAreDrained()
    {
        Assert.True(WantedVenomDrain.Hits(Drain, true, true));
        Assert.False(WantedVenomDrain.Hits(Drain, true, false));  // clean hero standing in it: untouched
        Assert.False(WantedVenomDrain.Hits(Drain, false, true));  // poisoned but outside
        WantedSkillDef crush = WantedSkillTable.MireHydraQueen[2];
        Assert.True(WantedVenomDrain.Hits(crush, true, false));   // a normal skill ignores poison
        Assert.False(WantedVenomDrain.Hits(crush, false, true));
    }

    [Fact]
    public void DrainStartsOnAnyPoisonAndHoldsItPastTheStrike()
    {
        Assert.True(WantedVenomDrain.CanStartOn(Drain, 1f));
        Assert.False(WantedVenomDrain.CanStartOn(Drain, 0f));
        Assert.False(WantedVenomDrain.CanStartOn(WantedSkillTable.MireHydraQueen[1], 4000f)); // not a drain skill
        Assert.True(WantedVenomDrain.HoldPoisonMs(Drain) > Drain.TelegraphMs);          // still poisoned when the strike lands
        Assert.Equal(0, WantedVenomDrain.HoldPoisonMs(WantedSkillTable.MireHydraQueen[0]));
        // Why the hold exists: a poison from the previous skill has at most (duration - gap - execute) left when the next skill may start.
        float bestLeft = 0f;
        foreach (WantedSkillDef s in WantedSkillTable.MireHydraQueen)
            if (s.StatusKind == StatusKind.Poison) bestLeft = System.Math.Max(bestLeft, s.StatusMs - WantedSkillClock.SkillGapMs - 350f);
        Assert.True(bestLeft < Drain.TelegraphMs);
        var st = new MonsterStatus();
        st.Apply(new StatusSpec(StatusKind.Poison, 15f, 4000), null, false, 100f);
        st.Tick(3400f, null);
        st.HoldDot(StatusKind.Poison, WantedVenomDrain.HoldPoisonMs(Drain));
        st.Tick(Drain.TelegraphMs, null);
        Assert.True(st.Has(StatusKind.Poison)); // there at the strike
    }

    [Fact]
    public void BossHealsHalfTheHpTakenCappedAtTwoPercentOfItsMaxHp()
    {
        const float max = 34800f; // Mire Hydra Queen declared HP at --wanted-hp-scale 1
        Assert.Equal(300f, WantedVenomDrain.Heal(Drain, 600f, max), 3);
        Assert.Equal(max * 0.02f, WantedVenomDrain.Heal(Drain, 100000f, max), 3); // 40 drained heroes: the cap holds
        Assert.Equal(0f, WantedVenomDrain.Heal(Drain, 0f, max));
        Assert.Equal(0f, WantedVenomDrain.Heal(Drain, -5f, max));
        Assert.Equal(0f, WantedVenomDrain.Heal(WantedSkillTable.MireHydraQueen[0], 600f, max)); // not a drain skill
    }

    [Fact]
    public void DrainFxTimingMatchesTheTelegraphAndEndsLongBeforeTheCooldown()
    {
        WantedSkillDef d = Drain;
        Assert.Equal(d.TelegraphMs, WantedSkillLightTiming.LeadMs(d));              // the light FX starts with the telegraph
        Assert.Equal(1f, WantedSkillLightTiming.WarningFill(d, 0f), 4);              // circle full exactly at the strike
        Assert.Equal(0f, WantedSkillLightTiming.WarningFill(d, d.TelegraphMs), 4);
        Assert.Equal(0.3f, WantedSkillLightTiming.TetherAlpha(d, -d.TelegraphMs), 4);
        Assert.Equal(1f, WantedSkillLightTiming.TetherAlpha(d, 0f), 4);              // tethers peak at the server's damage instant
        Assert.Equal(0f, WantedSkillLightTiming.TetherAlpha(d, WantedSkillLightTiming.DrainPulseMs), 4);
        Assert.Equal(1.6f, WantedSkillLightTiming.TetherWidth(0f), 4);
        Assert.Equal(0f, WantedSkillLightTiming.HealFlash(0f));
        Assert.Equal(1f, WantedSkillLightTiming.HealFlash(WantedSkillLightTiming.GlowPeakMs), 4);
        Assert.Equal(0f, WantedSkillLightTiming.HealFlash(WantedSkillLightTiming.HealFlashMs));
        Assert.True(WantedSkillLightTiming.ImpactEndMs(d.Id) >= WantedSkillLightTiming.DrainPulseMs);
        Assert.True(WantedSkillLightTiming.ImpactEndMs(d.Id) < d.CooldownMs);
    }

    [Fact]
    public void PoisonTimeLeftAndCureOnTheEngineFreeStatus()
    {
        var st = new MonsterStatus();
        st.Apply(new StatusSpec(StatusKind.Poison, 15f, 4000), null, false, 100f);
        st.Tick(1000f, null);
        Assert.Equal(3000f, st.DotMsLeft(StatusKind.Poison), 2);
        st.Apply(new StatusSpec(StatusKind.Slow, 30f, 2000), null, false, 100f);
        st.ClearDot(StatusKind.Poison);
        Assert.False(st.Has(StatusKind.Poison));
        Assert.Equal(0f, st.DotMsLeft(StatusKind.Poison));
        Assert.True(st.Has(StatusKind.Slow)); // the cure only takes the poison
    }
}
