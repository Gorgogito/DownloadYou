using DownloadYou.Domain.Enums;

namespace DownloadYou.Domain.Entities;

/// <param name="FormatId">Id del formato de yt-dlp elegido; permite que "repetir descarga" seleccione exactamente la misma calidad.</param>
public sealed record HistoryRecord(
    Guid Id,
    string Url,
    string Title,
    DateTimeOffset Date,
    DownloadKind Kind,
    string FormatId,
    string QualityLabel,
    string OutputFile,
    JobStatus Status,
    TimeSpan ProcessDuration);
