using System.Collections.Generic;
using LiteNetLib.Utils;
using Xunit;
using ZenoniaProto.Net;

public class SnapshotTests
{
    static List<HeroSnap> Heroes(int n) { var l = new List<HeroSnap>(); for (int i = 0; i < n; i++) l.Add(new HeroSnap { Id = (ushort)(i + 1), X = (short)i, Y = (short)(i * 2), HpPct = 100 }); return l; }
    static List<MonsterSnap> Monsters(int n) { var l = new List<MonsterSnap>(); for (int i = 0; i < n; i++) l.Add(new MonsterSnap { Id = (ushort)(1000 + i), X = (short)i, HpPct = 50 }); return l; }

    static void Bump(List<HeroSnap> h, int i) { var x = h[i]; x.X += 8; h[i] = x; }

    /// <summary>Server + client halves of one connection, so deltas can be checked end to end.</summary>
    sealed class Link
    {
        public readonly SnapshotRing ServerRing = new SnapshotRing(), ClientRing = new SnapshotRing();
        public readonly SnapshotLookup ServerLookup = new SnapshotLookup(), ClientLookup = new SnapshotLookup();
        public readonly NetDataWriter W = new NetDataWriter();
        public int HeroCursor, MonsterCursor;
        public uint Seq, Acked;

        public int Send(List<HeroSnap> h, List<MonsterSnap> m, out SnapshotMsg got, out SnapshotReadResult result, bool lost = false, ushort lastInput = 5)
        {
            uint seq = ++Seq, baseSeq = 0;
            SnapshotRing.Slot slot;
            if (Acked != 0 && seq - Acked < SnapshotRing.Size - 2 && ServerRing.TryGet(Acked, out slot)) { baseSeq = Acked; ServerLookup.Load(slot); }
            SnapshotRing.Slot sent = ServerRing.Begin(seq);
            SnapshotPacker.Write(W, seq, seq * 66, lastInput, baseSeq, baseSeq != 0 ? ServerLookup : null, true, new OwnerSim { DashLeft = 90, ComboStep = 2 }, h, m,
                ref HeroCursor, ref MonsterCursor, sent.Heroes, sent.Monsters);
            got = new SnapshotMsg(); result = SnapshotReadResult.Malformed;
            if (lost) return W.Length;
            var r = new NetDataReader(); r.SetSource(W.CopyData());
            Assert.Equal(NetOp.Snapshot, r.GetByte());
            result = SnapshotPacker.TryRead(r, got, ClientRing, ClientLookup);
            if (result == SnapshotReadResult.Ok) { SnapshotPacker.Remember(ClientRing, got); Acked = got.Seq; } // the ack travels back at once in this model
            return W.Length;
        }
    }

    [Fact]
    public void FullSnapshotRoundTripsAndFitsTheBudget()
    {
        var link = new Link(); SnapshotMsg s; SnapshotReadResult res;
        int bytes = link.Send(Heroes(40), Monsters(25), out s, out res);
        Assert.Equal(SnapshotReadResult.Ok, res);
        Assert.True(bytes <= NetProtocol.SnapshotMaxBytes);
        Assert.Equal(1u, s.Seq); Assert.Equal(5, s.LastInputSeq); Assert.True(s.HasOwner);
        Assert.Equal(90, s.Owner.DashLeft); Assert.Equal(2, s.Owner.ComboStep);
        Assert.Equal(40, s.Heroes.Count); Assert.Equal(25, s.Monsters.Count);
        Assert.Equal(40, s.Heroes[39].Id); Assert.Equal(1024, s.Monsters[24].Id);
    }

    [Fact]
    public void UnchangedWorldCostsThreeBytesPerEntityAndDecodesToTheSameStates()
    {
        var link = new Link(); SnapshotMsg s; SnapshotReadResult res;
        var h = Heroes(20); var m = Monsters(30);
        int full = link.Send(h, m, out s, out res);
        int delta = link.Send(h, m, out s, out res);
        Assert.Equal(SnapshotReadResult.Ok, res);
        Assert.Equal(SnapshotPacker.HeaderBytes + 7 + 50 * 3, delta);      // header + owner block (3 B + the dash timer as a float) + 50 entities * 3 B
        Assert.True(delta < full / 3, "delta " + delta + " full " + full);
        Assert.Equal(h[7].Y, s.Heroes[7].Y); Assert.Equal(m[29].HpPct, s.Monsters[29].HpPct);
        Assert.Equal(1u, s.BaselineSeq);
    }

    [Fact]
    public void ChangedFieldsApplyOnTopOfTheBaselineWithSmallAndLargePositionSteps()
    {
        var link = new Link(); SnapshotMsg s; SnapshotReadResult res;
        var h = Heroes(3); var m = Monsters(2);
        link.Send(h, m, out s, out res);
        var h0 = h[0]; h0.X += 100; h0.Y -= 3; h0.Pose = 3; h0.Progress = 77; h[0] = h0;     // small step: sbyte deltas
        var h1 = h[1]; h1.X += 4000; h1.FacingFlags = 9; h1.HpPct = 12; h[1] = h1;           // large step: shorts
        var m1 = m[1]; m1.AttackBits = 5; m1.DeathProgress = 200; m1.Y = 1000; m[1] = m1;
        link.Send(h, m, out s, out res);
        Assert.Equal(SnapshotReadResult.Ok, res);
        Assert.Equal(h[0].X, s.Heroes[0].X); Assert.Equal(h[0].Y, s.Heroes[0].Y); Assert.Equal(3, s.Heroes[0].Pose); Assert.Equal(77, s.Heroes[0].Progress);
        Assert.Equal(h[1].X, s.Heroes[1].X); Assert.Equal(9, s.Heroes[1].FacingFlags); Assert.Equal(12, s.Heroes[1].HpPct);
        Assert.Equal(h[2].X, s.Heroes[2].X);
        Assert.Equal(5, s.Monsters[1].AttackBits); Assert.Equal(200, s.Monsters[1].DeathProgress); Assert.Equal(1000, s.Monsters[1].Y);
    }

    [Fact]
    public void NewEntitiesAreSentFullAndLostSnapshotsKeepTheBaselineValid()
    {
        var link = new Link(); SnapshotMsg s; SnapshotReadResult res;
        var h = Heroes(2); var m = Monsters(1);
        link.Send(h, m, out s, out res);                      // 1: acked
        Bump(h, 0);
        link.Send(h, m, out s, out res, lost: true);          // 2: lost, never acked
        h.Add(new HeroSnap { Id = 99, X = 640, Y = 480, HpPct = 40 });
        Bump(h, 0);
        link.Send(h, m, out s, out res);                      // 3: still a delta against 1; hero 99 is new -> full entry
        Assert.Equal(SnapshotReadResult.Ok, res);
        Assert.Equal(1u, s.BaselineSeq);
        Assert.Equal(3, s.Heroes.Count);
        Assert.Equal(h[0].X, s.Heroes[0].X); Assert.Equal(640, s.Heroes[2].X); Assert.Equal(40, s.Heroes[2].HpPct);
    }

    [Fact]
    public void MissingBaselineIsReportedAndOldBaselinesFallBackToFull()
    {
        var link = new Link(); SnapshotMsg s; SnapshotReadResult res;
        var h = Heroes(2); var m = Monsters(1);
        link.Send(h, m, out s, out res);
        for (int i = 0; i < 20; i++) link.Send(h, m, out s, out res, lost: true); // client hears nothing for 20 snapshots
        link.Send(h, m, out s, out res);                                          // baseline 1 is 21 old: sent full, decodes fine
        Assert.Equal(SnapshotReadResult.Ok, res); Assert.Equal(0u, s.BaselineSeq);
        // a delta whose baseline the client never stored
        var w = new NetDataWriter(); int hc = 0, mc = 0;
        var server = new SnapshotRing(); var look = new SnapshotLookup();
        SnapshotRing.Slot slot = server.Begin(50); slot.Heroes.AddRange(h);
        look.Load(slot);
        SnapshotPacker.Write(w, 51, 1, 0, 50, look, false, default(OwnerSim), h, m, ref hc, ref mc, null, null);
        var r = new NetDataReader(); r.SetSource(w.CopyData()); r.GetByte();
        Assert.Equal(SnapshotReadResult.MissingBaseline, SnapshotPacker.TryRead(r, new SnapshotMsg(), new SnapshotRing(), new SnapshotLookup()));
    }

    [Theory]
    [InlineData(200, 25)] [InlineData(200, 0)] [InlineData(0, 120)] [InlineData(255, 255)] [InlineData(1, 1)]
    public void NeverExceedsTheCapAndKeepsMonstersVisible(int heroes, int monsters)
    {
        var link = new Link(); SnapshotMsg s; SnapshotReadResult res;
        int bytes = link.Send(Heroes(heroes), Monsters(monsters), out s, out res);
        Assert.True(bytes <= NetProtocol.SnapshotMaxBytes, "size " + bytes);
        Assert.Equal(SnapshotReadResult.Ok, res);
        if (monsters > 0 && heroes > 0) Assert.True(s.Monsters.Count >= System.Math.Min(monsters, 25)); // heroes never starve the monsters
        Assert.True(s.Heroes.Count <= heroes && s.Monsters.Count <= monsters);
    }

    [Fact]
    public void OverflowedHeroesLeadTheNextSnapshotRoundRobin()
    {
        var h = Heroes(200); var m = Monsters(10);
        var link = new Link(); SnapshotMsg s; SnapshotReadResult res;
        var seen = new HashSet<ushort>();
        for (int pass = 0; pass < 6 && seen.Count < 200; pass++)
        {
            link.Send(h, m, out s, out res);
            Assert.Equal(SnapshotReadResult.Ok, res);
            foreach (HeroSnap x in s.Heroes) seen.Add(x.Id);
        }
        Assert.Equal(200, seen.Count); // every hero was sent within a few snapshots
    }

    [Fact]
    public void TruncatedOrGarbageSnapshotIsRejectedWithoutThrowing()
    {
        var w = new NetDataWriter(); int hc = 0, mc = 0;
        SnapshotPacker.Write(w, 1, 1, 1, 0, null, true, default(OwnerSim), Heroes(5), Monsters(3), ref hc, ref mc, null, null);
        byte[] data = w.CopyData();
        for (int cut = 1; cut < data.Length; cut++)
        {
            var part = new byte[cut]; System.Array.Copy(data, part, cut);
            var r = new NetDataReader(); r.SetSource(part); r.GetByte();
            Assert.NotEqual(SnapshotReadResult.Ok, SnapshotPacker.TryRead(r, new SnapshotMsg(), new SnapshotRing(), new SnapshotLookup()));
        }
        var rng = new System.Random(5);
        for (int i = 0; i < 300; i++)
        {
            var junk = new byte[rng.Next(1, 60)]; rng.NextBytes(junk);
            var r = new NetDataReader(); r.SetSource(junk);
            SnapshotPacker.TryRead(r, new SnapshotMsg(), new SnapshotRing(), new SnapshotLookup()); // must not throw
        }
    }

    [Fact]
    public void OwnerSimRoundTripsAndIdleCostsThreeBytes()
    {
        var w = new NetDataWriter();
        var idle = default(OwnerSim);
        idle.Write(w);
        Assert.Equal(3, w.Length);
        var s = new OwnerSim { DashDir = 5, ComboPhase = 2, ComboStep = 3, ComboLast = 2, ComboQueued = true, DashLeft = 100.25f, DashCooldown = 600, ComboTimer = 55.5f, ChainTimer = 300, Invuln = 499.99997f, Hurt = 20, CastLeft = 700, StabLeft = 40 };
        w.Reset(); s.Write(w);
        var r = new NetDataReader(); r.SetSource(w.CopyData());
        OwnerSim got;
        Assert.True(OwnerSim.TryRead(r, out got));
        Assert.Equal(s.DashDir, got.DashDir); Assert.Equal(s.ComboPhase, got.ComboPhase); Assert.True(got.ComboQueued); Assert.Equal(3, got.ComboStep); Assert.Equal(2, got.ComboLast);
        Assert.Null(got.Cd); Assert.Equal(0f, got.AtkSpeedLeft); Assert.Equal(s.ComboTimer, got.ComboTimer); Assert.Equal(s.Invuln, got.Invuln); Assert.Equal(s.StabLeft, got.StabLeft); Assert.Equal(s.CastLeft, got.CastLeft); Assert.Equal(s.Hurt, got.Hurt); Assert.Equal(s.Invuln, got.Invuln);
    }

    [Fact]
    public void OwnerSimSkillBlockRoundTripsAndStaysOffTheWireWhenIdle()
    {
        var w = new NetDataWriter();
        var s = new OwnerSim { CastLeft = 450, CastSkill = 5, AtkSpeedLeft = 8000, AtkSpeedPct = 15, Cd = new float[] { 0, 9000.5f, 0, 0, 120, 0, 0, 33000 } };
        s.Write(w);
        var r = new NetDataReader(); r.SetSource(w.CopyData());
        OwnerSim got;
        Assert.True(OwnerSim.TryRead(r, out got));
        Assert.Equal(0, r.AvailableBytes);
        Assert.Equal(5, got.CastSkill); Assert.Equal(450f, got.CastLeft); Assert.Equal(8000f, got.AtkSpeedLeft); Assert.Equal(15f, got.AtkSpeedPct);
        for (int i = 0; i < 8; i++) Assert.Equal(s.Cd[i], got.CdOf(i));
        // a cast of skill 0 with nothing else on cooldown needs no extension block
        w.Reset(); new OwnerSim { CastLeft = 300 }.Write(w);
        Assert.Equal(7, w.Length);
        // a hostile extension (skill index 9) is rejected
        w.Reset(); s.Write(w);
        byte[] bad = w.CopyData();
        bad[7] = 9;
        r.SetSource(bad);
        Assert.False(OwnerSim.TryRead(r, out got));
    }
}
