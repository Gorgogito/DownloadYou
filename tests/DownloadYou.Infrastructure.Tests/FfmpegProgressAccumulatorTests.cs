using DownloadYou.Infrastructure.MediaProcessing;

namespace DownloadYou.Infrastructure.Tests;

public class FfmpegProgressAccumulatorTests
{
    [Fact]
    public void Ingest_ReturnsNull_UntilProgressLineClosesTheBlock()
    {
        var sut = new FfmpegProgressAccumulator();

        Assert.Null(sut.Ingest("frame=284"));
        Assert.Null(sut.Ingest("fps=0.00"));
        Assert.Null(sut.Ingest("out_time_us=19133243"));
        Assert.Null(sut.Ingest("out_time_ms=19133243"));
        Assert.Null(sut.Ingest("speed=481x"));
    }

    [Fact]
    public void Ingest_ReturnsElapsedTime_WhenProgressLineCloses()
    {
        var sut = new FfmpegProgressAccumulator();
        sut.Ingest("out_time_us=19133243");

        var elapsed = sut.Ingest("progress=continue");

        Assert.Equal(TimeSpan.FromMicroseconds(19133243), elapsed);
    }

    [Fact]
    public void Ingest_StartsFreshBlock_AfterProgressLine()
    {
        var sut = new FfmpegProgressAccumulator();
        sut.Ingest("out_time_us=1000000");
        sut.Ingest("progress=continue");

        sut.Ingest("out_time_us=2000000");
        var second = sut.Ingest("progress=continue");

        Assert.Equal(TimeSpan.FromMicroseconds(2000000), second);
    }

    [Fact]
    public void Ingest_ReturnsNull_WhenBlockNeverReportedOutTime()
    {
        var sut = new FfmpegProgressAccumulator();
        sut.Ingest("bitrate=131.8kbits/s");

        var elapsed = sut.Ingest("progress=end");

        Assert.Null(elapsed);
    }

    [Fact]
    public void Ingest_IgnoresLinesWithoutEqualsSign()
    {
        var sut = new FfmpegProgressAccumulator();

        Assert.Null(sut.Ingest("ffmpeg version 9.0.1"));
    }
}
