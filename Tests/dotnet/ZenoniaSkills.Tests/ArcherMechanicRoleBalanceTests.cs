using Xunit;
using Xunit.Abstractions;
using ZenoniaProto;
using ZenoniaProto.Rpg;

/// <summary>
/// Archer vs Mechanic role split (ChatGPT chat "Ragnarok Và Đạo Chúa", user approved 2026-11-26): Archer = mobile precision DPS (basic range 7-8 m,
/// fast basic attacks, best on one target / a boss by ~10-15 %, squishy); Mechanic = artillery / area control (basic range 5.5-6.5 m, best on packs
/// of 4-6 by ~15-20 %, sturdier). 1 tile = 1 m = 16 world units (Map3D.M). DPS from RangedRoleDpsModel (expected value, same level and gear).
/// </summary>
public class ArcherMechanicRoleBalanceTests
{
    const float M = 16f; // world units per metre (one tile)
    readonly ITestOutputHelper log;
    public ArcherMechanicRoleBalanceTests(ITestOutputHelper log) { this.log = log; }

    /// <summary>Reach of a basic shot from the hero's body: the Archer's arrows leave MuzzleAhead in front, the Mechanic's ranges count from the body.</summary>
    static float ArcherReach(ComboHit h) { return h.Bolt.Range + RangedAimRules.MuzzleAhead; }

    [Fact]
    public void BasicAttackRangesFollowTheRoles()
    {
        foreach (ComboHit h in ComboConfigs.ArcherHits) Assert.InRange(ArcherReach(h) / M, 7f, 8f);
        foreach (ComboHit h in ComboConfigs.MechanicHits) Assert.InRange(h.Bolt.Range / M, 5.5f, 6.5f);
    }

    [Fact]
    public void ArcherArrowStillLeavesOnAttackFrameFive()
    {
        foreach (ComboHit h in ComboConfigs.ArcherHits) Assert.Equal(5f / 8f, h.WindupMs / h.TotalMs, 3); // 8-frame attack body, release = f5
    }

    [Fact]
    public void ArcherIsSquishierThanMechanic()
    {
        BaseStats a = Leveling.StatsForLevel(50, "archer"), m = Leveling.StatsForLevel(50, "mechanic");
        Assert.True(a.MaxHp < m.MaxHp && a.Def < m.Def);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(35)]
    [InlineData(50)]
    public void ArcherWinsOneTargetMechanicWinsAPack(int level)
    {
        var a1 = RangedRoleDpsModel.Estimate("archer", ComboConfigs.ArcherHits, level, 1);
        var m1 = RangedRoleDpsModel.Estimate("mechanic", ComboConfigs.MechanicHits, level, 1);
        var a5 = RangedRoleDpsModel.Estimate("archer", ComboConfigs.ArcherHits, level, 5);
        var m5 = RangedRoleDpsModel.Estimate("mechanic", ComboConfigs.MechanicHits, level, 5);
        float single = a1.Total / m1.Total, pack = m5.Total / a5.Total;
        log.WriteLine("Lv " + level + "  1 target: Archer " + a1.Total.ToString("0") + " (basic " + a1.Basic.ToString("0") + ", skills " + a1.Skills.ToString("0") + ")  Mechanic " + m1.Total.ToString("0") + " (basic " + m1.Basic.ToString("0") + ", skills " + m1.Skills.ToString("0") + ")  Archer/Mechanic " + single.ToString("0.000"));
        log.WriteLine("        5 targets: Archer " + a5.Total.ToString("0") + " (basic " + a5.Basic.ToString("0") + ", skills " + a5.Skills.ToString("0") + ")  Mechanic " + m5.Total.ToString("0") + " (basic " + m5.Basic.ToString("0") + ", skills " + m5.Skills.ToString("0") + ")  Mechanic/Archer " + pack.ToString("0.000"));
        log.WriteLine("  plans: A1 " + a1.Plan + " | M1 " + m1.Plan + " | A5 " + a5.Plan + " | M5 " + m5.Plan);
        if (level == 50) foreach (int n in new[] { 1, 5 }) log.WriteLine("  skills x" + n + ": A " + RangedRoleDpsModel.Breakdown("archer", level, n) + " | M " + RangedRoleDpsModel.Breakdown("mechanic", level, n));
        // Lv 50 (every skill unlocked): the approved targets. Earlier levels have fewer skills, so only a looser band around them.
        bool end = level >= 50;
        Assert.InRange(single, end ? 1.10f : 1.05f, end ? 1.15f : 1.20f);
        Assert.InRange(pack, end ? 1.15f : 1.10f, end ? 1.20f : 1.25f);
    }
}
