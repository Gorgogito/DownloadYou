namespace DownloadYou.Infrastructure.Configuration;

public sealed class HistoryOptions
{
    public const string SectionName = "History";

    /// <summary>Admite variables de entorno como %AppData%.</summary>
    public string DatabasePath { get; set; } = @"%AppData%\DownloadYou\history.db";
}
