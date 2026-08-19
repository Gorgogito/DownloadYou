using DownloadYou.Application.Abstractions;
using DownloadYou.Infrastructure.Configuration;
using DownloadYou.Infrastructure.ExternalTools;
using DownloadYou.Infrastructure.History;
using DownloadYou.Infrastructure.MediaProcessing;
using DownloadYou.Infrastructure.Processes;
using DownloadYou.Infrastructure.VideoSources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DownloadYou.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ToolsOptions>(configuration.GetSection(ToolsOptions.SectionName));
        services.Configure<HistoryOptions>(configuration.GetSection(HistoryOptions.SectionName));
        services.Configure<SettingsOptions>(configuration.GetSection(SettingsOptions.SectionName));

        services.AddSingleton<IExternalToolLocator, ExternalToolLocator>();
        services.AddSingleton<IExternalProcessRunner, ExternalProcessRunner>();
        services.AddSingleton<IVideoSource, YtDlpVideoSource>();
        services.AddSingleton<IMediaProcessor, FfmpegMediaProcessor>();
        services.AddSingleton<IHistoryRepository, SqliteHistoryRepository>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();

        return services;
    }
}
