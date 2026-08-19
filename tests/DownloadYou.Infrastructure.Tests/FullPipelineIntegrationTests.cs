using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;
using DownloadYou.Infrastructure.Configuration;
using DownloadYou.Infrastructure.ExternalTools;
using DownloadYou.Infrastructure.MediaProcessing;
using DownloadYou.Infrastructure.Processes;
using DownloadYou.Infrastructure.VideoSources;
using Microsoft.Extensions.Options;

namespace DownloadYou.Infrastructure.Tests;

/// <summary>
/// Extremo a extremo con binarios reales: Analizar → Descargar → Convertir → Verificar
/// → archivo final, para los dos caminos del pipeline (Video con streams DASH separados,
/// y Audio MP3). Este es el flujo completo que ejecuta el botón "Descargar" de la UI.
/// </summary>
[Trait("Category", "Integration")]
public class FullPipelineIntegrationTests : IDisposable
{
    private const string StableTestVideoUrl = "https://youtu.be/jNQXAC9IVRw";
    private readonly string _targetDir = Directory.CreateTempSubdirectory("dy-pipeline-it-").FullName;

    private static (YtDlpVideoSource VideoSource, FfmpegMediaProcessor Processor) BuildEngines()
    {
        var locator = new ExternalToolLocator(Options.Create(new ToolsOptions()));
        var runner = new ExternalProcessRunner();
        return (new YtDlpVideoSource(locator, runner), new FfmpegMediaProcessor(locator, runner));
    }

    [Fact]
    public async Task Pipeline_DownloadsMuxesAndVerifies_PairedVideoAndAudio()
    {
        var (videoSource, processor) = BuildEngines();
        var mediaInfo = await videoSource.AnalyzeAsync(StableTestVideoUrl);
        var videoOnlyFormat = mediaInfo.AvailableFormats.Single(f => f.FormatId == "160");
        var job = DownloadJobFactory.Create(mediaInfo, videoOnlyFormat, DownloadKind.Video, _targetDir, "{title}.{ext}", 192);

        await new DownloadService(videoSource).RunAsync(job);
        Assert.Equal(JobStatus.Converting, job.Status);

        await new ConversionService(processor).RunAsync(job);

        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.True(File.Exists(job.OutputFilePath));
        Assert.EndsWith(".mp4", job.OutputFilePath);
        Assert.Equal("Me at the zoo.mp4", Path.GetFileName(job.OutputFilePath));
    }

    [Fact]
    public async Task Pipeline_DownloadsAndExtractsMp3()
    {
        var (videoSource, processor) = BuildEngines();
        var mediaInfo = await videoSource.AnalyzeAsync(StableTestVideoUrl);
        var audioFormat = mediaInfo.AvailableFormats.Where(f => f.Kind == Domain.Enums.StreamKind.AudioOnly)
            .OrderByDescending(f => f.AudioBitrateKbps ?? 0).First();
        var job = DownloadJobFactory.Create(mediaInfo, audioFormat, DownloadKind.AudioMp3, _targetDir, "{title}.{ext}", 192);

        await new DownloadService(videoSource).RunAsync(job);
        Assert.Equal(JobStatus.Converting, job.Status);

        await new ConversionService(processor).RunAsync(job);

        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.True(File.Exists(job.OutputFilePath));
        Assert.EndsWith(".mp3", job.OutputFilePath);
    }

    public void Dispose() => Directory.Delete(_targetDir, recursive: true);
}
