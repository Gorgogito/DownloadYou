using DownloadYou.Application.Abstractions;
using DownloadYou.Domain.Entities;

namespace DownloadYou.Presentation.Tests.TestDoubles;

/// <summary>Como NoOpVideoSource, pero AnalyzeAsync devuelve un MediaInfo configurable en vez de lanzar.</summary>
public sealed class FakeAnalyzableVideoSource(MediaInfo mediaInfoToReturn) : IVideoSource
{
    public Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<MediaInfo> AnalyzeAsync(string url, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(mediaInfoToReturn);

    public Task DownloadAsync(
        string url, string formatId, string outputFilePath,
        Action<DownloadProgressUpdate>? onProgress = null, Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default) =>
        Task.Delay(Timeout.Infinite, cancellationToken);
}
