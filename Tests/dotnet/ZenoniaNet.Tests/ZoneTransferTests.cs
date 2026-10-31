using System.Collections.Generic;
using LiteNetLib.Utils;
using Xunit;
using ZenoniaProto;
using ZenoniaProto.Net;

/// <summary>The pure rules behind a portal transfer: wire messages, the epoch cut, the baselines, the input stream, the portal gate, the boss flags and the idle clock.</summary>
public class ZoneTransferTests
{
    static NetDataReader Reader(NetDataWriter w, byte op)
    {
        var r = new NetDataReader(); r.SetSource(w.CopyData());
        byte got; Assert.True(NetMessageReader.ReadOpcode(r, out got)); Assert.Equal(op, got);
        return r;
    }

    // ---- wire ----

    [Fact]
    public void ZoneChangedRoundTrip()
    {
        var w = new NetDataWriter();
        NetMessageWriter.ZoneChanged(w, new ZoneChangedMsg { Epoch = 7, HeroNetId = 412, MapId = "frostfangDen", X = 1234, Y = -5, Facing = 2 });
        ZoneChangedMsg m; Assert.True(NetMessageReader.TryZoneChanged(Reader(w, NetOp.ZoneChanged), out m));
        Assert.Equal(7, m.Epoch); Assert.Equal(412, m.HeroNetId); Assert.Equal("frostfangDen", m.MapId); Assert.Equal(1234, m.X); Assert.Equal(-5, m.Y); Assert.Equal(2, m.Facing);
    }

    [Fact]
    public void ZoneChangedAndWelcomeRefuseAMissingMap()
    {
        var w = new NetDataWriter(); ZoneChangedMsg z; WelcomeMsg wm;
        NetMessageWriter.ZoneChanged(w, new ZoneChangedMsg { Epoch = 1, HeroNetId = 5, MapId = "" });
        Assert.False(NetMessageReader.TryZoneChanged(Reader(w, NetOp.ZoneChanged), out z));
        NetMessageWriter.ZoneChanged(w, new ZoneChangedMsg { Epoch = 1, HeroNetId = 0, MapId = "x" });
        Assert.False(NetMessageReader.TryZoneChanged(Reader(w, NetOp.ZoneChanged), out z)); // id 0 is nobody
        NetMessageWriter.Welcome(w, new WelcomeMsg { PlayerId = 1, HeroNetId = 2, MapId = "", TickRate = 30, SnapDiv = 2 });
        Assert.False(NetMessageReader.TryWelcome(Reader(w, NetOp.Welcome), out wm));
    }

    [Fact]
    public void ProtocolVersionWasBumpedForTheNewWireFormat() { Assert.True(NetProtocol.Version >= 4); }

    // ---- snapshots across a transfer ----

    sealed class Link
    {
        public readonly SnapshotRing ServerRing = new SnapshotRing(), ClientRing = new SnapshotRing();
        public readonly SnapshotLookup ServerLookup = new SnapshotLookup(), ClientLookup = new SnapshotLookup();
        public readonly SnapshotMsg Msg = new SnapshotMsg();
        public readonly NetDataWriter W = new NetDataWriter();
        public uint Seq, Ack;
        public byte Epoch;
        int hc, mc;

        public void Send(short x, byte epoch, ushort id = 1)
        {
            uint seq = ++Seq;
            SnapshotRing.Slot baseSlot;
            uint baseSeq = 0;
            if (Ack != 0 && ServerRing.TryGet(Ack, out baseSlot)) { baseSeq = Ack; ServerLookup.Load(baseSlot); }
            SnapshotRing.Slot sent = ServerRing.Begin(seq);
            var heroes = new List<HeroSnap> { new HeroSnap { Id = id, X = x, Y = 8, HpPct = 100 } };
            SnapshotPacker.Write(W, seq, seq * 66, 0, baseSeq, baseSeq != 0 ? ServerLookup : null, false, default(OwnerSim), heroes, new List<MonsterSnap>(), ref hc, ref mc, sent.Heroes, sent.Monsters, epoch: epoch);
        }

        public SnapshotReadResult Receive()
        {
            var r = new NetDataReader(); r.SetSource(W.CopyData());
            byte op; NetMessageReader.ReadOpcode(r, out op);
            SnapshotReadResult res = SnapshotPacker.TryRead(r, Msg, ClientRing, ClientLookup, Epoch);
            if (res == SnapshotReadResult.Ok) SnapshotPacker.Remember(ClientRing, Msg);
            return res;
        }
    }

    [Fact]
    public void SnapshotOfTheZoneBeforeIsDroppedAtTheHeader()
    {
        var l = new Link();
        l.Send(100, 0);
        Assert.Equal(SnapshotReadResult.Ok, l.Receive());
        l.Ack = l.Seq;
        l.Send(110, 0); byte[] late = l.W.CopyData(); // a delta of the old zone, still on its way when the client moves

        l.Epoch = 1; l.ClientRing.Clear();
        l.W.Reset(); l.W.Put(late);
        Assert.Equal(SnapshotReadResult.StaleEpoch, l.Receive());
        Assert.Empty(l.Msg.Heroes);
    }

    [Fact]
    public void SnapshotOfTheNewZoneIsDroppedUntilTheClientHearsOfTheMove()
    {
        var l = new Link();
        l.Send(100, 1, 5); // ZoneChanged is reliable, snapshots are not: this one overtook it
        Assert.Equal(SnapshotReadResult.StaleEpoch, l.Receive());
    }

    [Fact]
    public void FirstSnapshotAfterATransferIsFullEvenWhenAnIdComesBack()
    {
        var l = new Link();
        l.Send(100, 0, 3);
        Assert.Equal(SnapshotReadResult.Ok, l.Receive());
        l.Ack = l.Seq;

        // Server side of the transfer: baselines gone, ack void. Id 3 is another entity in the new zone, far from where the old one stood.
        l.ServerRing.Clear(); l.Ack = 0;
        l.Epoch = 1; l.ClientRing.Clear();
        l.Send(-3000, 1, 3);
        Assert.Equal(SnapshotReadResult.Ok, l.Receive());
        Assert.Equal(0u, l.Msg.BaselineSeq);
        Assert.Equal(-3000, l.Msg.Heroes[0].X);

        // Deltas work again from the new baseline, and the sequence numbers simply went on.
        l.Ack = l.Seq;
        l.Send(-2990, 1, 3);
        Assert.Equal(SnapshotReadResult.Ok, l.Receive());
        Assert.NotEqual(0u, l.Msg.BaselineSeq);
        Assert.Equal(-2990, l.Msg.Heroes[0].X);
    }

    [Fact]
    public void BufferForgetsEntitiesButKeepsTheClock()
    {
        var b = new SnapshotBuffer();
        var m = new SnapshotMsg { Seq = 10, ServerTimeMs = 5000 };
        m.Heroes.Add(new HeroSnap { Id = 7, X = 80, Y = 80, HpPct = 100 });
        Assert.True(b.Push(m, 1000.0));
        double now = b.ServerNow(1000.0);
        HeroSample s;
        Assert.True(b.TryHero(7, 5000.0, out s));

        b.ClearEntities();
        Assert.False(b.TryHero(7, 5000.0, out s));
        Assert.True(b.HasClock);
        Assert.Equal(now, b.ServerNow(1000.0));
        Assert.Equal(10u, b.LastSeq);
        m.Seq = 9;
        Assert.False(b.Push(m, 1001.0)); // the sequence goes on: an old packet is still old
    }

    // ---- the input stream ----

    static InputFrame[] Frames(params uint[] seqs)
    {
        var f = new InputFrame[seqs.Length];
        for (int i = 0; i < seqs.Length; i++) f[i] = new InputFrame { Seq = seqs[i], Bits = InputBits.Right };
        return f;
    }

    [Fact]
    public void RestartDropsTheQueueThePressesAndTheCredit()
    {
        var s = new ServerInputState();
        s.Accept(new[] { new InputFrame { Seq = 1, Bits = InputBits.Right | InputBits.HpPotion }, new InputFrame { Seq = 2, Bits = InputBits.Right | InputBits.Attack } }, 2, 100);
        Assert.True(s.Advance(100)); // consumes frame 1 and its potion press
        s.AddCredit(5);
        Assert.Equal(1, s.Queued);

        s.RestartStream();
        Assert.Equal(0, s.Queued);
        Assert.Equal(0, s.Current);
        Assert.False(s.TakePotion(true));
        Assert.True(s.Advance(5000)); // nothing queued, old stream stale: the hero idles
        Assert.Equal(0, s.Current);
        Assert.Equal(1u, s.LastSeq); // snapshots must not claim a frame of the new stream early
    }

    [Fact]
    public void RestartedStreamDoesNotFillTheGapWithTheOldDirection()
    {
        var s = new ServerInputState();
        s.Accept(Frames(1, 2, 3), 3, 100);
        s.RestartStream();

        s.Accept(new[] { new InputFrame { Seq = 9, Bits = InputBits.Left } }, 1, 200); // frames 4-8 were pressed on the old map and dropped by epoch
        Assert.Equal(1, s.Queued);
        Assert.True(s.Advance(200));
        Assert.Equal(9u, s.LastSeq);
        Assert.Equal(InputBits.Left, s.Current);

        s.Accept(new[] { new InputFrame { Seq = 12, Bits = InputBits.Up } }, 1, 210); // and from here on the normal gap fill is back
        Assert.Equal(3, s.Queued);
    }

    [Fact]
    public void RedundancyForgetsFramesButTheSequenceGoesOn()
    {
        var red = new InputRedundancy();
        red.Push(InputBits.Right); red.Push(InputBits.Right); red.Push(InputBits.Attack);
        red.Clear();
        var into = new InputFrame[NetProtocol.MaxInputFrames];
        Assert.Equal(0, red.Fill(into));
        InputFrame f = red.Push(InputBits.Up);
        Assert.Equal(4u, f.Seq);
        Assert.Equal(1, red.Fill(into)); // the first packet of the new zone repeats nothing of the old one
        Assert.Equal(InputBits.Up, into[0].Bits);
    }

    // ---- portal gate ----

    [Fact]
    public void PortalGateNeedsTheFeetOffTheTriggerAndTheCooldown()
    {
        var g = new PortalGate();
        Assert.False(g.Tick(true, 5000f));  // placed on a trigger: stays until the hero steps off
        Assert.False(g.Tick(false, 33f));
        Assert.True(g.Tick(true, 33f));
        g.Disarm();                         // arrived
        Assert.False(g.Tick(false, 33f));
        Assert.False(g.Tick(true, 33f));    // stepped off and on at once: the cooldown still runs
        Assert.False(g.Tick(false, PortalGate.CooldownMs));
        Assert.True(g.Tick(true, 33f));
    }

    [Fact]
    public void PortalTileMatchesTheOfflineFeetRule()
    {
        int tx, ty;
        // Feet 2 units above the bottom of tile (3, 10) of a 24-row map at 16 units, the spot TileFeet gives.
        PortalTile.OfFeet((3 + 0.5f) * 16f, 384f - (11 * 16f - 2f), 16f, 24, out tx, out ty);
        Assert.Equal(3, tx); Assert.Equal(10, ty);
        // The top 4 units of the tile below already count as this tile (the web samples 4 above the feet).
        PortalTile.OfFeet(56f, 384f - 11 * 16f - 3f, 16f, 24, out tx, out ty);
        Assert.Equal(10, ty);
        PortalTile.OfFeet(56f, 384f - 11 * 16f - 5f, 16f, 24, out tx, out ty);
        Assert.Equal(11, ty);
    }

    // ---- boss flags ----

    [Fact]
    public void BossProgressIsPerHeroAndKeepsTheTwoFlagsApart()
    {
        BossProgress a = new BossProgress(), b = new BossProgress();
        Assert.True(a.Mark("frostfangDen", true));
        Assert.False(a.Mark("frostfangDen", true)); // second kill after the respawn: nothing new
        Assert.True(a.IsDefeatedOn("frostfangDen", true));
        Assert.False(b.IsDefeatedOn("frostfangDen", true));
        Assert.False(a.IsDefeatedOn("sunkenReliquary", true));
        Assert.False(a.IsDefeatedOn("frostHollow", false));  // the King's flag is another flag
        Assert.True(b.Mark("frostHollow", false));
        Assert.True(b.IsDefeatedOn("somewhereElse", false)); // every map without its own boss shares it, as in a save
    }

    // ---- idle zones ----

    [Fact]
    public void ZoneWithHeroesNeverPauses()
    {
        var c = new ZoneIdleClock();
        for (int i = 0; i < 1000; i++) Assert.False(c.TickAwake(1, 33f));
        Assert.False(c.Paused);
    }

    [Fact]
    public void EmptyZonePausesAfterTheGraceAndAHeroResetsIt()
    {
        var c = new ZoneIdleClock();
        Assert.False(c.TickAwake(0, ZoneIdleClock.GraceMs - 1f));
        Assert.False(c.TickAwake(1, 33f));                             // somebody came through: the grace starts over
        Assert.False(c.TickAwake(0, ZoneIdleClock.GraceMs - 1f));
        Assert.True(c.TickAwake(0, 2f));
    }

    [Fact]
    public void WakeReportsHowLongTheZoneSlept()
    {
        var c = new ZoneIdleClock();
        Assert.Equal(0f, c.Wake(100f)); // was awake
        c.MarkPaused(1000f);
        Assert.True(c.Paused);
        Assert.False(c.TickAwake(0, 33f)); // a paused zone costs no decision
        Assert.Equal(60000f, c.Wake(61000f));
        Assert.False(c.Paused);
        Assert.Equal(0f, c.Wake(70000f));
        Assert.False(c.TickAwake(0, ZoneIdleClock.GraceMs - 1f)); // and it gets the full grace again
    }

    [Fact]
    public void RespawnClockPaysTheSleepBackAndNeverGoesNegative()
    {
        var timers = new List<float> { 8000f, 500f, 30000f };
        RespawnClock.FastForward(timers, 5000f);
        Assert.Equal(new List<float> { 3000f, 0f, 25000f }, timers);
    }

    [Fact]
    public void BossTimerElapsedDuringASleepFiresOnce()
    {
        var t = new BossRespawnTimer();
        t.Start(30000f);
        Assert.False(t.Tick(10000f));
        Assert.True(t.Tick(3600000f)); // the zone slept an hour: the boss is due, once
        Assert.False(t.Tick(33f));
        Assert.False(t.Active);
    }

    [Fact]
    public void BossTimerNotYetDueAfterASleepKeepsTheRest()
    {
        var t = new BossRespawnTimer();
        t.Start(1800000f);
        Assert.False(t.Tick(600000f));
        Assert.Equal(1200000f, t.LeftMs);
    }
}
