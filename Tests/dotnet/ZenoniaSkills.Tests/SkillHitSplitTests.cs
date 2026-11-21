using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

public class SkillHitSplitTests
{
    [Theory]
    [InlineData(1, 4)]
    [InlineData(3, 4)]
    [InlineData(7, 8)]
    [InlineData(100, 3)]
    [InlineData(12345, 6)]
    [InlineData(65535, 8)]
    public void SplitAddsUpToTheRealDamageExactly(int total, int parts)
    {
        for (int seed = -50; seed < 50; seed++)
        {
            int[] r = SkillHitSplit.Split(total, parts, seed * 7919);
            Assert.Equal(total, r.Sum());
            Assert.All(r, v => Assert.True(v >= 1));
            Assert.True(r.Length <= parts && r.Length <= total);
        }
    }

    [Fact]
    public void PartsVaryOnlySlightlyAroundAnEvenShare()
    {
        for (int seed = 0; seed < 200; seed++)
        {
            int[] r = SkillHitSplit.Split(10000, 5, seed);
            Assert.Equal(5, r.Length);
            Assert.All(r, v => Assert.InRange(v, 1690, 2350)); // weights 0.9-1.1, renormalised so the sum stays exact
        }
    }

    [Fact]
    public void PartCountIsClampedToOneToEight()
    {
        Assert.Single(SkillHitSplit.Split(500, 0, 1));
        Assert.Equal(8, SkillHitSplit.Split(500, 20, 1).Length);
        Assert.Equal(new[] { 0 }, SkillHitSplit.Split(0, 5, 1));
    }

    [Fact]
    public void EveryDamagingSkillOfEveryClassHasARowWithinBounds()
    {
        foreach (SkillDef d in SkillTable.All)
        {
            if (!SkillHitSplit.Damages(d)) continue;
            Assert.True(SkillHitSplit.Has(d.Id), d.Id + " has no SkillHitSplit row");
            HitSplit s = SkillHitSplit.For(d.Id);
            Assert.InRange(s.Hits, SkillHitSplit.MinHits, SkillHitSplit.MaxHits);
            Assert.InRange(s.FinisherHits, SkillHitSplit.MinHits, SkillHitSplit.MaxHits);
            Assert.InRange(s.IntervalMs, SkillHitSplit.MinIntervalMs, SkillHitSplit.MaxIntervalMs);
            // a skill that already lands several real hits must not overlap its own parts with the next real hit
            foreach (SkillEffect e in d.Effects)
                if (e.Kind == EffectKind.Strike && e.Hits > 1 && e.IntervalMs > 0)
                    Assert.True((s.Hits - 1) * s.IntervalMs < e.IntervalMs, d.Id + ": parts outlast the real hit interval");
        }
    }

    [Fact]
    public void SingleStrongHitSkillsPopSeveralNumbersAndUltimatesTheMost()
    {
        Assert.InRange(SkillHitSplit.For("warrior.flame-cleave").Hits, 3, 5);
        Assert.InRange(SkillHitSplit.For("fighter.dragon-fist").Hits, 6, 8);
        Assert.InRange(SkillHitSplit.For("cleric.divine-ascension").Hits, 6, 8);
        Assert.Equal(1, SkillHitSplit.Basic.Hits);
        Assert.Equal(2, SkillHitSplit.Basic.PartsFor(true));
        Assert.True(SkillHitSplit.For("warrior.inferno-crash").PartsFor(true) > SkillHitSplit.For("warrior.inferno-crash").PartsFor(false));
    }

    [Fact]
    public void BuffsAndHealsAreNotDamagingSkills()
    {
        Assert.False(SkillHitSplit.Damages(SkillTable.ById("warrior.berserker-awakening")));
        Assert.False(SkillHitSplit.Damages(SkillTable.ById("cleric.heal")));
        Assert.False(SkillHitSplit.Damages(SkillTable.ById("cleric.sanctuary")));
        Assert.False(SkillHitSplit.Damages(SkillTable.ById("assassin.death-mark")));
    }

    [Fact]
    public void CrowdLodCutsPartsWhenManyNumbersAreLive()
    {
        Assert.Equal(6, SkillHitSplit.CrowdParts(6, 10));
        Assert.Equal(4, SkillHitSplit.CrowdParts(6, 60));
        Assert.Equal(2, SkillHitSplit.CrowdParts(6, 100));
        Assert.Equal(1, SkillHitSplit.CrowdParts(6, 200));
    }

    [Fact]
    public void ActiveWindowCoversTheTimelineAndLegacySkills()
    {
        Assert.True(SkillHitSplit.ActiveMs(SkillTable.ById("warrior.inferno-crash")) >= 1100);
        Assert.True(SkillHitSplit.ActiveMs(SkillTable.ById("warrior.blade-cyclone")) >= 2500);
        Assert.True(SkillHitSplit.ActiveMs(SkillTable.ById("assassin.shadow-clone")) >= 8000);
    }
}
