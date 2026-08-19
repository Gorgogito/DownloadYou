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
        services.AddSingleton<SettingsService>();

        // La concurrencia de la cola se fija al construirla (arranca N workers de una);
        // cambiar "Descargas simultáneas" en Configuración aplica desde el próximo inicio.
        services.AddSingleton(sp => new DownloadQueue(
            sp.GetRequiredService<DownloadService>(),
            sp.GetRequiredService<ConversionService>(),
            sp.GetRequiredService<SettingsService>().Current.MaxConcurrentDownloads));

        services.AddSingleton<HistoryService>();
        return services;
    }
}
