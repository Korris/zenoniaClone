using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>The Elder's "First Hunt": any 5 kills, then the class starter hat (hat sources, user decision 19/11/2026). AutoStart: counts from a new character's first kill.</summary>
public class NewbieQuestTests
{
    static readonly QuestDef Q = QuestCatalog.Get("first-hunt");

    [Fact]
    public void ANewCharacterHasItRunningAlready()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        Assert.True(Q.AutoStart);
        Assert.True(Q.RewardStarterHat);
        Assert.Equal(5, Q.Count);
        Assert.Equal(QuestState.Active, p.Quests.StateOf(Q));
        Assert.Equal(0, p.Quests.ProgressOf(Q));
        Assert.False(p.AcceptQuest(Q.Id).Ok); // nothing to take at the Elder
        Assert.Equal(QuestState.Active, p.Quests.StateOf(Q));
        Assert.NotSame(Q, p.Quests.TalkAbout("elderRowan")); // he offers his other quest meanwhile
    }

    [Fact]
    public void TheFirstKillsCountWithoutTalkingToAnyone()
    {
        PlayerState p = PlayerState.NewGame("wizard");
        string[] kills = { "hornRabbit", "meadowSlime", "frostfangKing", "iceImp" };
        for (int i = 0; i < kills.Length; i++)
        {
            Assert.True(p.Quests.CountKill(kills[i]));
            Assert.Equal(i + 1, p.Quests.ProgressOf(Q));
        }
        Assert.Equal(QuestState.Active, p.Quests.StateOf(Q));
        p.Quests.CountKill("rimeclawTyrant");
        Assert.Equal(QuestState.ReadyToTurnIn, p.Quests.StateOf(Q));
        Assert.Same(Q, p.Quests.TalkAbout("elderRowan")); // the hand-in comes first
        Assert.Equal(QuestMarker.Ready, p.Quests.MarkerFor("elderRowan"));
        Assert.False(p.Quests.CountKill("hornRabbit")); // past the goal nothing moves
    }

    [Fact]
    public void KillsArePersistedWithTheCharacter()
    {
        PlayerState p = PlayerState.NewGame("assassin");
        for (int i = 0; i < 3; i++) p.Quests.CountKill("hornRabbit");
        SaveData d = SaveSerializer.Capture(p, false, 0f, 0f, false);
        Assert.Contains(d.quests, e => e.id == Q.Id && e.state == (int)QuestState.Active && e.progress == 3);
        PlayerState back = SaveSerializer.Restore(SaveSerializer.Parse(SaveSerializer.ToJson(d), 24));
        Assert.Equal(3, back.Quests.ProgressOf(Q));
        back.Quests.CountKill("hornRabbit"); back.Quests.CountKill("hornRabbit");
        Assert.Equal(QuestState.ReadyToTurnIn, back.Quests.StateOf(Q));
    }

    [Fact]
    public void AnOldSaveThatNeverTookItStartsIt()
    {
        var log = new QuestLog();
        log.Load(new List<QuestEntry> { new QuestEntry { id = "crystal-survey", state = (int)QuestState.Done, progress = 5 } });
        Assert.Equal(QuestState.Active, log.StateOf(Q));
        Assert.Equal(0, log.ProgressOf(Q));
        Assert.Equal(QuestState.Done, log.StateOf(QuestCatalog.Get("crystal-survey")));
        log.Load(null); // a save from before quests existed
        Assert.Equal(QuestState.Active, log.StateOf(Q));
    }

    [Theory]
    [InlineData(QuestState.Active, 2)]
    [InlineData(QuestState.ReadyToTurnIn, 5)]
    [InlineData(QuestState.Done, 5)]
    public void AnOldSaveThatTookItKeepsItsState(QuestState state, int progress)
    {
        var log = new QuestLog();
        log.Load(new List<QuestEntry> { new QuestEntry { id = Q.Id, state = (int)state, progress = progress } });
        Assert.Equal(state, log.StateOf(Q));
        Assert.Equal(progress, log.ProgressOf(Q));
        Assert.Single(log.ToList()); // no second entry
    }

    [Theory]
    [InlineData("warrior", 1)]
    [InlineData("wizard", 2)]
    [InlineData("mechanic", 3)]
    [InlineData("assassin", 4)]
    [InlineData("cleric", 5)]
    public void TurnInGivesTheClassStarterHatOnce(string cls, int hatId)
    {
        PlayerState p = PlayerState.NewGame(cls);
        for (int i = 0; i < Q.Count; i++) p.Quests.CountKill("hornRabbit");
        int stones = p.UpgradeStone;
        Assert.True(p.TurnInQuest(Q.Id).Ok);
        Item hat = p.Inventory.Find(i => i.slot == Slots.Headgear);
        Assert.Equal(hatId, HeadgearCatalog.IdOf(hat));
        Assert.Equal(Q.RewardGold, p.Gold);
        Assert.Equal(stones, p.UpgradeStone); // RewardStones 0 pays no stone
        Assert.False(p.TurnInQuest(Q.Id).Ok);
        Assert.Single(p.Inventory.FindAll(i => i.slot == Slots.Headgear));
        Assert.Equal(0, p.Appearance.equippedHeadgearId); // a gift goes to the bag; the player puts it on
    }

    [Fact]
    public void AFullBagKeepsTheQuestOpen()
    {
        PlayerState p = PlayerState.NewGame("cleric");
        for (int i = 0; i < Q.Count; i++) p.Quests.CountKill("hornRabbit");
        while (p.Inventory.Count < PlayerState.InventorySize) p.AddToInventory(HeadgearCatalog.CreateItem(HeadgearCatalog.Get(6), RpgRandom.Default));
        ActionResult r = p.TurnInQuest(Q.Id);
        Assert.False(r.Ok);
        Assert.Equal("Inventory full", r.Text);
        Assert.Equal(QuestState.ReadyToTurnIn, p.Quests.StateOf(Q));
        Assert.Equal(0, p.Gold);
    }

    [Fact]
    public void RewardTextNamesTheHat()
    {
        Assert.Equal("+30 G, Copper Goggles", Q.RewardText("mechanic"));
    }
}
