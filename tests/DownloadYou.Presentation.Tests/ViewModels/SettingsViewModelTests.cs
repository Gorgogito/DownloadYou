using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;
using DownloadYou.Presentation.Tests.TestDoubles;
using DownloadYou.Presentation.ViewModels;

namespace DownloadYou.Presentation.Tests.ViewModels;

public class SettingsViewModelTests
{
    private static (SettingsViewModel vm, SettingsService service, FakeSettingsStore store) Build(AppSettings? initial = null)
    {
        var store = new FakeSettingsStore { Stored = initial ?? new AppSettings() };
        var service = new SettingsService(store);
        return (new SettingsViewModel(service), service, store);
    }

    [Fact]
    public void Constructor_LoadsFieldsFromCurrentSettings()
    {
        var (vm, _, _) = Build(new AppSettings
        {
            DownloadFolder = @"D:\Descargas",
            DefaultKind = DownloadKind.AudioMp3,
            DefaultAudioBitrateKbps = 320,
            MaxConcurrentDownloads = 5,
            ExistingFileBehavior = ExistingFileBehavior.Skip,
            FileNameTemplate = "{title}.{ext}",
            ThemePreference = ThemePreference.Dark,
            ShowLegalDisclaimer = false
        });

        Assert.Equal(@"D:\Descargas", vm.DownloadFolder);
        Assert.False(vm.IsDefaultVideo);
        Assert.True(vm.IsDefaultAudioMp3);
        Assert.Equal(320, vm.DefaultAudioBitrateKbps);
        Assert.Equal(5, vm.MaxConcurrentDownloads);
        Assert.Equal(ExistingFileBehavior.Skip, vm.ExistingFileBehavior);
        Assert.Equal(ThemePreference.Dark, vm.ThemePreference);
        Assert.False(vm.ShowLegalDisclaimer);
    }

    [Fact]
    public void IsDefaultVideo_And_IsDefaultAudioMp3_AreMutuallyExclusive()
    {
        var (vm, _, _) = Build();

        vm.IsDefaultAudioMp3 = true;
        Assert.False(vm.IsDefaultVideo);

        vm.IsDefaultVideo = true;
        Assert.False(vm.IsDefaultAudioMp3);
    }

    [Fact]
    public void SelectDefaultVideoCommand_And_SelectDefaultAudioCommand_ToggleExclusively()
    {
        var (vm, _, _) = Build();

        vm.SelectDefaultAudioCommand.Execute(null);
        Assert.True(vm.IsDefaultAudioMp3);
        Assert.False(vm.IsDefaultVideo);

        vm.SelectDefaultVideoCommand.Execute(null);
        Assert.True(vm.IsDefaultVideo);
        Assert.False(vm.IsDefaultAudioMp3);
    }

    [Fact]
    public void SelectAudioBitrateCommand_ParsesStringParameter()
    {
        var (vm, _, _) = Build();

        vm.SelectAudioBitrateCommand.Execute("256");

        Assert.Equal(256, vm.DefaultAudioBitrateKbps);
    }

    [Fact]
    public void SelectExistingFileBehaviorCommand_UpdatesProperty()
    {
        var (vm, _, _) = Build();

        vm.SelectExistingFileBehaviorCommand.Execute(ExistingFileBehavior.Overwrite);

        Assert.Equal(ExistingFileBehavior.Overwrite, vm.ExistingFileBehavior);
    }

    [Theory]
    [InlineData(9, 10)]
    [InlineData(10, 10)]
    public void IncrementMaxConcurrent_ClampsAtTen(int startAt, int expected)
    {
        var (vm, _, _) = Build(new AppSettings { MaxConcurrentDownloads = startAt });

        vm.IncrementMaxConcurrentCommand.Execute(null);

        Assert.Equal(expected, vm.MaxConcurrentDownloads);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(1, 1)]
    public void DecrementMaxConcurrent_ClampsAtOne(int startAt, int expected)
    {
        var (vm, _, _) = Build(new AppSettings { MaxConcurrentDownloads = startAt });

        vm.DecrementMaxConcurrentCommand.Execute(null);

        Assert.Equal(expected, vm.MaxConcurrentDownloads);
    }

    [Fact]
    public void FileNamePreview_UpdatesLiveAsTemplateChanges()
    {
        var (vm, _, _) = Build();

        vm.FileNameTemplate = "{title} [{quality}].{ext}";

        Assert.Equal("Mi Video de Ejemplo [1080p].mp4", vm.FileNamePreview);
    }

    [Fact]
    public void FileNamePreview_FallsBackToDefaultTemplate_WhenBlank()
    {
        var (vm, _, _) = Build();

        vm.FileNameTemplate = "   ";

        Assert.Equal("Mi Video de Ejemplo.mp4", vm.FileNamePreview);
    }

    [Fact]
    public void SelectTheme_UpdatesThemePreference()
    {
        var (vm, _, _) = Build();

        vm.SelectThemeCommand.Execute(ThemePreference.Light);

        Assert.Equal(ThemePreference.Light, vm.ThemePreference);
    }

    [Fact]
    public void Save_PersistsAllFieldsToSettingsService()
    {
        var (vm, service, store) = Build();
        vm.DownloadFolder = @"E:\Media";
        vm.IsDefaultAudioMp3 = true;
        vm.DefaultAudioBitrateKbps = 320;
        vm.MaxConcurrentDownloads = 7;
        vm.ExistingFileBehavior = ExistingFileBehavior.Overwrite;
        vm.FileNameTemplate = "{title}.{ext}";
        vm.ThemePreference = ThemePreference.Dark;
        vm.ShowLegalDisclaimer = false;

        vm.SaveCommand.Execute(null);

        Assert.NotNull(store.Saved);
        Assert.Equal(@"E:\Media", store.Saved!.DownloadFolder);
        Assert.Equal(DownloadKind.AudioMp3, store.Saved.DefaultKind);
        Assert.Equal(320, store.Saved.DefaultAudioBitrateKbps);
        Assert.Equal(7, store.Saved.MaxConcurrentDownloads);
        Assert.Equal(ExistingFileBehavior.Overwrite, store.Saved.ExistingFileBehavior);
        Assert.Equal(ThemePreference.Dark, store.Saved.ThemePreference);
        Assert.False(store.Saved.ShowLegalDisclaimer);
        Assert.Same(store.Saved, service.Current);
    }

    [Fact]
    public void Save_FallsBackToDefaultTemplate_WhenBlank()
    {
        var (vm, _, store) = Build();
        vm.FileNameTemplate = "   ";

        vm.SaveCommand.Execute(null);

        Assert.Equal("{title}.{ext}", store.Saved!.FileNameTemplate);
    }

    [Fact]
    public void Save_SetsNonEmptyStatusMessage()
    {
        var (vm, _, _) = Build();

        vm.SaveCommand.Execute(null);

        Assert.False(string.IsNullOrWhiteSpace(vm.SaveStatus));
    }
}
