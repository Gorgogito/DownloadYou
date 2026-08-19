namespace DownloadYou.Infrastructure.Configuration;

public sealed class SettingsOptions
{
    public const string SectionName = "Settings";

    /// <summary>Admite variables de entorno como %AppData%.</summary>
    public string FilePath { get; set; } = @"%AppData%\DownloadYou\settings.json";
}
