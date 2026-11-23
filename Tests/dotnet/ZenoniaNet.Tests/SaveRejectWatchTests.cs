using Xunit;
using ZenoniaProto.Net;

// The alarm for "the API is older than this zone": three refusals in ten minutes.
public class SaveRejectWatchTests
{
    [Fact]
    public void ThirdRefusalInTheWindowIsRepeated()
    {
        var w = new SaveRejectWatch();
        Assert.False(SaveRejectWatch.IsRepeated(w.Note(1000)));
        Assert.False(SaveRejectWatch.IsRepeated(w.Note(2000)));
        Assert.True(SaveRejectWatch.IsRepeated(w.Note(3000)));
        Assert.Equal(4, w.Note(4000));
    }

    [Fact]
    public void RefusalsFarApartDoNotAddUp()
    {
        var w = new SaveRejectWatch();
        w.Note(0); w.Note(1000);
        Assert.Equal(1, w.Note(SaveRejectWatch.WindowMs + 5000)); // the window restarted
        Assert.False(SaveRejectWatch.IsRepeated(1));
    }
}
