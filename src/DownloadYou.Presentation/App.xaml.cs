using System.Windows;
using System.Windows.Threading;
using DownloadYou.Application.DependencyInjection;
using DownloadYou.Application.Services;
using DownloadYou.Infrastructure.DependencyInjection;
using DownloadYou.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DownloadYou.Presentation;

public partial class App : System.Windows.Application
{
    private readonly IHost _host;

    public App()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddSingleton(Dispatcher.CurrentDispatcher);
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        _host = builder.Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = _host.Services.GetRequiredService<SettingsService>();
        ThemeApplier.Apply(settingsService.Current.ThemePreference);

        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
