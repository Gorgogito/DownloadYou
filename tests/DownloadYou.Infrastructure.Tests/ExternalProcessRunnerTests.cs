using DownloadYou.Infrastructure.Processes;

namespace DownloadYou.Infrastructure.Tests;

public class ExternalProcessRunnerTests
{
    private static readonly string CmdExePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

    [Fact]
    public async Task RunAsync_CapturesStandardOutput_LineByLine()
    {
        var runner = new ExternalProcessRunner();
        var streamedLines = new List<string>();

        var result = await runner.RunAsync(
            CmdExePath,
            ["/c", "echo", "hello-download-you"],
            onOutputLine: streamedLines.Add);

        Assert.True(result.Succeeded);
        Assert.Contains("hello-download-you", result.StandardOutput);
        Assert.Contains("hello-download-you", streamedLines);
    }

    [Fact]
    public async Task RunAsync_ReportsNonZeroExitCode_WithoutThrowing()
    {
        var runner = new ExternalProcessRunner();

        var result = await runner.RunAsync(CmdExePath, ["/c", "exit", "7"]);

        Assert.False(result.Succeeded);
        Assert.Equal(7, result.ExitCode);
    }
}
