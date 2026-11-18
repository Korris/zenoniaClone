using Xunit;
using ZenoniaProto.Rpg;

/// <summary>The Elder's "First Hunt": any 5 kills, then the class starter hat (hat sources, user decision 19/11/2026).</summary>
public class NewbieQuestTests
{
    static readonly QuestDef Q = QuestCatalog.Get("first-hunt");

    [Fact]
    public void TheElderOffersItFirst()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        Assert.Same(Q, p.Quests.TalkAbout("elderRowan"));
        Assert.True(Q.RewardStarterHat);
        Assert.Equal(5, Q.Count);
    }

    [Fact]
    public void AnyMonsterCountsOnceTaken()
    {
        PlayerState p = PlayerState.NewGame("wizard");
        Assert.False(p.Quests.CountKill("hornRabbit")); // not taken yet
        p.AcceptQuest(Q.Id);
        foreach (string m in new[] { "hornRabbit", "meadowSlime", "frostfangKing", "iceImp" }) Assert.True(p.Quests.CountKill(m));
        Assert.Equal(QuestState.Active, p.Quests.StateOf(Q));
        p.Quests.CountKill("rimeclawTyrant");
        Assert.Equal(QuestState.ReadyToTurnIn, p.Quests.StateOf(Q));
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
        p.AcceptQuest(Q.Id);
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
        p.AcceptQuest(Q.Id);
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
