using LiteNetLib.Utils;
using Xunit;
using ZenoniaProto;
using ZenoniaProto.Net;

/// <summary>Protocol 22: the WantedStatus message, the server rule that fills it (WantedStatusRules) and the client's copy with its own countdowns (WantedClientStatus).</summary>
public class WantedStatusTests
{
    const long SevenHours = 7L * 3_600_000L;

    static NetDataReader Reader(NetDataWriter w)
    {
        var r = new NetDataReader(); r.SetSource(w.CopyData());
        byte op; Assert.True(NetMessageReader.ReadOpcode(r, out op)); Assert.Equal(NetOp.WantedStatus, op);
        return r;
    }

    [Fact]
    public void TheOpcodeIsAppendedAfterTradeStateAndTheVersionIsAtLeast22()
    {
        Assert.Equal(25, NetOp.WantedStatus);
        Assert.Equal(24, NetOp.TradeState);
        Assert.True(NetProtocol.Version >= 22); // 23 added the appearance colours (MessageRoundTripTests pins the exact number)
    }

    [Fact]
    public void RoundTripsAliveAndRespawning()
    {
        var w = new NetDataWriter();
        new WantedStatusMsg { BossType = "glacialColossus", DayKey = 20773, Alive = true, ResetInMs = 27_735_000 }.Write(w);
        WantedStatusMsg m; Assert.True(WantedStatusMsg.TryRead(Reader(w), out m));
        Assert.Equal("glacialColossus", m.BossType); Assert.Equal(20773, m.DayKey); Assert.True(m.Alive); Assert.Equal(0u, m.RespawnInMs); Assert.Equal(27_735_000u, m.ResetInMs);

        new WantedStatusMsg { BossType = "mireHydraQueen", DayKey = 20774, Alive = false, RespawnInMs = 1_800_000, ResetInMs = uint.MaxValue }.Write(w);
        Assert.True(WantedStatusMsg.TryRead(Reader(w), out m));
        Assert.Equal("mireHydraQueen", m.BossType); Assert.Equal(20774, m.DayKey); Assert.False(m.Alive); Assert.Equal(1_800_000u, m.RespawnInMs); Assert.Equal(uint.MaxValue, m.ResetInMs);
        Assert.Equal(1 + (2 + "mireHydraQueen".Length) + 4 + 1 + 4 + 4, w.Length); // opcode, string (LiteNetLib: ushort length + bytes), day, alive, two countdowns: 30 bytes
    }

    [Fact]
    public void RefusesNoBossNoDayABadFlagAndTruncation()
    {
        var w = new NetDataWriter(); WantedStatusMsg m;
        new WantedStatusMsg { BossType = "", DayKey = 20773, Alive = true }.Write(w);
        Assert.False(WantedStatusMsg.TryRead(Reader(w), out m));
        new WantedStatusMsg { BossType = "rimeclawTyrant", DayKey = 0, Alive = true }.Write(w);
        Assert.False(WantedStatusMsg.TryRead(Reader(w), out m));
        w.Reset(); w.Put(NetOp.WantedStatus); w.Put("rimeclawTyrant", WantedStatusMsg.MaxBossTypeLength); w.Put(20773); w.Put((byte)2); w.Put(0u); w.Put(0u);
        Assert.False(WantedStatusMsg.TryRead(Reader(w), out m));
        w.Reset(); w.Put(NetOp.WantedStatus); w.Put("rimeclawTyrant", WantedStatusMsg.MaxBossTypeLength); w.Put(20773); w.Put((byte)1); w.Put(0u);
        Assert.False(WantedStatusMsg.TryRead(Reader(w), out m)); // the last countdown is missing
    }

    [Fact]
    public void GarbageNeverThrows()
    {
        var rng = new System.Random(22);
        for (int i = 0; i < 2000; i++)
        {
            var data = new byte[rng.Next(0, 40)]; rng.NextBytes(data);
            var r = new NetDataReader(); r.SetSource(data);
            WantedStatusMsg m; WantedStatusMsg.TryRead(r, out m);
        }
    }

    [Fact]
    public void AStandingBossIsAliveWhateverTheClockSays()
    {
        WantedStatusMsg m = WantedStatusRules.Build("rimeclawTyrant", 20773, true, false, -1f, 0f, SevenHours);
        Assert.True(m.Alive); Assert.Equal(0u, m.RespawnInMs); Assert.Equal((uint)SevenHours, m.ResetInMs);
        Assert.Equal("rimeclawTyrant", m.BossType); Assert.Equal(20773, m.DayKey);
    }

    [Fact]
    public void AKilledBossCountsDownItsRespawnClock()
    {
        WantedStatusMsg m = WantedStatusRules.Build("rimeclawTyrant", 20773, false, true, 1_799_999.2f, 0f, SevenHours);
        Assert.False(m.Alive); Assert.Equal(1_800_000u, m.RespawnInMs); // rounded up: never "0 s" while it is still waiting
    }

    [Fact]
    public void ASleepingZoneOwesItsSleepToTheRespawnClock()
    {
        // The zone paused 2 s after the kill with 29:58 left and has slept 20 minutes: 9:58 are left, although its own timer has not moved.
        WantedStatusMsg m = WantedStatusRules.Build("rimeclawTyrant", 20773, false, true, 1_798_000f, 1_200_000f, SevenHours);
        Assert.False(m.Alive); Assert.Equal(598_000u, m.RespawnInMs);
        // Slept past the due time: waking the zone places the boss at once, so it counts as alive already.
        m = WantedStatusRules.Build("rimeclawTyrant", 20773, false, true, 1_798_000f, 1_798_000f, SevenHours);
        Assert.True(m.Alive); Assert.Equal(0u, m.RespawnInMs);
        m = WantedStatusRules.Build("rimeclawTyrant", 20773, false, true, 1_798_000f, 9_000_000f, SevenHours);
        Assert.True(m.Alive);
    }

    [Fact]
    public void NoBossAndNoClockMeansTheSpawnerIsAboutToPlaceIt()
    {
        // The day just turned (or the server just started): the new boss's zone has neither a boss nor a clock yet.
        WantedStatusMsg m = WantedStatusRules.Build("glacialColossus", 20774, false, false, -1f, 5_000_000f, 86_400_000L);
        Assert.True(m.Alive); Assert.Equal(0u, m.RespawnInMs);
    }

    [Fact]
    public void TheResetCountdownIsClampedToTheWire()
    {
        Assert.Equal(0u, WantedStatusRules.Build("a", 1, true, false, 0f, 0f, -5L).ResetInMs);
        Assert.Equal(uint.MaxValue, WantedStatusRules.Build("a", 1, true, false, 0f, 0f, long.MaxValue).ResetInMs);
    }

    [Fact]
    public void OnlyAnotherBossAnotherDayOrDeathAndReturnAreNews()
    {
        WantedStatusMsg alive = WantedStatusRules.Build("rimeclawTyrant", 20773, true, false, 0f, 0f, SevenHours);
        WantedStatusMsg later = WantedStatusRules.Build("rimeclawTyrant", 20773, true, false, 0f, 0f, SevenHours - 60_000L);
        WantedStatusMsg dead = WantedStatusRules.Build("rimeclawTyrant", 20773, false, true, 1_800_000f, 0f, SevenHours);
        WantedStatusMsg deadLater = WantedStatusRules.Build("rimeclawTyrant", 20773, false, true, 900_000f, 0f, SevenHours);
        WantedStatusMsg nextDay = WantedStatusRules.Build("glacialColossus", 20774, true, false, 0f, 0f, 86_400_000L);
        WantedStatusMsg pinnedNextDay = WantedStatusRules.Build("rimeclawTyrant", 20774, true, false, 0f, 0f, 86_400_000L);
        Assert.False(WantedStatusRules.IsNews(alive, later));
        Assert.False(WantedStatusRules.IsNews(dead, deadLater));
        Assert.True(WantedStatusRules.IsNews(alive, dead));
        Assert.True(WantedStatusRules.IsNews(dead, alive));
        Assert.True(WantedStatusRules.IsNews(alive, nextDay));
        Assert.True(WantedStatusRules.IsNews(alive, pinnedNextDay)); // a pinned boss: the day still turns, yesterday's claims are over
    }

    [Fact]
    public void TheClientCountsDownFromTheMomentOfArrival()
    {
        var s = new WantedClientStatus();
        Assert.False(s.Known); Assert.Equal(0L, s.RespawnLeftMs(5000.0)); Assert.Equal(0L, s.ResetLeftMs(5000.0));

        int v = s.Version;
        s.Apply(new WantedStatusMsg { BossType = "rimeclawTyrant", DayKey = 20773, Alive = false, RespawnInMs = 600_000, ResetInMs = 3_600_000 }, 10_000.0);
        Assert.True(s.Known); Assert.True(s.Version > v);
        Assert.Equal("rimeclawTyrant", s.BossType); Assert.Equal(20773, s.DayKey); Assert.False(s.Alive);
        Assert.Equal(600_000L, s.RespawnLeftMs(10_000.0));
        Assert.Equal(540_000L, s.RespawnLeftMs(70_000.0));
        Assert.Equal(3_540_000L, s.ResetLeftMs(70_000.0));
        Assert.Equal(0L, s.RespawnLeftMs(10_000.0 + 600_000.0)); // due: never negative
        Assert.Equal(0L, s.RespawnLeftMs(99_000_000.0));
        Assert.Equal(600_000L, s.RespawnLeftMs(9_000.0));        // a clock read before the arrival does not add time

        s.Apply(new WantedStatusMsg { BossType = "rimeclawTyrant", DayKey = 20773, Alive = true, RespawnInMs = 0, ResetInMs = 3_000_000 }, 610_000.0);
        Assert.True(s.Alive); Assert.Equal(0L, s.RespawnLeftMs(610_000.0)); Assert.Equal(2_999_000L, s.ResetLeftMs(611_000.0));

        v = s.Version;
        s.Clear();
        Assert.False(s.Known); Assert.Null(s.BossType); Assert.Equal(0, s.DayKey); Assert.Equal(0L, s.ResetLeftMs(611_000.0)); Assert.True(s.Version > v);
    }

    [Fact]
    public void AnIdleClockTellsHowLongItsZoneHasSlept()
    {
        var idle = new ZoneIdleClock();
        Assert.Equal(0f, idle.SleptMs(5000f)); // awake
        idle.MarkPaused(5000f);
        Assert.Equal(0f, idle.SleptMs(4000f));
        Assert.Equal(1_200_000f, idle.SleptMs(1_205_000f));
        Assert.Equal(idle.SleptMs(1_205_000f), idle.Wake(1_205_000f)); // exactly what waking pays back
        Assert.Equal(0f, idle.SleptMs(1_300_000f));
    }
}
