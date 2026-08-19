using DownloadYou.Application.Abstractions;

namespace DownloadYou.Presentation.Tests.TestDoubles;

public sealed class NoOpMediaProcessor : IMediaProcessor
{
    public Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task MuxAsync(
        string videoFilePath, string audioFilePath, string outputFilePath,
        Action<TimeSpan>? onProgress = null, Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task ExtractAudioAsync(
        string sourceFilePath, string outputFilePath, int bitrateKbps,
        Action<TimeSpan>? onProgress = null, Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<MediaVerificationResult> VerifyAsync(
        string filePath, TimeSpan expectedDuration, bool requireVideoStream,
        Action<string>? onOutputLine = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
