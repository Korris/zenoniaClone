using System.Collections.Generic;
using LiteNetLib.Utils;
using Xunit;
using ZenoniaProto.Net;

/// <summary>Protocol 38: the Ground skill point rides in the input frame that presses the skill, and SkillCast events carry the clamped centre.</summary>
public class GroundAimProtocolTests
{
    static NetDataReader Reader(NetDataWriter w, byte expectedOp)
    {
        var r = new NetDataReader(); r.SetSource(w.CopyData());
        byte op; Assert.True(NetMessageReader.ReadOpcode(r, out op)); Assert.Equal(expectedOp, op);
        return r;
    }

    [Fact]
    public void AimedFrameRoundTripsWithFourMoreBytes()
    {
        var red = new InputRedundancy();
        red.Push(InputBits.Up);
        red.Push(InputBits.WithSkill(InputBits.Left, 3), true, 123.5f, -40.25f);
        red.Push(InputBits.Attack, true, 9f, 9f); // no skill press: the point is dropped
        var frames = new InputFrame[NetProtocol.MaxInputFrames];
        int n = red.Fill(frames);
        Assert.Equal(3, n);
        Assert.False(frames[0].HasAim); Assert.True(frames[1].HasAim); Assert.False(frames[2].HasAim);
        var w = new NetDataWriter();
        NetMessageWriter.Input(w, 1, 2, frames, n);
        Assert.Equal(1 + 1 + 2 + 2 + 1 + 3 * InputFrame.WireBytes + 1 + InputFrame.AimWireBytes, w.Length);
        var buf = new InputFrame[NetProtocol.MaxInputFrames]; byte epoch; ushort ack; int count;
        Assert.True(NetMessageReader.TryInput(Reader(w, NetOp.Input), out epoch, out ack, buf, out count));
        Assert.True(buf[1].HasAim); Assert.False(buf[0].HasAim); Assert.False(buf[2].HasAim);
        Assert.Equal(123.5f, buf[1].AimWorldX); Assert.Equal(-40.25f, buf[1].AimWorldY);
        Assert.Equal(3, InputBits.SkillIndex(buf[1].Bits));
    }

    [Fact]
    public void InputRejectsAnAimForAMissingFrameOrATruncatedPoint()
    {
        var w = new NetDataWriter(); var buf = new InputFrame[NetProtocol.MaxInputFrames]; byte epoch; ushort ack; int n;
        w.Put(NetOp.Input); w.Put((byte)0); w.Put((ushort)1); w.Put((ushort)1); w.Put((byte)1); w.Put((ushort)InputBits.WithSkill(0, 2)); w.Put((byte)2); // bit 1 set, 1 frame
        Assert.False(NetMessageReader.TryInput(Reader(w, NetOp.Input), out epoch, out ack, buf, out n));
        w.Reset(); w.Put(NetOp.Input); w.Put((byte)0); w.Put((ushort)1); w.Put((ushort)1); w.Put((byte)1); w.Put((ushort)InputBits.WithSkill(0, 2)); w.Put((byte)1); w.Put((short)5);
        Assert.False(NetMessageReader.TryInput(Reader(w, NetOp.Input), out epoch, out ack, buf, out n));
    }

    static InputFrame Aimed(uint seq, int skill, short x, short y) { return new InputFrame { Seq = seq, Bits = InputBits.WithSkill(0, skill), HasAim = true, AimX = x, AimY = y }; }

    [Fact]
    public void ServerSeesThePointOnlyOnItsFrame()
    {
        var s = new ServerInputState();
        s.Accept(new[] { new InputFrame { Seq = 1 }, Aimed(2, 4, 800, -16), new InputFrame { Seq = 3 } }, 3, 0);
        Assert.True(s.Advance(0)); Assert.False(s.CurrentAim.HasAim);
        Assert.True(s.Advance(0)); Assert.True(s.CurrentAim.HasAim); Assert.Equal(100f, s.CurrentAim.AimWorldX); Assert.Equal(-2f, s.CurrentAim.AimWorldY);
        Assert.True(s.Advance(0)); Assert.False(s.CurrentAim.HasAim);
    }

    [Fact]
    public void ACutFrameKeepsTheAimOfTheSkillPressItCarries()
    {
        var s = new ServerInputState();
        // frame 2 (skill 3, aimed) and 3 (skill 6, aimed elsewhere) are cut; the first press survives (InputBits.Merge) and so must its point
        s.Accept(new[] { new InputFrame { Seq = 1 }, Aimed(2, 3, 80, 80), Aimed(3, 6, 400, 400), new InputFrame { Seq = 4 }, new InputFrame { Seq = 5 } }, 5, 0);
        s.Accept(new[] { new InputFrame { Seq = 6 }, new InputFrame { Seq = 7 }, new InputFrame { Seq = 8 }, new InputFrame { Seq = 9 }, new InputFrame { Seq = 10 } }, 5, 1);
        int seen = 0;
        while (s.Queued > 0)
        {
            Assert.True(s.Advance(1));
            if (InputBits.SkillIndex(s.Current) < 0) { Assert.False(s.CurrentAim.HasAim); continue; }
            seen++;
            Assert.Equal(3, InputBits.SkillIndex(s.Current));
            Assert.True(s.CurrentAim.HasAim); Assert.Equal(10f, s.CurrentAim.AimWorldX);
        }
        Assert.Equal(1, seen);
    }

    [Fact]
    public void SkillCastCarriesTheGroundCentreOnlyWhenSet()
    {
        var plain = new NetEvent { Type = NetEventType.SkillCast, Id = 1, Step = SkillCastCode.Pack(2, 3), X = 7, Y = 8, Facing = 1, Other = 99 };
        var ground = plain; ground.Id = 2;
        SkillCastCode.SetGround(ref ground, 1234.5f, -77.125f);
        var w = new NetDataWriter();
        Assert.Equal(2, NetMessageWriter.EventBatch(w, 10, new List<NetEvent> { plain, ground }, 0));
        var got = new List<NetEvent>(); uint t;
        Assert.True(NetMessageReader.TryEventBatch(Reader(w, NetOp.EventBatch), out t, got));
        float x, y;
        Assert.False(SkillCastCode.TryGround(got[0], out x, out y));
        Assert.True(SkillCastCode.TryGround(got[1], out x, out y));
        Assert.Equal(1234.5f, x); Assert.Equal(-77.125f, y);
        Assert.Equal(99, got[1].Other); Assert.Equal(2, SkillCastCode.Index(got[1].Step));
    }
}
