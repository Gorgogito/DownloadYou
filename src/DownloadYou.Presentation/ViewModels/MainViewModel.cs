using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DownloadYou.Application.Abstractions;
using DownloadYou.Application.Diagnostics;
using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;
using DownloadYou.Presentation.Models;

namespace DownloadYou.Presentation.ViewModels;

public sealed partial class MainViewModel(
    EngineDiagnosticsService diagnosticsService,
    AnalyzeUrlService analyzeUrlService,
    DownloadService downloadService,
    ConversionService conversionService,
    Dispatcher dispatcher) : ObservableObject
{
    private const string DefaultFileNameTemplate = "{title} - {author} [{quality}].{ext}";
    private const int DefaultAudioBitrateKbps = 192;
    private static readonly string[] PipelineStageNames = ["Analizando", "Descargando", "Convirtiendo", "Verificando", "Finalizado"];

    private CancellationTokenSource? _downloadCts;

    public ObservableCollection<string> LogLines { get; } = [];
    public ObservableCollection<FormatOption> AvailableFormats { get; } = [];
    public ObservableCollection<PipelineStepViewModel> PipelineSteps { get; } = [];

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
    private bool _hasMediaInfo;

    [ObservableProperty]
    private string _analysisStatus = "Pega una URL de YouTube y presiona Analizar.";

    [ObservableProperty]
    private FormatOption? _selectedFormat;

    [ObservableProperty]
    private bool _isVideoKind = true;

    [ObservableProperty]
    private bool _isAudioMp3Kind;

    [ObservableProperty]
    private string _targetFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private string _downloadStatus = string.Empty;

    [ObservableProperty]
    private double _downloadProgressPercent;

    [ObservableProperty]
    private string _downloadSpeedLabel = string.Empty;

    [ObservableProperty]
    private string _downloadedSizeLabel = string.Empty;

    [ObservableProperty]
    private string _downloadEtaLabel = string.Empty;

    [ObservableProperty]
    private JobStatus? _currentJobStatus;

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
        SelectedFormat = null;
        AvailableFormats.Clear();
        LogLines.Clear();
        AnalysisStatus = "Analizando...";
        ResetDownloadProgress();

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

    partial void OnMediaInfoChanged(MediaInfo? value)
    {
        HasMediaInfo = value is not null;
        DownloadCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void SelectVideoKind() => IsVideoKind = true;

    [RelayCommand]
    private void SelectAudioKind() => IsAudioMp3Kind = true;

    partial void OnIsVideoKindChanged(bool value)
    {
        if (value)
        {
            IsAudioMp3Kind = false;
        }
    }

    partial void OnIsAudioMp3KindChanged(bool value)
    {
        if (value)
        {
            IsVideoKind = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync()
    {
        if (MediaInfo is null || SelectedFormat is null)
        {
            return;
        }

        IsDownloading = true;
        ResetDownloadProgress();
        _downloadCts = new CancellationTokenSource();

        try
        {
            var kind = IsAudioMp3Kind ? DownloadKind.AudioMp3 : DownloadKind.Video;
            var job = DownloadJobFactory.Create(
                MediaInfo, SelectedFormat, kind, TargetFolder, DefaultFileNameTemplate, DefaultAudioBitrateKbps);

            await downloadService.RunAsync(
                job,
                onOutputLine: AppendLine,
                onProgressChanged: () => dispatcher.Invoke(() => ReflectJobProgress(job)),
                cancellationToken: _downloadCts.Token);

            if (job.Status == JobStatus.Converting)
            {
                await conversionService.RunAsync(
                    job,
                    onOutputLine: AppendLine,
                    onProgressChanged: () => dispatcher.Invoke(() => ReflectJobProgress(job)),
                    cancellationToken: _downloadCts.Token);
            }

            DownloadStatus = job.Status switch
            {
                JobStatus.Completed => $"Completado: {job.OutputFilePath}",
                JobStatus.Canceled => "Cancelado.",
                JobStatus.Failed => $"Error: {job.ErrorMessage}",
                _ => job.Status.ToString()
            };
        }
        catch (NoCompatibleAudioStreamException ex)
        {
            CurrentJobStatus = JobStatus.Failed;
            DownloadStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
            _downloadCts?.Dispose();
            _downloadCts = null;
        }
    }

    private bool CanDownload() =>
        !IsDownloading && MediaInfo is not null && SelectedFormat is not null && !string.IsNullOrWhiteSpace(TargetFolder);

    partial void OnIsDownloadingChanged(bool value)
    {
        DownloadCommand.NotifyCanExecuteChanged();
        CancelDownloadCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedFormatChanged(FormatOption? value) => DownloadCommand.NotifyCanExecuteChanged();

    partial void OnTargetFolderChanged(string value) => DownloadCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanCancelDownload))]
    private void CancelDownload() => _downloadCts?.Cancel();

    private bool CanCancelDownload() => IsDownloading;

    private void ReflectJobProgress(DownloadJob job)
    {
        DownloadProgressPercent = job.ProgressPercent;
        DownloadSpeedLabel = FormatSpeed(job.SpeedBytesPerSecond);
        DownloadedSizeLabel = FormatSize(job.DownloadedBytes, job.TotalBytes);
        DownloadEtaLabel = FormatEta(job.Eta);
        DownloadStatus = job.Status.ToString();
        CurrentJobStatus = job.Status;
        RebuildPipelineSteps(job.Status);
    }

    private void ResetDownloadProgress()
    {
        DownloadProgressPercent = 0;
        DownloadSpeedLabel = string.Empty;
        DownloadedSizeLabel = string.Empty;
        DownloadEtaLabel = string.Empty;
        DownloadStatus = string.Empty;
        CurrentJobStatus = null;
        PipelineSteps.Clear();
    }

    private void RebuildPipelineSteps(JobStatus status)
    {
        PipelineSteps.Clear();

        if (status is JobStatus.Failed or JobStatus.Canceled)
        {
            var failedIndex = status == JobStatus.Failed ? CurrentPipelineIndex(status) : PipelineStageNames.Length - 1;
            for (var i = 0; i < PipelineStageNames.Length; i++)
            {
                var state = i < failedIndex ? PipelineStepState.Done : i == failedIndex ? PipelineStepState.Error : PipelineStepState.Pending;
                PipelineSteps.Add(new PipelineStepViewModel(PipelineStageNames[i], state));
            }
            return;
        }

        if (status == JobStatus.Completed)
        {
            foreach (var name in PipelineStageNames)
            {
                PipelineSteps.Add(new PipelineStepViewModel(name, PipelineStepState.Done));
            }
            return;
        }

        var currentIndex = CurrentPipelineIndex(status);
        for (var i = 0; i < PipelineStageNames.Length; i++)
        {
            var state = i < currentIndex ? PipelineStepState.Done : i == currentIndex ? PipelineStepState.Current : PipelineStepState.Pending;
            PipelineSteps.Add(new PipelineStepViewModel(PipelineStageNames[i], state));
        }
    }

    private static int CurrentPipelineIndex(JobStatus status) => status switch
    {
        JobStatus.Queued or JobStatus.Analyzing => 0,
        JobStatus.Downloading => 1,
        JobStatus.Converting => 2,
        JobStatus.Verifying => 3,
        _ => 0
    };

    private static string FormatDuration(TimeSpan duration) =>
        duration.Hours > 0 ? duration.ToString(@"h\:mm\:ss") : duration.ToString(@"mm\:ss");

    private static string FormatSpeed(double? bytesPerSecond) =>
        bytesPerSecond is null ? string.Empty : $"{FormatSize(bytesPerSecond.Value)}/s";

    private static string FormatSize(long? downloaded, long? total)
    {
        if (downloaded is null)
        {
            return string.Empty;
        }

        return total is null
            ? FormatSize(downloaded.Value)
            : $"{FormatSize(downloaded.Value)} / {FormatSize(total.Value)}";
    }

    private static string FormatSize(double bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = bytes;
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return $"{value:0.#} {units[unitIndex]}";
    }

    private static string FormatEta(TimeSpan? eta) =>
        eta is null ? string.Empty : FormatDuration(eta.Value);

    private void AppendLine(string line) => dispatcher.Invoke(() => LogLines.Add(line));
}
