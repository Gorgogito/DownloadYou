using DownloadYou.Application.Abstractions;
using DownloadYou.Infrastructure.Configuration;
using DownloadYou.Infrastructure.ExternalTools;
using DownloadYou.Infrastructure.Processes;
using DownloadYou.Infrastructure.VideoSources;
using Microsoft.Extensions.Options;

namespace DownloadYou.Infrastructure.Tests;

public class YtDlpVideoSourceRetryTests : IDisposable
{
    private readonly string _toolsDir = Directory.CreateTempSubdirectory("dy-retry-").FullName;
    private readonly string _outputPath;

    public YtDlpVideoSourceRetryTests()
    {
        File.WriteAllText(Path.Combine(_toolsDir, "yt-dlp.exe"), "stub");
        _outputPath = Path.Combine(_toolsDir, "out.mp4");
    }

    private YtDlpVideoSource BuildSource(FakeProcessRunner runner)
    {
        var locator = new ExternalToolLocator(Options.Create(new ToolsOptions { ToolsDirectory = _toolsDir }));
        return new YtDlpVideoSource(locator, runner, retryBaseDelay: TimeSpan.FromMilliseconds(5));
    }

    [Fact]
    public async Task DownloadAsync_RetriesTransientFailures_AndEventuallySucceeds()
    {
        var runner = new FakeProcessRunner(
        [
            FakeAttempt.Failure("ERROR: unable to download webpage: HTTP Error 503: Service Unavailable"),
            FakeAttempt.Failure("ERROR: The read operation timed out"),
            FakeAttempt.Success(_outputPath)
        ]);
        var source = BuildSource(runner);
        var lines = new List<string>();

        await source.DownloadAsync("https://youtu.be/x", "18", _outputPath, onOutputLine: lines.Add);

        Assert.Equal(3, runner.CallCount);
        Assert.Contains(lines, l => l.Contains("reintento"));
    }

    [Fact]
    public async Task DownloadAsync_DoesNotRetry_PermanentErrors()
    {
        var runner = new FakeProcessRunner([FakeAttempt.Failure("ERROR: [youtube] x: Private video")]);
        var source = BuildSource(runner);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => source.DownloadAsync("https://youtu.be/x", "18", _outputPath));

        Assert.Contains("Private video", ex.Message);
        Assert.Equal(1, runner.CallCount);
    }

    [Fact]
    public async Task DownloadAsync_GivesUp_AfterMaxRetries_OnPersistentTransientFailure()
    {
        var runner = new FakeProcessRunner(Enumerable.Range(0, 10)
            .Select(_ => FakeAttempt.Failure("ERROR: Connection reset by peer"))
            .ToArray());
        var source = BuildSource(runner);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => source.DownloadAsync("https://youtu.be/x", "18", _outputPath));

        Assert.Equal(4, runner.CallCount); // intento inicial + 3 reintentos
    }

    public void Dispose() => Directory.Delete(_toolsDir, recursive: true);

    private sealed record FakeAttempt(bool Succeeds, string? ErrorMessage, string? OutputPathToCreate)
    {
        public static FakeAttempt Success(string outputPath) => new(true, null, outputPath);
        public static FakeAttempt Failure(string errorMessage) => new(false, errorMessage, null);
    }

    private sealed class FakeProcessRunner(IReadOnlyList<FakeAttempt> attempts) : IExternalProcessRunner
    {
        public int CallCount { get; private set; }

        public Task<ExternalProcessResult> RunAsync(
            string executablePath,
            IReadOnlyList<string> arguments,
            Action<string>? onOutputLine = null,
            Action<string>? onErrorLine = null,
            CancellationToken cancellationToken = default)
        {
            var attempt = attempts[Math.Min(CallCount, attempts.Count - 1)];
            CallCount++;

            if (attempt.Succeeds)
            {
                File.WriteAllText(attempt.OutputPathToCreate!, "contenido");
                return Task.FromResult(new ExternalProcessResult(0, [], []));
            }

            onErrorLine?.Invoke(attempt.ErrorMessage!);
            return Task.FromResult(new ExternalProcessResult(1, [], [attempt.ErrorMessage!]));
        }
    }
}
