using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

public class SkillTableTests
{
    [Fact]
    public void EightSkillsPerClassWithUniqueIndicesAndIds()
    {
        Assert.Equal(40, SkillTable.All.Count);
        Assert.Equal(40, SkillTable.All.Select(s => s.Id).Distinct().Count());
        foreach (string c in RpgClasses.Ids)
        {
            SkillDef[] l = SkillTable.ForClass(c);
            Assert.Equal(8, l.Length);
            for (int i = 0; i < 8; i++) { Assert.Equal(i, l[i].Index); Assert.Equal(c, l[i].Class); Assert.Same(l[i], SkillTable.ById(l[i].Id)); }
        }
        Assert.Empty(SkillTable.ForClass("nobody"));
        Assert.Null(SkillTable.Get("wizard", 8));
    }

    [Fact]
    public void PrereqsPointToEarlierSkillsOfTheSameClassWithLegalRanks()
    {
        foreach (SkillDef s in SkillTable.All)
        {
            Assert.InRange(s.UnlockLevel, 1, 60);
            Assert.True(s.Mp > 0 && s.CooldownMs > 0 && s.CastLockMs > 0, s.Id);
            foreach (SkillReq r in s.Prereqs)
            {
                Assert.InRange(r.Index, 0, 7);
                Assert.InRange(r.Rank, 1, SkillDef.MaxRank);
                Assert.True(SkillTable.Get(s.Class, r.Index).UnlockLevel < s.UnlockLevel, s.Id + " prereq must unlock earlier"); // also rules out cycles
            }
        }
    }

    [Fact]
    public void EveryClassHasAFreeFirstSkillAndAnUltimateAtLevel50()
    {
        foreach (string c in RpgClasses.Ids)
        {
            SkillDef[] l = SkillTable.ForClass(c);
            Assert.Equal(1, l[0].UnlockLevel); Assert.Empty(l[0].Prereqs);
            Assert.Equal(50, l[7].UnlockLevel); Assert.Equal(SkillTier.Ultimate, l[7].Tier);
            Assert.Equal(2, l[7].Prereqs.Length);
        }
    }

    [Fact]
    public void KnownRowsMatchTheDesign()
    {
        SkillDef ice = SkillTable.Get("wizard", 2);
        Assert.Equal("Ice Storm", ice.Name); Assert.Equal(28, ice.Mp); Assert.Equal(12000, ice.CooldownMs); Assert.Equal(LegacySkill.IceStorm, ice.Legacy);
        Assert.Equal(LegacySkill.EarthBreak, SkillTable.Get("warrior", 2).Legacy);
        SkillDef orbital = SkillTable.Get("mechanic", 7);
        Assert.Equal(50, orbital.UnlockLevel); Assert.Equal(80, orbital.Mp); Assert.Equal(45000, orbital.CooldownMs);
        Assert.Equal(6, orbital.Effects[0].Count); Assert.Equal(140f, orbital.Effects[0].Pct); Assert.Equal(EffectKind.Rain, orbital.Effects[0].Kind);
        Assert.Equal("wizard.fireball", SkillTable.Get("wizard", 1).Id);
        SkillDef step = SkillTable.Get("assassin", 1);
        Assert.Equal(SkillTargeting.NearestEnemy, step.Targeting); Assert.Equal(30f, step.Effects[1].BehindBonusPct);
        Assert.Equal(220f, SkillTable.Get("cleric", 0).Effects[0].Pct);
    }

    [Fact]
    public void LegacySkillsHaveNoEffectsAndEveryOtherSkillHasAtLeastOne()
    {
        foreach (SkillDef s in SkillTable.All)
            if (s.Legacy != LegacySkill.None) Assert.Empty(s.Effects); else Assert.NotEmpty(s.Effects);
    }

    [Fact]
    public void ScalingIsLinearInRankAndMpIsMonotonic()
    {
        SkillDef s = SkillTable.Get("warrior", 0);
        Assert.Equal(1f, s.DamageMul(1));
        Assert.Equal(1f + 9 * s.DamageGrowth, s.DamageMul(10), 4);
        Assert.Equal(170f, s.PctAt(s.Effects[0], 1));
        foreach (SkillDef d in SkillTable.All)
            for (int r = 1; r < SkillDef.MaxRank; r++) Assert.True(d.MpAt(r + 1) > d.MpAt(r), d.Id);
        Assert.Equal(27, SkillTable.Get("warrior", 1).MpAt(10)); // Whirlwind: basic tier, +1 MP per rank
        Assert.Equal(80 + 45, SkillTable.Get("mechanic", 7).MpAt(10)); // ultimate: +5 MP per rank
    }

    [Fact]
    public void TextShowsCurrentAndNextRank()
    {
        SkillDef fireball = SkillTable.Get("wizard", 1);
        Assert.Equal("MP 18 -> 19", SkillText.Mp(fireball, 1));
        Assert.Contains("Damage 230% -> 258%", SkillText.RankLines(fireball, 1));
        Assert.Equal("MP 18", SkillText.Mp(fireball, 0));
        Assert.Equal("MP " + fireball.MpAt(10), SkillText.Mp(fireball, 10));
        Assert.Contains("Cooldown 6s", SkillText.Cooldown(fireball));
    }
}
