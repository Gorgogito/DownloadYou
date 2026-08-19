using DownloadYou.Application.Abstractions;
using DownloadYou.Domain.Entities;

namespace DownloadYou.Presentation.Tests.TestDoubles;

public sealed class FakeSettingsStore : ISettingsStore
{
    public AppSettings Stored { get; set; } = new();
    public AppSettings? Saved { get; private set; }

    public AppSettings Load() => Stored;

    public void Save(AppSettings settings)
    {
        Saved = settings;
        Stored = settings;
    }
}
