using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

/// <summary>The scripted first duel on a real DuelMatch: every step is legal and flips what the lesson text says.</summary>
public class DuelTutorialScriptTests
{
    [Fact]
    public void BothDecksAreValidFullDecksOfCommonCards()
    {
        foreach (string[] deck in new[] { DuelTutorialScript.PlayerDeck, DuelTutorialScript.OrinDeck })
        {
            Assert.Equal(DeckError.None, DeckRules.Validate(deck, null, true));
            foreach (string id in deck) Assert.Equal(CardRarity.N, CardTable.Get(id).Rarity);
        }
    }

    [Fact]
    public void ScriptPlaysOnARealMatchWithTheFlipsTheLessonsSay()
    {
        DuelMatch m = DuelMatch.CreateFixed(DuelTutorialScript.PlayerDeck, DuelTutorialScript.OrinDeck, 0, 0);
        Assert.False(m.HasClock);
        var masks = new List<int>();
        foreach (DuelScriptStep s in DuelTutorialScript.Steps)
        {
            Assert.Equal(s.Side, m.ToMove);
            int h = new List<int>(m.Hand(s.Side)).IndexOf(CardTable.Index(s.CardId));
            Assert.True(h >= 0, s.CardId + " must be in the hand");
            DuelPlay p = m.Place(s.Side, h, s.Cell, 0);
            Assert.True(p.Ok, p.Error);
            masks.Add(p.Move.FlipMask);
        }
        Assert.Equal(new[] { 0, 0, 1 << 5, 1 << 4, 0 }, masks.ToArray());
        // after turn 5: Orin owns B1 (4) and C1 (8); the player A1, A2, B2
        Assert.Equal(3, m.Board.Score(0));
        Assert.Equal(2, m.Board.Score(1));
        Assert.Equal(1, m.ToMove);
    }

    [Fact]
    public void FixedMatchDoesNotShuffleAndHasNoClock()
    {
        DuelMatch m = DuelMatch.CreateFixed(DuelTutorialScript.PlayerDeck, DuelTutorialScript.OrinDeck, 0, 1000);
        for (int i = 0; i < DuelMatch.HandSize; i++) Assert.Equal(CardTable.Index(DuelTutorialScript.PlayerDeck[i]), m.Hand(0)[i]);
        Assert.Equal(0, m.ToMove);
        Assert.False(m.Tick(long.MaxValue - 1).Ok); // no clock: never auto-places
        Assert.Equal(1, DuelMatch.CreateFixed(DuelTutorialScript.PlayerDeck, DuelTutorialScript.OrinDeck, 1, 0).ToMove);
    }

    [Fact]
    public void CreateWithoutAClockNeverTimesOut()
    {
        DuelMatch m = DuelMatch.Create(3, DuelTestDecks.A, DuelTestDecks.A, 0, 0);
        Assert.False(m.Tick(10L * 60 * 1000).Ok);
        DuelMatch clocked = DuelMatch.Create(3, DuelTestDecks.A, DuelTestDecks.A, 0);
        Assert.True(clocked.HasClock);
        Assert.True(clocked.Tick(DuelMatch.TurnMs).Ok);
    }

    [Fact]
    public void LessonNumbersCountThePlayersStepsOnly()
    {
        Assert.Equal(3, DuelTutorialScript.Lessons);
        Assert.Equal(1, DuelTutorialScript.LessonAt(0));
        Assert.Equal(2, DuelTutorialScript.LessonAt(1));
        Assert.Equal(2, DuelTutorialScript.LessonAt(2));
        Assert.Equal(3, DuelTutorialScript.LessonAt(4));
        Assert.Equal(0, DuelTutorialScript.LessonAt(5));
    }
}
