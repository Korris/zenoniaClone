using Xunit;
using ZenoniaProto.Rpg;

/// <summary>The Mire Hydra Queen shader FX (MireHydraLightFx) must strike exactly when the server deals the damage (sprite-rules.md VFX rule 1).</summary>
public class WantedSkillLightTimingTests
{
    static WantedSkillDef[] Mire { get { return WantedSkillTable.For(WantedSkillLightTiming.LightBoss); } }

    [Fact]
    public void TheLightBossIsTheMireHydraQueenWithFourSkills()
    {
        Assert.Equal("mireHydraQueen", WantedSkillLightTiming.LightBoss);
        Assert.Same(WantedSkillTable.MireHydraQueen, Mire);
        Assert.Equal(4, Mire.Length);
    }

    [Fact]
    public void WarningStartsEmptyAtTheTelegraphAndIsFullExactlyAtTheStrike()
    {
        foreach (WantedSkillDef s in Mire)
        {
            Assert.Equal(0f, WantedSkillLightTiming.WarningFill(s, s.TelegraphMs), 3);
            Assert.Equal(1f, WantedSkillLightTiming.WarningFill(s, 0f), 3);
            Assert.True(WantedSkillLightTiming.WarningFill(s, 1f) < 1f, s.Name + ": full before the strike");
            Assert.Equal(0.5f, WantedSkillLightTiming.WarningFill(s, s.TelegraphMs * 0.5f), 3);
        }
    }

    [Fact]
    public void ImpactPiecesReachFullSizeExactlyAtTheStrikeInstant()
    {
        Assert.Equal(1f, WantedSkillLightTiming.StreamLength(0f), 3);            // breath streams touch the cone rim at the hit
        Assert.True(WantedSkillLightTiming.StreamLength(-1f) < 1f);
        Assert.Equal(0f, WantedSkillLightTiming.StreamLength(-WantedSkillLightTiming.StreamGrowMs), 3);
        Assert.Equal(1f, WantedSkillLightTiming.DropProgress(0f), 3);            // Toxic Reign drops land at the hit
        Assert.True(WantedSkillLightTiming.DropProgress(-1f) < 1f);
        Assert.Equal(0f, WantedSkillLightTiming.Shockwave(0f), 3);               // Tail Crush wave leaves the boss at the hit ...
        Assert.Equal(1f, WantedSkillLightTiming.Shockwave(WantedSkillLightTiming.ShockwaveMs), 3);
        for (int id = 0; id < 4; id++) Assert.Equal(0f, WantedSkillLightTiming.Glow(id, 0f), 3); // nothing glows before the hit
    }

    [Fact]
    public void EveryPieceStartsWithinTheTelegraphAndEndsLongBeforeTheNextCast()
    {
        foreach (WantedSkillDef s in Mire)
        {
            Assert.True(WantedSkillLightTiming.ChargeMs <= s.TelegraphMs, s.Name + ": charge longer than the warning");
            Assert.True(WantedSkillLightTiming.DropFallMs <= s.TelegraphMs, s.Name + ": a drop would start before its warning");
            Assert.True(WantedSkillLightTiming.StreamGrowMs <= s.TelegraphMs);
            Assert.Equal(s.TelegraphMs, WantedSkillLightTiming.LeadMs(s));
            int end = WantedSkillLightTiming.ImpactEndMs(s.Id);
            Assert.InRange(end, 600, 1200);
            Assert.True(end + s.TelegraphMs < s.CooldownMs, s.Name + ": FX outlives its cooldown");
            Assert.Equal(0f, WantedSkillLightTiming.Glow(s.Id, end), 3);
            Assert.True(WantedSkillLightTiming.Glow(s.Id, WantedSkillLightTiming.GlowPeakMs) > 0.99f);
        }
    }

    [Fact]
    public void ChargeOnlyRunsInTheLastStretchBeforeTheStrike()
    {
        Assert.Equal(0f, WantedSkillLightTiming.Charge(WantedSkillLightTiming.ChargeMs + 1f), 3);
        Assert.Equal(1f, WantedSkillLightTiming.Charge(0f), 3);
        Assert.InRange(WantedSkillLightTiming.Charge(WantedSkillLightTiming.ChargeMs * 0.5f), 0.49f, 0.51f);
    }
}
