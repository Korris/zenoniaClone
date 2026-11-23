using System.Linq;
using Xunit;
using ZenoniaProto.Rpg;

// Phase 05: the practice opponents. The AI only plays legal moves, never looks at the player's hand, Hard beats Easy, and a whole NPC session runs to the end by the clock.
public class DuelNpcAiTests
{
    /// <summary>Plays one match with both sides driven by DuelNpcAi (side 0 = `level0` with NPC `npc0`'s deck, side 1 likewise). Returns the outcome.</summary>
    static DuelOutcome Fight(ulong seed, int npc0, int npc1)
    {
        DuelNpcDef d0 = DuelNpcTable.Get(npc0), d1 = DuelNpcTable.Get(npc1);
        DuelMatch m = DuelMatch.Create(seed, d0.Deck, d1.Deck, 0);
        var r0 = new DuelRng(seed + 1); var r1 = new DuelRng(seed + 2);
        while (!m.Finished)
        {
            int side = m.ToMove, h, c;
            Assert.True(DuelNpcAi.Pick(m, side, side == 0 ? d0.Level : d1.Level, side == 0 ? r0 : r1, out h, out c));
            Assert.True(m.Place(side, h, c, 0).Ok, "the AI proposed an illegal move");
        }
        return m.Outcome;
    }

    [Fact]
    public void TheAiNeverPlaysAnIllegalMoveForAnyNpcOrSeed()
    {
        for (ulong seed = 1; seed <= 60; seed++)
            for (int a = 0; a < DuelNpcTable.Count; a++)
                for (int b = 0; b < DuelNpcTable.Count; b++) Fight(seed, a, b);
    }

    [Fact]
    public void HardWithItsDeckBeatsEasyInMoreThanSeventyPercentOfFiveHundredGames()
    {
        int hardWins = 0, easyWins = 0;
        for (ulong seed = 1; seed <= 500; seed++)
        {
            DuelOutcome o = Fight(seed, 2, 0); // side 0 Hard, side 1 Easy; the first mover is random
            if (o == DuelOutcome.Side0Wins) hardWins++; else if (o == DuelOutcome.Side1Wins) easyWins++;
        }
        Assert.True(hardWins > 350, "Hard won " + hardWins + " / 500 (Easy " + easyWins + ")");
    }

    [Fact]
    public void NormalBeatsEasyMostOfTheTime()
    {
        int wins = 0;
        for (ulong seed = 1; seed <= 300; seed++) if (Fight(seed, 1, 0) == DuelOutcome.Side0Wins) wins++;
        Assert.True(wins > 150, "Normal won " + wins + " / 300");
    }

    [Fact]
    public void TheGreedyAiTakesAFlipWhenOneExists()
    {
        // find a seed where the AI has a flipping move on its first turn after the player played; the pick must flip at least as many as any other legal move
        for (ulong seed = 1; seed <= 100; seed++)
        {
            DuelMatch m = DuelMatch.Create(seed, DuelNpcTable.Get(0).Deck, DuelNpcTable.Get(2).Deck, 0);
            var rng = new DuelRng(seed);
            for (int guard = 0; guard < 6 && !m.Finished; guard++)
            {
                int side = m.ToMove;
                if (side == 1)
                {
                    int h, c;
                    Assert.True(DuelNpcAi.Pick(m, 1, DuelNpcLevel.Hard, rng, out h, out c));
                    int picked = DuelNpcAi.PopCount(m.Board.PreviewFlips(1, m.Hand(1)[h], c)), best = 0;
                    for (int hh = 0; hh < m.Hand(1).Count; hh++)
                        for (int cc = 0; cc < 16; cc++) if (m.Board.IsEmpty(cc)) best = System.Math.Max(best, DuelNpcAi.PopCount(m.Board.PreviewFlips(1, m.Hand(1)[hh], cc)));
                    Assert.Equal(best, picked);
                }
                int cell = Enumerable.Range(0, 16).First(x => m.Board.IsEmpty(x));
                m.Place(side, 0, cell, 0);
            }
        }
    }

    [Fact]
    public void EasyPicksOnlyAmongItsThreeBestMoves()
    {
        DuelMatch m = DuelMatch.Create(77, DuelNpcTable.Get(0).Deck, DuelNpcTable.Get(1).Deck, 0);
        while (m.ToMove != 1) m.Place(m.ToMove, 0, Enumerable.Range(0, 16).First(c => m.Board.IsEmpty(c)), 0);
        var seen = new System.Collections.Generic.HashSet<string>();
        for (ulong s = 1; s <= 80; s++) { int h, c; DuelNpcAi.Pick(m, 1, DuelNpcLevel.Easy, new DuelRng(s), out h, out c); seen.Add(h + ":" + c); }
        Assert.InRange(seen.Count, 1, 3);
    }

    [Fact]
    public void ASessionRunsToTheEndByTheClockWithAnIdlePlayer()
    {
        for (int npc = 0; npc < DuelNpcTable.Count; npc++)
        {
            DuelNpcSession s = DuelNpcSession.Start(1234UL + (ulong)npc, DuelNpcTable.Get(0).Deck, npc, 0);
            long now = 0;
            int guard = 0;
            while (!s.Finished && guard++ < 10000) { now += 250; s.Tick(now); } // the player never moves: the timeout places for them
            Assert.True(s.Finished);
            Assert.Equal(16, s.Match.Moves.Count);
            Assert.Equal(8, s.Match.Moves.Count(m => m.Side == 0 && m.Auto)); // every move of the idle player was a timeout placement
            Assert.Equal(0, s.Match.Moves.Count(m => m.Side == 1 && m.Auto)); // the NPC never times out
        }
    }

    [Fact]
    public void ASessionAnswersAPlacementAfterTheThinkingDelayNotBefore()
    {
        for (ulong seed = 1; seed < 200; seed++)
        {
            DuelNpcSession s = DuelNpcSession.Start(seed, DuelNpcTable.Get(0).Deck, 1, 0);
            long now = 0;
            if (s.Match.ToMove == DuelNpcSession.Npc)
            {
                Assert.False(s.Tick(599)); // the NPC is still thinking
                now = 1300; Assert.True(s.Tick(now)); // 0.6 - 1.2 s
            }
            Assert.Equal(DuelNpcSession.Player, s.Match.ToMove);
            int before = s.Match.Moves.Count;
            Assert.True(s.PlayerPlace(0, Enumerable.Range(0, 16).First(c => s.Match.Board.IsEmpty(c)), now).Ok);
            Assert.Equal(before + 1, s.Match.Moves.Count);
            Assert.False(s.Tick(now + 599));
            Assert.True(s.Tick(now + 1300));
            Assert.Equal(before + 2, s.Match.Moves.Count);
            return;
        }
    }

    [Fact]
    public void AForfeitIsALossAndTheResultIsHandedOutOnce()
    {
        DuelNpcSession s = DuelNpcSession.Start(9, DuelNpcTable.Get(0).Deck, 0, 0);
        Assert.Null(s.TakeResult());
        s.Forfeit();
        Assert.Equal(DuelResult.Lose, s.TakeResult());
        Assert.Null(s.TakeResult());
        Assert.Equal(1, s.View(0).EndReason);
    }

    [Fact]
    public void UnknownNpcDoesNotStart()
    {
        Assert.Null(DuelNpcSession.Start(1, DuelNpcTable.Get(0).Deck, 7, 0));
        Assert.Null(DuelNpcSession.Start(1, DuelNpcTable.Get(0).Deck, -1, 0));
    }
}
