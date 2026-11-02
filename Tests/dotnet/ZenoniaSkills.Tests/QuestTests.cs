using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

public class QuestTests
{
    static readonly QuestDef Q = QuestCatalog.All[0];

    static PlayerState Hero() { return PlayerState.NewGame("warrior"); }

    static void KillAll(PlayerState p) { for (int i = 0; i < Q.Count; i++) p.Quests.CountKill(Q.MonsterType); }

    [Fact]
    public void EveryQuestHasAGiverThatExistsAndARealGoal()
    {
        var ids = new HashSet<string>();
        foreach (QuestDef q in QuestCatalog.All)
        {
            Assert.True(ids.Add(q.Id), q.Id);
            Assert.False(string.IsNullOrEmpty(q.GiverNpcId), q.Id);
            Assert.True(q.Count > 0, q.Id);
        }
    }

    [Fact]
    public void MarkerFollowsTheQuest()
    {
        PlayerState p = Hero();
        Assert.Equal(QuestMarker.Available, p.Quests.MarkerFor(Q.GiverNpcId));
        Assert.Equal(QuestMarker.None, p.Quests.MarkerFor("villagerMia"));
        Assert.True(p.AcceptQuest(Q.Id).Ok);
        Assert.Equal(QuestMarker.None, p.Quests.MarkerFor(Q.GiverNpcId));
        KillAll(p);
        Assert.Equal(QuestMarker.Ready, p.Quests.MarkerFor(Q.GiverNpcId));
        Assert.True(p.TurnInQuest(Q.Id).Ok);
        Assert.Equal(QuestMarker.None, p.Quests.MarkerFor(Q.GiverNpcId));
    }

    [Fact]
    public void OnlyTheRightMonsterCountsAndOnlyOnceTaken()
    {
        PlayerState p = Hero();
        Assert.False(p.Quests.CountKill(Q.MonsterType));
        Assert.Equal(0, p.Quests.ProgressOf(Q));
        p.AcceptQuest(Q.Id);
        Assert.False(p.Quests.CountKill("someOtherMonster"));
        Assert.True(p.Quests.CountKill(Q.MonsterType));
        Assert.Equal(1, p.Quests.ProgressOf(Q));
        Assert.Equal(QuestState.Active, p.Quests.StateOf(Q));
    }

    [Fact]
    public void KillsPastTheGoalChangeNothing()
    {
        PlayerState p = Hero();
        p.AcceptQuest(Q.Id);
        KillAll(p);
        Assert.Equal(QuestState.ReadyToTurnIn, p.Quests.StateOf(Q));
        Assert.False(p.Quests.CountKill(Q.MonsterType));
        Assert.Equal(Q.Count, p.Quests.ProgressOf(Q));
    }

    [Fact]
    public void RewardIsPaidOnceAndOnlyWhenFinished()
    {
        PlayerState p = Hero();
        int gold = p.Gold, stones = p.UpgradeStone, potions = p.HpPotions;
        Assert.False(p.TurnInQuest(Q.Id).Ok); // not taken
        p.AcceptQuest(Q.Id);
        Assert.False(p.TurnInQuest(Q.Id).Ok); // not finished
        Assert.Equal(gold, p.Gold);
        KillAll(p);
        Assert.True(p.TurnInQuest(Q.Id).Ok);
        Assert.Equal(gold + Q.RewardGold, p.Gold);
        Assert.Equal(stones + Q.RewardStones, p.UpgradeStone);
        Assert.Equal(potions + Q.RewardHpPotions, p.HpPotions);
        Assert.False(p.TurnInQuest(Q.Id).Ok); // a repeated request pays nothing
        Assert.Equal(gold + Q.RewardGold, p.Gold);
    }

    [Fact]
    public void AQuestCannotBeTakenTwiceOrAfterItIsDone()
    {
        PlayerState p = Hero();
        Assert.False(p.AcceptQuest("no-such-quest").Ok);
        Assert.True(p.AcceptQuest(Q.Id).Ok);
        p.Quests.CountKill(Q.MonsterType);
        Assert.False(p.AcceptQuest(Q.Id).Ok);
        Assert.Equal(1, p.Quests.ProgressOf(Q)); // a second accept does not start the count over
        for (int i = 1; i < Q.Count; i++) p.Quests.CountKill(Q.MonsterType);
        p.TurnInQuest(Q.Id);
        Assert.False(p.AcceptQuest(Q.Id).Ok);
    }

    [Fact]
    public void TheGiverMustBeInReach()
    {
        Assert.Null(QuestCatalog.Refusal(Q.Id, QuestCatalog.ReachUnits));
        Assert.NotNull(QuestCatalog.Refusal(Q.Id, QuestCatalog.ReachUnits + 0.1f));
        Assert.NotNull(QuestCatalog.Refusal(Q.Id, float.MaxValue)); // not on this map
        Assert.NotNull(QuestCatalog.Refusal("no-such-quest", 0f));
    }

    [Fact]
    public void ProgressSurvivesASaveAndALoad()
    {
        PlayerState p = Hero();
        p.AcceptQuest(Q.Id);
        p.Quests.CountKill(Q.MonsterType);
        SaveData d = SaveSerializer.Capture(p, false, 0f, 0f, false);
        PlayerState back = SaveSerializer.Restore(d);
        Assert.Equal(QuestState.Active, back.Quests.StateOf(Q));
        Assert.Equal(1, back.Quests.ProgressOf(Q));
        p.Quests.CountKill(Q.MonsterType);
        Assert.Equal(1, back.Quests.ProgressOf(Q)); // the save shares nothing with the live log
    }

    [Fact]
    public void AnOldSaveHasEverythingAvailable()
    {
        Assert.Empty(QuestLog.Sanitize(null));
        var log = new QuestLog();
        log.Load(null);
        Assert.Equal(QuestState.Available, log.StateOf(Q));
    }

    [Fact]
    public void AHandEditedSaveIsCleanedUp()
    {
        List<QuestEntry> clean = QuestLog.Sanitize(new List<QuestEntry>
        {
            null,
            new QuestEntry { id = "no-such-quest", state = 1 },
            new QuestEntry { id = Q.Id, state = 0 },
            new QuestEntry { id = Q.Id, state = 99 },
            new QuestEntry { id = Q.Id, state = 2, progress = 0 }, // claims finished with nothing killed
            new QuestEntry { id = Q.Id, state = 3 }, // a repeat of a quest already listed
        });
        Assert.Single(clean);
        Assert.Equal((int)QuestState.Active, clean[0].state);

        clean = QuestLog.Sanitize(new List<QuestEntry> { new QuestEntry { id = Q.Id, state = 1, progress = 9999 } });
        Assert.Equal(Q.Count, clean[0].progress);
        Assert.Equal((int)QuestState.ReadyToTurnIn, clean[0].state); // never stuck Active with the goal already met
    }
}
