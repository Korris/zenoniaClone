using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>"The First Card Duel": only after the first card, finished by the NPC duel, pays 300 G + 3 cards once.</summary>
public class CardDuelQuestTests
{
    static readonly QuestDef Q = QuestCatalog.Get(CardDuelQuest.Id);

    static PlayerState Hero() { return PlayerState.NewGame("warrior"); }

    [Fact]
    public void QuestIsLastInTheCatalogAndTheCardsAreCommon()
    {
        Assert.Same(Q, QuestCatalog.All[QuestCatalog.All.Length - 1]);
        Assert.True(Q.CardDuel);
        Assert.Equal(300, Q.RewardGold);
        Assert.Equal(3, Q.RewardCards.Length);
        foreach (string id in Q.RewardCards) Assert.Equal(CardRarity.N, CardTable.Get(id).Rarity);
        Assert.Contains("Horn Rabbit card", Q.RewardText("warrior"));
        Assert.Equal("Play a card duel with Orin", Q.Objective);
        Assert.False(Q.Counts("hornRabbit")); // kills never move it
    }

    [Fact]
    public void NotOfferedBeforeTheFirstCard()
    {
        PlayerState p = Hero();
        Assert.Equal(QuestMarker.None, p.Quests.MarkerFor(Q.GiverNpcId));
        Assert.Null(p.Quests.TalkAbout(Q.GiverNpcId));
        Assert.False(p.AcceptQuest(Q.Id).Ok);
        p.AddCard("hornRabbit");
        Assert.Equal(QuestMarker.Available, p.Quests.MarkerFor(Q.GiverNpcId));
        Assert.Same(Q, p.Quests.TalkAbout(Q.GiverNpcId));
        Assert.True(p.AcceptQuest(Q.Id).Ok);
    }

    [Fact]
    public void ATakenQuestStaysReachableEvenIfTheCardIsSoldLater()
    {
        PlayerState p = Hero();
        p.AddCard("hornRabbit");
        Assert.True(p.AcceptQuest(Q.Id).Ok);
        Assert.True(p.Cards.Remove("hornRabbit"));
        Assert.Same(Q, p.Quests.TalkAbout(Q.GiverNpcId)); // the album keeps the first card anyway
        Assert.Equal(QuestState.Active, p.Quests.StateOf(Q));
    }

    [Fact]
    public void DuelMovesItAndTurnInPaysOnce()
    {
        PlayerState p = Hero();
        p.AddCard("meadowSlime");
        Assert.False(p.Quests.CountCardDuel()); // not accepted: nothing moves
        Assert.True(p.AcceptQuest(Q.Id).Ok);
        Assert.False(p.TurnInQuest(Q.Id).Ok);
        Assert.True(p.Quests.CountCardDuel());
        Assert.Equal(QuestState.ReadyToTurnIn, p.Quests.StateOf(Q));
        Assert.Equal(QuestMarker.Ready, p.Quests.MarkerFor(Q.GiverNpcId));
        int gold = p.Gold;
        Assert.True(p.TurnInQuest(Q.Id).Ok);
        Assert.Equal(gold + 300, p.Gold);
        foreach (string id in Q.RewardCards) { Assert.Equal(1, p.Cards.Count(id)); Assert.True(p.Cards.InAlbum(id)); }
        Assert.False(p.TurnInQuest(Q.Id).Ok);
        Assert.False(p.Quests.CountCardDuel());
        Assert.Equal(gold + 300, p.Gold);
        Assert.Equal(1, p.Cards.Count("hornRabbit"));
    }

    [Fact]
    public void RewardCardOnAFullStackStaysAtTheCap()
    {
        PlayerState p = Hero();
        p.Cards.Add("hornRabbit", CardTable.MaxStack);
        p.AcceptQuest(Q.Id);
        p.Quests.CountCardDuel();
        Assert.True(p.TurnInQuest(Q.Id).Ok);
        Assert.Equal(CardTable.MaxStack, p.Cards.Count("hornRabbit"));
    }

    [Fact]
    public void SanitizeKeepsTheEntryAndTheStateSurvivesASave()
    {
        var raw = new List<QuestEntry> { new QuestEntry { id = Q.Id, state = (int)QuestState.ReadyToTurnIn, progress = 1 } };
        List<QuestEntry> clean = QuestLog.Sanitize(raw);
        Assert.Single(clean);
        Assert.Equal((int)QuestState.ReadyToTurnIn, clean[0].state);
        PlayerState p = Hero();
        p.AddCard("meadowSlime");
        p.AcceptQuest(Q.Id);
        p.Quests.CountCardDuel();
        PlayerState back = SaveSerializer.Restore(SaveSerializer.Capture(p, false, 0f, 0f, false));
        Assert.Equal(QuestState.ReadyToTurnIn, back.Quests.StateOf(Q));
        Assert.True(back.Quests.Unlocked(Q));
    }

    [Fact]
    public void PvpRiskHintBitIsAccepted()
    {
        PlayerState p = Hero();
        Assert.True(CardOps.MarkHelp(p, CardCollection.HelpPvpRisk).Ok);
        Assert.Equal(2, p.Cards.Help);
        Assert.True(CardOps.MarkHelp(p, 3).Ok);
        Assert.False(CardOps.MarkHelp(p, 4).Ok);
        Assert.False(CardOps.MarkHelp(p, 7).Ok);
    }

    [Fact]
    public void EndNoteFollowsTheQuestStateAndTheQuestMovesBeforeTheFinalView()
    {
        Assert.Equal(CardDuelQuest.EndNote.CutShort, CardDuelQuest.NoteFor(QuestState.Active));
        Assert.Equal(CardDuelQuest.EndNote.HandIn, CardDuelQuest.NoteFor(QuestState.ReadyToTurnIn));
        Assert.Equal(CardDuelQuest.EndNote.Practice, CardDuelQuest.NoteFor(QuestState.Done));
        PlayerState p = Hero();
        p.AddCard("meadowSlime");
        Assert.True(p.AcceptQuest(Q.Id).Ok);

        // dropped (death / hostile map / idle): Forfeit, the quest is NOT moved -> the banner says cut short
        DuelNpcSession dropped = DuelNpcSession.Tutorial(0);
        dropped.Forfeit();
        Assert.Equal(CardDuelQuest.EndNote.CutShort, CardDuelQuest.NoteFor(p.Quests.StateOf(Q)));

        // given up: the owner takes the result and moves the quest BEFORE building the final view, so the view's banner reads hand-in
        DuelNpcSession gaveUp = DuelNpcSession.Tutorial(0);
        gaveUp.Forfeit();
        Assert.NotNull(gaveUp.TakeResult());
        Assert.True(p.Quests.CountCardDuel());
        Assert.Equal(1, gaveUp.View(0).EndReason);
        Assert.Equal(CardDuelQuest.EndNote.HandIn, CardDuelQuest.NoteFor(p.Quests.StateOf(Q)));
        Assert.Null(gaveUp.TakeResult()); // once only
    }
}
