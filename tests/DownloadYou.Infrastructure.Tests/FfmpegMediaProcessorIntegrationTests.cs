using DownloadYou.Infrastructure.Configuration;
using DownloadYou.Infrastructure.ExternalTools;
using DownloadYou.Infrastructure.MediaProcessing;
using DownloadYou.Infrastructure.Processes;
using DownloadYou.Infrastructure.VideoSources;
using Microsoft.Extensions.Options;

namespace DownloadYou.Infrastructure.Tests;

/// <summary>
/// Descarga con yt-dlp real un video-only (160) y un audio-only (139) de "Me at the
/// zoo" y ejercita FfmpegMediaProcessor real: mux, extracción a MP3 y verificación
/// con ffprobe. Requiere los binarios de tools/.
/// </summary>
[Trait("Category", "Integration")]
public class FfmpegMediaProcessorIntegrationTests : IAsyncLifetime
{
    private const string StableTestVideoUrl = "https://youtu.be/jNQXAC9IVRw";
    private readonly string _dir = Directory.CreateTempSubdirectory("dy-ffmpeg-it-").FullName;
    private string _videoPath = null!;
    private string _audioPath = null!;
    private FfmpegMediaProcessor _processor = null!;

    public async Task InitializeAsync()
    {
        var locator = new ExternalToolLocator(Options.Create(new ToolsOptions()));
        var runner = new ExternalProcessRunner();
        var videoSource = new YtDlpVideoSource(locator, runner);
        _processor = new FfmpegMediaProcessor(locator, runner);

        _videoPath = Path.Combine(_dir, "v.mp4");
        _audioPath = Path.Combine(_dir, "a.m4a");
        await videoSource.DownloadAsync(StableTestVideoUrl, "160", _videoPath);
        await videoSource.DownloadAsync(StableTestVideoUrl, "139", _audioPath);
    }

    public Task DisposeAsync()
    {
        Directory.Delete(_dir, recursive: true);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task MuxAsync_CombinesRealStreams_AndPassesVerification()
    {
        var outputPath = Path.Combine(_dir, "muxed.mp4");
        var progressTicks = new List<TimeSpan>();

        await _processor.MuxAsync(_videoPath, _audioPath, outputPath, progressTicks.Add);

        Assert.True(File.Exists(outputPath));
        Assert.True(new FileInfo(outputPath).Length > 0);

        var verification = await _processor.VerifyAsync(outputPath, TimeSpan.FromSeconds(19), requireVideoStream: true);
        Assert.True(verification.IsValid, verification.Error);
    }

    [Fact]
    public async Task ExtractAudioAsync_ProducesValidMp3()
    {
        var outputPath = Path.Combine(_dir, "extracted.mp3");

        await _processor.ExtractAudioAsync(_audioPath, outputPath, bitrateKbps: 128);

        Assert.True(File.Exists(outputPath));

        var verification = await _processor.VerifyAsync(outputPath, TimeSpan.FromSeconds(19), requireVideoStream: false);
        Assert.True(verification.IsValid, verification.Error);
    }

    [Fact]
    public async Task VerifyAsync_Fails_WhenDurationIsWayOff()
    {
        var outputPath = Path.Combine(_dir, "muxed2.mp4");
        await _processor.MuxAsync(_videoPath, _audioPath, outputPath);

        var verification = await _processor.VerifyAsync(outputPath, TimeSpan.FromMinutes(10), requireVideoStream: true);

        Assert.False(verification.IsValid);
    }
}
