using DownloadYou.Application.Abstractions;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Application.Services;

/// <summary>
/// Decide, a partir de la calidad elegida por el usuario, si hace falta emparejar un
/// stream de audio por separado (video-only / DASH) antes de crear el DownloadJob.
/// </summary>
public static class DownloadJobFactory
{
    /// <exception cref="NoCompatibleAudioStreamException" />
    public static DownloadJob Create(
        MediaInfo mediaInfo,
        FormatOption selectedFormat,
        DownloadKind kind,
        string targetDirectory,
        string fileNameTemplate,
        int targetAudioBitrateKbps)
    {
        FormatOption? pairedAudio = null;

        if (kind == DownloadKind.Video && selectedFormat.Kind == StreamKind.VideoOnly)
        {
            // Prioriza un audio de la misma familia de contenedor que el video: permite que
            // FfmpegMediaProcessor.MuxAsync remuxe con "-c copy" (rápido, sin pérdida) en el
            // caso común, en vez de depender de su fallback de transcodificación de audio.
            pairedAudio = mediaInfo.AvailableFormats
                .Where(f => f.Kind == StreamKind.AudioOnly)
                .OrderByDescending(f => ContainerFamily(f.Container) == ContainerFamily(selectedFormat.Container))
                .ThenByDescending(f => f.AudioBitrateKbps ?? 0)
                .FirstOrDefault()
                ?? throw new NoCompatibleAudioStreamException(mediaInfo.VideoId);
        }

        return new DownloadJob
        {
            Id = Guid.NewGuid(),
            MediaInfo = mediaInfo,
            SelectedFormat = selectedFormat,
            PairedAudioFormat = pairedAudio,
            Kind = kind,
            TargetDirectory = targetDirectory,
            FileNameTemplate = fileNameTemplate,
            TargetAudioBitrateKbps = targetAudioBitrateKbps,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static string ContainerFamily(string container) => container.ToLowerInvariant() switch
    {
        "mp4" or "m4a" or "m4v" or "mov" => "mp4",
        "webm" => "webm",
        var other => other
    };
}
