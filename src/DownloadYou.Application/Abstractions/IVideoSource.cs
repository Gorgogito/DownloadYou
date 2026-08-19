using DownloadYou.Domain.Entities;

namespace DownloadYou.Application.Abstractions;

public interface IVideoSource
{
    Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">yt-dlp no pudo analizar la URL o no devolvió metadatos utilizables.</exception>
    Task<MediaInfo> AnalyzeAsync(string url, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Descarga el stream identificado por <paramref name="formatId"/> a la ruta exacta indicada.
    /// yt-dlp escribe a "&lt;outputFilePath&gt;.part" mientras descarga y renombra al finalizar.
    /// </summary>
    /// <exception cref="InvalidOperationException">yt-dlp terminó con error.</exception>
    Task DownloadAsync(
        string url,
        string formatId,
        string outputFilePath,
        Action<DownloadProgressUpdate>? onProgress = null,
        Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default);
}
