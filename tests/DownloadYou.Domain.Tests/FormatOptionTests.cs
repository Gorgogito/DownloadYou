using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Domain.Tests;

public class FormatOptionTests
{
    [Fact]
    public void DisplayLabel_ForAudioOnly_ShowsBitrate()
    {
        var format = new FormatOption("140", StreamKind.AudioOnly, "m4a", null, "aac", null, null, null, 128, null);

        Assert.Equal("128 kbps", format.DisplayLabel);
    }

    [Fact]
    public void DisplayLabel_ForVideoOnlyAt60Fps_IncludesFps()
    {
        var format = new FormatOption("299", StreamKind.VideoOnly, "mp4", "avc1", null, 1080, 60, 4500, null, null);

        Assert.Equal("1080p 60fps", format.DisplayLabel);
    }

    [Fact]
    public void RequiresMux_IsFalse_OnlyForMuxedStreams()
    {
        var muxed = new FormatOption("18", StreamKind.Muxed, "mp4", "avc1", "aac", 360, 30, 500, 96, null);
        var videoOnly = new FormatOption("299", StreamKind.VideoOnly, "mp4", "avc1", null, 1080, 60, 4500, null, null);

        Assert.False(muxed.RequiresMux);
        Assert.True(videoOnly.RequiresMux);
    }
}
