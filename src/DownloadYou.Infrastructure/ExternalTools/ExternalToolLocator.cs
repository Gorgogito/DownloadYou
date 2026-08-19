using DownloadYou.Application.Abstractions;
using DownloadYou.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace DownloadYou.Infrastructure.ExternalTools;

public sealed class ExternalToolLocator(IOptions<ToolsOptions> toolsOptions) : IExternalToolLocator
{
    private static readonly Dictionary<ExternalTool, string> ExecutableNames = new()
    {
        [ExternalTool.YtDlp] = "yt-dlp.exe",
        [ExternalTool.FfMpeg] = "ffmpeg.exe",
        [ExternalTool.FfProbe] = "ffprobe.exe"
    };

    public Task<string> ResolveAsync(ExternalTool tool, CancellationToken cancellationToken = default)
    {
        var exeName = ExecutableNames[tool];
        var searched = new List<string>();

        foreach (var candidateDir in EnumerateToolsDirectories())
        {
            var candidatePath = Path.Combine(candidateDir, exeName);
            searched.Add(candidatePath);
            if (File.Exists(candidatePath))
            {
                return Task.FromResult(candidatePath);
            }
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidatePath = Path.Combine(dir, exeName);
            if (File.Exists(candidatePath))
            {
                return Task.FromResult(candidatePath);
            }
        }
        searched.Add("PATH del sistema");

        throw new ExternalToolNotFoundException(tool, searched);
    }

    private IEnumerable<string> EnumerateToolsDirectories()
    {
        var configuredDir = toolsOptions.Value.ToolsDirectory;

        if (Path.IsPathRooted(configuredDir))
        {
            yield return configuredDir;
            yield break;
        }

        // AppContext.BaseDirectory apunta a una carpeta temporal de autoextracción en un
        // publish self-contained/single-file (PublishSingleFile + IncludeAllContentForSelfExtract),
        // no a la carpeta real de instalación — ahí nunca encontraría la carpeta "tools" que
        // arma el instalador junto al .exe. Environment.ProcessPath sí apunta al .exe real en disco.
        var startDirectory = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
        var dir = new DirectoryInfo(startDirectory);
        for (var i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
        {
            yield return Path.Combine(dir.FullName, configuredDir);
        }
    }
}
