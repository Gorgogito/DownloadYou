using System.Text.RegularExpressions;
using DownloadYou.Application.Abstractions;
using DownloadYou.Infrastructure.Processes;

namespace DownloadYou.Infrastructure.MediaProcessing;

public sealed partial class FfmpegMediaProcessor(IExternalToolLocator toolLocator, ExternalProcessRunner processRunner) : IMediaProcessor
{
    public async Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
    {
        var exePath = await toolLocator.ResolveAsync(ExternalTool.FfMpeg, cancellationToken);

        var result = await processRunner.RunAsync(
            exePath,
            ["-version"],
            onOutputLine,
            onOutputLine,
            cancellationToken);

        var firstLine = result.StandardOutput.FirstOrDefault() ?? string.Empty;
        var match = VersionPattern().Match(firstLine);

        if (!result.Succeeded || !match.Success)
        {
            throw new InvalidOperationException($"ffmpeg terminó con código {result.ExitCode}.");
        }

        return match.Groups[1].Value;
    }

    [GeneratedRegex(@"ffmpeg version (\S+)")]
    private static partial Regex VersionPattern();
}
