using DownloadYou.Application.Abstractions;
using DownloadYou.Infrastructure.Configuration;
using DownloadYou.Infrastructure.ExternalTools;
using DownloadYou.Infrastructure.Processes;
using DownloadYou.Infrastructure.VideoSources;
using Microsoft.Extensions.Options;

namespace DownloadYou.Infrastructure.Tests;

/// <summary>
/// Descarga con yt-dlp real el stream de audio más pequeño de "Me at the zoo"
/// (~115 KB) para mantener la prueba rápida. Requiere los binarios de tools/.
/// </summary>
[Trait("Category", "Integration")]
public class DownloadIntegrationTests : IDisposable
{
    private const string StableTestVideoUrl = "https://youtu.be/jNQXAC9IVRw";
    private readonly string _tempDir = Directory.CreateTempSubdirectory("dy-download-it-").FullName;

    [Fact]
    public async Task DownloadAsync_DownloadsRealFile_AndReportsProgress()
    {
        var locator = new ExternalToolLocator(Options.Create(new ToolsOptions()));
        var videoSource = new YtDlpVideoSource(locator, new ExternalProcessRunner());
        var outputPath = Path.Combine(_tempDir, "audio.m4a");
        var updates = new List<DownloadProgressUpdate>();

        await videoSource.DownloadAsync(StableTestVideoUrl, "139", outputPath, updates.Add);

        Assert.True(File.Exists(outputPath));
        Assert.True(new FileInfo(outputPath).Length > 0);
        Assert.NotEmpty(updates);
        Assert.Contains(updates, u => u.Status == "finished");
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
