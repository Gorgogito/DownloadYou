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
        int targetAudioBitrateKbps,
        ExistingFileBehavior existingFileBehavior = ExistingFileBehavior.Rename,
        string? preferredAudioLanguage = null)
    {
        FormatOption? pairedAudio = null;

        if (kind == DownloadKind.Video && selectedFormat.Kind == StreamKind.VideoOnly)
        {
            var candidates = mediaInfo.AvailableFormats.Where(f => f.Kind == StreamKind.AudioOnly);

            // Si el video tiene varios idiomas de audio (doblajes) y el usuario eligió uno,
            // priorizarlo por sobre todo lo demás; si ese idioma no está disponible para
            // este video (raro, pero posible), cae al comportamiento de siempre sin fallar.
            if (!string.IsNullOrWhiteSpace(preferredAudioLanguage) &&
                candidates.Any(f => string.Equals(f.Language, preferredAudioLanguage, StringComparison.OrdinalIgnoreCase)))
            {
                candidates = candidates.Where(f => string.Equals(f.Language, preferredAudioLanguage, StringComparison.OrdinalIgnoreCase));
            }

            // Prioriza un audio de la misma familia de contenedor que el video: permite que
            // FfmpegMediaProcessor.MuxAsync remuxe con "-c copy" (rápido, sin pérdida) en el
            // caso común, en vez de depender de su fallback de transcodificación de audio.
            pairedAudio = candidates
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
            CreatedAt = DateTimeOffset.UtcNow,
            ExistingFileBehavior = existingFileBehavior
        };
    }

    private static string ContainerFamily(string container) => container.ToLowerInvariant() switch
    {
        "mp4" or "m4a" or "m4v" or "mov" => "mp4",
        "webm" => "webm",
        var other => other
    };
}
