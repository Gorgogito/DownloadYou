using DownloadYou.Application.Abstractions;
using DownloadYou.Application.Validation;
using DownloadYou.Domain.Entities;

namespace DownloadYou.Application.Services;

public sealed class AnalyzeUrlService(IVideoSource videoSource)
{
    /// <exception cref="InvalidYouTubeUrlException" />
    /// <exception cref="InvalidOperationException" />
    public Task<MediaInfo> AnalyzeAsync(string url, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
    {
        if (!YouTubeUrlValidator.IsValid(url))
        {
            throw new InvalidYouTubeUrlException(url);
        }

        return videoSource.AnalyzeAsync(url.Trim(), onOutputLine, cancellationToken);
    }
}
