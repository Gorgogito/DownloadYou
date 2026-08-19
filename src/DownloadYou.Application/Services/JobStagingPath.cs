namespace DownloadYou.Application.Services;

public static class JobStagingPath
{
    public static string For(Guid jobId) => Path.Combine(Path.GetTempPath(), "DownloadYou", jobId.ToString("N"));
}
