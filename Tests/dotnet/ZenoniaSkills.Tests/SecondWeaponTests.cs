using Xunit;
using ZenoniaProto.Rpg;

public class SecondWeaponTests
{
    static PlayerState Hero(string cls, int level, int plus)
    {
        var p = PlayerState.NewGame(cls);
        p.Level = level;
        p.GetEquipped(Slots.Offhand).enhance = plus;
        p.RecomputeStats();
        return p;
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(6, 0)]
    [InlineData(7, 1)]
    [InlineData(8, 1)]
    [InlineData(9, 2)]
    [InlineData(11, 3)]
    [InlineData(13, 4)]
    [InlineData(14, 4)]
    [InlineData(15, 5)]
    public void EnhanceLookBandsCoverEveryThreshold(int plus, int band)
    {
        Assert.Equal(band, EnhanceStats.VisualBand(new Item { slot = Slots.Weapon, enhance = plus }));
        Assert.Equal(band, EnhanceStats.VisualBand(new Item { slot = Slots.Offhand, enhance = plus }));
    }

    [Fact]
    public void CurveIsMonotonicWithFixedEnds()
    {
        Assert.Equal(0.0, SecondWeapon.F(0));
        Assert.Equal(1.0, SecondWeapon.F(15), 12);
        for (int l = 1; l <= 15; l++) Assert.True(SecondWeapon.F(l) > SecondWeapon.F(l - 1));
        Assert.Equal(6.0 / 33, SecondWeapon.F(6), 12);
        Assert.Equal(12.0 / 33, SecondWeapon.F(9), 12);
        Assert.Equal(21.0 / 33, SecondWeapon.F(12), 12);
        Assert.Equal(SecondWeapon.F(15), SecondWeapon.F(99)); // clamped
    }

    [Fact]
    public void EachClassOnlyHasItsOwnKeys()
    {
        SecondWeaponStats w = SecondWeapon.For("warrior", 15), m = SecondWeapon.For("wizard", 15), r = SecondWeapon.For("mechanic", 15),
            a = SecondWeapon.For("assassin", 15), c = SecondWeapon.For("cleric", 15);
        Assert.Equal(new[] { 0.30, 0.20, 0.10 }, new[] { w.DefPct, w.HpPct, w.DmgReduction });
        Assert.Equal(0, w.MpPct + w.CritRate + w.RobotDmg + w.HealPower);
        Assert.Equal(new[] { 0.30, 0.25, 0.20 }, new[] { m.MpPct, m.CastSpeed, m.MpCostCut });
        Assert.Equal(0, m.DefPct + m.CritRate + m.RobotDmg + m.HealPower);
        Assert.Equal(new[] { 0.60, 0.40 }, new[] { r.RobotDmg, r.RobotRate });
        Assert.Equal(0, r.DefPct + r.MpPct + r.CritRate + r.HealPower);
        Assert.Equal(new[] { 0.10, 0.50 }, new[] { a.CritRate, a.CritDmg });
        Assert.Equal(0, a.DefPct + a.MpPct + a.RobotDmg + a.HealPower);
        Assert.Equal(new[] { 0.40, 0.40 }, new[] { c.HealPower, c.ShieldPower });
        Assert.Equal(0, c.DefPct + c.MpPct + c.CritRate + c.RobotDmg);
        Assert.Equal(0.30 * SecondWeapon.F(6), SecondWeapon.For("warrior", 6).DefPct, 12);
    }

    [Fact]
    public void CombosRunFromBaseToCap()
    {
        Assert.Equal(0.04, SecondWeapon.ComboEarthShieldPct(0), 12);
        Assert.Equal(0.12, SecondWeapon.ComboEarthShieldPct(15), 12);
        Assert.Equal(1.0, SecondWeapon.ComboChillSeconds(0), 12);
        Assert.Equal(3.0, SecondWeapon.ComboChillSeconds(15), 12);
        Assert.Equal(0.4, SecondWeapon.ComboVolleyMul(0), 12);
        Assert.Equal(1.0, SecondWeapon.ComboVolleyMul(15), 12);
        Assert.Equal(0.30, SecondWeapon.ComboNextCritPts(0), 12);
        Assert.Equal(0.80, SecondWeapon.ComboNextCritPts(15), 12);
        Assert.Equal(0.15, SecondWeapon.ComboHealShieldPct(0), 12);
        Assert.Equal(0.40, SecondWeapon.ComboHealShieldPct(15), 12);
    }

    [Fact]
    public void WarriorPlus15ShieldScalesDefByThirtyPercent()
    {
        PlayerState p = Hero("warrior", 60, 15);
        BaseStats b = Leveling.StatsForLevel(60, "warrior");
        Item shield = p.GetEquipped(Slots.Offhand);
        int raw = b.Def + p.GetEquipped(Slots.Weapon).stats.def + shield.stats.def; // offhand base stays flat
        Assert.Equal(RpgMath.Round(raw * 1.3), p.Def);
        Assert.Equal(RpgMath.Round((b.MaxHp + shield.stats.maxHp) * 1.2), p.MaxHp);
        Assert.Equal(1.0, p.Second.F, 12);
    }

    [Fact]
    public void UnequippingOffhandZeroesSecondAndRevertsStats()
    {
        PlayerState p = Hero("warrior", 30, 12);
        int defWith = p.Def, hpWith = p.MaxHp;
        Assert.True(p.Second.Equipped);
        Assert.True(p.Unequip(Slots.Offhand).Ok);
        Assert.Equal(SecondWeaponStats.Zero, p.Second);
        Assert.True(p.Def < defWith && p.MaxHp < hpWith);
        Assert.Equal(Leveling.StatsForLevel(30, "warrior").Def, p.Def);
    }

    [Fact]
    public void AssassinOffhandAddsCritRate()
    {
        float at0 = Hero("assassin", 20, 0).CritBonus;
        float at15 = Hero("assassin", 20, 15).CritBonus;
        Assert.Equal(0.10f, at15 - at0, 4);
    }

    [Fact]
    public void WizardOffhandRaisesMaxMp()
    {
        PlayerState p0 = Hero("wizard", 20, 0), p15 = Hero("wizard", 20, 15);
        Assert.True(p15.MaxMp > p0.MaxMp);
        int raw = Leveling.StatsForLevel(20, "wizard").MaxMp + p0.GetEquipped(Slots.Offhand).stats.maxMp + p0.GetEquipped(Slots.Weapon).stats.maxMp;
        Assert.Equal(RpgMath.Round(raw * 1.3), p15.MaxMp);
    }

    [Fact]
    public void OldSaveStarterOffhandPlusSevenMapsToPlusSevenRow()
    {
        Item raw = ItemFactory.CreateStarterItem(Slots.Offhand, "warrior");
        raw.enhance = 7; raw.name = "Shield"; // pre-rename save
        Item loaded = ItemFactory.Sanitize(raw);
        var p = new PlayerState("warrior");
        p.SetEquippedRaw(Slots.Offhand, loaded);
        p.RecomputeStats();
        Assert.Equal(7, p.Second.Level);
        Assert.Equal(0.30 * SecondWeapon.F(7), p.Second.DefPct, 12);
        Assert.Equal("starter-offhand", loaded.uid);
    }

    [Fact]
    public void OffhandEnhanceNoLongerScalesBaseStat()
    {
        Item o = ItemFactory.CreateStarterItem(Slots.Offhand, "warrior");
        o.enhance = 15;
        Assert.Equal(o.stats.def, EnhanceStats.Effective(o).def);
        Assert.Equal(0, EnhanceStats.OffhandScale);
    }

    [Fact]
    public void PreviewListsSecondWeaponStatsForOffhandAndAtkForWeapon()
    {
        Item o = ItemFactory.CreateStarterItem(Slots.Offhand, "assassin");
        o.enhance = 6;
        string text = ItemInfo.EnhancePreview(o);
        Assert.StartsWith("+6 -> +7", text);
        Assert.Contains("Crit rate +1.8% -> +2.4%", text);
        Assert.Contains("Crit damage +9.1% -> +12.1%", text);
        Assert.Contains("ATK", ItemInfo.EnhancePreview(ItemFactory.CreateStarterItem(Slots.Weapon, "assassin")));
    }
}
