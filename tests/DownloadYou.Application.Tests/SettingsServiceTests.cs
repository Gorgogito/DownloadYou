using DownloadYou.Application.Abstractions;
using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Application.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void Constructor_LoadsCurrentSettings_FromStore()
    {
        var store = new FakeSettingsStore { Stored = new AppSettings { MaxConcurrentDownloads = 7 } };

        var service = new SettingsService(store);

        Assert.Equal(7, service.Current.MaxConcurrentDownloads);
    }

    [Fact]
    public void Save_PersistsToStore_AndUpdatesCurrent()
    {
        var store = new FakeSettingsStore { Stored = new AppSettings() };
        var service = new SettingsService(store);
        var updated = new AppSettings { MaxConcurrentDownloads = 9, ThemePreference = ThemePreference.Dark };

        service.Save(updated);

        Assert.Equal(9, service.Current.MaxConcurrentDownloads);
        Assert.Equal(ThemePreference.Dark, service.Current.ThemePreference);
        Assert.Same(updated, store.Saved);
    }

    [Fact]
    public void Save_RaisesSettingsChanged_WithTheNewSettings()
    {
        var store = new FakeSettingsStore { Stored = new AppSettings() };
        var service = new SettingsService(store);
        AppSettings? raised = null;
        service.SettingsChanged += s => raised = s;
        var updated = new AppSettings { Language = "en" };

        service.Save(updated);

        Assert.Same(updated, raised);
    }

    private sealed class FakeSettingsStore : ISettingsStore
    {
        public AppSettings Stored { get; set; } = new();
        public AppSettings? Saved { get; private set; }

        public AppSettings Load() => Stored;

        public void Save(AppSettings settings) => Saved = settings;
    }
}
