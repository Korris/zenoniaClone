using Xunit;
using ZenoniaProto.Rpg;

// Phase 05: the flip rule of the 4 x 4 card duel, one truth table row per test.
public class DuelBoardTests
{
    static int I(string id) { return CardTable.Index(id); }

    // meadowSlime T6 R3 B5 L4; hornRabbit T3 R7 B2 L4; thornBoar T4 R3 B7 L2; highlandIbex T5 R4 B3 L8
    [Fact]
    public void CoordinatesRunA1ToD4()
    {
        Assert.Equal("A1", DuelBoard.Coord(0));
        Assert.Equal("A4", DuelBoard.Coord(3));
        Assert.Equal("B3", DuelBoard.Coord(6));
        Assert.Equal("D4", DuelBoard.Coord(15));
        for (int c = 0; c < 16; c++) Assert.Equal(c, DuelBoard.ParseCoord(DuelBoard.Coord(c)));
        Assert.Equal(-1, DuelBoard.ParseCoord("E1"));
        Assert.Equal(-1, DuelBoard.ParseCoord("A5"));
        Assert.Equal(-1, DuelBoard.ParseCoord("A"));
    }

    [Fact]
    public void HigherEdgeFlipsTheCardBelow()
    {
        var b = new DuelBoard();
        b.Set(5, I("hornRabbit"), 1); // B2, enemy, Top 3
        int mask = b.Place(0, I("meadowSlime"), 1); // A2 above it: my Bottom 5 > 3
        Assert.Equal(1 << 5, mask);
        Assert.Equal(0, b.OwnerAt(5));
        Assert.Equal(2, b.Score(0));
        Assert.Equal(0, b.Score(1));
    }

    [Fact]
    public void EqualEdgesDoNotFlip()
    {
        var b = new DuelBoard();
        b.Set(5, I("highlandIbex"), 1); // Top 5
        Assert.Equal(0, b.Place(0, I("meadowSlime"), 1)); // my Bottom 5 = 5
        Assert.Equal(1, b.OwnerAt(5));
    }

    [Fact]
    public void LowerEdgeDoesNotFlip()
    {
        var b = new DuelBoard();
        b.Set(5, I("thornBoar"), 1); // Top 4
        Assert.Equal(0, b.Place(0, I("hornRabbit"), 1)); // my Bottom 2 < 4
    }

    [Fact]
    public void OneCardCanFlipThreeNeighboursInThreeDirections()
    {
        var b = new DuelBoard();
        b.Set(0, I("thornBoar"), 1);   // A1 left of A2: my Left 4 > its Right 3
        b.Set(2, I("thornBoar"), 1);   // A3 right of A2: my Right 3 > its Left 2
        b.Set(5, I("hornRabbit"), 1);  // B2 below: my Bottom 5 > its Top 3
        int mask = b.Place(0, I("meadowSlime"), 1);
        Assert.Equal((1 << 0) | (1 << 2) | (1 << 5), mask);
        Assert.Equal(4, b.Score(0));
    }

    [Fact]
    public void FourDirectionsAllCount()
    {
        var b = new DuelBoard();
        b.Set(1, I("hornRabbit"), 1);  // above B2: my Top 6 > its Bottom 2
        b.Set(4, I("thornBoar"), 1);   // left: my Left 4 > its Right 3
        b.Set(6, I("thornBoar"), 1);   // right: my Right 3 > its Left 2
        b.Set(9, I("hornRabbit"), 1);  // below: my Bottom 5 > its Top 3
        Assert.Equal((1 << 1) | (1 << 4) | (1 << 6) | (1 << 9), b.Place(0, I("meadowSlime"), 5));
    }

    [Fact]
    public void OwnCardsAreNeverFlippedAndNothingChains()
    {
        var b = new DuelBoard();
        b.Set(5, I("hornRabbit"), 0);   // mine, below
        b.Set(9, I("hornRabbit"), 1);   // enemy under my card: would flip if the flip chained
        Assert.Equal(0, b.Place(0, I("meadowSlime"), 1));
        Assert.Equal(1, b.OwnerAt(9));
    }

    [Fact]
    public void BoardEdgesDoNotWrapAround()
    {
        var b = new DuelBoard();
        b.Set(4, I("thornBoar"), 1); // B1: the cell "to the right" of A4 if the row wrapped (my Right 3 > its Left 2)
        Assert.Equal(0, b.Place(0, I("meadowSlime"), 3)); // A4
        Assert.Equal(1, b.OwnerAt(4));
        var c = new DuelBoard();
        c.Set(3, I("thornBoar"), 1); // A4: the cell "to the left" of B1 if the row wrapped
        Assert.Equal(0, c.Place(0, I("meadowSlime"), 4)); // B1: my Left 4 > A4's Right 3 would flip on a wrap
    }

    [Fact]
    public void PreviewChangesNothingAndTakenCellsAreRefused()
    {
        var b = new DuelBoard();
        b.Set(5, I("hornRabbit"), 1);
        Assert.Equal(1 << 5, b.PreviewFlips(0, I("meadowSlime"), 1));
        Assert.Equal(1, b.OwnerAt(5));
        Assert.Equal(-1, b.CardAt(1));
        Assert.Equal(0, b.PreviewFlips(0, I("meadowSlime"), 5)); // taken
        Assert.Throws<System.ArgumentException>(() => b.Place(0, I("meadowSlime"), 5));
    }
}
