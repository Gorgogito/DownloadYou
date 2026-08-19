using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Presentation.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadFromCurrent();
    }

    [ObservableProperty]
    private string _downloadFolder = string.Empty;

    [ObservableProperty]
    private bool _isDefaultVideo = true;

    [ObservableProperty]
    private bool _isDefaultAudioMp3;

    [ObservableProperty]
    private int _defaultAudioBitrateKbps = 192;

    [ObservableProperty]
    private int _maxConcurrentDownloads = 3;

    [ObservableProperty]
    private ExistingFileBehavior _existingFileBehavior = ExistingFileBehavior.Rename;

    [ObservableProperty]
    private string _fileNameTemplate = "{title} - {author} [{quality}].{ext}";

    [ObservableProperty]
    private ThemePreference _themePreference = ThemePreference.System;

    [ObservableProperty]
    private bool _showLegalDisclaimer = true;

    [ObservableProperty]
    private string _saveStatus = string.Empty;

    public string FileNamePreview => FileNameTemplateEngine.Resolve(
        string.IsNullOrWhiteSpace(FileNameTemplate) ? "{title}.{ext}" : FileNameTemplate,
        "Mi Video de Ejemplo", "Un Canal", "1080p", "mp4");

    partial void OnFileNameTemplateChanged(string value) => OnPropertyChanged(nameof(FileNamePreview));

    partial void OnIsDefaultVideoChanged(bool value)
    {
        if (value)
        {
            IsDefaultAudioMp3 = false;
        }
    }

    partial void OnIsDefaultAudioMp3Changed(bool value)
    {
        if (value)
        {
            IsDefaultVideo = false;
        }
    }

    [RelayCommand]
    private void SelectDefaultVideo() => IsDefaultVideo = true;

    [RelayCommand]
    private void SelectDefaultAudio() => IsDefaultAudioMp3 = true;

    [RelayCommand]
    private void SelectAudioBitrate(string kbps) => DefaultAudioBitrateKbps = int.Parse(kbps);

    [RelayCommand]
    private void SelectExistingFileBehavior(ExistingFileBehavior behavior) => ExistingFileBehavior = behavior;

    [RelayCommand]
    private void SelectTheme(ThemePreference preference)
    {
        ThemePreference = preference;
        ThemeApplier.Apply(preference); // vista previa inmediata; se persiste recién al Guardar
    }

    [RelayCommand]
    private void IncrementMaxConcurrent() => MaxConcurrentDownloads = Math.Min(10, MaxConcurrentDownloads + 1);

    [RelayCommand]
    private void DecrementMaxConcurrent() => MaxConcurrentDownloads = Math.Max(1, MaxConcurrentDownloads - 1);

    [RelayCommand]
    private void Save()
    {
        var updated = new AppSettings
        {
            DownloadFolder = DownloadFolder,
            DefaultKind = IsDefaultAudioMp3 ? DownloadKind.AudioMp3 : DownloadKind.Video,
            DefaultAudioBitrateKbps = DefaultAudioBitrateKbps,
            MaxConcurrentDownloads = MaxConcurrentDownloads,
            ExistingFileBehavior = ExistingFileBehavior,
            FileNameTemplate = string.IsNullOrWhiteSpace(FileNameTemplate) ? "{title}.{ext}" : FileNameTemplate,
            Language = _settingsService.Current.Language,
            ThemePreference = ThemePreference,
            ShowLegalDisclaimer = ShowLegalDisclaimer
        };

        _settingsService.Save(updated);
        SaveStatus = "Configuración guardada. La cantidad de descargas simultáneas se aplica la próxima vez que abras la app.";
    }

    public void SetDownloadFolder(string folder) => DownloadFolder = folder;

    private void LoadFromCurrent()
    {
        var settings = _settingsService.Current;
        DownloadFolder = settings.DownloadFolder;
        IsDefaultVideo = settings.DefaultKind == DownloadKind.Video;
        IsDefaultAudioMp3 = settings.DefaultKind == DownloadKind.AudioMp3;
        DefaultAudioBitrateKbps = settings.DefaultAudioBitrateKbps;
        MaxConcurrentDownloads = settings.MaxConcurrentDownloads;
        ExistingFileBehavior = settings.ExistingFileBehavior;
        FileNameTemplate = settings.FileNameTemplate;
        ThemePreference = settings.ThemePreference;
        ShowLegalDisclaimer = settings.ShowLegalDisclaimer;
    }
}
