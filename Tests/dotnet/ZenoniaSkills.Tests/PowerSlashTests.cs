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
    public void GatherIsShortAndOnlyTheFirstHitPulls()
    {
        var def = SkillTable.Get("warrior", 0);
        Assert.Equal(3000, def.CooldownMs);
        Assert.Equal(0.75f, def.Effects[0].PullTiles);
        for (int i = 1; i < def.Effects.Length; i++) Assert.Equal(0f, def.Effects[i].PullTiles);
        Assert.Contains("GATHER", SkillDescribe.StatusLines(def));
        Assert.Contains("immune to pull", SkillDescribe.StatusLines(def));
        Assert.Equal(0f, SkillTable.Get("warrior", 1).Effects[0].PullTiles);
    }

    [Fact]
    public void BarrageHitsEightTimesAndEndsOnTheFinisher()
    {
        var def = SkillTable.Get("warrior", 0);
        SkillStep[] steps = SkillSchedule.Build(def.Effects);
        Assert.Equal(new[] { 90, 135, 180, 225, 270, 315, 365, 440 }, System.Array.ConvertAll(steps, s => s.AtMs));
        Assert.Equal(610f, def.StrikeTotalPct);
        SkillEffect last = def.Effects[steps[steps.Length - 1].EffectIndex];
        Assert.True(last.Finisher);
        Assert.Equal(180f, last.Pct);
        foreach (SkillEffect e in def.Effects) Assert.Equal(160f, e.AngleDeg);
    }

    [Fact]
    public void DefIsSplitOverTheChainSoSmallHitsLoseNothing()
    {
        var def = SkillTable.Get("warrior", 0);
        float shares = 0f;
        foreach (SkillEffect e in def.Effects) shares += def.DefShare(e) * e.Hits;
        Assert.Equal(1f, shares, 4);
        var single = SkillTable.Get("warrior", 3); // Flame Cleave: one strike keeps the full DEF
        Assert.Equal(1f, single.DefShare(single.Effects[0]));
    }

    [Fact]
    public void InfernoCrashLandsThenBarragesFifteenTimesEndingOnTheFinisher()
    {
        var def = SkillTable.Get("warrior", 6);
        Assert.Equal(EffectKind.Motion, def.Effects[0].Kind); // InfernoCrashUltimate reads the leap from effect 0
        Assert.Equal(400, def.Effects[0].DurationMs);           // = InfernoCrashUltimate.LandMs
        SkillStep[] steps = SkillSchedule.Build(def.Effects);
        int strikes = 0;
        foreach (SkillStep st in steps) if (def.Effects[st.EffectIndex].Kind == EffectKind.Strike) { strikes++; Assert.True(st.AtMs >= 450); }
        Assert.Equal(15, strikes);
        Assert.Equal(1100, SkillSchedule.LastStepMs(steps));
        Assert.True(def.Effects[steps[steps.Length - 1].EffectIndex].Finisher);
        Assert.Equal(1140f, def.StrikeTotalPct);
    }
}
