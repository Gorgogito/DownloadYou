namespace DownloadYou.Application.Abstractions;

public interface IVideoSource
{
    Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default);
}
