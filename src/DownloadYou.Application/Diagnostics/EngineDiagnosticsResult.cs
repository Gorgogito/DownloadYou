namespace DownloadYou.Application.Diagnostics;

public sealed record EngineDiagnosticsResult(
    bool YtDlpAvailable,
    string? YtDlpVersion,
    string? YtDlpError,
    bool FfmpegAvailable,
    string? FfmpegVersion,
    string? FfmpegError);
