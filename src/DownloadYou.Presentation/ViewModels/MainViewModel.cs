using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DownloadYou.Application.Diagnostics;

namespace DownloadYou.Presentation.ViewModels;

public sealed partial class MainViewModel(EngineDiagnosticsService diagnosticsService, Dispatcher dispatcher) : ObservableObject
{
    public ObservableCollection<string> LogLines { get; } = [];

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _summary = "Sin ejecutar todavía.";

    [RelayCommand(CanExecute = nameof(CanRunDiagnostics))]
    private async Task RunDiagnosticsAsync()
    {
        IsRunning = true;
        LogLines.Clear();
        Summary = "Comprobando yt-dlp y ffmpeg...";

        void AppendLine(string line) => dispatcher.Invoke(() => LogLines.Add(line));

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
}
