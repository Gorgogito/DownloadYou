using DownloadYou.Application.Abstractions;

namespace DownloadYou.Application.Diagnostics;

public sealed class EngineDiagnosticsService(IVideoSource videoSource, IMediaProcessor mediaProcessor)
{
    public async Task<EngineDiagnosticsResult> CheckAsync(Action<string> onLine, CancellationToken cancellationToken = default)
    {
        var (ytDlpOk, ytDlpVersion, ytDlpError) = await ProbeAsync(
            "yt-dlp",
            line => videoSource.GetVersionAsync(line, cancellationToken),
            onLine,
            cancellationToken);

        var (ffmpegOk, ffmpegVersion, ffmpegError) = await ProbeAsync(
            "ffmpeg",
            line => mediaProcessor.GetVersionAsync(line, cancellationToken),
            onLine,
            cancellationToken);

        return new EngineDiagnosticsResult(ytDlpOk, ytDlpVersion, ytDlpError, ffmpegOk, ffmpegVersion, ffmpegError);
    }

    private static async Task<(bool Ok, string? Version, string? Error)> ProbeAsync(
        string label,
        Func<Action<string>, Task<string>> probe,
        Action<string> onLine,
        CancellationToken cancellationToken)
    {
        onLine($"[{label}] resolviendo ejecutable...");
        try
        {
            var version = await probe(line => onLine($"[{label}] {line}"));
            onLine($"[{label}] disponible — versión {version}");
            return (true, version, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            onLine($"[{label}] no disponible — {ex.Message}");
            return (false, null, ex.Message);
        }
    }
}
