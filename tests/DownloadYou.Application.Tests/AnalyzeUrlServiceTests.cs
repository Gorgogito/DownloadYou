using DownloadYou.Application.Abstractions;
using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;

namespace DownloadYou.Application.Tests;

public class AnalyzeUrlServiceTests
{
    [Fact]
    public async Task AnalyzeAsync_RejectsInvalidUrl_WithoutCallingVideoSource()
    {
        var videoSource = new RecordingVideoSource();
        var service = new AnalyzeUrlService(videoSource);

        await Assert.ThrowsAsync<InvalidYouTubeUrlException>(() => service.AnalyzeAsync("https://vimeo.com/123"));

        Assert.False(videoSource.WasCalled);
    }

    [Fact]
    public async Task AnalyzeAsync_DelegatesToVideoSource_ForValidUrl()
    {
        var expected = new MediaInfo("https://youtu.be/BaW_jenozKc", "BaW_jenozKc", "Título", "Autor", TimeSpan.FromMinutes(1), null, []);
        var videoSource = new RecordingVideoSource { Result = expected };
        var service = new AnalyzeUrlService(videoSource);

        var result = await service.AnalyzeAsync("https://youtu.be/BaW_jenozKc");

        Assert.Same(expected, result);
        Assert.True(videoSource.WasCalled);
    }

    private sealed class RecordingVideoSource : IVideoSource
    {
        public bool WasCalled { get; private set; }
        public MediaInfo? Result { get; set; }

        public Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<MediaInfo> AnalyzeAsync(string url, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(Result!);
        }

        public Task DownloadAsync(
            string url,
            string formatId,
            string outputFilePath,
            Action<DownloadProgressUpdate>? onProgress = null,
            Action<string>? onOutputLine = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
