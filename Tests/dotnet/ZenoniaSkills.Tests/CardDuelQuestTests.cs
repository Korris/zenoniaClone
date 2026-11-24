using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>
/// Taurus V4 Card Master Orin chain: "Orin's First Deck" (after Rowan's trial: 5 kills -> the 5 starter cards) then "The First Card Duel" (must WIN; a loss / draw / give-up replays for free;
/// the winner picks ONE of Orin's 8 Common cards, once per character, the index checked against the authority's own list).
/// </summary>
public class CardDuelQuestTests
{
    static readonly QuestDef Deck = TaurusQuests.FirstDeck, Duel = CardDuelQuest.First;

    /// <summary>A character that finished Rowan's trial (the gate of Orin's first quest).</summary>
    static PlayerState AfterTrial()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        Assert.True(p.AcceptQuest(TaurusQuests.TrialId).Ok);
        for (int i = 0; i < 5; i++) p.Quests.CountKill("tauCloverSlime");
        Assert.True(p.TurnInQuest(TaurusQuests.TrialId).Ok);
        return p;
    }

    /// <summary>A character holding the 5 starter cards (first-deck Done) with the duel quest accepted.</summary>
    static PlayerState DuelActive()
    {
        PlayerState p = AfterTrial();
        Assert.True(p.AcceptQuest(Deck.Id).Ok);
        for (int i = 0; i < 5; i++) p.Quests.CountKill("tauCloverSlime");
        Assert.True(p.TurnInQuest(Deck.Id).Ok);
        Assert.True(p.AcceptQuest(Duel.Id).Ok);
        return p;
    }

    [Fact]
    public void BothQuestsAreLiveAndOrinGivesThemAfterRowansTrial()
    {
        Assert.Contains(Deck, QuestCatalog.Live);
        Assert.Contains(Duel, QuestCatalog.Live);
        Assert.DoesNotContain(Duel, QuestCatalog.Legacy);
        Assert.Equal(TaurusQuests.TrialId, Deck.Requires);
        Assert.Equal(TaurusQuests.FirstDeckId, Duel.Requires);
        Assert.Equal("cardMaster", Deck.GiverNpcId);
        Assert.Equal("cardMaster", Duel.GiverNpcId);
        Assert.True(Duel.CardDuel);
        Assert.False(Duel.Counts("tauCloverSlime")); // kills never move the duel quest
        Assert.True(Deck.Counts("tauCloverSlime") && Deck.Counts("anythingElse")); // any 5 monsters
        Assert.Equal(5, Deck.Count);
        Assert.Equal("Win a card duel against Orin", Duel.Objective);
    }

    [Fact]
    public void OrinShowsNothingBeforeRowansTrialThenAMarkThenTheDeckQuestFirst()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        Assert.Equal(QuestMarker.None, p.Quests.MarkerFor("cardMaster"));
        Assert.False(p.AcceptQuest(Deck.Id).Ok);
        Assert.False(p.AcceptQuest(Duel.Id).Ok);
        p = AfterTrial();
        Assert.Equal(QuestMarker.Available, p.Quests.MarkerFor("cardMaster"));
        Assert.Same(Deck, p.Quests.TalkAbout("cardMaster"));
        Assert.False(p.AcceptQuest(Duel.Id).Ok); // the duel waits for the deck
        Assert.True(p.AcceptQuest(Deck.Id).Ok);
    }

    [Fact]
    public void FiveKillsOfAnyMonsterPayExactlyTheFiveStarterCardsOnce()
    {
        PlayerState p = AfterTrial();
        Assert.True(p.AcceptQuest(Deck.Id).Ok);
        for (int i = 0; i < 4; i++) p.Quests.CountKill(i % 2 == 0 ? "tauCloverSlime" : "tauHornRabbit");
        Assert.False(p.TurnInQuest(Deck.Id).Ok);
        p.Quests.CountKill("tauReedFrog");
        Assert.Equal(QuestState.ReadyToTurnIn, p.Quests.StateOf(Deck));
        Assert.Equal(QuestMarker.Ready, p.Quests.MarkerFor("cardMaster"));
        Assert.True(p.TurnInQuest(Deck.Id).Ok);
        Assert.Equal(new[] { "tauCloverSlime", "tauHornRabbit", "tauReedFrog", "tauMossMushroom", "tauLeafMantis" }, TaurusQuests.StarterCards);
        foreach (string id in TaurusQuests.StarterCards) { Assert.Equal(1, p.Cards.Count(id)); Assert.True(p.Cards.InAlbum(id)); Assert.Equal(CardRarity.N, CardTable.Get(id).Rarity); }
        Assert.Equal(5, p.Cards.AlbumCount);
        Assert.False(p.TurnInQuest(Deck.Id).Ok); // once
        Assert.Equal(1, p.Cards.Count("tauCloverSlime"));
    }

    [Fact]
    public void TheDuelQuestIsOfferedAfterTheDeckAndOnlyAWinMovesIt()
    {
        PlayerState p = DuelActive();
        Assert.Equal(QuestState.Active, p.Quests.StateOf(Duel));
        Assert.False(p.TurnInQuest(Duel.Id, 0).Ok); // not won yet
        // a loss, draw, give-up or dropped duel never calls CountCardDuel: the quest stays Active and the duel can be played again
        Assert.Equal(CardDuelQuest.EndNote.CutShort, CardDuelQuest.NoteFor(p.Quests.StateOf(Duel)));
        Assert.True(p.Quests.CountCardDuel()); // the win
        Assert.Equal(QuestState.ReadyToTurnIn, p.Quests.StateOf(Duel));
        Assert.Equal(CardDuelQuest.EndNote.HandIn, CardDuelQuest.NoteFor(p.Quests.StateOf(Duel)));
        Assert.Equal(QuestMarker.Ready, p.Quests.MarkerFor("cardMaster"));
    }

    [Fact]
    public void OrinsEightCardsAreCommonAndThreeAreNotInTheStarterDeck()
    {
        Assert.Equal(8, Duel.RewardPickCards.Length);
        Assert.Equal(8, Duel.RewardPickCards.Distinct().Count());
        foreach (string id in Duel.RewardPickCards) Assert.Equal(CardRarity.N, CardTable.Get(id).Rarity); // no Rare: the choice would be no choice
        Assert.Equal(new[] { "tauBrambleBoar", "tauRootling", "tauAmberSporeling" }.OrderBy(x => x), Duel.RewardPickCards.Except(TaurusQuests.StarterCards).OrderBy(x => x));
        Assert.Contains("1 card of your choice", Duel.RewardText("warrior"));
        Assert.Equal(0, Duel.RewardGold);
        Assert.Null(Duel.RewardCards);
    }

    [Fact]
    public void PickingPaysTheChosenCardOnceAndARepeatOrBadIndexIsRefused()
    {
        PlayerState p = DuelActive();
        p.Quests.CountCardDuel();
        int cards = p.Cards.OwnedKinds;
        Assert.False(p.TurnInQuest(Duel.Id, 8).Ok);   // past the list
        Assert.False(p.TurnInQuest(Duel.Id, 255).Ok);
        Assert.False(p.TurnInQuest(Duel.Id, -1).Ok);
        Assert.Equal(QuestState.ReadyToTurnIn, p.Quests.StateOf(Duel)); // a refused pick closes nothing
        Assert.Equal(cards, p.Cards.OwnedKinds);
        int pick = System.Array.IndexOf(Duel.RewardPickCards, "tauBrambleBoar");
        Assert.True(pick >= 0);
        Assert.True(p.TurnInQuest(Duel.Id, pick).Ok);
        Assert.Equal(1, p.Cards.Count("tauBrambleBoar"));
        Assert.Equal(cards + 1, p.Cards.OwnedKinds); // exactly one new kind, no other card
        Assert.Equal(QuestState.Done, p.Quests.StateOf(Duel));
        Assert.False(p.TurnInQuest(Duel.Id, pick).Ok);                       // the same request again
        Assert.False(p.TurnInQuest(Duel.Id, System.Array.IndexOf(Duel.RewardPickCards, "tauRootling")).Ok); // or another pick
        Assert.Equal(1, p.Cards.Count("tauBrambleBoar"));
        Assert.Equal(0, p.Cards.Count("tauRootling"));
        Assert.False(p.Quests.CountCardDuel()); // a replayed win does nothing
    }

    [Fact]
    public void ChoosingACardYouAlreadyOwnJustAddsACopy()
    {
        PlayerState p = DuelActive();
        p.Quests.CountCardDuel();
        Assert.True(p.TurnInQuest(Duel.Id, System.Array.IndexOf(Duel.RewardPickCards, "tauHornRabbit")).Ok);
        Assert.Equal(2, p.Cards.Count("tauHornRabbit"));
    }

    [Fact]
    public void BorrowedCardsAreNeverInTheCollection()
    {
        PlayerState p = DuelActive();
        foreach (string id in DuelTutorialScript.Borrowed) { Assert.Equal(0, p.Cards.Count(id)); Assert.False(p.Cards.InAlbum(id)); }
        // playing the whole tutorial (the session builds its deck from the script, not from the collection) changes nothing in it
        DuelNpcSession s = DuelNpcSession.Tutorial(0);
        s.Forfeit();
        Assert.NotNull(s.TakeResult());
        foreach (string id in DuelTutorialScript.Borrowed) Assert.Equal(0, p.Cards.Count(id));
        Assert.Equal(5, p.Cards.OwnedKinds);
        Assert.Equal(CardDuelQuest.EndNote.CutShort, CardDuelQuest.NoteFor(p.Quests.StateOf(Duel)));
    }

    [Fact]
    public void ARealWinOfTheTutorialIsTheOnlyResultThatCountsAsAWin()
    {
        // give-up = a loss for the player
        DuelNpcSession gaveUp = DuelNpcSession.Tutorial(0);
        gaveUp.Forfeit();
        Assert.Equal(DuelResult.Lose, gaveUp.TakeResult());
        Assert.Null(gaveUp.TakeResult()); // once only
        // a played-out game ends Win / Lose / Draw and TakeResult says which (the owner moves the quest on Win only)
        DuelNpcSession played = DuelNpcSession.Tutorial(0);
        long now = 0;
        for (int guard = 0; guard < 200 && !played.Finished; guard++)
        {
            now += 5000;
            played.Tick(now);
            if (played.Finished || played.Match.ToMove != DuelNpcSession.Player) continue;
            DuelScriptStep step = played.CurrentStep;
            int h, cell;
            if (step != null) { h = played.Match.Hand(0).IndexOf(CardTable.Index(step.CardId)); cell = step.Cell; }
            else if (!DuelNpcAi.Best(played.Match.Board, played.Match.Hand(0), 0, null, true, out h, out cell)) break;
            Assert.True(played.PlayerPlace(h, cell, now).Ok);
        }
        Assert.True(played.Finished);
        DuelResult? r = played.TakeResult();
        Assert.True(r == DuelResult.Win || r == DuelResult.Lose || r == DuelResult.Draw);
    }

    [Fact]
    public void TheTutorialIsEasyForAPlayerWhoFollowsTheScriptAndTheHint()
    {
        // BIBLE Q04: most newcomers must win within 1-2 tries. The Hint button = DuelNpcAi careful mode.
        int win = 0, lose = 0;
        for (ulong seed = 1; seed <= 200; seed++)
        {
            DuelNpcSession t = DuelNpcSession.Tutorial(0, seed);
            long now = 0;
            for (int guard = 0; guard < 100 && !t.Finished; guard++)
            {
                now += 5000;
                t.Tick(now);
                if (t.Finished || t.Match.ToMove != DuelNpcSession.Player) continue;
                DuelScriptStep step = t.CurrentStep;
                int h, cell;
                if (step != null) { h = t.Match.Hand(0).IndexOf(CardTable.Index(step.CardId)); cell = step.Cell; }
                else if (!DuelNpcAi.Best(t.Match.Board, t.Match.Hand(0), 0, null, true, out h, out cell)) break;
                Assert.True(t.PlayerPlace(h, cell, now).Ok);
            }
            DuelResult? r = t.TakeResult();
            if (r == DuelResult.Win) win++; else if (r == DuelResult.Lose) lose++;
        }
        Assert.True(win >= 140, "won " + win + " of 200"); // about 80 % measured; draws replay for free
        Assert.True(lose <= 30, "lost " + lose + " of 200");
    }

    [Fact]
    public void QuestStatesSurviveASave()
    {
        PlayerState p = DuelActive();
        p.Quests.CountCardDuel();
        PlayerState back = SaveSerializer.Restore(SaveSerializer.Capture(p, false, 0f, 0f, false));
        Assert.Equal(QuestState.Done, back.Quests.StateOf(Deck));
        Assert.Equal(QuestState.ReadyToTurnIn, back.Quests.StateOf(Duel));
        Assert.True(back.Quests.Unlocked(Duel));
        Assert.Equal(1, back.Cards.Count("tauMossMushroom"));
        var raw = new List<QuestEntry> { new QuestEntry { id = Duel.Id, state = (int)QuestState.ReadyToTurnIn, progress = 1 } };
        Assert.Single(QuestLog.Sanitize(raw));
    }

    [Fact]
    public void PvpRiskHintBitIsAccepted()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        Assert.True(CardOps.MarkHelp(p, CardCollection.HelpPvpRisk).Ok);
        Assert.Equal(2, p.Cards.Help);
        Assert.True(CardOps.MarkHelp(p, 3).Ok);
        Assert.True(CardOps.MarkHelp(p, CardCollection.HelpGuidance).Ok);
        Assert.False(CardOps.MarkHelp(p, 8).Ok);
    }

    [Fact]
    public void EndNoteFollowsTheQuestState()
    {
        Assert.Equal(CardDuelQuest.EndNote.CutShort, CardDuelQuest.NoteFor(QuestState.Active));
        Assert.Equal(CardDuelQuest.EndNote.HandIn, CardDuelQuest.NoteFor(QuestState.ReadyToTurnIn));
        Assert.Equal(CardDuelQuest.EndNote.Practice, CardDuelQuest.NoteFor(QuestState.Done));
    }
}
