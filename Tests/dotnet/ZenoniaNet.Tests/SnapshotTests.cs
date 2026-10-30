using System.Collections.Generic;
using LiteNetLib.Utils;
using Xunit;
using ZenoniaProto.Net;

public class SnapshotTests
{
    static List<HeroSnap> Heroes(int n) { var l = new List<HeroSnap>(); for (int i = 0; i < n; i++) l.Add(new HeroSnap { Id = (ushort)(i + 1), X = (short)i, Y = (short)(i * 2), HpPct = 100 }); return l; }
    static List<MonsterSnap> Monsters(int n) { var l = new List<MonsterSnap>(); for (int i = 0; i < n; i++) l.Add(new MonsterSnap { Id = (ushort)(1000 + i), X = (short)i, HpPct = 50 }); return l; }

    static SnapshotMsg Read(NetDataWriter w)
    {
        var r = new NetDataReader(); r.SetSource(w.CopyData());
        Assert.Equal(NetOp.Snapshot, r.GetByte());
        var s = new SnapshotMsg(); Assert.True(SnapshotPacker.TryRead(r, s)); return s;
    }

    [Fact]
    public void SmallSnapshotRoundTripsAndMatchesTheBudgetFormula()
    {
        var w = new NetDataWriter(); int hc = 0, mc = 0;
        var h = Heroes(40); var m = Monsters(25);
        SnapshotPacker.Write(w, 9, 1234, 77, h, m, ref hc, ref mc);
        Assert.Equal(SnapshotPacker.HeaderBytes + 40 * HeroSnap.WireBytes + 25 * MonsterSnap.WireBytes, w.Length); // plan budget: 40 heroes + 25 monsters
        Assert.True(w.Length <= NetProtocol.SnapshotMaxBytes);
        SnapshotMsg s = Read(w);
        Assert.Equal(9u, s.Seq); Assert.Equal(1234u, s.ServerTimeMs); Assert.Equal(77u, s.LastInputSeq);
        Assert.Equal(40, s.Heroes.Count); Assert.Equal(25, s.Monsters.Count);
        Assert.Equal(40, s.Heroes[39].Id); Assert.Equal(1024, s.Monsters[24].Id);
        Assert.Equal(0, hc); Assert.Equal(0, mc);
    }

    [Theory]
    [InlineData(200, 25)] [InlineData(200, 0)] [InlineData(0, 120)] [InlineData(255, 255)] [InlineData(1, 1)]
    public void NeverExceedsTheCapAndKeepsMonstersVisible(int heroes, int monsters)
    {
        var w = new NetDataWriter(); int hc = 0, mc = 0;
        SnapshotPacker.Write(w, 1, 1, 1, Heroes(heroes), Monsters(monsters), ref hc, ref mc);
        Assert.True(w.Length <= NetProtocol.SnapshotMaxBytes, "size " + w.Length);
        SnapshotMsg s = Read(w);
        if (monsters > 0 && heroes > 0) Assert.True(s.Monsters.Count >= System.Math.Min(monsters, 25)); // heroes never starve the monsters
        Assert.True(s.Heroes.Count <= heroes && s.Monsters.Count <= monsters);
    }

    [Fact]
    public void OverflowedHeroesLeadTheNextSnapshotRoundRobin()
    {
        var h = Heroes(200); var m = Monsters(10);
        int hc = 0, mc = 0;
        var seen = new HashSet<ushort>();
        var w = new NetDataWriter();
        for (int pass = 0; pass < 6 && seen.Count < 200; pass++)
        {
            SnapshotPacker.Write(w, (uint)pass, 0, 0, h, m, ref hc, ref mc);
            foreach (HeroSnap s in Read(w).Heroes) seen.Add(s.Id);
        }
        Assert.Equal(200, seen.Count); // every hero was sent within a few snapshots
    }

    [Fact]
    public void TruncatedSnapshotIsRejected()
    {
        var w = new NetDataWriter(); int hc = 0, mc = 0;
        SnapshotPacker.Write(w, 1, 1, 1, Heroes(5), Monsters(3), ref hc, ref mc);
        byte[] data = w.CopyData();
        var cut = new byte[data.Length - 7]; System.Array.Copy(data, cut, cut.Length);
        var r = new NetDataReader(); r.SetSource(cut); r.GetByte();
        Assert.False(SnapshotPacker.TryRead(r, new SnapshotMsg()));
    }
}
