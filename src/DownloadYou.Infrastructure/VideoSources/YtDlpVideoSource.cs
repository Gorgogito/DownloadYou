using DownloadYou.Application.Abstractions;
using DownloadYou.Domain.Entities;
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

    public async Task<MediaInfo> AnalyzeAsync(string url, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
    {
        var exePath = await toolLocator.ResolveAsync(ExternalTool.YtDlp, cancellationToken);

        var result = await processRunner.RunAsync(
            exePath,
            ["--dump-json", "--no-playlist", "--no-warnings", url],
            onOutputLine,
            onOutputLine,
            cancellationToken);

        if (!result.Succeeded)
        {
            var detail = result.StandardError.Count > 0 ? string.Join(' ', result.StandardError) : $"código de salida {result.ExitCode}";
            throw new InvalidOperationException($"yt-dlp no pudo analizar la URL: {detail}");
        }

        var jsonLine = result.StandardOutput.FirstOrDefault(l => l.TrimStart().StartsWith('{'));
        if (jsonLine is null)
        {
            throw new InvalidOperationException("yt-dlp no devolvió metadatos JSON para esta URL.");
        }

        return YtDlpMetadataParser.Parse(jsonLine, url);
    }

    public async Task DownloadAsync(
        string url,
        string formatId,
        string outputFilePath,
        Action<DownloadProgressUpdate>? onProgress = null,
        Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default)
    {
        var exePath = await toolLocator.ResolveAsync(ExternalTool.YtDlp, cancellationToken);

        void HandleStandardOutput(string line)
        {
            var update = YtDlpProgressParser.TryParse(line);
            if (update is not null)
            {
                onProgress?.Invoke(update);
            }
            else
            {
                onOutputLine?.Invoke(line);
            }
        }

        var result = await processRunner.RunAsync(
            exePath,
            [
                "-f", formatId,
                "--newline",
                "--no-warnings",
                "--no-playlist",
                "--progress-template",
                $"download:{YtDlpProgressParser.LinePrefix}%(progress.status)s|%(progress.downloaded_bytes)s|%(progress.total_bytes)s|%(progress.total_bytes_estimate)s|%(progress.speed)s|%(progress.eta)s",
                "-o", outputFilePath,
                url
            ],
            HandleStandardOutput,
            onOutputLine,
            cancellationToken);

        if (!result.Succeeded)
        {
            var detail = result.StandardError.Count > 0 ? string.Join(' ', result.StandardError) : $"código de salida {result.ExitCode}";
            throw new InvalidOperationException($"yt-dlp no pudo descargar el formato '{formatId}': {detail}");
        }

        if (!File.Exists(outputFilePath))
        {
            throw new InvalidOperationException($"yt-dlp reportó éxito pero no se encontró el archivo esperado en '{outputFilePath}'.");
        }
    }
}
