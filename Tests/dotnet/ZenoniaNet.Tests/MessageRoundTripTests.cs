using System.Collections.Generic;
using LiteNetLib.Utils;
using Xunit;
using ZenoniaProto.Net;

public class MessageRoundTripTests
{
    static NetDataReader Reader(NetDataWriter w, byte expectedOp)
    {
        var r = new NetDataReader(); r.SetSource(w.CopyData());
        byte op; Assert.True(NetMessageReader.ReadOpcode(r, out op)); Assert.Equal(expectedOp, op);
        return r;
    }

    [Fact]
    public void Hello()
    {
        var w = new NetDataWriter();
        NetMessageWriter.Hello(w, new HelloMsg { Version = NetProtocol.Version, Name = "Tay", HeroClass = 3, Token = "dev-key" });
        HelloMsg m; Assert.True(NetMessageReader.TryHello(Reader(w, NetOp.Hello), out m));
        Assert.Equal("Tay", m.Name); Assert.Equal(3, m.HeroClass); Assert.Equal("dev-key", m.Token); Assert.Equal(NetProtocol.Version, m.Version);
    }

    [Fact]
    public void InputPacketCarriesFramesAtTwoBytesEach()
    {
        var w = new NetDataWriter();
        var frames = new[]
        {
            new InputFrame { Seq = 7, Bits = InputBits.Left | InputBits.Skill },
            new InputFrame { Seq = 8 }, new InputFrame { Seq = 9, Bits = InputBits.Up | InputBits.Attack | InputBits.MpPotion },
        };
        NetMessageWriter.Input(w, 41, frames, 3);
        Assert.Equal(1 + 2 + 2 + 1 + 3 * InputFrame.WireBytes, w.Length);
        var buf = new InputFrame[NetProtocol.MaxInputFrames]; ushort ack; int n;
        Assert.True(NetMessageReader.TryInput(Reader(w, NetOp.Input), out ack, buf, out n));
        Assert.Equal(41, ack); Assert.Equal(3, n);
        Assert.Equal(7u, buf[0].Seq); Assert.Equal(8u, buf[1].Seq); Assert.Equal(9u, buf[2].Seq);
        Assert.Equal(frames[0].Bits, buf[0].Bits); Assert.Equal(frames[2].Bits, buf[2].Bits);
    }

    [Fact]
    public void InputSequenceWrapsAtSixteenBits()
    {
        var w = new NetDataWriter();
        var frames = new[] { new InputFrame { Seq = 65535 }, new InputFrame { Seq = 65536 }, new InputFrame { Seq = 65537 } };
        NetMessageWriter.Input(w, 0, frames, 3);
        var buf = new InputFrame[NetProtocol.MaxInputFrames]; ushort ack; int n;
        Assert.True(NetMessageReader.TryInput(Reader(w, NetOp.Input), out ack, buf, out n));
        Assert.Equal(65535u, buf[0].Seq); Assert.Equal(0u, buf[1].Seq); Assert.Equal(1u, buf[2].Seq);
    }

    [Fact]
    public void InputRejectsBadFrameCountsAndTruncation()
    {
        var w = new NetDataWriter(); var buf = new InputFrame[NetProtocol.MaxInputFrames]; ushort ack; int n;
        w.Put(NetOp.Input); w.Put((ushort)1); w.Put((ushort)1); w.Put((byte)0);
        Assert.False(NetMessageReader.TryInput(Reader(w, NetOp.Input), out ack, buf, out n));
        w.Reset(); w.Put(NetOp.Input); w.Put((ushort)1); w.Put((ushort)1); w.Put((byte)(NetProtocol.MaxInputFrames + 1));
        Assert.False(NetMessageReader.TryInput(Reader(w, NetOp.Input), out ack, buf, out n));
        w.Reset(); w.Put(NetOp.Input); w.Put((ushort)1); w.Put((ushort)1); w.Put((byte)2); w.Put((ushort)5);
        Assert.False(NetMessageReader.TryInput(Reader(w, NetOp.Input), out ack, buf, out n));
    }

    [Fact]
    public void SkillInvActionsRoundTripAndOneBeyondTheLastIsRejected()
    {
        var w = new NetDataWriter();
        foreach (InvOp op in new[] { InvOp.SkillLearn, InvOp.SkillAssign, InvOp.SkillRespec })
        {
            NetMessageWriter.InvAction(w, new InvActionMsg { ReqId = 5, Op = op, ItemUid = "3", Slot = 255 });
            InvActionMsg m; Assert.True(NetMessageReader.TryInvAction(Reader(w, NetOp.InvAction), out m));
            Assert.Equal(op, m.Op); Assert.Equal("3", m.ItemUid); Assert.Equal(255, m.Slot);
        }
        w.Reset(); w.Put(NetOp.InvAction); w.Put(1u); w.Put((byte)InvOp.SkillRespec + 1); w.Put("x"); w.Put((byte)0);
        InvActionMsg bad; Assert.False(NetMessageReader.TryInvAction(Reader(w, NetOp.InvAction), out bad));
        Assert.Equal(3, NetProtocol.Version);
    }

    [Fact]
    public void InvActionRoundTripAndBadOpRejected()
    {
        var w = new NetDataWriter();
        NetMessageWriter.InvAction(w, new InvActionMsg { ReqId = 99, Op = InvOp.Enhance, ItemUid = "abc-123", Slot = 2 });
        InvActionMsg m; Assert.True(NetMessageReader.TryInvAction(Reader(w, NetOp.InvAction), out m));
        Assert.Equal(99u, m.ReqId); Assert.Equal(InvOp.Enhance, m.Op); Assert.Equal("abc-123", m.ItemUid); Assert.Equal(2, m.Slot);
        w.Reset(); w.Put(NetOp.InvAction); w.Put(1u); w.Put((byte)77); w.Put("x"); w.Put((byte)0);
        Assert.False(NetMessageReader.TryInvAction(Reader(w, NetOp.InvAction), out m));
    }

    [Fact]
    public void InputFrameCarriesTheSkillIndexInTheSameTwoBytes()
    {
        var w = new NetDataWriter();
        var frames = new[]
        {
            new InputFrame { Seq = 1, Bits = InputBits.WithSkill(InputBits.Right, 7) },
            new InputFrame { Seq = 2, Bits = InputBits.WithSkill(InputBits.Attack | InputBits.MpPotion, 0) },
            new InputFrame { Seq = 3, Bits = InputBits.Up },
        };
        NetMessageWriter.Input(w, 0, frames, 3);
        Assert.Equal(1 + 2 + 2 + 1 + 3 * InputFrame.WireBytes, w.Length);
        var buf = new InputFrame[NetProtocol.MaxInputFrames]; ushort ack; int n;
        Assert.True(NetMessageReader.TryInput(Reader(w, NetOp.Input), out ack, buf, out n));
        Assert.Equal(7, InputBits.SkillIndex(buf[0].Bits)); Assert.True((buf[0].Bits & InputBits.Right) != 0);
        Assert.Equal(0, InputBits.SkillIndex(buf[1].Bits)); Assert.True((buf[1].Bits & InputBits.MpPotion) != 0);
        Assert.Equal(-1, InputBits.SkillIndex(buf[2].Bits));
    }

    [Fact]
    public void WelcomeRejectDespawnSelfStateRpgState()
    {
        var w = new NetDataWriter();
        NetMessageWriter.Welcome(w, new WelcomeMsg { PlayerId = 12, HeroNetId = 300, ZoneId = 1, MapId = 2, ServerTimeMs = 123456, TickRate = 30, SnapDiv = 2 });
        WelcomeMsg wm; Assert.True(NetMessageReader.TryWelcome(Reader(w, NetOp.Welcome), out wm));
        Assert.Equal(300, wm.HeroNetId); Assert.Equal(123456u, wm.ServerTimeMs); Assert.Equal(2, wm.SnapDiv);

        NetMessageWriter.Reject(w, new RejectMsg { Reason = RejectReason.Version, Text = "old client" });
        RejectMsg rm; Assert.True(NetMessageReader.TryReject(Reader(w, NetOp.Reject), out rm));
        Assert.Equal(RejectReason.Version, rm.Reason); Assert.Equal("old client", rm.Text);

        NetMessageWriter.Despawn(w, new DespawnMsg { Id = 55, Cause = DespawnCause.PickedUp });
        DespawnMsg dm; Assert.True(NetMessageReader.TryDespawn(Reader(w, NetOp.Despawn), out dm));
        Assert.Equal(55, dm.Id); Assert.Equal(DespawnCause.PickedUp, dm.Cause);

        NetMessageWriter.SelfState(w, new SelfStateMsg { Hp = 120, MaxHp = 200, Mp = 30, MaxMp = 60, Exp = 4000, Level = 9, SkillCds = new ushort[] { 0, 1500, 0, 0, 0, 0, 0, 65535 } });
        SelfStateMsg sm; Assert.True(NetMessageReader.TrySelfState(Reader(w, NetOp.SelfState), out sm));
        Assert.Equal(120, sm.Hp); Assert.Equal(4000u, sm.Exp); Assert.Equal(8, sm.SkillCds.Length); Assert.Equal(1500, sm.SkillCds[1]); Assert.Equal(65535, sm.SkillCds[7]);

        NetMessageWriter.RpgState(w, new RpgStateMsg { ReqId = 4, Result = 1, Text = "ok", SaveJson = "{\"a\":1}" });
        RpgStateMsg pm; Assert.True(NetMessageReader.TryRpgState(Reader(w, NetOp.RpgState), out pm));
        Assert.Equal("{\"a\":1}", pm.SaveJson);
    }

    [Fact]
    public void SpawnPayloadsPerKind()
    {
        var w = new NetDataWriter(); SpawnMsg m;
        new SpawnMsg { Id = 1, Kind = EntityKind.Hero, Type = 2, X = NetQuantize.Pos(100.5f), Y = NetQuantize.Pos(50f), Facing = 3, PlayerId = 8, Name = "Bob", Level = 17, Look = new NetAppearance { WeaponTier = 3, OffTier = 2, EnhanceBand = 4, Flags = 1 } }.Write(w);
        Assert.True(SpawnMsg.TryRead(Reader(w, NetOp.Spawn), out m));
        Assert.Equal("Bob", m.Name); Assert.Equal(17, m.Level); Assert.Equal(4, m.Look.EnhanceBand); Assert.Equal(100.5f, NetQuantize.Pos(m.X));

        new SpawnMsg { Id = 2, Kind = EntityKind.Monster, Type = 5, Hp = 90, MaxHp = 120, State = 2 }.Write(w);
        Assert.True(SpawnMsg.TryRead(Reader(w, NetOp.Spawn), out m));
        Assert.Equal(90, m.Hp); Assert.Equal(120, m.MaxHp); Assert.Equal(2, m.State);

        new SpawnMsg { Id = 3, Kind = EntityKind.Loot, Type = 4, Amount = 250, ItemDefId = 77, Rarity = 3, Enhance = 5, OwnerPlayerId = 9, FreeAtMs = 987654 }.Write(w);
        Assert.True(SpawnMsg.TryRead(Reader(w, NetOp.Spawn), out m));
        Assert.Equal(250, m.Amount); Assert.Equal(9, m.OwnerPlayerId); Assert.Equal(987654u, m.FreeAtMs);
    }

    [Fact]
    public void SpawnNameIsTruncatedToSixteenChars()
    {
        var w = new NetDataWriter(); SpawnMsg m;
        new SpawnMsg { Kind = EntityKind.Hero, Name = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" }.Write(w);
        Assert.True(SpawnMsg.TryRead(Reader(w, NetOp.Spawn), out m));
        Assert.True(m.Name.Length <= NetProtocol.MaxNameLength);
    }

    [Fact]
    public void EveryEventTypeRoundTrips()
    {
        var events = new List<NetEvent>
        {
            new NetEvent { Type = NetEventType.AttackStep, Id = 1, Step = 2, Facing = 3 },
            new NetEvent { Type = NetEventType.BoltFired, Id = 2, Other = 1, Step = 3, X = -40, Y = 90, Angle = 128, Flags = 1 },
            new NetEvent { Type = NetEventType.BoltEnd, Id = 2, X = 10, Y = 20, Flags = 1 },
            new NetEvent { Type = NetEventType.MonsterHit, Id = 300, Other = 1, Dmg = 65000, HpLeft = 12, Flags = HitFlags.Crit | HitFlags.Quiet, KnockX = -30, KnockY = 127 },
            new NetEvent { Type = NetEventType.HeroHit, Id = 1, Dmg = 14, Other = 300 },
            new NetEvent { Type = NetEventType.SkillCast, Id = 1, Step = SkillCastCode.Pack(6, 4), X = 7, Y = 8, Facing = 1, Other = 65001 },
            new NetEvent { Type = NetEventType.HeroDied, Id = 1, X = 5, Y = 6 },
            new NetEvent { Type = NetEventType.HeroRespawned, Id = 1, X = 5, Y = 6 },
            new NetEvent { Type = NetEventType.ExpGain, Value = 123456, Step = 2 },
            new NetEvent { Type = NetEventType.LevelUp, Id = 1, Step = 30 },
            new NetEvent { Type = NetEventType.PotionUsed, Id = 1, Step = 1 },
            new NetEvent { Type = NetEventType.AppearanceChanged, Id = 1, Look = new NetAppearance { WeaponTier = 7, Flags = 1 } },
            new NetEvent { Type = NetEventType.Toast, Text = "Inventory full", Value = 0xFF8855FF },
            new NetEvent { Type = NetEventType.BossSpawned, Id = 400 },
            new NetEvent { Type = NetEventType.BossDied, Id = 400 },
        };
        var w = new NetDataWriter();
        int written = NetMessageWriter.EventBatch(w, 5000, events, 0);
        Assert.Equal(events.Count, written);
        var got = new List<NetEvent>(); uint t;
        Assert.True(NetMessageReader.TryEventBatch(Reader(w, NetOp.EventBatch), out t, got));
        Assert.Equal(5000u, t); Assert.Equal(events.Count, got.Count);
        for (int i = 0; i < events.Count; i++) { Assert.Equal(events[i].Type, got[i].Type); Assert.Equal(events[i].Id, got[i].Id); }
        Assert.Equal(65000, got[3].Dmg); Assert.Equal(-30, got[3].KnockX); Assert.Equal(127, got[3].KnockY);
        Assert.Equal(123456u, got[8].Value); Assert.Equal("Inventory full", got[12].Text); Assert.Equal(0xFF8855FFu, got[12].Value);
        Assert.Equal(7, got[11].Look.WeaponTier);
        Assert.Equal(6, SkillCastCode.Index(got[5].Step)); Assert.Equal(4, SkillCastCode.Rank(got[5].Step)); Assert.Equal(65001, got[5].Other); Assert.Equal(1, got[5].Facing);
    }

    [Fact]
    public void EventBatchSplitsAtSixtyFourAndRejectsUnknownTypes()
    {
        var events = new List<NetEvent>();
        for (int i = 0; i < 100; i++) events.Add(new NetEvent { Type = NetEventType.BossDied, Id = (ushort)i });
        var w = new NetDataWriter();
        Assert.Equal(64, NetMessageWriter.EventBatch(w, 1, events, 0));
        Assert.Equal(36, NetMessageWriter.EventBatch(w, 1, events, 64));
        w.Reset(); w.Put(NetOp.EventBatch); w.Put(1u); w.Put((byte)1); w.Put((byte)200);
        var got = new List<NetEvent>(); uint t;
        Assert.False(NetMessageReader.TryEventBatch(Reader(w, NetOp.EventBatch), out t, got));
    }

    [Fact]
    public void GarbageNeverThrows()
    {
        var rng = new System.Random(7);
        var buf = new InputFrame[3];
        for (int i = 0; i < 2000; i++)
        {
            var data = new byte[rng.Next(0, 40)]; rng.NextBytes(data);
            var r = new NetDataReader(); r.SetSource(data);
            HelloMsg h; SpawnMsg s; WelcomeMsg wm; SelfStateMsg sm; ushort a; int n; uint t; var snap = new SnapshotMsg();
            NetMessageReader.TryHello(r, out h); r.SetPosition(0);
            SpawnMsg.TryRead(r, out s); r.SetPosition(0);
            NetMessageReader.TryWelcome(r, out wm); r.SetPosition(0);
            NetMessageReader.TrySelfState(r, out sm); r.SetPosition(0);
            NetMessageReader.TryInput(r, out a, buf, out n); r.SetPosition(0);
            NetMessageReader.TryEventBatch(r, out t, new List<NetEvent>()); r.SetPosition(0);
            SnapshotPacker.TryRead(r, snap, new SnapshotRing(), new SnapshotLookup());
        }
    }
}
