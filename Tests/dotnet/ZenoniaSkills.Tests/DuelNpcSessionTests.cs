using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

public class DuelNpcSessionTests
{
    static long now;

    /// <summary>Plays the player's next scripted step.</summary>
    static DuelPlay PlayStep(DuelNpcSession s)
    {
        DuelScriptStep st = s.CurrentStep;
        int h = new List<int>(s.Match.Hand(0)).IndexOf(CardTable.Index(st.CardId));
        return s.PlayerPlace(h, st.Cell, now);
    }

    static void Orin(DuelNpcSession s) { now += DuelNpcSession.ThinkMinMs + DuelNpcSession.ThinkSpanMs; Assert.True(s.Tick(now)); }

    [Fact]
    public void TutorialRunsTheScriptThenFreePlay()
    {
        now = 0;
        DuelNpcSession s = DuelNpcSession.Tutorial(now);
        Assert.Equal(1, s.ScriptStep);
        Assert.True(PlayStep(s).Ok);
        Assert.Equal(2, s.ScriptStep);
        Assert.False(s.Tick(now)); // Orin is still thinking
        Orin(s);
        Assert.True(PlayStep(s).Ok);
        Orin(s);
        Assert.Equal(3, s.ScriptStep);
        Assert.True(PlayStep(s).Ok);
        Assert.Equal(0, s.ScriptStep); // script over
        Assert.Equal(5, s.Match.Moves.Count);
        Orin(s); // turn 6: the AI
        Assert.Equal(6, s.Match.Moves.Count);
        Assert.Equal(0, s.Match.ToMove);
        Assert.True(s.View(now).Npc);
    }

    [Fact]
    public void ScriptRefusesAnyOtherMoveAndSkipFreesIt()
    {
        now = 0;
        DuelNpcSession s = DuelNpcSession.Tutorial(now);
        DuelPlay wrongCell = s.PlayerPlace(0, 15, now);
        Assert.False(wrongCell.Ok);
        Assert.Equal(DuelPlayError.Script, wrongCell.Code);
        Assert.False(s.PlayerPlace(4, 0, now).Ok); // wrong card (hand slot 4 is Forest Wolf)
        Assert.Equal(0, s.Match.Moves.Count);
        Assert.True(s.SkipScript(now));
        Assert.Equal(0, s.ScriptStep);
        Assert.False(s.SkipScript(now));
        Assert.True(s.PlayerPlace(4, 15, now).Ok); // free now
        Orin(s); // Orin uses the AI, not step 2
        Assert.Equal(2, s.Match.Moves.Count);
    }

    [Fact]
    public void NoClockForThePlayer()
    {
        now = 0;
        DuelNpcSession s = DuelNpcSession.Tutorial(now);
        Assert.False(s.Tick(now + 10 * 60 * 1000));
        Assert.Equal(0, s.Match.Moves.Count);
        Assert.Equal(0, s.View(now).MsLeft);
    }

    [Fact]
    public void OrinThinksBetweenHalfAndOneAndAQuarterSeconds()
    {
        for (ulong seed = 1; seed < 20; seed++)
        {
            now = 0;
            DuelNpcSession s = DuelNpcSession.Practice(seed, null, now);
            if (s.Match.ToMove == 0) { Assert.True(s.PlayerPlace(0, 0, now).Ok); }
            Assert.False(s.Tick(now + DuelNpcSession.ThinkMinMs - 1));
            Assert.True(s.Tick(now + DuelNpcSession.ThinkMinMs + DuelNpcSession.ThinkSpanMs));
        }
    }

    [Fact]
    public void FullDuelEndsAndTheResultComesOnce()
    {
        now = 0;
        DuelNpcSession s = DuelNpcSession.Tutorial(now);
        int guard = 0;
        while (!s.Finished && guard++ < 100)
        {
            if (s.Match.ToMove == 0)
            {
                if (s.CurrentStep != null) PlayStep(s);
                else
                {
                    int h, c;
                    Assert.True(DuelNpcAi.Best(s.Match.Board, s.Match.Hand(0), 0, null, true, out h, out c));
                    Assert.True(s.PlayerPlace(h, c, now).Ok);
                }
            }
            else { now += 2000; s.Tick(now); }
        }
        Assert.True(s.Finished);
        Assert.Equal(16, s.Match.Moves.Count);
        Assert.NotNull(s.TakeResult());
        Assert.Null(s.TakeResult());
        Assert.False(s.PlayerGaveUp);
    }

    [Fact]
    public void ForfeitEndsItAsALossAndCountsAsGivenUp()
    {
        now = 0;
        DuelNpcSession s = DuelNpcSession.Tutorial(now);
        s.Forfeit();
        Assert.True(s.Finished);
        Assert.True(s.PlayerGaveUp);
        Assert.Equal(DuelResult.Lose, s.TakeResult());
        Assert.Null(s.TakeResult());
        Assert.Equal(1, s.View(now).EndReason);
    }

    [Fact]
    public void PracticeUsesTheSavedDeckWhenValidElseTheBorrowedOne()
    {
        var cards = new CardCollection();
        Assert.Equal(DuelTutorialScript.PlayerDeck, new List<string>(DuelNpcSession.PracticeDeck(cards)));
        cards.Add("meadowSlime", 8);
        cards.Deck.AddRange(new[] { "meadowSlime", "meadowSlime", "meadowSlime", "meadowSlime", "meadowSlime", "meadowSlime", "meadowSlime", "meadowSlime" });
        Assert.Equal(cards.Deck, new List<string>(DuelNpcSession.PracticeDeck(cards)));
        cards.Deck.RemoveAt(0); // 7 cards: not a full deck
        Assert.Equal(DuelTutorialScript.PlayerDeck, new List<string>(DuelNpcSession.PracticeDeck(cards)));
        // a deck of cards the player does not own is not used either
        cards.Deck.Clear();
        cards.Deck.AddRange(DuelTutorialScript.OrinDeck);
        Assert.Equal(DuelTutorialScript.PlayerDeck, new List<string>(DuelNpcSession.PracticeDeck(cards)));
        Assert.False(DuelNpcSession.Practice(5, new[] { "gloomWolf" }, 0).Match.Finished); // a bad deck does not throw: the borrowed one is used
    }

    [Fact]
    public void PracticeIsShuffledAndFree()
    {
        DuelNpcSession a = DuelNpcSession.Practice(11, null, 0), b = DuelNpcSession.Practice(11, null, 0);
        Assert.Equal(new List<int>(a.Match.Hand(0)), new List<int>(b.Match.Hand(0)));
        Assert.Equal(0, a.ScriptStep);
        Assert.Equal(DuelNpcMode.Practice, a.Mode);
        Assert.False(a.Match.HasClock);
    }

    [Fact]
    public void IdleDropsAfterTenMinutesWithoutAPlayerAction()
    {
        now = 0;
        DuelNpcSession s = DuelNpcSession.Tutorial(now);
        Assert.False(s.Idle(DuelNpcSession.IdleLimitMs)); // exactly the limit: still fine
        Assert.True(s.Idle(DuelNpcSession.IdleLimitMs + 1));
        now = 9 * 60 * 1000L;
        Assert.True(PlayStep(s).Ok); // a player action restarts the count
        Assert.False(s.Idle(now + DuelNpcSession.IdleLimitMs));
        Assert.True(s.Idle(now + DuelNpcSession.IdleLimitMs + 1));
        long orinAt = now + 5 * 60 * 1000L;
        Assert.True(s.Tick(orinAt)); // Orin's own move is not a player action
        Assert.True(s.Idle(now + DuelNpcSession.IdleLimitMs + 1));
        s.Forfeit();
        Assert.False(s.Idle(long.MaxValue / 2)); // a finished duel is not idle, there is nothing to drop
        DuelNpcSession p = DuelNpcSession.Practice(3, null, 0);
        Assert.True(p.Idle(DuelNpcSession.IdleLimitMs + 1));
        Assert.True(p.SkipScript(100) == false && p.Idle(DuelNpcSession.IdleLimitMs + 1)); // no script: Skip is no action
    }
}
