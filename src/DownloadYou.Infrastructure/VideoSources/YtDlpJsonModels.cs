namespace DownloadYou.Infrastructure.VideoSources;

internal sealed record YtDlpVideoJson(
    string? Id,
    string? Title,
    string? Uploader,
    string? Channel,
    double? Duration,
    string? Thumbnail,
    List<YtDlpFormatJson>? Formats);

internal sealed record YtDlpFormatJson(
    string? FormatId,
    string? Ext,
    string? Vcodec,
    string? Acodec,
    int? Height,
    double? Fps,
    double? Tbr,
    double? Vbr,
    double? Abr,
    long? Filesize,
    long? FilesizeApprox,
    string? Language,
    int? LanguagePreference);
