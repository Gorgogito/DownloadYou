using DownloadYou.Application.Abstractions;
using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Application.Tests;

public class DownloadJobFactoryTests
{
    private static readonly FormatOption Muxed360p = new("18", StreamKind.Muxed, "mp4", "avc1", "mp4a", 360, 30, 500, 96, null);
    private static readonly FormatOption VideoOnly1080p = new("299", StreamKind.VideoOnly, "mp4", "avc1", null, 1080, 60, 4500, null, null);
    private static readonly FormatOption AudioOnly128 = new("140", StreamKind.AudioOnly, "m4a", null, "aac", null, null, null, 128, null);
    private static readonly FormatOption AudioOnly160 = new("251", StreamKind.AudioOnly, "webm", null, "opus", null, null, null, 160, null);

    private static MediaInfo BuildMediaInfo(params FormatOption[] formats) =>
        new("https://youtu.be/x", "x", "Título", "Autor", TimeSpan.FromMinutes(1), null, formats);

    [Fact]
    public void Create_DoesNotPairAudio_WhenSelectedFormatIsAlreadyMuxed()
    {
        var mediaInfo = BuildMediaInfo(Muxed360p);

        var job = DownloadJobFactory.Create(mediaInfo, Muxed360p, DownloadKind.Video, "C:\\out", "{title}.{ext}", 192);

        Assert.Null(job.PairedAudioFormat);
        Assert.False(job.RequiresConversion);
    }

    [Fact]
    public void Create_PrefersContainerCompatibleAudio_OverHigherBitrateMismatchedContainer()
    {
        // VideoOnly1080p es mp4/avc1; AudioOnly128 es m4a (misma familia "mp4", remux directo);
        // AudioOnly160 es webm/opus, con más bitrate pero exigiría transcodificar en el mux.
        var mediaInfo = BuildMediaInfo(VideoOnly1080p, AudioOnly128, AudioOnly160);

        var job = DownloadJobFactory.Create(mediaInfo, VideoOnly1080p, DownloadKind.Video, "C:\\out", "{title}.{ext}", 192);

        Assert.Equal("140", job.PairedAudioFormat?.FormatId);
        Assert.True(job.RequiresConversion);
    }

    [Fact]
    public void Create_FallsBackToHighestBitrate_WhenNoContainerFamilyMatches()
    {
        var mediaInfo = BuildMediaInfo(VideoOnly1080p, AudioOnly160);

        var job = DownloadJobFactory.Create(mediaInfo, VideoOnly1080p, DownloadKind.Video, "C:\\out", "{title}.{ext}", 192);

        Assert.Equal("251", job.PairedAudioFormat?.FormatId);
    }

    [Fact]
    public void Create_Throws_WhenVideoOnlySelectedButNoAudioAvailable()
    {
        var mediaInfo = BuildMediaInfo(VideoOnly1080p);

        Assert.Throws<NoCompatibleAudioStreamException>(
            () => DownloadJobFactory.Create(mediaInfo, VideoOnly1080p, DownloadKind.Video, "C:\\out", "{title}.{ext}", 192));
    }

    [Fact]
    public void Create_RequiresConversion_ForAudioMp3Kind_EvenWithoutPairing()
    {
        var mediaInfo = BuildMediaInfo(AudioOnly160);

        var job = DownloadJobFactory.Create(mediaInfo, AudioOnly160, DownloadKind.AudioMp3, "C:\\out", "{title}.{ext}", 192);

        Assert.Null(job.PairedAudioFormat);
        Assert.True(job.RequiresConversion);
    }
}
