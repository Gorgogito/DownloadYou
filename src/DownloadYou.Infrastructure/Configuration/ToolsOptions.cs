namespace DownloadYou.Infrastructure.Configuration;

public sealed class ToolsOptions
{
    public const string SectionName = "Tools";

    public string ToolsDirectory { get; set; } = "tools";
}
