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
    private readonly EngineDiagnosticsService _diagnosticsService;
    private readonly AnalyzeUrlService _analyzeUrlService;
    private readonly DownloadQueue _downloadQueue;
    private readonly HistoryService _historyService;
    private readonly SettingsService _settingsService;
    private readonly Dispatcher _dispatcher;

    public MainViewModel(
        EngineDiagnosticsService diagnosticsService,
        AnalyzeUrlService analyzeUrlService,
        DownloadQueue downloadQueue,
        HistoryService historyService,
        SettingsService settingsService,
        SettingsViewModel settingsViewModel,
        Dispatcher dispatcher)
    {
        _diagnosticsService = diagnosticsService;
        _analyzeUrlService = analyzeUrlService;
        _downloadQueue = downloadQueue;
        _historyService = historyService;
        _settingsService = settingsService;
        Settings = settingsViewModel;
        _dispatcher = dispatcher;

        _downloadQueue.JobEnqueued += OnJobEnqueued;
        _downloadQueue.JobUpdated += OnJobUpdated;
        _historyService.RecordAdded += OnHistoryRecordAdded;
        _settingsService.SettingsChanged += OnSettingsChanged;

        var current = _settingsService.Current;
        _targetFolder = current.DownloadFolder;
        _isVideoKind = current.DefaultKind == DownloadKind.Video;
        _isAudioMp3Kind = current.DefaultKind == DownloadKind.AudioMp3;
        _showLegalDisclaimerBanner = current.ShowLegalDisclaimer;

        _ = LoadHistoryAsync();
    }

    public SettingsViewModel Settings { get; }

    public ObservableCollection<string> LogLines { get; } = [];
    public ObservableCollection<FormatOption> AvailableFormats { get; } = [];
    public ObservableCollection<AudioLanguageOption> AvailableAudioLanguages { get; } = [];
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
    private AudioLanguageOption? _selectedAudioLanguage;

    [ObservableProperty]
    private bool _hasMultipleAudioLanguages;

    [ObservableProperty]
    private bool _isVideoKind;

    [ObservableProperty]
    private bool _isAudioMp3Kind;

    [ObservableProperty]
    private string _targetFolder = string.Empty;

    [ObservableProperty]
    private string _enqueueStatus = string.Empty;

    [ObservableProperty]
    private bool _showLegalDisclaimerBanner;

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
        AvailableAudioLanguages.Clear();
        SelectedAudioLanguage = null;
        HasMultipleAudioLanguages = false;
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

            PopulateAudioLanguages(info);

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

    /// <summary>
    /// Algunos videos ofrecen la misma pista de audio en varios idiomas (doblajes) — yt-dlp
    /// expone un FormatOption.Language por cada una. Solo tiene sentido mostrar el selector
    /// cuando hay más de un idioma real disponible; si todo el audio es del mismo idioma
    /// (el caso normal), no se agrega nada y la app se comporta como siempre.
    /// </summary>
    private void PopulateAudioLanguages(MediaInfo info)
    {
        var languages = info.AvailableFormats
            .Where(f => f.Kind == StreamKind.AudioOnly && !string.IsNullOrWhiteSpace(f.Language))
            .GroupBy(f => f.Language!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new AudioLanguageOption(g.Key, LanguageNames.Resolve(g.Key), g.Max(f => f.LanguagePreference) > 0))
            .OrderByDescending(l => l.IsOriginal)
            .ThenBy(l => l.DisplayName)
            .ToList();

        HasMultipleAudioLanguages = languages.Count > 1;

        if (!HasMultipleAudioLanguages)
        {
            return;
        }

        foreach (var language in languages)
        {
            AvailableAudioLanguages.Add(language);
        }

        SelectedAudioLanguage = languages.FirstOrDefault(l => l.IsOriginal) ?? languages[0];
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
            var settings = _settingsService.Current;
            var kind = IsAudioMp3Kind ? DownloadKind.AudioMp3 : DownloadKind.Video;
            var job = DownloadJobFactory.Create(
                MediaInfo, SelectedFormat, kind, TargetFolder, settings.FileNameTemplate,
                settings.DefaultAudioBitrateKbps, settings.ExistingFileBehavior, SelectedAudioLanguage?.Code);

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
        _dispatcher.Invoke(() =>
        {
            QueueItems.Insert(0, new DownloadJobViewModel(job, _downloadQueue));
            TrimQueueItems(QueueItems, MaxQueueItemsRetained);
        });

    private void OnJobUpdated(DownloadJob job) =>
        _dispatcher.Invoke(() =>
        {
            QueueItems.FirstOrDefault(q => q.Id == job.Id)?.Refresh();
            TrimQueueItems(QueueItems, MaxQueueItemsRetained);
        });

    /// <summary>Límite de tarjetas retenidas en la cola visible; ver <see cref="TrimQueueItems"/>.</summary>
    public const int MaxQueueItemsRetained = 50;

    /// <summary>
    /// En una sesión larga con muchas descargas, <paramref name="items"/> (y el árbol visual
    /// de cada tarjeta) crecería sin límite si nunca se sacara nada — cada job terminado se
    /// queda ahí para siempre. Recorta solo terminados (Completed/Failed/Canceled), de más
    /// viejo a más nuevo (van al final de la lista, ya que los nuevos se insertan al
    /// principio); un job activo o pausado nunca se saca de la vista aunque se supere el
    /// límite. El historial completo sigue disponible en la sección Historial.
    /// </summary>
    public static void TrimQueueItems(ObservableCollection<DownloadJobViewModel> items, int maxRetained)
    {
        for (var i = items.Count - 1; i >= 0 && items.Count > maxRetained; i--)
        {
            if (items[i].Status is JobStatus.Completed or JobStatus.Failed or JobStatus.Canceled)
            {
                items.RemoveAt(i);
            }
        }
    }

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
            var settings = _settingsService.Current;
            var job = await _historyService.RepeatAsync(
                entry.Record, TargetFolder, settings.FileNameTemplate, settings.DefaultAudioBitrateKbps,
                settings.ExistingFileBehavior, AppendLine);
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

    private void OnSettingsChanged(AppSettings settings) =>
        _dispatcher.Invoke(() => ShowLegalDisclaimerBanner = settings.ShowLegalDisclaimer);
}
