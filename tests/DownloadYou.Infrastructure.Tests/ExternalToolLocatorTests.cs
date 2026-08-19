using DownloadYou.Application.Abstractions;
using DownloadYou.Infrastructure.Configuration;
using DownloadYou.Infrastructure.ExternalTools;
using Microsoft.Extensions.Options;

namespace DownloadYou.Infrastructure.Tests;

public class ExternalToolLocatorTests : IDisposable
{
    private readonly string _tempDir = Directory.CreateTempSubdirectory("dy-tools-").FullName;

    [Fact]
    public async Task ResolveAsync_FindsExecutable_InConfiguredAbsoluteDirectory()
    {
        var exePath = Path.Combine(_tempDir, "ffmpeg.exe");
        File.WriteAllText(exePath, "stub");

        var locator = new ExternalToolLocator(Options.Create(new ToolsOptions { ToolsDirectory = _tempDir }));

        var resolved = await locator.ResolveAsync(ExternalTool.FfMpeg);

        Assert.Equal(exePath, resolved);
    }

    [Fact]
    public async Task ResolveAsync_Throws_WithSearchedLocations_WhenNotFound()
    {
        var locator = new ExternalToolLocator(Options.Create(new ToolsOptions { ToolsDirectory = _tempDir }));

        var ex = await Assert.ThrowsAsync<ExternalToolNotFoundException>(() => locator.ResolveAsync(ExternalTool.YtDlp));

        Assert.Equal(ExternalTool.YtDlp, ex.Tool);
        Assert.NotEmpty(ex.SearchedLocations);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
