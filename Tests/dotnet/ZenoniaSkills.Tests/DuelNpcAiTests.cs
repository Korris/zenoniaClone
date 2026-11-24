using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Rpg;

public class DuelNpcAiTests
{
    static int Idx(string id) { return CardTable.Index(id); }

    [Fact]
    public void PicksTheMoveThatFlipsTheMost()
    {
        var b = new DuelBoard();
        b.Place(0, Idx("meadowSlime"), 5);   // B2, player card (L4, T6, R3, B5)
        b.Place(0, Idx("thornBoar"), 9);     // C2 below it
        // Orin (side 1) with a Forest Wolf (Top 7) and a Horn Rabbit: wolf at... best flips available
        var hand = new List<int> { Idx("hornRabbit"), Idx("forestWolf") };
        int h, c;
        Assert.True(DuelNpcAi.Best(b, hand, 1, new DuelRng(5), false, out h, out c));
        int best = 0;
        for (int i = 0; i < hand.Count; i++)
            for (int cell = 0; cell < DuelBoard.Cells; cell++)
                if (b.IsEmpty(cell)) best = System.Math.Max(best, DuelBoard.PopCount(b.PreviewFlips(1, hand[i], cell)));
        Assert.True(best > 0);
        Assert.Equal(best, DuelBoard.PopCount(b.PreviewFlips(1, hand[h], c)));
        Assert.True(b.IsEmpty(c));
    }

    [Fact]
    public void NoMoveWhenTheBoardIsFullOrTheHandEmpty()
    {
        var b = new DuelBoard();
        int h, c;
        Assert.False(DuelNpcAi.Best(b, new List<int>(), 1, null, true, out h, out c));
        for (int i = 0; i < DuelBoard.Cells; i++) b.Place(i % 2, Idx("meadowSlime"), i);
        Assert.False(DuelNpcAi.Best(b, new List<int> { Idx("forestWolf") }, 1, null, false, out h, out c));
        Assert.Equal(-1, h);
    }

    [Fact]
    public void HintIsDeterministicAndPrefersFewerWeakOpenEdges()
    {
        var b = new DuelBoard();
        var hand = new List<int> { Idx("meadowMantis"), Idx("thornBoar"), Idx("forestWolf") };
        int h1, c1, h2, c2;
        Assert.True(DuelNpcAi.Best(b, hand, 0, null, true, out h1, out c1));
        Assert.True(DuelNpcAi.Best(b, hand, 0, new DuelRng(99), true, out h2, out c2));
        Assert.Equal(h1, h2);
        Assert.Equal(c1, c2);
        // an empty board flips nothing, so the hint is the placement with the least weak edges facing empty cells: a corner
        Assert.Contains(c1, new[] { 0, 3, 12, 15 });
    }

    [Fact]
    public void OrinPlaysTheSameFromTheSameSeed()
    {
        var b = new DuelBoard();
        var hand = new List<int> { Idx("meadowMantis"), Idx("thornBoar"), Idx("forestWolf"), Idx("hornRabbit"), Idx("meadowSlime") };
        int h1, c1, h2, c2;
        DuelNpcAi.Best(b, hand, 1, new DuelRng(7), false, out h1, out c1);
        DuelNpcAi.Best(b, hand, 1, new DuelRng(7), false, out h2, out c2);
        Assert.Equal(h1, h2);
        Assert.Equal(c1, c2);
    }
}
