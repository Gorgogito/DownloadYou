using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DownloadYou.Application.Abstractions;
using DownloadYou.Application.Diagnostics;
using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;

namespace DownloadYou.Presentation.ViewModels;

public sealed partial class MainViewModel(
    EngineDiagnosticsService diagnosticsService,
    AnalyzeUrlService analyzeUrlService,
    Dispatcher dispatcher) : ObservableObject
{
    public ObservableCollection<string> LogLines { get; } = [];
    public ObservableCollection<FormatOption> AvailableFormats { get; } = [];

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _summary = "Sin ejecutar todavía.";

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private bool _isAnalyzing;

    [ObservableProperty]
    private MediaInfo? _mediaInfo;

    [ObservableProperty]
    private string _analysisStatus = "Pega una URL de YouTube y presiona Analizar.";

    [RelayCommand(CanExecute = nameof(CanRunDiagnostics))]
    private async Task RunDiagnosticsAsync()
    {
        IsRunning = true;
        LogLines.Clear();
        Summary = "Comprobando yt-dlp y ffmpeg...";

        try
        {
            var result = await diagnosticsService.CheckAsync(AppendLine);

            Summary = (result.YtDlpAvailable, result.FfmpegAvailable) switch
            {
                (true, true) => $"OK — yt-dlp {result.YtDlpVersion} · ffmpeg {result.FfmpegVersion}",
                (false, true) => "Falta yt-dlp.exe. Colócalo en la carpeta 'tools'.",
                (true, false) => "Falta ffmpeg.exe. Colócalo en la carpeta 'tools'.",
                _ => "Faltan yt-dlp.exe y ffmpeg.exe en la carpeta 'tools'."
            };
        }
        finally
        {
            IsRunning = false;
        }
    }

    private bool CanRunDiagnostics() => !IsRunning;

    partial void OnIsRunningChanged(bool value) => RunDiagnosticsCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync()
    {
        IsAnalyzing = true;
        MediaInfo = null;
        AvailableFormats.Clear();
        LogLines.Clear();
        AnalysisStatus = "Analizando...";

        try
        {
            var info = await analyzeUrlService.AnalyzeAsync(Url, AppendLine);

            MediaInfo = info;
            foreach (var format in info.AvailableFormats
                         .OrderByDescending(f => f.Height ?? 0)
                         .ThenByDescending(f => f.AudioBitrateKbps ?? 0))
            {
                AvailableFormats.Add(format);
            }

            AnalysisStatus = $"{info.Title} · {FormatDuration(info.Duration)} · {info.AvailableFormats.Count} formatos disponibles";
        }
        catch (InvalidYouTubeUrlException)
        {
            AnalysisStatus = "Esa URL no parece ser de YouTube.";
        }
        catch (Exception ex)
        {
            AnalysisStatus = $"No se pudo analizar: {ex.Message}";
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    private bool CanAnalyze() => !IsAnalyzing;

    partial void OnIsAnalyzingChanged(bool value) => AnalyzeCommand.NotifyCanExecuteChanged();

    private static string FormatDuration(TimeSpan duration) =>
        duration.Hours > 0 ? duration.ToString(@"h\:mm\:ss") : duration.ToString(@"mm\:ss");

    private void AppendLine(string line) => dispatcher.Invoke(() => LogLines.Add(line));
}
