using DownloadYou.Domain.Entities;

namespace DownloadYou.Application.Abstractions;

public interface IVideoSource
{
    Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">yt-dlp no pudo analizar la URL o no devolvió metadatos utilizables.</exception>
    Task<MediaInfo> AnalyzeAsync(string url, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default);
}
