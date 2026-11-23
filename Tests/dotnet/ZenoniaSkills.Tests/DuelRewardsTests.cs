using Xunit;
using ZenoniaProto.Rpg;

// Phase 05: NPC duel pay: first win per NPC once, the daily "win 3" task, nothing for a loss / draw, saved through relog.
public class DuelRewardsTests
{
    static PlayerState Hero() { return PlayerState.NewGame("warrior"); }

    [Fact]
    public void FirstWinPaysThatNpcsGoldOnceAndEndsTheTutorial()
    {
        PlayerState p = Hero(); int gold = p.Gold;
        DuelReward r = DuelRewards.ApplyNpcResult(p, 0, DuelResult.Win, 100);
        Assert.Equal(1000, r.FirstWinGold);
        Assert.True(r.TutorialDone);
        Assert.Equal(gold + 1000, p.Gold);
        DuelReward again = DuelRewards.ApplyNpcResult(p, 0, DuelResult.Win, 100);
        Assert.Equal(0, again.FirstWinGold);
        Assert.False(again.TutorialDone);
        Assert.Equal(gold + 1000, p.Gold);
    }

    [Fact]
    public void EachNpcPaysItsOwnFirstWin()
    {
        PlayerState p = Hero(); int gold = p.Gold;
        Assert.Equal(1000, DuelRewards.ApplyNpcResult(p, 0, DuelResult.Win, 1).FirstWinGold);
        DuelReward r1 = DuelRewards.ApplyNpcResult(p, 1, DuelResult.Win, 1);
        Assert.Equal(2500, r1.FirstWinGold);
        Assert.False(r1.TutorialDone); // already done by the first NPC
        Assert.Equal(5000, DuelRewards.ApplyNpcResult(p, 2, DuelResult.Win, 2).FirstWinGold);
        Assert.Equal(gold + 8500, p.Gold); // days 1, 1, 2: never 3 wins on one day, so no daily bonus
    }

    [Fact]
    public void LossAndDrawPayNothingAndCountNothing()
    {
        PlayerState p = Hero(); int gold = p.Gold;
        Assert.Equal(0, DuelRewards.ApplyNpcResult(p, 0, DuelResult.Lose, 5).Total);
        Assert.Equal(0, DuelRewards.ApplyNpcResult(p, 0, DuelResult.Draw, 5).Total);
        Assert.Equal(0, DuelRewards.ApplyNpcResult(p, 0, DuelResult.Playing, 5).Total);
        Assert.Equal(gold, p.Gold);
        Assert.Equal(0, p.Cards.NpcFirstWins);
        Assert.Equal(0, p.Cards.Daily.npcWins);
    }

    [Fact]
    public void ThirdNpcWinOfTheDayPaysFiveHundredOnce()
    {
        PlayerState p = Hero();
        DuelRewards.ApplyNpcResult(p, 0, DuelResult.Win, 10);
        int gold = p.Gold;
        Assert.Equal(0, DuelRewards.ApplyNpcResult(p, 0, DuelResult.Win, 10).DailyGold); // win 2
        Assert.Equal(500, DuelRewards.ApplyNpcResult(p, 0, DuelResult.Win, 10).DailyGold); // win 3
        Assert.Equal(gold + 500, p.Gold);
        Assert.Equal(0, DuelRewards.ApplyNpcResult(p, 0, DuelResult.Win, 10).DailyGold); // win 4: the task is done
        Assert.Equal(4, p.Cards.Daily.npcWins);
    }

    [Fact]
    public void ANewDayRestartsTheDailyCounter()
    {
        PlayerState p = Hero();
        for (int i = 0; i < 3; i++) DuelRewards.ApplyNpcResult(p, 0, DuelResult.Win, 10);
        int gold = p.Gold;
        Assert.Equal(0, DuelRewards.ApplyNpcResult(p, 0, DuelResult.Win, 11).DailyGold);
        Assert.Equal(1, p.Cards.Daily.npcWins);
        Assert.Equal(11, p.Cards.Daily.day);
        DuelRewards.ApplyNpcResult(p, 0, DuelResult.Win, 11);
        Assert.Equal(500, DuelRewards.ApplyNpcResult(p, 0, DuelResult.Win, 11).DailyGold);
        Assert.Equal(gold + 500, p.Gold);
    }

    [Fact]
    public void UnknownNpcPaysNothing()
    {
        PlayerState p = Hero(); int gold = p.Gold;
        Assert.Equal(0, DuelRewards.ApplyNpcResult(p, 9, DuelResult.Win, 1).Total);
        Assert.Equal(gold, p.Gold);
    }

    [Fact]
    public void GoldNeverPassesTheCapAndAFirstWinThatDoesNotFitIsHeldNotLost()
    {
        PlayerState p = Hero(); p.Gold = PlayerState.GoldCap - 100;
        DuelRewards.ApplyNpcResult(p, 2, DuelResult.Win, 1);
        Assert.Equal(PlayerState.GoldCap - 100, p.Gold); // 5,000 does not fit: held, the bit stays unset for a later win
        Assert.Equal(0, p.Cards.NpcFirstWins);
        PlayerState q = Hero(); q.Gold = PlayerState.GoldCap - 100; q.Cards.Daily = new CardDailyData { day = 1, npcWins = 2 };
        DuelRewards.ApplyNpcResult(q, 0, DuelResult.Win, 1); // the third win of the day: the 500 daily bonus is cut by AddGoldCapped, never past the cap
        Assert.True(q.Gold <= PlayerState.GoldCap);
    }

    [Fact]
    public void FirstWinBitsSurviveASaveAndLoad()
    {
        PlayerState p = Hero();
        DuelRewards.ApplyNpcResult(p, 1, DuelResult.Win, 3);
        var d = new SaveData();
        p.Cards.ToSave(d);
        var again = new CardCollection().Load(d);
        Assert.Equal(DuelNpcTable.FirstWinBit(1), again.NpcFirstWins);
        Assert.Equal(1, again.Daily.npcWins);
        Assert.Equal(3, again.Daily.day);
    }

    [Fact]
    public void ASavedDeckSurvivesAndIsCutToWhatIsStillOwned()
    {
        PlayerState p = Hero();
        p.Cards.Add("meadowSlime", 1);
        p.Cards.Deck.Add("meadowSlime");
        var d = new SaveData(); p.Cards.ToSave(d);
        d.cardDeck.Add("meadowSlime"); // forged second copy
        d.cardDeck.Add("hornRabbit");  // not owned
        var again = new CardCollection().Load(d);
        Assert.Equal(new[] { "meadowSlime" }, again.Deck.ToArray());
    }
}

// the deck as the character keeps it, and the duel bound to it
public class DuelNpcRunTests
{
    static PlayerState HeroWithDeck()
    {
        PlayerState p = PlayerState.NewGame("warrior");
        foreach (string id in DuelNpcTable.Get(0).Deck) p.Cards.Add(id, 1);
        Assert.Equal(DeckError.None, DeckRules.Save(p, DuelNpcTable.Get(0).Deck));
        return p;
    }

    [Fact]
    public void SaveDeckReplacesTheDeckOnlyWhenAllowed()
    {
        PlayerState p = HeroWithDeck();
        Assert.Equal(8, p.Cards.Deck.Count);
        Assert.Equal(DeckError.NotEnough, DeckRules.Save(p, new[] { "frostfangKing" })); // not owned
        Assert.Equal(8, p.Cards.Deck.Count); // unchanged
        Assert.Equal(DeckError.None, DeckRules.Save(p, new[] { "meadowSlime", "hornRabbit" }));
        Assert.Equal(2, p.Cards.Deck.Count);
        Assert.Equal(DeckError.None, DeckRules.Save(p, p.Cards.Deck)); // saving the deck onto itself is fine
        Assert.Equal(2, p.Cards.InDeck("meadowSlime") + p.Cards.InDeck("hornRabbit"));
    }

    [Fact]
    public void ADuelNeedsAFullValidDeck()
    {
        PlayerState p = HeroWithDeck();
        DeckRules.Save(p, new[] { "meadowSlime" });
        DeckError e;
        Assert.Null(DuelNpcRun.Start(p, 0, 1, 0, out e));
        Assert.Equal(DeckError.NotFull, e);
        DeckRules.Save(p, DuelNpcTable.Get(0).Deck);
        Assert.NotNull(DuelNpcRun.Start(p, 0, 1, 0, out e));
        Assert.Null(DuelNpcRun.Start(p, 5, 1, 0, out e)); // no such NPC
    }

    [Fact]
    public void AWinIsPaidOnceAndALossNever()
    {
        PlayerState p = HeroWithDeck();
        int gold = p.Gold;
        DeckError e;
        DuelNpcRun run = DuelNpcRun.Start(p, 0, 5, 0, out e);
        Assert.False(run.Settle(100)); // still running
        // play it out with the best side: run the NPC AI for the player too
        var rng = new DuelRng(3);
        long now = 0;
        while (!run.Finished)
        {
            now += 2000;
            if (run.Session.Match.ToMove == DuelNpcSession.Player)
            {
                int h, c; DuelNpcAi.Pick(run.Session.Match, 0, DuelNpcLevel.Hard, rng, out h, out c);
                run.Session.PlayerPlace(h, c, now);
            }
            else run.Session.Tick(now);
        }
        Assert.True(run.Settle(100));
        Assert.False(run.Settle(100)); // never twice
        DuelView v = run.View(now);
        if (v.Result == DuelResult.Win) { Assert.Equal(1000, v.RewardFirst); Assert.Equal(gold + 1000, p.Gold); }
        else { Assert.Equal(0, v.RewardFirst); Assert.Equal(gold, p.Gold); }
    }

    [Fact]
    public void AForfeitedDuelPaysNothing()
    {
        PlayerState p = HeroWithDeck();
        int gold = p.Gold;
        DeckError e;
        DuelNpcRun run = DuelNpcRun.Start(p, 0, 5, 0, out e);
        run.Session.Forfeit();
        Assert.True(run.Settle(100));
        Assert.Equal(gold, p.Gold);
        Assert.Equal(0, p.Cards.NpcFirstWins);
        Assert.Equal(DuelResult.Lose, run.View(0).Result);
    }
}
