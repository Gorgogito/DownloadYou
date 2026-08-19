using DownloadYou.Application.Abstractions;
using DownloadYou.Infrastructure.Configuration;
using DownloadYou.Infrastructure.ExternalTools;
using DownloadYou.Infrastructure.Processes;
using DownloadYou.Infrastructure.VideoSources;
using Microsoft.Extensions.Options;

namespace DownloadYou.Infrastructure.Tests;

/// <summary>
/// Cubre el fix para el HTTP 403 que YouTube empezó a devolver con el cliente que yt-dlp
/// elige por defecto (android_vr): forzar una lista de clientes de respaldo por
/// --extractor-args, más --js-runtimes cuando hay un runtime de JS (deno) empaquetado.
/// </summary>
public class YtDlpVideoSourceYouTubeArgsTests : IDisposable
{
    private readonly string _toolsDir = Directory.CreateTempSubdirectory("dy-ytargs-").FullName;

    public YtDlpVideoSourceYouTubeArgsTests() => File.WriteAllText(Path.Combine(_toolsDir, "yt-dlp.exe"), "stub");

    private YtDlpVideoSource BuildSource(RecordingProcessRunner runner) =>
        new(new ExternalToolLocator(Options.Create(new ToolsOptions { ToolsDirectory = _toolsDir })), runner);

    [Fact]
    public async Task AnalyzeAsync_PassesExtractorArgs_WithClientFallbackList()
    {
        var runner = new RecordingProcessRunner("""{"id":"x","title":"t","uploader":"a","duration":1,"formats":[]}""");
        var source = BuildSource(runner);

        await source.AnalyzeAsync("https://youtu.be/x");

        AssertHasExtractorArgs(runner.LastArguments!);
    }

    [Fact]
    public async Task DownloadAsync_PassesExtractorArgs_WithClientFallbackList()
    {
        var outputPath = Path.Combine(_toolsDir, "out.mp4");
        var runner = new RecordingProcessRunner(outputPath: outputPath);
        var source = BuildSource(runner);

        await source.DownloadAsync("https://youtu.be/x", "18", outputPath);

        AssertHasExtractorArgs(runner.LastArguments!);
    }

    [Fact]
    public async Task DownloadAsync_AddsJsRuntimeArg_WhenDenoIsBundled()
    {
        var denoPath = Path.Combine(_toolsDir, "deno.exe");
        File.WriteAllText(denoPath, "stub");
        var outputPath = Path.Combine(_toolsDir, "out.mp4");
        var runner = new RecordingProcessRunner(outputPath: outputPath);
        var source = BuildSource(runner);

        await source.DownloadAsync("https://youtu.be/x", "18", outputPath);

        var args = runner.LastArguments!;
        var runtimesIndex = args.IndexOf("--js-runtimes");
        Assert.True(runtimesIndex >= 0, "Se esperaba --js-runtimes cuando deno.exe está empaquetado.");
        Assert.Equal($"deno:{denoPath}", args[runtimesIndex + 1]);
    }

    [Fact]
    public async Task DownloadAsync_OmitsJsRuntimeArg_WhenDenoIsNotBundled()
    {
        var outputPath = Path.Combine(_toolsDir, "out.mp4");
        var runner = new RecordingProcessRunner(outputPath: outputPath);
        var source = BuildSource(runner);

        await source.DownloadAsync("https://youtu.be/x", "18", outputPath);

        Assert.DoesNotContain("--js-runtimes", runner.LastArguments!);
    }

    private static void AssertHasExtractorArgs(List<string> args)
    {
        var index = args.IndexOf("--extractor-args");
        Assert.True(index >= 0, "Se esperaba --extractor-args en los argumentos de yt-dlp.");
        Assert.Equal("youtube:player_client=web_embedded,android_vr,tv,mweb", args[index + 1]);
    }

    public void Dispose() => Directory.Delete(_toolsDir, recursive: true);

    private sealed class RecordingProcessRunner(string? jsonOutput = null, string? outputPath = null) : IExternalProcessRunner
    {
        public List<string>? LastArguments { get; private set; }

        public Task<ExternalProcessResult> RunAsync(
            string executablePath,
            IReadOnlyList<string> arguments,
            Action<string>? onOutputLine = null,
            Action<string>? onErrorLine = null,
            CancellationToken cancellationToken = default)
        {
            LastArguments = [.. arguments];

            if (outputPath is not null)
            {
                File.WriteAllText(outputPath, "contenido");
            }

            var stdout = jsonOutput is not null ? new List<string> { jsonOutput } : [];
            return Task.FromResult(new ExternalProcessResult(0, stdout, []));
        }
    }
}
