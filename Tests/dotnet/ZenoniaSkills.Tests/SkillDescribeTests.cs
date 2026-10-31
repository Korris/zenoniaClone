using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

public class SkillDescribeTests
{
    [Fact]
    public void EverySkillHasADescriptionAndARankBlockAtEveryRank()
    {
        foreach (SkillDef d in SkillTable.All)
            for (int r = 0; r <= SkillDef.MaxRank; r++)
            {
                string text = SkillDescribe.Description(d, r);
                Assert.True(text.Length > 10 && text.EndsWith("."), d.Id + " rank " + r + ": " + text);
                Assert.True(text.Length <= 140, d.Id + " does not fit the pane: " + text);
                Assert.NotNull(SkillDescribe.RankHeader(r));
                if (r < SkillDef.MaxRank) Assert.NotEmpty(SkillDescribe.RankLines(d, r));
            }
    }

    [Fact]
    public void DescriptionFollowsTheRankAndUnlearnedShowsRankOne()
    {
        SkillDef nova = SkillTable.ById("wizard.frost-nova");
        Assert.Equal("Hit enemies within 3 tiles for 280% ATK.", SkillDescribe.Description(nova, 0));
        Assert.Equal(SkillDescribe.Description(nova, 1), SkillDescribe.Description(nova, 0));
        Assert.Contains((int)System.Math.Round(nova.PctAt(nova.Effects[0], 5)) + "% ATK", SkillDescribe.Description(nova, 5));
    }

    [Fact]
    public void StatusLinesComeFromTheDataWithBossExceptions()
    {
        string nova = SkillDescribe.StatusLines(SkillTable.ById("wizard.frost-nova"));
        Assert.Contains("FREEZE", nova);
        Assert.Contains("Boss: 50% slow instead", nova);
        Assert.DoesNotContain("Boss", SkillDescribe.StatusLines(SkillTable.ById("wizard.fireball")));
        Assert.Contains("Boss: immune", SkillDescribe.StatusLines(SkillTable.ById("mechanic.rocket-punch")));
        Assert.Contains("BACKSTAB", SkillDescribe.StatusLines(SkillTable.ById("assassin.shadow-step")));
    }

    [Fact]
    public void RepeatedEffectsCountOnceInTextAndRankLines()
    {
        SkillDef rush = SkillTable.ById("assassin.phantom-rush");
        Assert.Contains("Repeats 3 times.", SkillDescribe.Description(rush, 1));
        Assert.Single(SkillDescribe.RankLines(rush, 1).Where(l => l.StartsWith("Damage")));
    }

    [Fact]
    public void RankLinesShowTheGainOrSayThereIsNone()
    {
        Assert.Contains("Damage 230% -> 258%", SkillDescribe.RankLines(SkillTable.ById("wizard.fireball"), 1));
        Assert.Contains("Ranks only raise the MP cost", SkillDescribe.RankLines(SkillTable.ById("warrior.berserker-awakening"), 1));
        Assert.Equal("MAX RANK", SkillDescribe.RankHeader(SkillDef.MaxRank));
        Assert.DoesNotContain(SkillDescribe.RankLines(SkillTable.ById("wizard.fireball"), SkillDef.MaxRank), l => l.Contains("->"));
    }

    [Fact]
    public void LegacySkillsDescribeTheirSharedNumbers()
    {
        Assert.Contains("380% ATK", SkillDescribe.Description(SkillTable.ById("warrior.earth-break"), 1));
        Assert.Contains("Boss: immune", SkillDescribe.StatusLines(SkillTable.ById("warrior.earth-break")));
        Assert.Contains("4 waves of 140% ATK", SkillDescribe.Description(SkillTable.ById("wizard.ice-storm"), 1));
        Assert.Contains("10 rockets", SkillDescribe.Description(SkillTable.ById("mechanic.rocket-barrage"), 1));
        Assert.Contains("400% ATK each", SkillDescribe.Description(SkillTable.ById("mechanic.rocket-barrage"), 1));
    }
}
