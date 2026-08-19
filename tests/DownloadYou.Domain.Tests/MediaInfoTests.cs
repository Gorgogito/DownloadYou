using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Domain.Tests;

public class MediaInfoTests
{
    [Fact]
    public void BestAudioBitrateKbps_ReturnsHighestAvailable()
    {
        FormatOption[] formats =
        [
            new("139", StreamKind.AudioOnly, "m4a", null, "aac", null, null, null, 48, null),
            new("140", StreamKind.AudioOnly, "m4a", null, "aac", null, null, null, 128, null),
            new("251", StreamKind.AudioOnly, "webm", null, "opus", null, null, null, 160, null),
        ];

        var mediaInfo = new MediaInfo("https://youtube.com/watch?v=abc", "abc", "Título", "Autor", TimeSpan.FromMinutes(3), null, formats);

        Assert.Equal(160, mediaInfo.BestAudioBitrateKbps);
    }

    [Fact]
    public void BestAudioBitrateKbps_IsNull_WhenNoAudioFormats()
    {
        FormatOption[] formats = [new("299", StreamKind.VideoOnly, "mp4", "avc1", null, 1080, 60, 4500, null, null)];

        var mediaInfo = new MediaInfo("https://youtube.com/watch?v=abc", "abc", "Título", "Autor", TimeSpan.FromMinutes(3), null, formats);

        Assert.Null(mediaInfo.BestAudioBitrateKbps);
    }
}
