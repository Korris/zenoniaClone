using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Taurus V4 (24/11/2026): no save migration. A save below MinSupportedVersion keeps only who the character is (class, name, look) and becomes a new game on the Taurus start map.</summary>
public class SaveResetTests
{
    static string OldSave(int version)
    {
        PlayerState p = PlayerState.NewGame("wizard");
        p.Level = 30; p.Gold = 5000; p.UpgradeStone = 9;
        SaveData d = SaveSerializer.Capture(p, true, 123f, 456f, true, 77, "frostHollow", new System.Collections.Generic.List<string> { "frostHollow" });
        d.version = version; d.name = "Aria";
        return SaveSerializer.ToJson(d);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(3)] [InlineData(6)]
    public void AnOlderSaveBecomesANewCharacterOfTheSameClassAndName(int version)
    {
        SaveData d = SaveSerializer.Parse(OldSave(version));
        Assert.NotNull(d);
        Assert.Equal(SaveSerializer.SaveVersion, d.version);
        Assert.Equal("wizard", d.classId); Assert.Equal("Aria", d.name);
        Assert.Equal(1, d.level); Assert.Equal(0, d.gold); Assert.Equal(0, d.upgradeStone);
        Assert.False(d.hasPosition); Assert.False(d.bossDefeated); Assert.Empty(d.defeatedBossMaps);
        Assert.Equal(WorldFlags.StartMapId, d.mapId);
        Assert.Equal(1, SaveSerializer.Restore(d).Level);
    }

    [Fact]
    public void ACurrentSaveIsLeftAlone()
    {
        SaveData d = SaveSerializer.Parse(OldSave(SaveSerializer.SaveVersion));
        Assert.Equal(30, d.level); Assert.Equal(5000, d.gold); Assert.True(d.hasPosition); Assert.Equal("frostHollow", d.mapId);
    }

    [Fact]
    public void ANewerOrBrokenSaveIsStillRejected()
    {
        Assert.Null(SaveSerializer.Parse(OldSave(SaveSerializer.SaveVersion + 1)));
        Assert.Null(SaveSerializer.Parse("not json"));
    }

    [Fact]
    public void TheStartMapIsTheTaurusVillage_AndWantedBossIsOffWithoutTheLegacyFlag()
    {
        Assert.Equal("tauWillowbrookVillage", WorldFlags.StartMapId);
        Assert.Equal(WorldFlags.LegacyEnabled, WantedBoss.Enabled);
        Assert.Equal(4 + 7 + 3, QuestCatalog.Live.Length); // Taurus: Rowan trial, Bram first upgrade, Orin first deck, Orin first duel + the 7-step meadow chain (phase 05) + the 3-step shrine chain (phase 06)
        Assert.NotEmpty(QuestCatalog.Legacy);
    }
}
