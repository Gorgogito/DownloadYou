namespace DownloadYou.Domain.Enums;

public enum JobStatus
{
    Queued,
    Analyzing,
    Downloading,
    Converting,
    Verifying,
    Completed,
    Failed,
    Canceled,
    Paused
}
