using Xunit;
using ZenoniaProto.Rpg;

public class SkillRanksTests
{
    static SkillDef W(int i) { return SkillTable.Get("warrior", i); }

    [Fact]
    public void LevelOneLearnsOnlyTheFirstSkill()
    {
        var r = new SkillRanks("warrior");
        Assert.Equal(LearnResult.Ok, r.CanLearn(W(0), 1));
        for (int i = 1; i < 8; i++) Assert.NotEqual(LearnResult.Ok, r.CanLearn(W(i), 1));
        Assert.Equal(LearnResult.LevelTooLow, r.CanLearn(W(1), 1));
        Assert.Equal(LearnResult.Ok, r.Learn(W(0), 1));
        Assert.Equal(1, r[0]);
        Assert.Equal(LearnResult.NoSp, r.Learn(W(0), 1)); // the only point is spent
        Assert.Equal(1, r[0]);
    }

    [Fact]
    public void PrereqRankGatesLearning()
    {
        var r = new SkillRanks("warrior");
        for (int i = 0; i < 2; i++) Assert.Equal(LearnResult.Ok, r.Learn(W(0), 20));
        Assert.Equal(LearnResult.PrereqMissing, r.CanLearn(W(1), 20)); // Whirlwind needs Power Slash 3
        r.Learn(W(0), 20);
        r.Learn(W(0), 20); r.Learn(W(0), 20);
        Assert.Equal(LearnResult.Ok, r.Learn(W(1), 20));
        Assert.Equal(LearnResult.Ok, r.CanLearn(W(2), 20)); // Earth Break: Power Slash 5 reached
        Assert.Equal(LearnResult.PrereqMissing, r.CanLearn(SkillTable.Get("wizard", 0), 20)); // other class
        Assert.Equal(LearnResult.PrereqMissing, r.CanLearn(null, 20));
    }

    [Fact]
    public void RankCapsAtTen()
    {
        var r = new SkillRanks("warrior");
        for (int i = 0; i < 10; i++) Assert.Equal(LearnResult.Ok, r.Learn(W(0), 60));
        Assert.Equal(LearnResult.MaxRank, r.Learn(W(0), 60));
        Assert.Equal(10, r[0]);
    }

    [Fact]
    public void UltimateNeedsLevel50AndBothPrereqsAtFive()
    {
        var r = new SkillRanks("warrior");
        r.Load(new[] { 10, 5, 5, 3, 3, 5, 5, 0 }, 60);
        Assert.Equal(36, r.Sum);
        Assert.Equal(LearnResult.LevelTooLow, r.CanLearn(W(7), 49));
        Assert.Equal(LearnResult.Ok, r.CanLearn(W(7), 60));
        r.Load(new[] { 10, 5, 5, 3, 3, 5, 4, 0 }, 60);
        Assert.Equal(LearnResult.PrereqMissing, r.CanLearn(W(7), 60));
    }

    [Fact]
    public void SpIsLevelMinusRanksAndLevel60CannotMaxEverything()
    {
        var r = new SkillRanks("warrior");
        Assert.Equal(60, r.Free(60));
        r.Load(new[] { 10, 10, 10, 10, 10, 10, 0, 0 }, 60);
        Assert.Equal(60, r.Sum); Assert.Equal(0, r.Free(60));
        Assert.Equal(LearnResult.NoSp, r.CanLearn(W(6), 60));
        Assert.True(8 * SkillDef.MaxRank > 60);
    }

    [Fact]
    public void SanitizeTrimsTamperedRanksDeterministically()
    {
        var r = new SkillRanks("warrior");
        r.Load(new[] { 99, -5, 0, 0, 0, 0, 0, 0 }, 60);
        Assert.Equal(10, r[0]); Assert.Equal(0, r[1]); // clamp
        r.Load(new[] { 0, 5, 0, 0, 0, 0, 0, 0 }, 60);
        Assert.Equal(0, r[1]); // Whirlwind without Power Slash 3
        r.Load(new[] { 3, 6, 0, 0, 0, 0, 0, 0 }, 8);
        Assert.Equal(new[] { 3, 5 }, new[] { r[0], r[1] }); // sum 9 > level 8: newest skill loses a rank
        r.Load(new[] { 5, 5, 5, 0, 0, 0, 0, 0 }, 10);
        Assert.Equal(new[] { 5, 5, 0 }, new[] { r[0], r[1], r[2] }); // Earth Break (newest) trimmed first
        r.Load(new[] { 3, 3, 0, 0, 0, 0, 0, 0 }, 3);
        Assert.Equal(new[] { 3, 0 }, new[] { r[0], r[1] }); // Whirlwind needs Lv 8: dropped
        r.Load(new[] { 5, 0, 0, 0, 0, 0, 0, 3 }, 60);
        Assert.Equal(0, r[7]); // ultimate without prereqs
        r.Load(null, 5); Assert.Equal(0, r.Sum);
        r.Load(new[] { 1, 1 }, 1); Assert.Equal(new[] { 1, 0 }, new[] { r[0], r[1] }); // short list + level-gated
    }

    [Fact]
    public void SanitizeIsIdempotentAndNeverExceedsLevel()
    {
        var rnd = new System.Random(7);
        for (int n = 0; n < 500; n++)
        {
            var raw = new int[8];
            for (int i = 0; i < 8; i++) raw[i] = rnd.Next(-2, 14);
            int level = rnd.Next(1, 61);
            var r = new SkillRanks("wizard");
            r.Load(raw, level);
            Assert.True(r.Sum <= level);
            var once = r.ToList();
            r.Sanitize(level);
            Assert.Equal(once, r.ToList());
            foreach (SkillDef d in SkillTable.ForClass("wizard"))
                if (r[d.Index] > 0) Assert.True(level >= d.UnlockLevel, d.Id);
        }
    }

    [Fact]
    public void UnknownClassLoadsEmpty()
    {
        var r = new SkillRanks("nobody");
        r.Load(new[] { 1, 1, 1, 1, 1, 1, 1, 1 }, 60);
        Assert.Equal(0, r.Sum);
    }
}
