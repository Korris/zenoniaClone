using System;
using System.IO;
using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

// Offline saves per character slot (SaveStore, zenonia-save-slot<N>.json): old one-per-class saves move into slots with .migrated backups,
// several heroes of one class live side by side, and a new hero's name follows the online rules and must be free on the device.
// Every test runs on its own temp folder through SaveStore.RootOverride (never the player's saves). One class = run one after another (static store).
public class OfflineSaveSlotsTests : IDisposable
{
    readonly string root = Path.Combine(Path.GetTempPath(), "zenonia-slots-" + Guid.NewGuid().ToString("N"));

    public OfflineSaveSlotsTests()
    {
        Directory.CreateDirectory(root);
        SaveStore.RootOverride = root;
        SaveStore.Enabled = true;
    }

    public void Dispose()
    {
        SaveStore.RootOverride = null;
        try { Directory.Delete(root, true); } catch (IOException) { }
    }

    static SaveData Hero(string classId, int level, string name = null)
    {
        PlayerState p = PlayerState.NewGame(classId);
        p.Level = level;
        p.Gold = level * 100;
        SaveData d = SaveSerializer.Capture(p, true, 120f, 64f, false, 1, "oakhavenVillage");
        d.name = name;
        return d;
    }

    /// <summary>An old save file as builds before slots wrote it (no name), plus its slot file when slot &gt;= 0. Returns the exact text written.</summary>
    string Legacy(string classId, int level, int slot = -1)
    {
        string text = SaveSerializer.ToJson(Hero(classId, level));
        File.WriteAllText(SaveStore.LegacyPathFor(classId), text);
        if (slot >= 0) File.WriteAllText(SaveStore.LegacySlotPathFor(classId), slot.ToString());
        return text;
    }

    [Fact]
    public void OldClassSavesMoveIntoSlotsWithBackups()
    {
        string warrior = Legacy("warrior", 30, 3);
        string wizard = Legacy("wizard", 12);           // no slot kept: first free
        string cleric = Legacy("cleric", 7, 3);         // same kept slot as the warrior: the warrior (class order) keeps it, the cleric takes a free one

        Assert.Equal(3, SaveStore.MigrateLegacy());

        Assert.Equal(warrior, File.ReadAllText(SaveStore.PathForSlot(3)));  // byte for byte
        Assert.Equal(wizard, File.ReadAllText(SaveStore.PathForSlot(0)));
        Assert.Equal(cleric, File.ReadAllText(SaveStore.PathForSlot(1)));
        foreach (string id in new[] { "warrior", "wizard", "cleric" })
        {
            Assert.False(File.Exists(SaveStore.LegacyPathFor(id)));
            Assert.True(File.Exists(SaveStore.LegacyPathFor(id) + SaveStore.MigratedSuffix)); // the old file is kept as a backup
        }
        Assert.Equal(warrior, File.ReadAllText(SaveStore.LegacyPathFor("warrior") + SaveStore.MigratedSuffix));
        Assert.True(File.Exists(SaveStore.LegacySlotPathFor("warrior") + SaveStore.MigratedSuffix));
        Assert.False(File.Exists(SaveStore.LegacySlotPathFor("cleric")));

        var all = SaveStore.LoadAll();
        Assert.Equal(new[] { 0, 1, 3 }, all.Select(e => e.Key).ToArray());
        SaveData w = SaveStore.Load(3);
        Assert.Equal("warrior", w.classId);
        Assert.Equal(30, w.level);
        Assert.Equal(3000, w.gold);
        Assert.Equal("Warrior", SaveStore.DisplayName(w)); // unnamed old heroes show their class name
        Assert.Equal(0, SaveStore.MigrateLegacy());          // nothing left to move
    }

    [Fact]
    public void ReadingSavesMigratesByItself()
    {
        Legacy("mechanic", 23, 4);
        var all = SaveStore.LoadAll();
        Assert.Single(all);
        Assert.Equal(4, all[0].Key);
        Assert.Equal(23, all[0].Value.level);
    }

    [Fact]
    public void AKeptSlotAlreadyUsedFallsBackToTheFirstFreeOne()
    {
        SaveStore.Write(Hero("assassin", 5, "Shade"), 2);
        SaveStore.Write(Hero("assassin", 6, "Vex"), 0);
        Legacy("wizard", 9, 2);
        SaveStore.MigrateLegacy();
        Assert.Equal("wizard", SaveStore.Load(1).classId);
        Assert.Equal("Shade", SaveStore.Load(2).name); // the slot save was not touched
    }

    [Fact]
    public void ACrashBetweenCopyAndRenameDoesNotDuplicate()
    {
        string text = Legacy("warrior", 15, 1);
        File.WriteAllText(SaveStore.PathForSlot(1), text); // copied, then the game died before renaming
        SaveStore.MigrateLegacy();
        Assert.Single(SaveStore.LoadAll());
        Assert.False(File.Exists(SaveStore.LegacyPathFor("warrior")));
    }

    [Fact]
    public void WithAllSixSlotsUsedTheOldSaveStaysWhereItIs()
    {
        for (int s = 0; s < SaveStore.MaxSlots; s++) SaveStore.Write(Hero("warrior", 2 + s, "Hero" + s), s);
        Legacy("cleric", 40);
        Assert.Equal(0, SaveStore.MigrateLegacy());
        Assert.True(File.Exists(SaveStore.LegacyPathFor("cleric"))); // not lost: tried again once a slot is free
        Assert.Equal(-1, SaveStore.FirstFreeSlot());
        SaveStore.Delete(5);
        Assert.Equal("cleric", SaveStore.Load(5).classId);
        Assert.False(File.Exists(SaveStore.LegacyPathFor("cleric")));
    }

    [Fact]
    public void TwoHeroesOfTheSameClassKeepTheirOwnSaves()
    {
        Assert.True(SaveStore.Write(Hero("warrior", 10, "Aldo"), 0));
        Assert.True(SaveStore.Write(Hero("warrior", 25, "Brann"), 4));

        var all = SaveStore.LoadAll();
        Assert.Equal(2, all.Count);
        Assert.All(all, e => Assert.Equal("warrior", e.Value.classId));
        Assert.Equal("Aldo", SaveStore.Load(0).name);
        Assert.Equal(10, SaveStore.Load(0).level);
        Assert.Equal("Brann", SaveStore.Load(4).name);
        Assert.Equal(25, SaveStore.Load(4).level);
        Assert.Equal(0, SaveStore.SlotOfClass("warrior"));
        Assert.Equal(1, SaveStore.FirstFreeSlot());

        SaveData brann = SaveStore.Load(4);
        brann.level = 26;
        SaveStore.Write(brann, 4);
        Assert.Equal(10, SaveStore.Load(0).level); // writing one never touches the other

        SaveStore.Delete(0);
        Assert.Null(SaveStore.Load(0));
        Assert.Equal(26, SaveStore.Load(4).level);
    }

    [Fact]
    public void NewNamesFollowTheOnlineRulesAndMustBeFreeOnTheDevice()
    {
        SaveStore.Write(Hero("wizard", 3, "Merlin"), 1);
        Legacy("cleric", 8); // unnamed: holds "Cleric"

        Assert.Null(SaveStore.NameProblem("Rowan"));
        Assert.Null(SaveStore.NameProblem("  Rowan_2  "));
        Assert.Equal(HeroNameRules.TakenText, SaveStore.NameProblem("merlin"));
        Assert.Equal(HeroNameRules.TakenText, SaveStore.NameProblem("CLERIC"));
        Assert.Equal(HeroNameRules.RuleText, SaveStore.NameProblem("Ro"));
        Assert.Equal(HeroNameRules.RuleText, SaveStore.NameProblem("Seventeen_letters"));
        Assert.Equal(HeroNameRules.RuleText, SaveStore.NameProblem("Bad name"));
        Assert.Equal(HeroNameRules.RuleText, SaveStore.NameProblem("Tên"));
        Assert.Equal(HeroNameRules.RuleText, SaveStore.NameProblem(""));
    }

    [Fact]
    public void TheNameSurvivesASaveRoundTripAndABrokenOneFallsBack()
    {
        SaveData d = SaveSerializer.Parse(SaveSerializer.ToJson(Hero("fighter", 4, "Kalen")));
        Assert.Equal("Kalen", d.name);
        SaveData bad = SaveSerializer.Parse(SaveSerializer.ToJson(Hero("fighter", 4, "<b>x</b>")));
        Assert.Equal("", bad.name);
        Assert.Equal("Fighter", SaveStore.DisplayName(bad));
    }

    [Fact]
    public void SavesOffTouchNothing()
    {
        SaveStore.Enabled = false;
        try
        {
            Legacy("warrior", 5);
            Assert.False(SaveStore.Write(Hero("warrior", 5, "Aldo"), 0));
            Assert.Empty(SaveStore.LoadAll());
            Assert.True(File.Exists(SaveStore.LegacyPathFor("warrior")));
            Assert.False(File.Exists(SaveStore.PathForSlot(0)));
        }
        finally { SaveStore.Enabled = true; }
    }
}
