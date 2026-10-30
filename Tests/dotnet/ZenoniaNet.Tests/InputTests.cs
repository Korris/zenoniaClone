using Xunit;
using ZenoniaProto.Net;

public class InputTests
{
    static InputFrame F(byte attack = 0, byte held = 0) { return new InputFrame { AttackCnt = attack, Held = held, Facing = 255 }; }

    [Fact]
    public void RedundancyRepeatsTheLastThreeFramesOldestFirst()
    {
        var r = new InputRedundancy();
        var buf = new InputFrame[3];
        Assert.Equal(0, r.Fill(buf));
        for (int i = 0; i < 5; i++) r.Push(F());
        Assert.Equal(3, r.Fill(buf));
        Assert.Equal(3u, buf[0].Seq); Assert.Equal(4u, buf[1].Seq); Assert.Equal(5u, buf[2].Seq);
    }

    [Fact]
    public void LostPacketDoesNotDropAPressBecauseTheNextOneRepeatsIt()
    {
        var r = new InputRedundancy();
        var s = new ServerInputState();
        var buf = new InputFrame[3];
        r.Push(F(0));
        int n = r.Fill(buf); s.Accept(buf, n, 0);                 // baseline
        r.Push(F(1));                                              // press, packet lost
        r.Push(F(1));
        n = r.Fill(buf); s.Accept(buf, n, 33);                     // next packet repeats frames 2 + 3
        Assert.True(s.TakePress(PressKind.Attack));
        Assert.False(s.TakePress(PressKind.Attack));               // exactly one press, not two
    }

    [Fact]
    public void DuplicatedPacketDoesNotFireTwice()
    {
        var s = new ServerInputState();
        var frames = new[] { new InputFrame { Seq = 1, AttackCnt = 0 }, new InputFrame { Seq = 2, AttackCnt = 1 } };
        Assert.Equal(2, s.Accept(frames, 2, 0));
        Assert.Equal(0, s.Accept(frames, 2, 10)); // exact duplicate
        Assert.True(s.TakePress(PressKind.Attack));
        Assert.False(s.TakePress(PressKind.Attack));
    }

    [Fact]
    public void CounterWrapsAroundAndSpamIsClamped()
    {
        var s = new ServerInputState();
        s.Accept(new[] { new InputFrame { Seq = 1, DashCnt = 255 } }, 1, 0);
        s.Accept(new[] { new InputFrame { Seq = 2, DashCnt = 1 } }, 1, 10);     // 255 -> 1 = 2 presses
        int taken = 0; while (s.TakePress(PressKind.Dash)) taken++;
        Assert.Equal(2, taken);
        s.Accept(new[] { new InputFrame { Seq = 3, SkillCnt = 200 } }, 1, 20);  // cheat: +200 in one frame
        taken = 0; while (s.TakePress(PressKind.Skill)) taken++;
        Assert.Equal(NetProtocol.MaxPressesPerFrame, taken);
    }

    [Fact]
    public void StaleInputReleasesHeldKeys()
    {
        var s = new ServerInputState();
        s.Accept(new[] { new InputFrame { Seq = 1, Held = InputBits.Right | InputBits.Attack } }, 1, 1000);
        s.Tick(1100);
        Assert.True(s.HeldBit(InputBits.Right));
        s.Tick(1000 + NetProtocol.InputStaleMs + 1);
        Assert.Equal(0, s.Held);
    }

    [Fact]
    public void OutOfOrderOlderFrameIsIgnoredAndSequenceWrapsSafely()
    {
        var s = new ServerInputState();
        s.Accept(new[] { new InputFrame { Seq = uint.MaxValue, Held = InputBits.Up } }, 1, 0);
        Assert.Equal(1, s.Accept(new[] { new InputFrame { Seq = 0, Held = InputBits.Down } }, 1, 5));
        Assert.Equal(0, s.Accept(new[] { new InputFrame { Seq = uint.MaxValue - 1, Held = InputBits.Left } }, 1, 6));
        Assert.Equal(InputBits.Down, s.Held);
    }
}
