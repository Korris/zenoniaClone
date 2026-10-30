using System;
using Xunit;
using ZenoniaProto.Net;

public class QuantizeTests
{
    [Theory]
    [InlineData(0f)] [InlineData(1f)] [InlineData(575.875f)] [InlineData(383.5f)] [InlineData(-12.25f)]
    public void PositionRoundTripsWithinHalfStep(float units)
    {
        Assert.InRange(Math.Abs(NetQuantize.Pos(NetQuantize.Pos(units)) - units), 0f, 0.5f / NetQuantize.PosScale + 1e-4f);
    }

    [Fact]
    public void PositionClampsInsteadOfOverflowing()
    {
        Assert.Equal(short.MaxValue, NetQuantize.Pos(1e9f));
        Assert.Equal(short.MinValue, NetQuantize.Pos(-1e9f));
        Assert.Equal(0, NetQuantize.Pos(float.NaN));
        Assert.True(NetQuantize.MaxPos > 576f); // the whole Frost Hollow map fits
    }

    [Fact]
    public void AngleWrapsAndQuantizesTo256Steps()
    {
        Assert.Equal(0, NetQuantize.Angle(0f));
        Assert.Equal(0, NetQuantize.Angle(360f));
        Assert.Equal(128, NetQuantize.Angle(180f));
        Assert.Equal(192, NetQuantize.Angle(-90f));
        Assert.Equal(0, NetQuantize.Angle(float.NaN));
        Assert.InRange(Math.Abs(NetQuantize.Angle(NetQuantize.Angle(100f)) - 100f), 0f, 1.5f);
    }

    [Fact]
    public void HpPercentKeepsLivingEntitiesVisible()
    {
        Assert.Equal(0, NetQuantize.HpPct(0f, 100f));
        Assert.Equal(1, NetQuantize.HpPct(0.1f, 1000f));
        Assert.Equal(100, NetQuantize.HpPct(100f, 100f));
        Assert.Equal(100, NetQuantize.HpPct(500f, 100f));
        Assert.Equal(0, NetQuantize.HpPct(5f, 0f));
    }

    [Fact]
    public void FacingAndBitsShareOneByte()
    {
        byte b = NetQuantize.PackFacing(3, HeroFlags.Dead | HeroFlags.Hurt);
        Assert.Equal(3, NetQuantize.Facing(b));
        Assert.Equal(HeroFlags.Dead | HeroFlags.Hurt, NetQuantize.Bits(b));
    }

    [Fact]
    public void WrapSafeNewerComparison()
    {
        Assert.True(NetQuantize.IsNewer(5, 4));
        Assert.False(NetQuantize.IsNewer(4, 4));
        Assert.True(NetQuantize.IsNewer(2, uint.MaxValue - 1));
    }
}
