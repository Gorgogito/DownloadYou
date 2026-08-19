using DownloadYou.Application.Diagnostics;
using DownloadYou.Infrastructure.Configuration;
using DownloadYou.Infrastructure.ExternalTools;
using DownloadYou.Infrastructure.MediaProcessing;
using DownloadYou.Infrastructure.Processes;
using DownloadYou.Infrastructure.VideoSources;
using Microsoft.Extensions.Options;

namespace DownloadYou.Infrastructure.Tests;

/// <summary>
/// Ejercita yt-dlp.exe / ffmpeg.exe reales resueltos desde la carpeta tools/ del repositorio.
/// Requiere los binarios descritos en tools/README.md; se ejecuta aparte de la suite unitaria
/// (ver estrategia de pruebas del documento de arquitectura, §15).
/// </summary>
[Trait("Category", "Integration")]
public class EngineIntegrationTests
{
    private static EngineDiagnosticsService BuildService()
    {
        var locator = new ExternalToolLocator(Options.Create(new ToolsOptions()));
        var runner = new ExternalProcessRunner();
        return new EngineDiagnosticsService(
            new YtDlpVideoSource(locator, runner),
            new FfmpegMediaProcessor(locator, runner));
    }

    [Fact]
    public async Task CheckAsync_ResolvesRealYtDlpAndFfmpeg_AndStreamsOutput()
    {
        var service = BuildService();
        var streamedLines = new List<string>();

        var result = await service.CheckAsync(streamedLines.Add);

        Assert.True(result.YtDlpAvailable, result.YtDlpError);
        Assert.True(result.FfmpegAvailable, result.FfmpegError);
        Assert.NotEmpty(result.YtDlpVersion!);
        Assert.NotEmpty(result.FfmpegVersion!);
        Assert.Contains(streamedLines, l => l.Contains("resolviendo ejecutable"));
        Assert.Contains(streamedLines, l => l.Contains("disponible — versión"));
    }
}
