using System.Collections.Generic;
using Xunit;
using ZenoniaProto.Net;

public class InterestTests
{
    // Frost Hollow is 576 x 384 units: 9 x 6 cells of 64.
    static InterestGrid Grid() { return new InterestGrid(576f, 384f); }

    [Fact]
    public void GridHasNineBySixCellsAndClampsOutOfMapPositions()
    {
        var g = Grid();
        Assert.Equal(9, g.Columns); Assert.Equal(6, g.Rows);
        Assert.Equal(0, g.CellX(-50f)); Assert.Equal(8, g.CellX(9999f)); Assert.Equal(5, g.CellY(383.9f)); Assert.Equal(1, g.CellX(64f));
    }

    [Fact]
    public void QueryReturnsTheSquareWindowAroundACell()
    {
        var g = Grid();
        g.Set(1, 100f, 100f);   // cell (1,1)
        g.Set(2, 330f, 100f);   // cell (5,1): 4 cells away
        g.Set(3, 230f, 200f);   // cell (3,3): 2 away
        var hits = new List<ushort>();
        g.Query(1, 1, 2, hits);
        Assert.Contains((ushort)1, hits); Assert.Contains((ushort)3, hits); Assert.DoesNotContain((ushort)2, hits);
    }

    [Fact]
    public void MovingAnEntityUpdatesItsCellAndRemoveForgetsIt()
    {
        var g = Grid();
        g.Set(7, 10f, 10f);
        g.Set(7, 500f, 300f);
        int cx, cy;
        Assert.True(g.TryCellOf(7, out cx, out cy)); Assert.Equal(7, cx); Assert.Equal(4, cy);
        var hits = new List<ushort>(); g.Query(0, 0, 1, hits); Assert.Empty(hits);
        g.Remove(7);
        Assert.False(g.TryCellOf(7, out cx, out cy));
        Assert.Equal(0, g.Count);
    }

    [Fact]
    public void EntitiesEnterAtTwoCellsAndOnlyLeaveBeyondThreeHysteresis()
    {
        var g = Grid(); var set = new InterestSet();
        var entered = new List<ushort>(); var left = new List<ushort>(); var gone = new List<ushort>();
        g.Set(1, 32f, 32f);          // viewer, cell (0,0)
        g.Set(2, 160f, 32f);         // cell (2,0): inside see radius
        g.Set(3, 224f, 32f);         // cell (3,0): outside see radius
        set.Update(g, 0, 0, entered, left, gone);
        Assert.Contains((ushort)1, entered); Assert.Contains((ushort)2, entered); Assert.DoesNotContain((ushort)3, entered);
        // entity 2 drifts to cell 3 (still within drop radius): stays, no flicker
        g.Set(2, 224f, 32f);
        set.Update(g, 0, 0, entered, left, gone);
        Assert.Empty(entered); Assert.Empty(left); Assert.True(set.Contains(2));
        // cell 4: beyond the drop radius -> leaves
        g.Set(2, 288f, 32f);
        set.Update(g, 0, 0, entered, left, gone);
        Assert.Equal(new ushort[] { 2 }, left.ToArray()); Assert.False(set.Contains(2));
        // coming back to cell 3 does NOT re-enter (needs <= 2)
        g.Set(2, 224f, 32f);
        set.Update(g, 0, 0, entered, left, gone);
        Assert.DoesNotContain((ushort)2, entered);
        g.Set(2, 160f, 32f);
        set.Update(g, 0, 0, entered, left, gone);
        Assert.Contains((ushort)2, entered);
    }

    [Fact]
    public void AnEntityThatNoLongerExistsIsReportedGone()
    {
        var g = Grid(); var set = new InterestSet();
        var entered = new List<ushort>(); var left = new List<ushort>(); var gone = new List<ushort>();
        g.Set(1, 10f, 10f); g.Set(5, 40f, 40f);
        set.Update(g, 0, 0, entered, left, gone);
        g.Clear(); g.Set(1, 10f, 10f);
        set.Update(g, 0, 0, entered, left, gone);
        Assert.Equal(new ushort[] { 5 }, gone.ToArray()); Assert.False(set.Contains(5)); Assert.True(set.Contains(1));
    }
}

public class SnapshotBufferTests
{
    static SnapshotMsg Msg(uint seq, uint ms, short heroX, short monsterX = 0, byte pose = 0)
    {
        var m = new SnapshotMsg { Seq = seq, ServerTimeMs = ms };
        m.Heroes.Add(new HeroSnap { Id = 1, X = heroX, Y = 0, Pose = pose, FacingFlags = NetQuantize.PackFacing(3, 0) });
        m.Monsters.Add(new MonsterSnap { Id = 9, X = monsterX, HpPct = 80 });
        return m;
    }

    [Fact]
    public void InterpolatesBetweenTwoSnapshotsAtTheRenderTime()
    {
        var b = new SnapshotBuffer();
        b.Push(Msg(1, 1000, 0), 5000);          // x = 0
        b.Push(Msg(2, 1066, 8 * 10), 5066);     // x = 10 units
        HeroSample s;
        Assert.True(b.TryHero(1, 1033, out s));
        Assert.InRange(s.X, 4.9f, 5.1f);
        Assert.True(b.TryHero(1, 1066, out s)); Assert.InRange(s.X, 9.99f, 10.01f);
    }

    [Fact]
    public void HoldsTheLastStateAfterTheNewestSampleAndTheFirstBeforeTheOldest()
    {
        var b = new SnapshotBuffer();
        b.Push(Msg(1, 1000, 16), 0); b.Push(Msg(2, 1066, 32), 66);
        HeroSample s;
        Assert.True(b.TryHero(1, 5000, out s)); Assert.InRange(s.X, 3.99f, 4.01f);
        Assert.True(b.TryHero(1, 10, out s)); Assert.InRange(s.X, 1.99f, 2.01f);
        Assert.False(b.TryHero(2, 1000, out s));
    }

    [Fact]
    public void ATeleportOverFortyEightUnitsSnapsInsteadOfSliding()
    {
        var b = new SnapshotBuffer();
        b.Push(Msg(1, 1000, 0), 0); b.Push(Msg(2, 1066, 8 * 200), 66);
        HeroSample s;
        b.TryHero(1, 1020, out s); Assert.InRange(s.X, -0.01f, 0.01f);
        b.TryHero(1, 1050, out s); Assert.InRange(s.X, 199.9f, 200.1f);
    }

    [Fact]
    public void OldOrDuplicateSnapshotsAreIgnoredAndMonstersTrackToo()
    {
        var b = new SnapshotBuffer();
        Assert.True(b.Push(Msg(5, 1000, 0, 0), 0));
        Assert.False(b.Push(Msg(4, 900, 99, 99), 10));
        Assert.False(b.Push(Msg(5, 1000, 99, 99), 10));
        Assert.True(b.Push(Msg(6, 1066, 0, 80), 66));
        MonsterSample m;
        Assert.True(b.TryMonster(9, 1033, out m)); Assert.InRange(m.X, 4.9f, 5.1f); Assert.Equal(80, m.HpPct);
    }

    [Fact]
    public void ClockOffsetTracksTheFastestPacketAndRenderTimeLagsByTheDelay()
    {
        var b = new SnapshotBuffer();
        b.Push(Msg(1, 1000, 0), 100);                // offset 900
        b.Push(Msg(2, 1066, 0), 120);                // slower packet (offset 946): barely moves the estimate
        Assert.InRange(b.ServerNow(200) - 200, 899.9, 902.0);
        b.Push(Msg(3, 1132, 0), 140);                // faster packet (offset 992)? no, slower again
        b.Push(Msg(4, 1198, 0), 280);                // offset 918: still above 900
        Assert.Equal(b.ServerNow(1000) - SnapshotBuffer.RenderDelayMs, b.RenderTime(1000), 6);
    }

    [Fact]
    public void ForgetDropsTheTrack()
    {
        var b = new SnapshotBuffer();
        b.Push(Msg(1, 1000, 0), 0);
        b.Forget(1);
        HeroSample s;
        Assert.False(b.TryHero(1, 1000, out s));
    }
}
