namespace DownloadYou.Application.Abstractions;

public interface IMediaProcessor
{
    Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default);
}
