using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

// Phase 05: one duel from seed and moves: turn order, refusals, hands, the 20 s auto placement, the 9+ / 8-8 result, and that board, score and log always agree.
public class DuelMatchTests
{
    static readonly string[] DeckA = DuelNpcTable.Get(0).Deck, DeckB = DuelNpcTable.Get(1).Deck;

    static DuelMatch New(ulong seed, long now = 0) { return DuelMatch.Create(seed, DeckA, DeckB, now); }

    /// <summary>Plays the match out: first card in hand on the first empty cell, for whoever is to move.</summary>
    static void PlayOut(DuelMatch m, long now = 0)
    {
        while (!m.Finished)
        {
            int cell = Enumerable.Range(0, 16).First(c => m.Board.IsEmpty(c));
            Assert.True(m.Place(m.ToMove, 0, cell, now).Ok);
        }
    }

    [Fact]
    public void SameSeedSameDealAndSameFirstMover()
    {
        DuelMatch a = New(42), b = New(42);
        Assert.Equal(a.ToMove, b.ToMove);
        for (int s = 0; s < 2; s++) Assert.Equal(a.Hand(s).ToArray(), b.Hand(s).ToArray());
    }

    [Fact]
    public void DifferentSeedsChooseBothFirstMoversAndShuffleHands()
    {
        var first = new HashSet<int>();
        var hands = new HashSet<string>();
        for (ulong seed = 1; seed <= 60; seed++) { DuelMatch m = New(seed); first.Add(m.ToMove); hands.Add(string.Join(",", m.Hand(0))); }
        Assert.Equal(2, first.Count);
        Assert.True(hands.Count > 5);
    }

    [Fact]
    public void EachSideStartsWithFiveCardsOfItsOwnDeckAndEightToPlace()
    {
        DuelMatch m = New(7);
        for (int s = 0; s < 2; s++)
        {
            Assert.Equal(DuelMatch.HandSize, m.Hand(s).Count);
            Assert.Equal(DuelMatch.DeckSize, m.CardsLeft(s));
        }
        var deckIdx = DeckA.Select(CardTable.Index).ToList();
        Assert.All(m.Hand(0), c => Assert.Contains(c, deckIdx));
    }

    [Fact]
    public void TurnsAlternateAndTheHandRefillsUntilTheDeckRunsOut()
    {
        DuelMatch m = New(9);
        int side = m.ToMove;
        var hand = new[] { 5, 5, 5, 4, 3, 2, 1, 0 }; // hand size seen by `side` after each of its own placements (it places 8 times)
        for (int turn = 1; turn <= 16; turn++)
        {
            Assert.Equal(turn, m.Turn);
            Assert.Equal(side, m.ToMove);
            int cell = Enumerable.Range(0, 16).First(c => m.Board.IsEmpty(c));
            int mine = m.Moves.Count(x => x.Side == side);
            Assert.True(m.Place(side, 0, cell, 0).Ok);
            Assert.Equal(hand[mine], m.Hand(side).Count);
            Assert.Equal(DuelMatch.DeckSize - mine - 1, m.CardsLeft(side));
            side = 1 - side;
        }
        Assert.True(m.Finished);
        Assert.Equal(16, m.Moves.Count);
    }

    [Fact]
    public void WrongTurnTakenCellAndBadHandIndexAreRefusedAndChangeNothing()
    {
        DuelMatch m = New(11);
        int other = 1 - m.ToMove;
        Assert.False(m.Place(other, 0, 0, 0).Ok);
        Assert.False(m.Place(m.ToMove, -1, 0, 0).Ok);
        Assert.False(m.Place(m.ToMove, 5, 0, 0).Ok);
        Assert.False(m.Place(m.ToMove, 0, 16, 0).Ok);
        Assert.False(m.Place(m.ToMove, 0, -1, 0).Ok);
        Assert.Empty(m.Moves);
        int side = m.ToMove;
        Assert.True(m.Place(side, 0, 6, 0).Ok);
        Assert.False(m.Place(1 - side, 0, 6, 0).Ok); // taken
        Assert.Single(m.Moves);
        Assert.Equal(1 - side, m.ToMove);
    }

    [Fact]
    public void SixteenMovesAlwaysEndTheMatchWithNineOrMoreOrEightAll()
    {
        int draws = 0;
        for (ulong seed = 1; seed <= 400; seed++)
        {
            DuelMatch m = New(seed);
            PlayOut(m);
            int a = m.Board.Score(0), b = m.Board.Score(1);
            Assert.Equal(16, a + b);
            if (a == 8) { Assert.Equal(DuelOutcome.Draw, m.Outcome); draws++; }
            else if (a >= 9) Assert.Equal(DuelOutcome.Side0Wins, m.Outcome);
            else Assert.Equal(DuelOutcome.Side1Wins, m.Outcome);
            Assert.False(m.Place(m.ToMove, 0, 0, 0).Ok); // nothing after the end
        }
        Assert.True(draws > 0, "an 8-8 draw must be reachable");
    }

    [Fact]
    public void EachSidePlacesItsEightCardsExactlyOnce()
    {
        DuelMatch m = New(21);
        PlayOut(m);
        for (int s = 0; s < 2; s++)
        {
            var placed = m.Moves.Where(x => x.Side == s).Select(x => x.Card).OrderBy(x => x).ToArray();
            var deck = (s == 0 ? DeckA : DeckB).Select(CardTable.Index).OrderBy(x => x).ToArray();
            Assert.Equal(deck, placed);
        }
    }

    [Fact]
    public void TimeoutPlacesARandomCardOnAnEmptyCellOnlyAfterTheDeadline()
    {
        DuelMatch m = New(33, 1000);
        Assert.Equal(21000, m.DeadlineMs);
        Assert.False(m.Tick(20999).Ok);
        Assert.Empty(m.Moves);
        int side = m.ToMove;
        DuelPlay p = m.Tick(21000);
        Assert.True(p.Ok);
        Assert.True(p.Move.Auto);
        Assert.Equal(side, p.Move.Side);
        Assert.Equal(1 - side, m.ToMove);
        Assert.Equal(41000, m.DeadlineMs); // the other side gets a fresh 20 s
        Assert.False(m.Moves[0].Cell < 0);
    }

    [Fact]
    public void ARealPlacementRestartsTheClock()
    {
        DuelMatch m = New(34, 0);
        Assert.True(m.Place(m.ToMove, 0, 0, 7000).Ok);
        Assert.Equal(27000, m.DeadlineMs);
    }

    [Fact]
    public void ARunOfTimeoutsFinishesTheMatchAndIsReplayable()
    {
        DuelMatch a = New(55), b = New(55);
        long now = 0;
        while (!a.Finished) { now += DuelMatch.TurnMs; Assert.True(a.Tick(now).Ok); }
        now = 0;
        while (!b.Finished) { now += DuelMatch.TurnMs; Assert.True(b.Tick(now).Ok); }
        Assert.Equal(16, a.Moves.Count);
        for (int i = 0; i < 16; i++) { Assert.Equal(a.Moves[i].Cell, b.Moves[i].Cell); Assert.Equal(a.Moves[i].Card, b.Moves[i].Card); }
        Assert.All(a.Moves, m => Assert.True(m.Auto));
    }

    [Fact]
    public void ForfeitGivesTheOtherSideTheWin()
    {
        DuelMatch m = New(3);
        m.Forfeit(0);
        Assert.True(m.Finished);
        Assert.Equal(DuelOutcome.Side1Wins, m.Outcome);
        Assert.Equal(0, m.Forfeited);
        Assert.False(m.Place(m.ToMove, 0, 0, 0).Ok);
    }

    [Fact]
    public void BadDecksAreRejected()
    {
        Assert.Throws<System.ArgumentException>(() => DuelMatch.Create(1, DeckA.Take(7).ToList(), DeckB, 0));
        Assert.Throws<System.ArgumentException>(() => DuelMatch.Create(1, DeckA.Take(7).Concat(new[] { "nope" }).ToList(), DeckB, 0));
    }

    [Fact]
    public void BoardScoreAndLogAreAlwaysDerivedFromTheSameMoves()
    {
        for (ulong seed = 1; seed <= 150; seed++)
        {
            DuelMatch m = New(seed);
            long now = 0;
            while (!m.Finished)
            {
                int cell = Enumerable.Range(0, 16).First(c => m.Board.IsEmpty(c));
                // alternate between a real placement and a timeout so both kinds of move are in the log
                if (m.Moves.Count % 3 == 2) Assert.True(m.Tick(m.DeadlineMs).Ok); else Assert.True(m.Place(m.ToMove, System.Math.Min(m.Moves.Count % 2, m.Hand(m.ToMove).Count - 1), cell, now).Ok);
                for (int mySide = 0; mySide < 2; mySide++)
                {
                    DuelView v = DuelView.From(m, mySide, 0, "Opp", now);
                    DuelBoard rebuilt = v.Board();
                    for (int c = 0; c < 16; c++)
                    {
                        Assert.Equal(m.Board.CardAt(c), rebuilt.CardAt(c));
                        int expectOwner = m.Board.OwnerAt(c) < 0 ? -1 : m.Board.OwnerAt(c) == mySide ? 0 : 1;
                        Assert.Equal(expectOwner, rebuilt.OwnerAt(c));
                    }
                    Assert.Equal(m.Board.Score(mySide), v.Score(DuelView.Me));
                    Assert.Equal(m.Board.Score(1 - mySide), v.Score(DuelView.Them));
                    Assert.Equal(m.Board.Placed, v.Moves.Count);
                    Assert.Equal(m.CardsLeft(mySide), v.CardsLeft(DuelView.Me) );
                    for (int i = 0; i < v.Moves.Count; i++)
                    {
                        string line = v.LogLine(i);
                        Assert.Contains(DuelBoard.Coord(v.Moves[i].Cell), line);
                        int flips = DuelNpcAi.PopCount(v.Moves[i].FlipMask);
                        if (flips == 0) { Assert.DoesNotContain("flipping", line); continue; }
                        // "X placed Y at B2, flipping Z (B3), W (C2).": every flipped cell is named once with its coordinate, after "flipping"
                        string tail = line.Substring(line.IndexOf("flipping ") + 9);
                        Assert.Equal(flips, tail.Count(ch => ch == '('));
                        for (int c = 0; c < 16; c++) if ((v.Moves[i].FlipMask & (1 << c)) != 0) Assert.Contains("(" + DuelBoard.Coord(c) + ")", tail);
                    }
                }
            }
            DuelView end = DuelView.From(m, 0, 0, "Opp", 0);
            Assert.NotEqual(DuelResult.Playing, end.Result);
            Assert.Equal(16, end.Score(0) + end.Score(1));
        }
    }

    [Fact]
    public void TheViewNeverCarriesTheOpponentsHand()
    {
        DuelMatch m = New(5);
        DuelView v = DuelView.From(m, 0, 0, "Opp", 0);
        Assert.Equal(m.Hand(0).ToArray(), v.Hand.ToArray());
        Assert.Equal(5, v.Hand.Count);
        Assert.Equal(m.ToMove == 0, v.YourTurn);
    }
}
