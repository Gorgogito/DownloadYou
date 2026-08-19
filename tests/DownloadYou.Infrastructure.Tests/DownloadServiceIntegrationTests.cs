using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;
using DownloadYou.Infrastructure.Configuration;
using DownloadYou.Infrastructure.ExternalTools;
using DownloadYou.Infrastructure.Processes;
using DownloadYou.Infrastructure.VideoSources;
using Microsoft.Extensions.Options;

namespace DownloadYou.Infrastructure.Tests;

/// <summary>
/// Extremo a extremo con yt-dlp real, cubriendo el caso DASH (video-only + audio-only
/// separados) de "Me at the zoo": el formato legacy muxed (id "18") de este video
/// devuelve 403 Forbidden al pedir los bytes reales — YouTube viene restringiendo
/// progresivamente los formatos combinados — así que el camino DASH probado aquí es,
/// en la práctica, el dominante, tal como anticipa §3 del documento de arquitectura.
/// </summary>
[Trait("Category", "Integration")]
public class DownloadServiceIntegrationTests : IDisposable
{
    private const string StableTestVideoUrl = "https://youtu.be/jNQXAC9IVRw";
    private readonly string _targetDir = Directory.CreateTempSubdirectory("dy-download-svc-it-").FullName;

    [Fact]
    public async Task RunAsync_DownloadsPairedVideoAndAudio_AndLeavesJobReadyForConversion()
    {
        var locator = new ExternalToolLocator(Options.Create(new ToolsOptions()));
        var videoSource = new YtDlpVideoSource(locator, new ExternalProcessRunner());
        var mediaInfo = await videoSource.AnalyzeAsync(StableTestVideoUrl);
        var videoOnlyFormat = mediaInfo.AvailableFormats.Single(f => f.FormatId == "160");

        var job = DownloadJobFactory.Create(
            mediaInfo, videoOnlyFormat, DownloadKind.Video, _targetDir, "{title}.{ext}", 192);
        var service = new DownloadService(videoSource);
        var progressTicks = 0;

        await service.RunAsync(job, onProgressChanged: () => progressTicks++);

        Assert.Equal(JobStatus.Converting, job.Status);
        Assert.NotNull(job.PairedAudioFormat);
        Assert.True(File.Exists(job.PrimaryFilePath));
        Assert.True(File.Exists(job.PairedAudioFilePath));
        Assert.Equal(100, job.ProgressPercent);
        Assert.True(progressTicks > 0);
    }

    public void Dispose() => Directory.Delete(_targetDir, recursive: true);
}
