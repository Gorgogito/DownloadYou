using DownloadYou.Application.Diagnostics;
using DownloadYou.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DownloadYou.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<EngineDiagnosticsService>();
        services.AddSingleton<AnalyzeUrlService>();
        services.AddSingleton<DownloadService>();
        services.AddSingleton<ConversionService>();
        services.AddSingleton<DownloadQueue>();
        return services;
    }
}
