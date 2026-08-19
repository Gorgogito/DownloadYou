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
    long? ApproxFileSizeBytes)
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
