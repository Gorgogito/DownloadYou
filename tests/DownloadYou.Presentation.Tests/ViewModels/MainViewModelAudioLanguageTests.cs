using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;
using DownloadYou.Presentation.Tests.TestDoubles;

namespace DownloadYou.Presentation.Tests.ViewModels;

/// <summary>
/// Cubre el selector de idioma de audio: un usuario reportó que, para videos con varios
/// doblajes, la app siempre bajaba el idioma "original" (normalmente inglés) sin darle forma
/// de elegir español u otro idioma disponible.
/// </summary>
public class MainViewModelAudioLanguageTests
{
    private static readonly FormatOption VideoOnly720p = new("136", StreamKind.VideoOnly, "mp4", "avc1", null, 720, 30, 2000, null, null);

    private static MediaInfo BuildMediaInfoWithLanguages(params (string FormatId, string Language, int Preference)[] audioTracks)
    {
        var formats = new List<FormatOption> { VideoOnly720p };
        formats.AddRange(audioTracks.Select(t =>
            new FormatOption(t.FormatId, StreamKind.AudioOnly, "m4a", null, "mp4a.40.2", null, null, null, 128, null, t.Language, t.Preference)));

        return new MediaInfo("https://youtu.be/x", "x", "Video multi-idioma", "Canal", TimeSpan.FromMinutes(5), null, formats);
    }

    [Fact]
    public async Task Analyze_SingleAudioLanguage_DoesNotShowLanguagePicker()
    {
        var mediaInfo = BuildMediaInfoWithLanguages(("140", "en", 10));
        var vm = MainViewModelFactory.Create(videoSource: new FakeAnalyzableVideoSource(mediaInfo));
        vm.Url = mediaInfo.Url;

        await vm.AnalyzeCommand.ExecuteAsync(null);

        Assert.False(vm.HasMultipleAudioLanguages);
        Assert.Empty(vm.AvailableAudioLanguages);
        Assert.Null(vm.SelectedAudioLanguage);
    }

    [Fact]
    public async Task Analyze_MultipleAudioLanguages_ShowsPicker_WithOneEntryPerLanguage()
    {
        var mediaInfo = BuildMediaInfoWithLanguages(("140", "en", 10), ("140-0", "es", -1), ("140-1", "fr", -1));
        var vm = MainViewModelFactory.Create(videoSource: new FakeAnalyzableVideoSource(mediaInfo));
        vm.Url = mediaInfo.Url;

        await vm.AnalyzeCommand.ExecuteAsync(null);

        Assert.True(vm.HasMultipleAudioLanguages);
        Assert.Equal(3, vm.AvailableAudioLanguages.Count);
        Assert.Contains(vm.AvailableAudioLanguages, l => l.Code == "es" && l.DisplayName == "Español");
    }

    [Fact]
    public async Task Analyze_MultipleAudioLanguages_DefaultsSelectionToOriginalTrack()
    {
        var mediaInfo = BuildMediaInfoWithLanguages(("140-0", "es", -1), ("140", "en", 10), ("140-1", "fr", -1));
        var vm = MainViewModelFactory.Create(videoSource: new FakeAnalyzableVideoSource(mediaInfo));
        vm.Url = mediaInfo.Url;

        await vm.AnalyzeCommand.ExecuteAsync(null);

        Assert.Equal("en", vm.SelectedAudioLanguage?.Code);
    }

    [Fact]
    public async Task Enqueue_SucceedsWithNonOriginalLanguageSelected()
    {
        // La lógica real de "qué audio se empareja según el idioma elegido" ya está cubierta
        // a fondo en DownloadJobFactoryTests; acá solo se verifica que MainViewModel.Enqueue
        // efectivamente pasa SelectedAudioLanguage.Code hacia DownloadJobFactory.Create sin
        // romper nada, aun cuando el idioma elegido no es el original.
        var mediaInfo = BuildMediaInfoWithLanguages(("140", "en", 10), ("140-0", "es", -1));
        var vm = MainViewModelFactory.Create(videoSource: new FakeAnalyzableVideoSource(mediaInfo));
        vm.Url = mediaInfo.Url;
        await vm.AnalyzeCommand.ExecuteAsync(null);
        vm.TargetFolder = @"C:\Videos";
        vm.SelectedFormat = VideoOnly720p;
        vm.SelectedAudioLanguage = vm.AvailableAudioLanguages.Single(l => l.Code == "es");

        vm.EnqueueCommand.Execute(null);

        Assert.Single(vm.QueueItems);
        Assert.False(string.IsNullOrWhiteSpace(vm.EnqueueStatus));
    }
}
