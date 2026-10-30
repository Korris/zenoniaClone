using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

public class SkillScheduleTests
{
    static SkillDef Skill(string cls, string name) { return SkillTable.ById(cls + "." + name); }

    [Fact]
    public void StrikeHitsExpandWithInterval()
    {
        SkillStep[] s = SkillSchedule.Build(Skill("warrior", "whirlwind").Effects);
        Assert.Equal(new[] { 0, 250, 500 }, s.Select(x => x.AtMs).ToArray());
        Assert.Equal(new[] { 0, 1, 2 }, s.Select(x => x.HitIndex).ToArray());
    }

    [Fact]
    public void RainDropsAndOrderedMixedEffects()
    {
        SkillStep[] rain = SkillSchedule.Build(Skill("wizard", "astral-cataclysm").Effects);
        Assert.Equal(8, rain.Length);
        Assert.Equal(1400, SkillSchedule.LastStepMs(rain));
        SkillStep[] rush = SkillSchedule.Build(Skill("assassin", "phantom-rush").Effects);
        Assert.Equal(new[] { 0, 180, 180, 360, 360, 540 }, rush.Select(x => x.AtMs).ToArray());
        Assert.True(rush.Zip(rush.Skip(1), (a, b) => a.AtMs <= b.AtMs).All(x => x));
    }

    [Fact]
    public void AdvanceFiresEachStepOnceAndOnlyWhenDue()
    {
        SkillStep[] s = SkillSchedule.Build(Skill("assassin", "blade-dance").Effects); // 0,150,300,450
        int c = SkillSchedule.Advance(s, 0, 0);
        Assert.Equal(1, c);
        Assert.Equal(1, SkillSchedule.Advance(s, c, 149));
        Assert.Equal(3, SkillSchedule.Advance(s, c, 300));
        Assert.Equal(4, SkillSchedule.Advance(s, 3, 99999));
        Assert.Equal(4, SkillSchedule.Advance(s, 4, 99999));
    }

    [Fact]
    public void EverySkillRowMapsToSupportedEffects()
    {
        foreach (SkillDef d in SkillTable.All)
        {
            if (d.Legacy != LegacySkill.None) { Assert.Empty(d.Effects); continue; }
            Assert.NotEmpty(d.Effects);
            SkillStep[] steps = SkillSchedule.Build(d.Effects);
            Assert.True(steps.Length >= d.Effects.Length, d.Id);
            foreach (SkillEffect e in d.Effects)
            {
                switch (e.Kind)
                {
                    case EffectKind.Strike: Assert.True(e.Shape != AreaShape.Circle || e.Radius > 0f, d.Id); Assert.True(e.Shape != AreaShape.Line || e.Length > 0f, d.Id); break;
                    case EffectKind.Projectile: Assert.True(e.Speed > 0f && e.Range > 0f && e.Count >= 1, d.Id); break;
                    case EffectKind.Motion: Assert.True(e.Motion != MotionKind.None && (e.MoveTiles > 0f), d.Id); break;
                    case EffectKind.Buff: Assert.True(e.Buff.Kind != BuffKind.None && e.Buff.Ms > 0, d.Id); break;
                    case EffectKind.Heal: Assert.True(e.Pct > 0f, d.Id); break;
                    case EffectKind.Summon: Assert.True(e.DurationMs > 0 && e.IntervalMs > 0 && e.Pct > 0f, d.Id); break;
                    case EffectKind.Rain: Assert.True(e.Count > 0 && e.ImpactRadius > 0f && e.IntervalMs > 0, d.Id); break;
                    case EffectKind.Zone: Assert.True(e.DurationMs > 0 && e.IntervalMs > 0 && e.Hits > 0 && e.Radius > 0f, d.Id); break;
                }
            }
        }
    }
}

public class SkillScatterTests
{
    [Fact]
    public void SameCastEventGivesSamePointsOnEverySide()
    {
        int a = SkillScatter.Seed(7, 7, 100.2f, 50.6f), b = SkillScatter.Seed(7, 7, 100.2f, 50.6f);
        Assert.Equal(a, b);
        Assert.NotEqual(a, SkillScatter.Seed(8, 7, 100.2f, 50.6f));
        Assert.NotEqual(a, SkillScatter.Seed(7, 7, 100.2f, 50.6f, salt: 1));
        for (int i = 0; i < 8; i++)
        {
            float x1, y1, x2, y2;
            SkillScatter.InDisc(a, i, 64f, out x1, out y1);
            SkillScatter.InDisc(b, i, 64f, out x2, out y2);
            Assert.Equal(x1, x2); Assert.Equal(y1, y2);
            Assert.True(x1 * x1 + y1 * y1 <= 64f * 64f + 0.01f);
        }
    }

    [Fact]
    public void PointsSpreadOverTheDisc()
    {
        int seed = SkillScatter.Seed(1, 1, 0f, 0f);
        float sx = 0f, sy = 0f; int far = 0;
        for (int i = 0; i < 400; i++)
        {
            float x, y; SkillScatter.InDisc(seed, i, 10f, out x, out y);
            sx += x; sy += y; if (x * x + y * y > 25f) far++;
        }
        Assert.InRange(sx / 400f, -1f, 1f); Assert.InRange(sy / 400f, -1f, 1f);
        Assert.InRange(far, 240, 360); // area outside half the radius = 75%
    }

    [Fact]
    public void FanAnglesAreSymmetricAndSingleShotFliesStraight()
    {
        Assert.Equal(0f, SkillScatter.FanAngle(0, 1, 60f));
        Assert.Equal(-30f, SkillScatter.FanAngle(0, 6, 60f));
        Assert.Equal(30f, SkillScatter.FanAngle(5, 6, 60f));
        Assert.Equal(0f, SkillScatter.FanAngle(1, 3, 60f) );
    }
}

public class BuffStateTests
{
    [Fact]
    public void AtkBuffsStackAcrossKindsAndRefreshWithinAKind()
    {
        var b = new BuffState();
        b.Add(BuffKind.AtkPct, 25f, 1000); b.Add(BuffKind.AtkPct, 15f, 5000); b.Add(BuffKind.AtkSpeedPct, 15f, 1000);
        Assert.Equal(1.25f, b.AtkMul, 3);
        Assert.Equal(1.15f, b.AtkSpeedMul, 3);
        b.Tick(2000);
        Assert.Equal(1.25f, b.AtkMul, 3); // refreshed to the longer 5 s
        Assert.Equal(1f, b.AtkSpeedMul, 3);
        b.Tick(3100);
        Assert.False(b.Any);
    }

    [Fact]
    public void ShieldSoaksDamageThenEnds()
    {
        var b = new BuffState();
        b.Add(BuffKind.Shield, 100f, 8000);
        Assert.Equal(0f, b.Absorb(60f));
        Assert.Equal(40f, b.ShieldLeft, 3);
        Assert.Equal(20f, b.Absorb(60f));
        Assert.Equal(0f, b.ShieldLeft);
        Assert.Equal(50f, b.Absorb(50f));
    }

    [Fact]
    public void NextHitsConsumeChargesAndIgnoreBadInput()
    {
        var b = new BuffState();
        b.Add(BuffKind.NextHitsPct, 50f, 5000, charges: 2);
        Assert.Equal(1.5f, b.ConsumeNextHit(), 3);
        Assert.Equal(1.5f, b.ConsumeNextHit(), 3);
        Assert.Equal(1f, b.ConsumeNextHit(), 3);
        b.Add(BuffKind.AtkPct, -5f, 1000); b.Add(BuffKind.None, 5f, 1000); b.Add(BuffKind.AtkPct, 5f, 0);
        Assert.False(b.Any);
    }
}

public class SkillCooldownsTests
{
    [Fact]
    public void StartTickReadyAndCaptureRestore()
    {
        var c = new SkillCooldowns();
        Assert.True(c.Ready(3));
        c.Start(3, 1000f);
        Assert.False(c.Ready(3));
        var snap = new float[SkillCooldowns.Slots];
        c.Capture(snap);
        c.Tick(1500f);
        Assert.True(c.Ready(3));
        c.Restore(snap);
        Assert.Equal(1000f, c.LeftMs(3));
        Assert.False(c.Ready(-1)); Assert.False(c.Ready(8)); Assert.Equal(0f, c.LeftMs(99));
    }
}

public class NewGameVitalsTests
{
    [Fact]
    public void NewCharactersStartAtFullHpAndMpEvenWithStarterGearBonuses()
    {
        foreach (string c in RpgClasses.Ids)
        {
            PlayerState p = PlayerState.NewGame(c);
            Assert.Equal(p.MaxHp, p.Hp);
            Assert.Equal(p.MaxMp, p.Mp);
        }
    }
}
