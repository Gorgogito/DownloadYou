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
        return services;
    }
}
