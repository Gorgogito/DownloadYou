namespace DownloadYou.Application.Abstractions;

public interface IExternalToolLocator
{
    /// <exception cref="ExternalToolNotFoundException" />
    Task<string> ResolveAsync(ExternalTool tool, CancellationToken cancellationToken = default);
}
