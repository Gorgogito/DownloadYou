using CliWrap;

namespace DownloadYou.Infrastructure.Processes;

public sealed class ExternalProcessRunner : IExternalProcessRunner
{
    public async Task<ExternalProcessResult> RunAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        Action<string>? onOutputLine = null,
        Action<string>? onErrorLine = null,
        CancellationToken cancellationToken = default)
    {
        var stdOut = new List<string>();
        var stdErr = new List<string>();

        var command = Cli.Wrap(executablePath)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
            {
                stdOut.Add(line);
                onOutputLine?.Invoke(line);
            }))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
            {
                stdErr.Add(line);
                onErrorLine?.Invoke(line);
            }));

        var result = await command.ExecuteAsync(cancellationToken);

        return new ExternalProcessResult(result.ExitCode, stdOut, stdErr);
    }
}

public sealed record ExternalProcessResult(int ExitCode, IReadOnlyList<string> StandardOutput, IReadOnlyList<string> StandardError)
{
    public bool Succeeded => ExitCode == 0;
}
