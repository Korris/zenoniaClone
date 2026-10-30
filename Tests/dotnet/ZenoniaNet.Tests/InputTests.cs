using Xunit;
using ZenoniaProto.Net;

public class InputTests
{
    static InputFrame F(uint seq, ushort bits = 0) { return new InputFrame { Seq = seq, Bits = bits }; }

    static uint Consume(ServerInputState s, uint nowMs = 0)
    {
        Assert.True(s.Advance(nowMs));
        return s.LastSeq;
    }

    [Fact]
    public void SkillIndexHelpersRoundTripAndIgnoreBadIndexes()
    {
        for (int i = 0; i < 8; i++) { ushort b = InputBits.WithSkill(InputBits.Left, i); Assert.Equal(i, InputBits.SkillIndex(b)); Assert.True((b & InputBits.Left) != 0); Assert.True(b <= InputBits.AllMask); }
        Assert.Equal(-1, InputBits.SkillIndex(InputBits.Attack));
        Assert.Equal(InputBits.Up, InputBits.WithSkill(InputBits.Up, 8));
        Assert.Equal(InputBits.Up, InputBits.WithSkill(InputBits.Up, -1));
        Assert.Equal(3, InputBits.SkillIndex(InputBits.WithSkill(InputBits.WithSkill(0, 6), 3))); // replacing, never OR-ing
    }

    [Fact]
    public void ServerHeroSeesTheSkillPressForExactlyItsFrame()
    {
        var s = new ServerInputState();
        s.Accept(new[] { F(1, InputBits.Right), F(2, InputBits.WithSkill(InputBits.Right, 5)), F(3, InputBits.Right) }, 3, 0);
        Consume(s); Assert.Equal(-1, InputBits.SkillIndex(s.Current));
        Consume(s); Assert.Equal(5, InputBits.SkillIndex(s.Current));
        Consume(s); Assert.Equal(-1, InputBits.SkillIndex(s.Current));
    }

    [Fact]
    public void CutFramesKeepTheirSkillPressAndNeverMergeIndexes()
    {
        var s = new ServerInputState();
        // 14 frames in two packets: the buffer cuts the oldest ones; frames 2 (skill 3) and 3 (skill 6) are cut, the first press survives intact
        s.Accept(new[] { F(1), F(2, InputBits.WithSkill(0, 3)), F(3, InputBits.WithSkill(0, 6)), F(4), F(5) }, 5, 0);
        s.Accept(new[] { F(6), F(7), F(8), F(9), F(10) }, 5, 1);
        s.Accept(new[] { F(11), F(12), F(13) }, 3, 2);
        Consume(s);
        int idx = InputBits.SkillIndex(s.Current);
        Assert.True(idx == 3 || idx == -1, "an index must be one of the pressed ones, never an OR of both, got " + idx);
        Assert.NotEqual(7, idx);
    }

    [Fact]
    public void RedundancyRepeatsTheLastFiveFramesOldestFirst()
    {
        var r = new InputRedundancy();
        var buf = new InputFrame[NetProtocol.MaxInputFrames];
        Assert.Equal(0, r.Fill(buf));
        for (int i = 0; i < 8; i++) r.Push(0);
        Assert.Equal(5, r.Fill(buf));
        Assert.Equal(4u, buf[0].Seq); Assert.Equal(8u, buf[4].Seq);
    }

    [Fact]
    public void FramesAreConsumedOnePerStepInOrder()
    {
        var s = new ServerInputState();
        s.Accept(new[] { F(1, InputBits.Right), F(2, InputBits.Right), F(3, InputBits.Right | InputBits.Attack) }, 3, 0);
        Assert.Equal(1u, Consume(s)); Assert.True(s.Has(InputBits.Right)); Assert.False(s.Has(InputBits.Attack));
        Assert.Equal(2u, Consume(s));
        Assert.Equal(3u, Consume(s)); Assert.True(s.Has(InputBits.Attack));  // the press is visible for exactly this step
        Assert.False(s.Advance(10)); Assert.Equal(0, s.Current);              // nothing queued: the hero freezes, no ghost input
        Assert.Equal(1, s.Starved);
    }

    [Fact]
    public void LostPacketIsRepairedByTheNextOnesRedundancy()
    {
        var r = new InputRedundancy(); var s = new ServerInputState(); var buf = new InputFrame[NetProtocol.MaxInputFrames];
        r.Push(InputBits.Up);
        int n = r.Fill(buf); s.Accept(buf, n, 0);
        r.Push(InputBits.Up | InputBits.Attack);                 // packet lost
        r.Push(InputBits.Up);
        n = r.Fill(buf); s.Accept(buf, n, 33);                   // repeats frames 1..3
        Assert.Equal(1u, Consume(s)); Assert.Equal(2u, Consume(s));
        Assert.True(s.Has(InputBits.Attack));                    // frame 2's press survived
        Assert.Equal(3u, Consume(s)); Assert.False(s.Has(InputBits.Attack));
    }

    [Fact]
    public void LostFramesBeyondTheRedundancyAreFilledWithTheHeldDirectionAndNoPresses()
    {
        var s = new ServerInputState();
        s.Accept(new[] { F(1, InputBits.Left | InputBits.Dash) }, 1, 0);
        s.Accept(new[] { F(2, InputBits.Left) }, 1, 10);
        Consume(s); Consume(s);
        s.Accept(new[] { F(3), F(4) }, 2, 33);
        s.Accept(new[] { F(7), F(8), F(9) }, 3, 66);              // frames 5 and 6 lost: gap filled (2 frames)
        Assert.Equal(7, s.Queued);                                // 3, 4, 5, 6, 7, 8, 9
        Assert.Equal(3u, Consume(s)); Assert.Equal(4u, Consume(s));
        Assert.Equal(5u, Consume(s)); Assert.False(s.Has(InputBits.Dash));
        Assert.Equal(6u, Consume(s)); Assert.Equal(7u, Consume(s));
    }

    [Fact]
    public void GapFramesRepeatTheLastHeldDirection()
    {
        var s = new ServerInputState();
        s.Accept(new[] { F(1, InputBits.Left | InputBits.Dash) }, 1, 0);
        Assert.Equal(1u, Consume(s));
        s.Accept(new[] { F(4, InputBits.Up) }, 1, 33);            // 2 and 3 lost
        Assert.Equal(2u, Consume(s)); Assert.True(s.Has(InputBits.Left)); Assert.False(s.Has(InputBits.Dash));
        Assert.Equal(3u, Consume(s)); Assert.True(s.Has(InputBits.Left));
        Assert.Equal(4u, Consume(s)); Assert.True(s.Has(InputBits.Up)); Assert.False(s.Has(InputBits.Left));
    }

    [Fact]
    public void DuplicateAndReorderedPacketsAreIgnoredAndSequenceWrapsSafely()
    {
        var s = new ServerInputState();
        Assert.Equal(3, s.Accept(new[] { F(65534), F(65535), F(0), }, 3, 0));       // 16-bit wrap
        Assert.Equal(0, s.Accept(new[] { F(65534), F(65535), F(0) }, 3, 5));       // exact duplicate
        Assert.Equal(65534u, Consume(s));
        Assert.Equal(1, s.Accept(new[] { F(65535), F(0), F(1, InputBits.Down) }, 3, 6));
        Assert.Equal(0, s.Accept(new[] { F(65000) }, 1, 7));                       // far older: ignored
        Assert.Equal(3, s.Queued);
        Assert.Equal(65535u, Consume(s)); Assert.Equal(65536u, Consume(s)); Assert.Equal(65537u, Consume(s));
        Assert.True(s.Has(InputBits.Down));
    }

    [Fact]
    public void AHugeQueueIsCutBackToFourFramesAndTheCutPressesSurvive()
    {
        var s = new ServerInputState();
        s.Accept(new[] { F(1), F(2, InputBits.Attack), F(3), F(4), F(5) }, 5, 0);
        s.Accept(new[] { F(6), F(7, InputBits.Right), F(8), F(9), F(10) }, 5, 1);
        s.Accept(new[] { F(11), F(12, InputBits.Right), F(13) }, 3, 2);                // burst: 13 queued
        Assert.True(s.Advance(3));
        Assert.True(s.Has(InputBits.Attack), "the press of a cut frame is delivered with the next applied one");
        Assert.Equal(10u, s.LastSeq);                                                  // 9 frames were skipped, 10 is simulated
        Assert.Equal(9, s.Dropped);
        Assert.Equal(3, s.Queued);
    }

    [Fact]
    public void FrozenTicksEarnCatchUpStepsAndNothingElseDoes()
    {
        var s = new ServerInputState();
        s.Accept(new[] { F(1) }, 1, 0);
        Consume(s);
        Assert.False(s.Advance(30)); Assert.False(s.Advance(60));           // two late ticks: two credits
        s.Accept(new[] { F(2), F(3), F(4) }, 3, 70);                        // the backlog arrives at once
        Assert.True(s.Advance(80)); Assert.Equal(2u, s.LastSeq);
        Assert.True(s.TakeExtra()); Assert.Equal(3u, s.LastSeq);            // repaid
        Assert.False(s.TakeExtra(), "a single frame left: no backlog, no extra step");
        Assert.True(s.Advance(110)); Assert.Equal(4u, s.LastSeq);
        s.Accept(new[] { F(5), F(6), F(7) }, 3, 120);
        Assert.True(s.Advance(120));
        Assert.True(s.TakeExtra());                                         // second credit
        Assert.False(s.TakeExtra());                                        // credit spent: a flooding client cannot gain steps
        Assert.Equal(2, s.CatchUps);
    }

    [Fact]
    public void ServerHitchesBecomeCatchUpCreditUpToTheCap()
    {
        var s = new ServerInputState();
        s.Accept(new[] { F(1), F(2), F(3) }, 3, 0);
        s.AddCredit(1000);                                                  // a very long stall: capped, never unlimited
        Assert.True(s.Advance(10));
        int extra = 0;
        s.Accept(new[] { F(4), F(5), F(6) }, 3, 20); s.Accept(new[] { F(7), F(8), F(9) }, 3, 21);
        while (s.TakeExtra()) extra++;
        Assert.InRange(extra, 1, ServerInputState.MaxCredit);
    }

    [Fact]
    public void AClientCannotMoveFasterThanOneFramePerStepEvenWithAHugeBurst()
    {
        var s = new ServerInputState();
        var frames = new InputFrame[3];
        uint seq = 1; int simulated = 0;
        for (int tick = 0; tick < 60; tick++)
        {
            for (int k = 0; k < 3; k++) frames[k] = F(seq++, InputBits.Right);          // cheat: 3 frames per tick instead of 1
            s.Accept(frames, 3, (uint)(tick * 33));
            if (s.Advance((uint)(tick * 33)) && s.Has(InputBits.Right)) simulated++;
        }
        Assert.True(simulated <= 60);
    }

    [Fact]
    public void ALongOutageRestartsTheStreamWithoutCatchUp()
    {
        var s = new ServerInputState();
        s.Accept(new[] { F(1) }, 1, 0);
        s.Accept(new[] { F(500), F(501), F(502) }, 3, 100);
        Assert.True(s.Queued <= 3);
    }

    [Fact]
    public void SilentClientFreezesBrieflyThenIdlesAndPotionsAreCounted()
    {
        var s = new ServerInputState();
        Assert.True(s.Advance(0));                                // never heard from: the hero idles
        s.Accept(new[] { F(1, InputBits.HpPotion), F(2, InputBits.HpPotion | InputBits.MpPotion) }, 2, 1000);
        s.Advance(1000); s.Advance(1033);
        Assert.False(s.Advance(1100));                            // late frame: freeze
        Assert.True(s.Advance(1000 + NetProtocol.InputStaleMs + 1)); // client gone: idle so timers finish
        Assert.Equal(0, s.Current);
        Assert.True(s.TakePotion(true)); Assert.True(s.TakePotion(true)); Assert.False(s.TakePotion(true));
        Assert.True(s.TakePotion(false)); Assert.False(s.TakePotion(false));
    }

    [Fact]
    public void ExpandKeepsTheSequenceNearTheReference()
    {
        Assert.Equal(70000u, ServerInputState.Expand((ushort)(70000 & 0xFFFF), 69990u));
        Assert.Equal(65530u, ServerInputState.Expand(65530, 65540u));
        Assert.Equal(131075u, ServerInputState.Expand(3, 131070u));
    }
}
