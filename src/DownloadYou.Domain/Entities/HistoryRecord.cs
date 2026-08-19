using DownloadYou.Domain.Enums;

namespace DownloadYou.Domain.Entities;

public sealed record HistoryRecord(
    Guid Id,
    string Url,
    string Title,
    DateTimeOffset Date,
    DownloadKind Kind,
    string QualityLabel,
    string OutputFile,
    JobStatus Status,
    TimeSpan ProcessDuration);
