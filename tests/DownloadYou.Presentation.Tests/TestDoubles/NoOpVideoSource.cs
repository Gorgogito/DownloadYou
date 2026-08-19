using DownloadYou.Application.Abstractions;
using DownloadYou.Domain.Entities;

namespace DownloadYou.Presentation.Tests.TestDoubles;

/// <summary>No-op: los tests de ViewModel no ejercitan el pipeline real de descarga, solo construyen los servicios que MainViewModel exige por constructor.</summary>
public sealed class NoOpVideoSource : IVideoSource
{
    public Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<MediaInfo> AnalyzeAsync(string url, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    /// <summary>
    /// Nunca completa (en vez de lanzar de inmediato): así el worker en background de
    /// DownloadQueue queda esperando indefinidamente sin volver a llamar a
    /// MainViewModel._dispatcher.Invoke() desde otro hilo — ese callback cruzado de hilos
    /// se bloquearía para siempre en los tests, porque aquí no hay un message loop de
    /// Dispatcher corriendo (WPF real) que lo procese.
    /// </summary>
    public Task DownloadAsync(
        string url, string formatId, string outputFilePath,
        Action<DownloadProgressUpdate>? onProgress = null, Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default) =>
        Task.Delay(Timeout.Infinite, cancellationToken);
}
