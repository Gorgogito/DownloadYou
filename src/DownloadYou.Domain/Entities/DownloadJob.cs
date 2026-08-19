using DownloadYou.Domain.Enums;

namespace DownloadYou.Domain.Entities;

public sealed class DownloadJob
{
    public required Guid Id { get; init; }
    public required MediaInfo MediaInfo { get; init; }
    public required FormatOption SelectedFormat { get; init; }
    public required DownloadKind Kind { get; init; }
    public required string TargetDirectory { get; init; }
    public required string FileNameTemplate { get; init; }
    public required int TargetAudioBitrateKbps { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public JobStatus Status { get; set; } = JobStatus.Queued;
    public double ProgressPercent { get; set; }
    public double? SpeedBytesPerSecond { get; set; }
    public long? DownloadedBytes { get; set; }
    public long? TotalBytes { get; set; }
    public TimeSpan? Eta { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OutputFilePath { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
