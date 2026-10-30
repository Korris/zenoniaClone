using Xunit;
using ZenoniaProto.Net;

public class AdaptiveDelayTests
{
    [Fact]
    public void StartsAtTwoSnapshotIntervalsAndStaysThereOnAPerfectLink()
    {
        var d = new AdaptiveInterpolationDelay(66.7);
        for (int i = 0; i < 200; i++) d.OnSnapshot(-5000.0);       // constant transit: zero jitter
        Assert.InRange(d.DelayMs, 133.0, 134.0);
        Assert.InRange(d.JitterMs, 0.0, 0.01);
    }

    [Fact]
    public void GrowsWithJitterWithinTheClampAndShrinksBackSlowly()
    {
        var d = new AdaptiveInterpolationDelay(66.7);
        var rng = new System.Random(3);
        for (int i = 0; i < 300; i++) d.OnSnapshot(rng.NextDouble() * 80.0);   // 0..80 ms of arrival jitter
        Assert.InRange(d.DelayMs, 180.0, AdaptiveInterpolationDelay.MaxMs);
        double rough = d.DelayMs;
        for (int i = 0; i < 15; i++) d.OnSnapshot(100.0);
        Assert.True(d.DelayMs > rough * 0.7, "a calm second must not collapse the delay at once");
        for (int i = 0; i < 600; i++) d.OnSnapshot(100.0);
        Assert.InRange(d.DelayMs, 133.0, 140.0);
    }

    [Fact]
    public void NeverLeavesTheOneTwentyToTwoFiftyWindow()
    {
        var fast = new AdaptiveInterpolationDelay(20.0);
        Assert.Equal(AdaptiveInterpolationDelay.MinMs, fast.DelayMs);
        var wild = new AdaptiveInterpolationDelay(66.7);
        for (int i = 0; i < 100; i++) wild.OnSnapshot(i % 2 == 0 ? 0.0 : 2000.0);
        Assert.Equal(AdaptiveInterpolationDelay.MaxMs, wild.DelayMs, 3);
    }

    [Fact]
    public void BufferRendersBehindTheServerClockByTheCurrentDelay()
    {
        var b = new SnapshotBuffer();
        var m = new SnapshotMsg { Seq = 1, ServerTimeMs = 1000 };
        b.Push(m, 100);
        Assert.Equal(b.ServerNow(500) - b.RenderDelayMs, b.RenderTime(500), 6);
    }
}
