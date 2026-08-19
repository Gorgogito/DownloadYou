using System.Collections.ObjectModel;
using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;
using DownloadYou.Presentation.Tests.TestDoubles;
using DownloadYou.Presentation.ViewModels;

namespace DownloadYou.Presentation.Tests.ViewModels;

/// <summary>
/// Cubre el recorte de QueueItems (Fase 12 — Optimización: sin esto, una sesión larga con
/// muchas descargas hace crecer la cola visible sin límite, ya que los jobs terminados nunca
/// se sacaban).
/// </summary>
public class MainViewModelQueueTrimmingTests
{
    private static readonly FormatOption Muxed = new("18", StreamKind.Muxed, "mp4", "avc1", "aac", 360, 30, 500, 96, null);

    private static readonly MediaInfo Info =
        new("https://youtu.be/x", "x", "Mi Video", "Autor", TimeSpan.FromMinutes(1), null, [Muxed]);

    private static DownloadQueue BuildQueue()
    {
        var downloadService = new DownloadService(new NoOpVideoSource());
        var conversionService = new ConversionService(new NoOpMediaProcessor());
        return new DownloadQueue(downloadService, conversionService, maxConcurrency: 1);
    }

    private static DownloadJobViewModel BuildViewModel(JobStatus status)
    {
        var job = new DownloadJob
        {
            Id = Guid.NewGuid(),
            MediaInfo = Info,
            SelectedFormat = Muxed,
            Kind = DownloadKind.Video,
            TargetDirectory = @"C:\Videos",
            FileNameTemplate = "{title}.{ext}",
            TargetAudioBitrateKbps = 192,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = status
        };
        return new DownloadJobViewModel(job, BuildQueue());
    }

    [Fact]
    public void TrimQueueItems_DoesNothing_WhileUnderTheLimit()
    {
        var items = new ObservableCollection<DownloadJobViewModel>();
        for (var i = 0; i < 10; i++)
        {
            items.Add(BuildViewModel(JobStatus.Completed));
        }

        MainViewModel.TrimQueueItems(items, maxRetained: 50);

        Assert.Equal(10, items.Count);
    }

    [Fact]
    public void TrimQueueItems_RemovesOldestTerminalItems_UntilBackAtTheLimit()
    {
        // Simula 60 descargas ya terminadas en una sesión larga; el índice 0 es la más
        // nueva (así se insertan en MainViewModel), el último índice la más vieja.
        var items = new ObservableCollection<DownloadJobViewModel>();
        var newest = BuildViewModel(JobStatus.Completed);
        items.Add(newest);
        for (var i = 0; i < 59; i++)
        {
            items.Add(BuildViewModel(JobStatus.Completed));
        }
        var oldest = items[^1];

        MainViewModel.TrimQueueItems(items, maxRetained: 50);

        Assert.Equal(50, items.Count);
        Assert.Contains(newest, items);
        Assert.DoesNotContain(oldest, items);
    }

    [Theory]
    [InlineData(JobStatus.Queued)]
    [InlineData(JobStatus.Downloading)]
    [InlineData(JobStatus.Converting)]
    [InlineData(JobStatus.Verifying)]
    [InlineData(JobStatus.Paused)]
    public void TrimQueueItems_NeverRemovesActiveOrPausedJobs_EvenPastTheLimit(JobStatus activeStatus)
    {
        var items = new ObservableCollection<DownloadJobViewModel>();
        for (var i = 0; i < 60; i++)
        {
            items.Add(BuildViewModel(activeStatus));
        }

        MainViewModel.TrimQueueItems(items, maxRetained: 50);

        Assert.Equal(60, items.Count);
    }

    [Fact]
    public void TrimQueueItems_RemovesOnlyTerminalItems_LeavingActiveOnesInPlace()
    {
        var items = new ObservableCollection<DownloadJobViewModel>();
        var active = BuildViewModel(JobStatus.Downloading);
        items.Add(active);
        for (var i = 0; i < 59; i++)
        {
            items.Add(BuildViewModel(JobStatus.Failed));
        }

        MainViewModel.TrimQueueItems(items, maxRetained: 50);

        Assert.Equal(50, items.Count);
        Assert.Contains(active, items);
        Assert.All(items.Where(i => i != active), i => Assert.Equal(JobStatus.Failed, i.Status));
    }
}
