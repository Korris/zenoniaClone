using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

public class TurretTests
{
    const float FrameMs = 33.3f;

    static TurretClock Clock() { return new TurretClock(TurretConfig.DurationMs, TurretConfig.FireIntervalMs, TurretConfig.FirstShotMs); }

    /// <summary>Plays a whole life with a target in range from `targetFromMs` on; the times of the shots.</summary>
    static List<float> Life(float frameMs, float targetFromMs = 0f)
    {
        var shots = new List<float>();
        TurretClock c = Clock();
        float t = 0f;
        while (!c.Expired && t < 60000f)
        {
            c.Tick(frameMs);
            t += frameMs;
            if (c.Expired || !c.ShotDue) continue;
            if (t < targetFromMs) { c.NoTarget(); continue; }
            c.Fired();
            shots.Add(t);
        }
        return shots;
    }

    [Fact]
    public void TheSkillIsTheOldSlotWithTheConfigNumbers()
    {
        SkillDef d = SkillTable.Get("mechanic", 5);
        Assert.Equal("Deploy Turret", d.Name);
        Assert.Equal("mechanic.deploy-turret", d.Id);
        Assert.Equal(32, d.UnlockLevel); Assert.Equal(28, d.Mp); Assert.Equal(18000, d.BaseCooldownMs); Assert.Equal(450, d.CastLockMs);
        Assert.Equal(SkillTargeting.Self, d.Targeting);
        Assert.Equal(3, d.Prereqs[0].Index);
        SkillEffect e = Assert.Single(d.Effects);
        Assert.Equal(EffectKind.Turret, e.Kind);
        Assert.Equal(12000, e.DurationMs); Assert.Equal(800, e.IntervalMs); Assert.Equal(6f, e.Radius);
        Assert.Null(SkillTable.ById("mechanic.robot-assault"));
    }

    [Fact]
    public void DamagePerShotFollowsTheRankLikeTheOldSkill()
    {
        SkillDef d = SkillTable.Get("mechanic", 5);
        SkillEffect e = d.Effects[0];
        Assert.Equal(65f, d.PctAt(e, 1), 3);
        Assert.Equal(65f * 1.12f, d.PctAt(e, 2), 3);
        Assert.Equal(65f * (1f + 0.12f * 9f), d.PctAt(e, 10), 3);
        Assert.Contains("Damage per shot 65% -> 73%", SkillDescribe.RankLines(d, 1));
        string text = SkillDescribe.Description(d, 1);
        Assert.Contains("12s", text); Assert.Contains("6 tiles", text); Assert.Contains("65% ATK", text); Assert.Contains("0.8s", text);
    }

    [Fact]
    public void LivesItsTimeThenEnds()
    {
        TurretClock c = Clock();
        c.Tick(11999f);
        Assert.False(c.Expired);
        Assert.Equal(1, c.RemainingMs);
        c.Tick(1f);
        Assert.True(c.Expired);
        Assert.Equal(0, c.RemainingMs);
        Assert.False(c.ShotDue);
    }

    [Theory]
    [InlineData(1000f / 30f)] // the server's tick
    [InlineData(1000f / 60f)]
    [InlineData(100f)]
    public void FiresFifteenTimesInItsLifeWhateverTheFrameLength(float frameMs)
    {
        List<float> shots = Life(frameMs);
        Assert.Equal(15, shots.Count); // 0.4 s, then every 0.8 s until 11.6 s
        Assert.InRange(shots[0], 400f, 400f + frameMs + 0.01f);
        Assert.InRange(shots[14], 11600f, 11600f + frameMs + 0.01f);
    }

    [Fact]
    public void WaitsReadyForATargetThenKeepsTheInterval()
    {
        List<float> shots = Life(FrameMs, 5000f);
        Assert.InRange(shots[0], 5000f, 5000f + FrameMs + 0.01f); // at once when a monster walks in
        Assert.InRange(shots[1] - shots[0], 800f - FrameMs - 0.01f, 800f + FrameMs + 0.01f);
        Assert.InRange(shots[2] - shots[1], 800f - FrameMs - 0.01f, 800f + FrameMs + 0.01f);
    }

    [Fact]
    public void TargetsTheNearestLiveMonsterInRange()
    {
        float reach = TurretConfig.RangeTiles * 16f; // 96 units
        var all = new List<TurretCandidate>
        {
            new TurretCandidate(200f, 100f, false),  // 100 away: out of range
            new TurretCandidate(150f, 100f, false),  // 50 away
            new TurretCandidate(130f, 100f, true),   // 30 away, but dying
            new TurretCandidate(100f, 170f, false),  // 70 away
        };
        Assert.Equal(1, TurretTargeting.Nearest(100f, 100f, reach, all));
        all.RemoveAt(1);
        Assert.Equal(2, TurretTargeting.Nearest(100f, 100f, reach, all));
        all.RemoveAt(2);
        Assert.Equal(-1, TurretTargeting.Nearest(100f, 100f, reach, all)); // only the far one and the dying one left: holds fire
        Assert.Equal(-1, TurretTargeting.Nearest(100f, 100f, reach, new List<TurretCandidate>()));
    }

    [Fact]
    public void ARecastReplacesTheOwnersTurretAndNoOneElses()
    {
        var roster = new TurretRoster<string>();
        Assert.Null(roster.Place(1, "a"));
        Assert.Null(roster.Place(2, "b"));
        Assert.Equal("a", roster.Place(1, "a2"));
        Assert.Equal(2, roster.Count);
        Assert.Equal("b", roster[0]); Assert.Equal("a2", roster[1]);
        roster.Remove("a"); // the replaced one ending later changes nothing
        Assert.Equal(2, roster.Count);
        roster.Remove("a2");
        Assert.Equal(1, roster.Count);
        Assert.Null(roster.Place(1, "a3"));
    }
}
