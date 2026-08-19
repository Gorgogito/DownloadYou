using System.Text.RegularExpressions;
using DownloadYou.Application.Abstractions;
using DownloadYou.Infrastructure.Processes;

namespace DownloadYou.Infrastructure.MediaProcessing;

public sealed partial class FfmpegMediaProcessor(IExternalToolLocator toolLocator, IExternalProcessRunner processRunner) : IMediaProcessor
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

    public async Task MuxAsync(
        string videoFilePath,
        string audioFilePath,
        string outputFilePath,
        Action<TimeSpan>? onProgress = null,
        Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default)
    {
        var exePath = await toolLocator.ResolveAsync(ExternalTool.FfMpeg, cancellationToken);
        var outputContainer = Path.GetExtension(outputFilePath).TrimStart('.');

        var remuxResult = await RunFfmpegAsync(
            exePath, BuildMuxArgs(videoFilePath, audioFilePath, outputFilePath, audioCodec: "copy"), onProgress, onOutputLine, cancellationToken);

        if (remuxResult.Succeeded)
        {
            return;
        }

        onOutputLine?.Invoke($"[ffmpeg] Remux directo falló (código {remuxResult.ExitCode}); reintentando con transcodificación de audio...");
        TryDelete(outputFilePath);

        var fallbackCodec = FallbackAudioCodec(outputContainer);
        var fallbackResult = await RunFfmpegAsync(
            exePath, BuildMuxArgs(videoFilePath, audioFilePath, outputFilePath, fallbackCodec), onProgress, onOutputLine, cancellationToken);

        if (!fallbackResult.Succeeded)
        {
            var detail = fallbackResult.StandardError.Count > 0 ? string.Join(' ', fallbackResult.StandardError) : $"código {fallbackResult.ExitCode}";
            throw new InvalidOperationException($"ffmpeg no pudo combinar video y audio: {detail}");
        }
    }

    public async Task ExtractAudioAsync(
        string sourceFilePath,
        string outputFilePath,
        int bitrateKbps,
        Action<TimeSpan>? onProgress = null,
        Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default)
    {
        var exePath = await toolLocator.ResolveAsync(ExternalTool.FfMpeg, cancellationToken);

        var result = await RunFfmpegAsync(
            exePath,
            [
                "-y", "-hide_banner", "-loglevel", "error",
                "-i", sourceFilePath,
                "-vn",
                "-c:a", "libmp3lame",
                "-b:a", $"{bitrateKbps}k",
                "-progress", "pipe:1", "-nostats",
                outputFilePath
            ],
            onProgress,
            onOutputLine,
            cancellationToken);

        if (!result.Succeeded)
        {
            var detail = result.StandardError.Count > 0 ? string.Join(' ', result.StandardError) : $"código {result.ExitCode}";
            throw new InvalidOperationException($"ffmpeg no pudo convertir a MP3: {detail}");
        }
    }

    public async Task<MediaVerificationResult> VerifyAsync(
        string filePath,
        TimeSpan expectedDuration,
        bool requireVideoStream,
        Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default)
    {
        var exePath = await toolLocator.ResolveAsync(ExternalTool.FfProbe, cancellationToken);

        // El stdout de ffprobe es el JSON completo: no se reenvía a onOutputLine (sería
        // ruido ilegible en el log); solo se reenvían eventuales errores de stderr.
        var result = await processRunner.RunAsync(
            exePath,
            ["-v", "quiet", "-print_format", "json", "-show_format", "-show_streams", filePath],
            null,
            onOutputLine,
            cancellationToken);

        if (!result.Succeeded)
        {
            return MediaVerificationResult.Invalid($"ffprobe terminó con código {result.ExitCode}.");
        }

        var json = string.Join('\n', result.StandardOutput);
        return FfprobeResultParser.Verify(json, expectedDuration, requireVideoStream);
    }

    private Task<ExternalProcessResult> RunFfmpegAsync(
        string exePath,
        IReadOnlyList<string> arguments,
        Action<TimeSpan>? onProgress,
        Action<string>? onOutputLine,
        CancellationToken cancellationToken)
    {
        var accumulator = new FfmpegProgressAccumulator();

        void HandleStandardOutput(string line)
        {
            var elapsed = accumulator.Ingest(line);
            if (elapsed is not null)
            {
                onProgress?.Invoke(elapsed.Value);
            }
        }

        return processRunner.RunAsync(exePath, arguments, HandleStandardOutput, onOutputLine, cancellationToken);
    }

    private static string[] BuildMuxArgs(string videoPath, string audioPath, string outputPath, string audioCodec) =>
    [
        "-y", "-hide_banner", "-loglevel", "error",
        "-i", videoPath,
        "-i", audioPath,
        "-map", "0:v:0",
        "-map", "1:a:0",
        "-c:v", "copy",
        "-c:a", audioCodec,
        "-shortest",
        "-progress", "pipe:1", "-nostats",
        outputPath
    ];

    private static string FallbackAudioCodec(string outputContainer) => outputContainer.ToLowerInvariant() switch
    {
        "webm" => "libopus",
        _ => "aac"
    };

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [GeneratedRegex(@"ffmpeg version (\S+)")]
    private static partial Regex VersionPattern();
}
