using System.Windows.Threading;
using DownloadYou.Application.Diagnostics;
using DownloadYou.Application.Services;
using DownloadYou.Presentation.ViewModels;

namespace DownloadYou.Presentation.Tests.TestDoubles;

/// <summary>Arma un MainViewModel real con dobles no-operativos para las dependencias que los tests de ViewModel no necesitan ejercitar (yt-dlp/ffmpeg/SQLite reales).</summary>
public static class MainViewModelFactory
{
    public static MainViewModel Create(SettingsService? settingsService = null, FakeSettingsStore? settingsStore = null)
    {
        var videoSource = new NoOpVideoSource();
        var mediaProcessor = new NoOpMediaProcessor();
        var diagnosticsService = new EngineDiagnosticsService(videoSource, mediaProcessor);
        var analyzeUrlService = new AnalyzeUrlService(videoSource);
        var downloadService = new DownloadService(videoSource);
        var conversionService = new ConversionService(mediaProcessor);
        var downloadQueue = new DownloadQueue(downloadService, conversionService, maxConcurrency: 1);
        var historyRepository = new InMemoryHistoryRepository();
        var historyService = new HistoryService(historyRepository, downloadQueue, analyzeUrlService);
        var settings = settingsService ?? new SettingsService(settingsStore ?? new FakeSettingsStore());
        var settingsViewModel = new SettingsViewModel(settings);
        var dispatcher = Dispatcher.CurrentDispatcher;

        return new MainViewModel(diagnosticsService, analyzeUrlService, downloadQueue, historyService, settings, settingsViewModel, dispatcher);
    }
}
