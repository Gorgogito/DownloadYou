using DownloadYou.Domain.Enums;

namespace DownloadYou.Domain.Entities;

public sealed record FormatOption(
    string FormatId,
    StreamKind Kind,
    string Container,
    string? VideoCodec,
    string? AudioCodec,
    int? Height,
    double? Fps,
    int? VideoBitrateKbps,
    int? AudioBitrateKbps,
    long? ApproxFileSizeBytes,
    /// <summary>Código de idioma ISO (p. ej. "es", "en") del audio; null en streams sin audio propio (video-only).</summary>
    string? Language = null,
    /// <summary>Señal de yt-dlp para identificar la pista "original"/default entre varios idiomas: mayor valor = más preferida.</summary>
    int LanguagePreference = 0)
{
    public bool RequiresMux => Kind != StreamKind.Muxed;

    public string DisplayLabel => Kind switch
    {
        StreamKind.VideoOnly => $"{Height}p" + (Fps is > 30 ? $" {Fps:0}fps" : string.Empty),
        StreamKind.AudioOnly => $"{AudioBitrateKbps} kbps",
        StreamKind.Muxed => $"{Height}p (combinado)",
        _ => FormatId
    };
}
