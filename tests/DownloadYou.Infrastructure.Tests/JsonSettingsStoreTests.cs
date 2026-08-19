using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;
using DownloadYou.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace DownloadYou.Infrastructure.Tests;

public class JsonSettingsStoreTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("dy-settings-").FullName;

    private JsonSettingsStore BuildStore() =>
        new(Options.Create(new SettingsOptions { FilePath = Path.Combine(_dir, "settings.json") }));

    [Fact]
    public void Load_ReturnsDefaults_WithResolvedDownloadFolder_WhenFileDoesNotExist()
    {
        var store = BuildStore();

        var settings = store.Load();

        Assert.Equal(DownloadKind.Video, settings.DefaultKind);
        Assert.Equal(3, settings.MaxConcurrentDownloads);
        Assert.False(string.IsNullOrWhiteSpace(settings.DownloadFolder));
        Assert.True(Path.IsPathRooted(settings.DownloadFolder));
    }

    [Fact]
    public void Save_ThenLoad_RoundTripsAllFields()
    {
        var store = BuildStore();
        var settings = new AppSettings
        {
            DownloadFolder = @"D:\Descargas",
            DefaultKind = DownloadKind.AudioMp3,
            DefaultAudioBitrateKbps = 256,
            MaxConcurrentDownloads = 5,
            ExistingFileBehavior = ExistingFileBehavior.Skip,
            FileNameTemplate = "{title}.{ext}",
            Language = "en",
            ThemePreference = ThemePreference.Dark,
            ShowLegalDisclaimer = false
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(settings.DownloadFolder, loaded.DownloadFolder);
        Assert.Equal(settings.DefaultKind, loaded.DefaultKind);
        Assert.Equal(settings.DefaultAudioBitrateKbps, loaded.DefaultAudioBitrateKbps);
        Assert.Equal(settings.MaxConcurrentDownloads, loaded.MaxConcurrentDownloads);
        Assert.Equal(settings.ExistingFileBehavior, loaded.ExistingFileBehavior);
        Assert.Equal(settings.FileNameTemplate, loaded.FileNameTemplate);
        Assert.Equal(settings.Language, loaded.Language);
        Assert.Equal(settings.ThemePreference, loaded.ThemePreference);
        Assert.Equal(settings.ShowLegalDisclaimer, loaded.ShowLegalDisclaimer);
    }

    [Fact]
    public void Save_WritesHumanReadableEnums_NotNumericValues()
    {
        var store = BuildStore();
        store.Save(new AppSettings { ThemePreference = ThemePreference.Dark });

        var json = File.ReadAllText(Path.Combine(_dir, "settings.json"));

        Assert.Contains("\"Dark\"", json);
    }

    [Fact]
    public void Load_FallsBackToDefaults_WhenFileIsCorruptJson()
    {
        var path = Path.Combine(_dir, "settings.json");
        Directory.CreateDirectory(_dir);
        File.WriteAllText(path, "{ esto no es json valido ");
        var store = BuildStore();

        var settings = store.Load();

        Assert.Equal(DownloadKind.Video, settings.DefaultKind);
        Assert.False(string.IsNullOrWhiteSpace(settings.DownloadFolder));
    }

    [Fact]
    public void Save_CreatesParentDirectory_WhenMissing()
    {
        var nestedPath = Path.Combine(_dir, "nested", "sub", "settings.json");
        var store = new JsonSettingsStore(Options.Create(new SettingsOptions { FilePath = nestedPath }));

        store.Save(new AppSettings());

        Assert.True(File.Exists(nestedPath));
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);
}
