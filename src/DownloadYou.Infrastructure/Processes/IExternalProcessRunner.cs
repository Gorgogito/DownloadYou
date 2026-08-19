namespace DownloadYou.Infrastructure.Processes;

public interface IExternalProcessRunner
{
    Task<ExternalProcessResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        Action<string>? onOutputLine = null,
        Action<string>? onErrorLine = null,
        CancellationToken cancellationToken = default);
}
