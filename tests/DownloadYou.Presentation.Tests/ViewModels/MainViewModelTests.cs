using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;
using DownloadYou.Presentation.Models;
using DownloadYou.Presentation.Tests.TestDoubles;

namespace DownloadYou.Presentation.Tests.ViewModels;

public class MainViewModelTests
{
    private static readonly FormatOption Muxed = new("18", StreamKind.Muxed, "mp4", "avc1", "aac", 360, 30, 500, 96, null);

    private static MediaInfo BuildMediaInfo() =>
        new("https://youtu.be/x", "x", "Mi Video", "Autor", TimeSpan.FromMinutes(1), null, [Muxed]);

    [Fact]
    public void Constructor_InitializesFromCurrentSettings_NotHardcodedDefaults()
    {
        var store = new FakeSettingsStore
        {
            Stored = new AppSettings
            {
                DownloadFolder = @"D:\Descargas",
                DefaultKind = DownloadKind.AudioMp3,
                ShowLegalDisclaimer = false
            }
        };
        var settingsService = new SettingsService(store);

        var vm = MainViewModelFactory.Create(settingsService);

        Assert.Equal(@"D:\Descargas", vm.TargetFolder);
        Assert.True(vm.IsAudioMp3Kind);
        Assert.False(vm.IsVideoKind);
        Assert.False(vm.ShowLegalDisclaimerBanner);
    }

    [Fact]
    public void SelectVideoKind_And_SelectAudioKind_AreMutuallyExclusive()
    {
        var vm = MainViewModelFactory.Create();

        vm.SelectAudioKindCommand.Execute(null);
        Assert.True(vm.IsAudioMp3Kind);
        Assert.False(vm.IsVideoKind);

        vm.SelectVideoKindCommand.Execute(null);
        Assert.True(vm.IsVideoKind);
        Assert.False(vm.IsAudioMp3Kind);
    }

    [Fact]
    public void EnqueueCommand_CannotExecute_WithoutMediaInfoOrFormatOrFolder()
    {
        var store = new FakeSettingsStore { Stored = new AppSettings { DownloadFolder = string.Empty } };
        var vm = MainViewModelFactory.Create(new SettingsService(store));

        Assert.False(vm.EnqueueCommand.CanExecute(null));
    }

    [Fact]
    public void EnqueueCommand_CanExecute_OnceMediaInfoFormatAndFolderAreSet()
    {
        var vm = MainViewModelFactory.Create();
        vm.TargetFolder = @"C:\Videos";

        vm.MediaInfo = BuildMediaInfo();
        vm.SelectedFormat = Muxed;

        Assert.True(vm.EnqueueCommand.CanExecute(null));
    }

    [Fact]
    public void EnqueueCommand_CannotExecute_WhenTargetFolderIsBlank()
    {
        var vm = MainViewModelFactory.Create();
        vm.TargetFolder = "   ";
        vm.MediaInfo = BuildMediaInfo();
        vm.SelectedFormat = Muxed;

        Assert.False(vm.EnqueueCommand.CanExecute(null));
    }

    [Fact]
    public void Enqueue_AddsAJobToTheQueue()
    {
        var vm = MainViewModelFactory.Create();
        vm.TargetFolder = @"C:\Videos";
        vm.MediaInfo = BuildMediaInfo();
        vm.SelectedFormat = Muxed;

        vm.EnqueueCommand.Execute(null);

        Assert.Single(vm.QueueItems);
        Assert.Equal("Mi Video", vm.QueueItems[0].Title);
        Assert.False(string.IsNullOrWhiteSpace(vm.EnqueueStatus));
    }

    [Fact]
    public void OnSettingsChanged_UpdatesLegalDisclaimerBanner_WhenSettingsAreSavedElsewhere()
    {
        var store = new FakeSettingsStore { Stored = new AppSettings { ShowLegalDisclaimer = true } };
        var settingsService = new SettingsService(store);
        var vm = MainViewModelFactory.Create(settingsService);
        Assert.True(vm.ShowLegalDisclaimerBanner);

        settingsService.Save(new AppSettings { ShowLegalDisclaimer = false });

        Assert.False(vm.ShowLegalDisclaimerBanner);
    }

    [Fact]
    public void SelectLibraryFilter_UpdatesSelectedLibraryFilter()
    {
        var vm = MainViewModelFactory.Create();

        vm.SelectLibraryFilterCommand.Execute(LibraryFilter.Favorites);

        Assert.Equal(LibraryFilter.Favorites, vm.SelectedLibraryFilter);
    }
}
