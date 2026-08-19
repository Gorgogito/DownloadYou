using DownloadYou.Infrastructure.VideoSources;

namespace DownloadYou.Infrastructure.Tests;

public class YtDlpProgressParserTests
{
    [Fact]
    public void TryParse_ReturnsNull_ForUnrelatedLines()
    {
        Assert.Null(YtDlpProgressParser.TryParse("[youtube] Extracting URL: https://youtu.be/x"));
        Assert.Null(YtDlpProgressParser.TryParse("[download] Destination: C:\\file.mp4"));
    }

    [Fact]
    public void TryParse_ParsesFullNumericLine()
    {
        var update = YtDlpProgressParser.TryParse("DYPROGRESS downloading|64512|117526|NA|9202562.31|3");

        Assert.NotNull(update);
        Assert.Equal("downloading", update!.Status);
        Assert.Equal(64512, update.DownloadedBytes);
        Assert.Equal(117526, update.TotalBytes);
        Assert.Equal(9202562.31, update.SpeedBytesPerSecond);
        Assert.Equal(TimeSpan.FromSeconds(3), update.Eta);
    }

    [Fact]
    public void TryParse_FallsBackToTotalBytesEstimate_WhenTotalBytesIsNA()
    {
        var update = YtDlpProgressParser.TryParse("DYPROGRESS downloading|1000|NA|50000|500|10");

        Assert.Equal(50000, update!.TotalBytes);
    }

    [Fact]
    public void TryParse_TreatsNA_AsNull_ForSpeedAndEta()
    {
        var update = YtDlpProgressParser.TryParse("DYPROGRESS downloading|1024|117526|NA|NA|NA");

        Assert.Null(update!.SpeedBytesPerSecond);
        Assert.Null(update.Eta);
    }

    [Fact]
    public void TryParse_ReturnsNull_WhenFieldCountDoesNotMatch()
    {
        Assert.Null(YtDlpProgressParser.TryParse("DYPROGRESS downloading|1024|117526"));
    }

    [Fact]
    public void PercentComplete_ComputesFromDownloadedAndTotal()
    {
        var update = YtDlpProgressParser.TryParse("DYPROGRESS downloading|25|100|NA|NA|NA");

        Assert.Equal(25, update!.PercentComplete);
    }
}
