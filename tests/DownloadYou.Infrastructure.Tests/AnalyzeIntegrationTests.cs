using DownloadYou.Infrastructure.Configuration;
using DownloadYou.Infrastructure.ExternalTools;
using DownloadYou.Infrastructure.Processes;
using DownloadYou.Infrastructure.VideoSources;
using Microsoft.Extensions.Options;

namespace DownloadYou.Infrastructure.Tests;

/// <summary>
/// Analiza con yt-dlp real "Me at the zoo" (https://youtu.be/jNQXAC9IVRw), el primer
/// video subido a YouTube — estable como referencia histórica de la plataforma. El
/// video de pruebas histórico de yt-dlp (BaW_jenozKc) dejó de estar disponible, lo
/// cual confirma en la práctica el riesgo documentado en §16 del documento de
/// arquitectura sobre la volatilidad del contenido de YouTube. Requiere los binarios
/// de tools/ — ver tools/README.md y §15 del documento de arquitectura.
/// </summary>
[Trait("Category", "Integration")]
public class AnalyzeIntegrationTests
{
    private const string StableTestVideoUrl = "https://youtu.be/jNQXAC9IVRw";

    [Fact]
    public async Task AnalyzeAsync_ReturnsRealMetadata_ForStableTestVideo()
    {
        var locator = new ExternalToolLocator(Options.Create(new ToolsOptions()));
        var videoSource = new YtDlpVideoSource(locator, new ExternalProcessRunner());
        var streamedLines = new List<string>();

        var info = await videoSource.AnalyzeAsync(StableTestVideoUrl, streamedLines.Add);

        Assert.Equal("jNQXAC9IVRw", info.VideoId);
        Assert.False(string.IsNullOrWhiteSpace(info.Title));
        Assert.NotEmpty(info.AvailableFormats);
        Assert.Contains(streamedLines, l => l.TrimStart().StartsWith('{'));
    }
}
