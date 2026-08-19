using DownloadYou.Domain.Enums;

namespace DownloadYou.Domain.Entities;

public sealed class AppSettings
{
    public string DownloadFolder { get; set; } = string.Empty;
    public DownloadKind DefaultKind { get; set; } = DownloadKind.Video;
    public int DefaultAudioBitrateKbps { get; set; } = 192;
    public int MaxConcurrentDownloads { get; set; } = 3;
    public ExistingFileBehavior ExistingFileBehavior { get; set; } = ExistingFileBehavior.Rename;
    public string FileNameTemplate { get; set; } = "{title} - {author} [{quality}].{ext}";
    public string Language { get; set; } = "es";
    public bool UseDarkTheme { get; set; }
    public bool ShowLegalDisclaimer { get; set; } = true;
}
