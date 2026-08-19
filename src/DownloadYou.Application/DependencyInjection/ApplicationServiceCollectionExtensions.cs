using DownloadYou.Application.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace DownloadYou.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<EngineDiagnosticsService>();
        return services;
    }
}
