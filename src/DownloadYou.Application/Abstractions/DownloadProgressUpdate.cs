namespace DownloadYou.Application.Abstractions;

public sealed record DownloadProgressUpdate(
    string Status,
    long? DownloadedBytes,
    long? TotalBytes,
    double? SpeedBytesPerSecond,
    TimeSpan? Eta)
{
    public double? PercentComplete => TotalBytes is > 0 && DownloadedBytes is not null
        ? Math.Clamp(DownloadedBytes.Value * 100.0 / TotalBytes.Value, 0, 100)
        : null;
}
