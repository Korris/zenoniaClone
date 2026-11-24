using Xunit;
using ZenoniaProto.Rpg;

/// <summary>Taurus V4 phase 02 newbie chain: Elder Rowan's "A Trial in the Meadows" (5 Clover Slimes -> 5 stones + 300 G) and Bram's "First Upgrade" (the Enhance objective).</summary>
public class TaurusNewbieQuestTests
{
    static readonly QuestDef Trial = TaurusQuests.Trial, Upgrade = TaurusQuests.FirstUpgrade;

    [Fact]
    public void BothQuestsAreLiveAndChained()
    {
        Assert.Contains(Trial, QuestCatalog.Live);
        Assert.Contains(Upgrade, QuestCatalog.Live);
        Assert.Same(Trial, QuestCatalog.Get(TaurusQuests.TrialId));
        Assert.False(Trial.AutoStart); // Rowan's mark shows from the first second, but the player takes it
        Assert.Equal("elderRowan", Trial.GiverNpcId);
        Assert.Equal("tauBram", Upgrade.GiverNpcId);
        Assert.Equal(TaurusQuests.TrialId, Upgrade.Requires);
    }

    [Fact]
    public void RowanOffersTheTrialToANewCharacterAndShowsAMark()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        Assert.True(p.Quests.Unlocked(Trial));
        Assert.Equal(QuestState.Available, p.Quests.StateOf(Trial));
        Assert.NotEqual(QuestMarker.None, p.Quests.MarkerFor("elderRowan"));
        Assert.False(p.Quests.Unlocked(Upgrade)); // Bram waits for the trial
        Assert.False(p.AcceptQuest(Upgrade.Id).Ok);
    }

    [Fact]
    public void FiveSlimeKillsAfterAcceptingPayFiveStonesAnd300GoldExactlyOnce()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        int gold = p.Gold, stones = p.UpgradeStone;
        Assert.True(p.AcceptQuest(Trial.Id).Ok);
        for (int i = 0; i < 4; i++) Assert.True(p.Quests.CountKill("tauCloverSlime"));
        Assert.Equal(QuestState.Active, p.Quests.StateOf(Trial));
        p.Quests.CountKill("hornRabbit"); // another monster does not count (the legacy first-hunt, on in the tests, counts any monster: look at this quest's own progress)
        Assert.Equal(4, p.Quests.ProgressOf(Trial));
        Assert.False(p.TurnInQuest(Trial.Id).Ok);
        p.Quests.CountKill("tauCloverSlime");
        Assert.Equal(QuestState.ReadyToTurnIn, p.Quests.StateOf(Trial));
        Assert.True(p.TurnInQuest(Trial.Id).Ok);
        Assert.Equal(gold + 300, p.Gold);
        Assert.Equal(stones + 5, p.UpgradeStone);
        Assert.False(p.TurnInQuest(Trial.Id).Ok); // once
        Assert.Equal(gold + 300, p.Gold);
        Assert.Equal(stones + 5, p.UpgradeStone);
    }

    [Fact]
    public void KillsBeforeAcceptingDoNotCount()
    {
        PlayerState p = PlayerState.NewGame("wizard");
        p.Quests.CountKill("tauCloverSlime");
        Assert.Equal(0, p.Quests.ProgressOf(Trial));
        Assert.True(p.AcceptQuest(Trial.Id).Ok);
        Assert.Equal(0, p.Quests.ProgressOf(Trial));
    }

    [Fact]
    public void FiveStonesCoverThreeSureEnhancesWithStonesToSpare()
    {
        Assert.Equal(1.0, StoneUpgradeMethod.Instance.SuccessChance(0));
        Assert.Equal(1.0, StoneUpgradeMethod.Instance.SuccessChance(2));
        Assert.True(Trial.RewardStones >= 3);
    }

    PlayerState AfterTrial()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        p.AcceptQuest(Trial.Id);
        for (int i = 0; i < 5; i++) p.Quests.CountKill("tauCloverSlime");
        p.TurnInQuest(Trial.Id);
        return p;
    }

    [Fact]
    public void ASuccessfulEnhanceAfterAcceptingFinishesBramsQuest()
    {
        PlayerState p = AfterTrial();
        Assert.True(p.Quests.Unlocked(Upgrade));
        Assert.True(p.AcceptQuest(Upgrade.Id).Ok);
        Assert.Equal(QuestState.Active, p.Quests.StateOf(Upgrade));
        p.Quests.CountKill("tauCloverSlime"); // not a kill quest
        Assert.Equal(0, p.Quests.ProgressOf(Upgrade));
        Item weapon = p.GetEquipped(Slots.Weapon);
        Assert.NotNull(weapon);
        EnhanceResult er = EnhanceSystem.Attempt(p, weapon);
        Assert.True(er.Ok && er.Success);
        Assert.True(p.Quests.CountEnhance());
        Assert.Equal(QuestState.ReadyToTurnIn, p.Quests.StateOf(Upgrade));
        int gold = p.Gold;
        Assert.True(p.TurnInQuest(Upgrade.Id).Ok);
        Assert.Equal(gold + 100, p.Gold);
        Assert.Equal(1, weapon.enhance);
    }

    [Fact]
    public void AnEnhanceBeforeTakingTheQuestCountsAtOnce()
    {
        PlayerState p = AfterTrial();
        EnhanceSystem.Attempt(p, p.GetEquipped(Slots.Weapon));
        Assert.False(p.Quests.CountEnhance()); // not accepted yet: nothing moves
        Assert.True(p.HoldsEnhancedPiece());
        Assert.True(p.AcceptQuest(Upgrade.Id).Ok);
        Assert.Equal(QuestState.ReadyToTurnIn, p.Quests.StateOf(Upgrade));
    }

    [Fact]
    public void AFailedOrMissingEnhanceDoesNotMoveIt()
    {
        PlayerState p = AfterTrial();
        p.AcceptQuest(Upgrade.Id);
        Assert.False(p.HoldsEnhancedPiece());
        Assert.Equal(QuestState.Active, p.Quests.StateOf(Upgrade));
        Assert.Equal("Enhance a weapon or off-hand once", Upgrade.Objective);
    }

    [Fact]
    public void TheEnhanceQuestSurvivesASaveRoundTrip()
    {
        PlayerState p = AfterTrial();
        p.AcceptQuest(Upgrade.Id);
        SaveData d = SaveSerializer.Capture(p, false, 0f, 0f, false);
        PlayerState back = SaveSerializer.Restore(SaveSerializer.Parse(SaveSerializer.ToJson(d), 24));
        Assert.Equal(QuestState.Active, back.Quests.StateOf(Upgrade));
        Assert.Equal(QuestState.Done, back.Quests.StateOf(Trial));
    }
}
