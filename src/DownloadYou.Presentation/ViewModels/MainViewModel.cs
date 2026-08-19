using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DownloadYou.Application.Abstractions;
using DownloadYou.Application.Diagnostics;
using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;
using DownloadYou.Presentation.Formatting;
using DownloadYou.Presentation.Models;

namespace DownloadYou.Presentation.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private const string DefaultFileNameTemplate = "{title} - {author} [{quality}].{ext}";
    private const int DefaultAudioBitrateKbps = 192;

    private readonly EngineDiagnosticsService _diagnosticsService;
    private readonly AnalyzeUrlService _analyzeUrlService;
    private readonly DownloadQueue _downloadQueue;
    private readonly HistoryService _historyService;
    private readonly Dispatcher _dispatcher;

    public MainViewModel(
        EngineDiagnosticsService diagnosticsService,
        AnalyzeUrlService analyzeUrlService,
        DownloadQueue downloadQueue,
        HistoryService historyService,
        Dispatcher dispatcher)
    {
        _diagnosticsService = diagnosticsService;
        _analyzeUrlService = analyzeUrlService;
        _downloadQueue = downloadQueue;
        _historyService = historyService;
        _dispatcher = dispatcher;

        _downloadQueue.JobEnqueued += OnJobEnqueued;
        _downloadQueue.JobUpdated += OnJobUpdated;
        _historyService.RecordAdded += OnHistoryRecordAdded;

        _ = LoadHistoryAsync();
    }

    public ObservableCollection<string> LogLines { get; } = [];
    public ObservableCollection<FormatOption> AvailableFormats { get; } = [];
    public ObservableCollection<DownloadJobViewModel> QueueItems { get; } = [];
    public ObservableCollection<HistoryEntryViewModel> HistoryEntries { get; } = [];
    public ObservableCollection<HistoryEntryViewModel> LibraryEntries { get; } = [];

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
    private string _enqueueStatus = string.Empty;

    [ObservableProperty]
    private string _historySearchQuery = string.Empty;

    [ObservableProperty]
    private string _historyStatus = string.Empty;

    [ObservableProperty]
    private LibraryFilter _selectedLibraryFilter = LibraryFilter.Recent;

    [RelayCommand(CanExecute = nameof(CanRunDiagnostics))]
    private async Task RunDiagnosticsAsync()
    {
        IsRunning = true;
        LogLines.Clear();
        Summary = "Comprobando yt-dlp y ffmpeg...";

        try
        {
            var result = await _diagnosticsService.CheckAsync(AppendLine);

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
        EnqueueStatus = string.Empty;

        try
        {
            var info = await _analyzeUrlService.AnalyzeAsync(Url, AppendLine);

            MediaInfo = info;
            foreach (var format in info.AvailableFormats
                         .OrderByDescending(f => f.Height ?? 0)
                         .ThenByDescending(f => f.AudioBitrateKbps ?? 0))
            {
                AvailableFormats.Add(format);
            }

            AnalysisStatus = $"{info.Title} · {DisplayFormat.Duration(info.Duration)} · {info.AvailableFormats.Count} formatos disponibles";
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
        EnqueueCommand.NotifyCanExecuteChanged();
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

    [RelayCommand(CanExecute = nameof(CanEnqueue))]
    private void Enqueue()
    {
        if (MediaInfo is null || SelectedFormat is null)
        {
            return;
        }

        try
        {
            var kind = IsAudioMp3Kind ? DownloadKind.AudioMp3 : DownloadKind.Video;
            var job = DownloadJobFactory.Create(
                MediaInfo, SelectedFormat, kind, TargetFolder, DefaultFileNameTemplate, DefaultAudioBitrateKbps);

            _downloadQueue.Enqueue(job);
            EnqueueStatus = $"Agregado a la cola: {job.MediaInfo.Title} ({job.SelectedFormat.DisplayLabel}).";
        }
        catch (NoCompatibleAudioStreamException ex)
        {
            EnqueueStatus = $"Error: {ex.Message}";
        }
    }

    private bool CanEnqueue() => MediaInfo is not null && SelectedFormat is not null && !string.IsNullOrWhiteSpace(TargetFolder);

    partial void OnSelectedFormatChanged(FormatOption? value) => EnqueueCommand.NotifyCanExecuteChanged();

    partial void OnTargetFolderChanged(string value) => EnqueueCommand.NotifyCanExecuteChanged();

    private void OnJobEnqueued(DownloadJob job) =>
        _dispatcher.Invoke(() => QueueItems.Insert(0, new DownloadJobViewModel(job, _downloadQueue)));

    private void OnJobUpdated(DownloadJob job) =>
        _dispatcher.Invoke(() => QueueItems.FirstOrDefault(q => q.Id == job.Id)?.Refresh());

    [RelayCommand]
    private async Task SearchHistoryAsync()
    {
        var results = await _historyService.SearchAsync(HistorySearchQuery);
        HistoryEntries.Clear();
        foreach (var record in results)
        {
            HistoryEntries.Add(new HistoryEntryViewModel(record));
        }
        RebuildLibrary();
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            await SearchHistoryAsync();
        }
        catch (Exception ex)
        {
            HistoryStatus = $"No se pudo cargar el historial: {ex.Message}";
        }
    }

    private void OnHistoryRecordAdded(HistoryRecord record) =>
        _dispatcher.Invoke(() =>
        {
            HistoryEntries.Insert(0, new HistoryEntryViewModel(record));
            RebuildLibrary();
        });

    [RelayCommand]
    private async Task RepeatDownloadAsync(HistoryEntryViewModel? entry)
    {
        if (entry is null)
        {
            return;
        }

        try
        {
            HistoryStatus = $"Analizando de nuevo: {entry.Title}...";
            var job = await _historyService.RepeatAsync(entry.Record, TargetFolder, DefaultFileNameTemplate, DefaultAudioBitrateKbps, AppendLine);
            HistoryStatus = $"Agregado a la cola: {job.MediaInfo.Title} ({job.SelectedFormat.DisplayLabel}).";
        }
        catch (Exception ex)
        {
            HistoryStatus = $"No se pudo repetir la descarga: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteHistoryEntryAsync(HistoryEntryViewModel? entry)
    {
        if (entry is null)
        {
            return;
        }

        await _historyService.DeleteAsync(entry.Id);
        HistoryEntries.Remove(entry);
        RebuildLibrary();
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(HistoryEntryViewModel? entry)
    {
        if (entry is null)
        {
            return;
        }

        var newValue = !entry.IsFavorite;

        try
        {
            await _historyService.SetFavoriteAsync(entry.Id, newValue);
            entry.IsFavorite = newValue;
            RebuildLibrary();
        }
        catch (Exception ex)
        {
            HistoryStatus = $"No se pudo actualizar favorito: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SelectLibraryFilter(LibraryFilter filter) => SelectedLibraryFilter = filter;

    partial void OnSelectedLibraryFilterChanged(LibraryFilter value) => RebuildLibrary();

    private void RebuildLibrary()
    {
        var completed = HistoryEntries.Where(e => e.IsCompleted && File.Exists(e.Record.OutputFile));

        IEnumerable<HistoryEntryViewModel> filtered = SelectedLibraryFilter switch
        {
            LibraryFilter.Videos => completed.Where(e => e.Record.Kind == DownloadKind.Video),
            LibraryFilter.Audio => completed.Where(e => e.Record.Kind == DownloadKind.AudioMp3),
            LibraryFilter.Favorites => completed.Where(e => e.IsFavorite),
            _ => completed.Take(20)
        };

        LibraryEntries.Clear();
        foreach (var entry in filtered)
        {
            LibraryEntries.Add(entry);
        }
    }

    [RelayCommand]
    private void OpenHistoryFolder(HistoryEntryViewModel? entry)
    {
        if (entry is null)
        {
            return;
        }

        var path = entry.Record.OutputFile;

        try
        {
            if (File.Exists(path))
            {
                var psi = new ProcessStartInfo("explorer.exe") { UseShellExecute = false };
                psi.ArgumentList.Add($"/select,\"{path}\"");
                Process.Start(psi);
                return;
            }

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
                return;
            }

            HistoryStatus = "El archivo y su carpeta ya no existen.";
        }
        catch (Exception ex)
        {
            HistoryStatus = $"No se pudo abrir la ubicación: {ex.Message}";
        }
    }

    private void AppendLine(string line) => _dispatcher.Invoke(() => LogLines.Add(line));
}
