namespace DownloadYou.Domain.Entities;

public sealed record MediaInfo(
    string Url,
    string VideoId,
    string Title,
    string Author,
    TimeSpan Duration,
    string? ThumbnailUrl,
    IReadOnlyList<FormatOption> AvailableFormats)
{
    public int? BestAudioBitrateKbps => AvailableFormats
        .Where(f => f.AudioBitrateKbps is not null)
        .Select(f => f.AudioBitrateKbps)
        .DefaultIfEmpty()
        .Max();
}
