using DownloadYou.Application.Abstractions;
using DownloadYou.Infrastructure.Processes;

namespace DownloadYou.Infrastructure.VideoSources;

public sealed class YtDlpVideoSource(IExternalToolLocator toolLocator, ExternalProcessRunner processRunner) : IVideoSource
{
    public async Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
    {
        var exePath = await toolLocator.ResolveAsync(ExternalTool.YtDlp, cancellationToken);

        var result = await processRunner.RunAsync(
            exePath,
            ["--version"],
            onOutputLine,
            onOutputLine,
            cancellationToken);

        if (!result.Succeeded || result.StandardOutput.Count == 0)
        {
            throw new InvalidOperationException($"yt-dlp terminó con código {result.ExitCode}.");
        }

        return result.StandardOutput[0].Trim();
    }
}
