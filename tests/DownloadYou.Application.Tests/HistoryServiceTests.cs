using DownloadYou.Application.Abstractions;
using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Application.Tests;

public class HistoryServiceTests : IDisposable
{
    private readonly string _targetDir = Directory.CreateTempSubdirectory("dy-history-svc-target-").FullName;

    private static readonly FormatOption Muxed360p = new("18", StreamKind.Muxed, "mp4", "avc1", "mp4a", 360, 30, 500, 96, null);
    private static readonly FormatOption VideoOnly1080p = new("299", StreamKind.VideoOnly, "mp4", "avc1", null, 1080, 60, 4500, null, null);

    private DownloadJob BuildJob(FormatOption format, string title = "Video de prueba") => new()
    {
        Id = Guid.NewGuid(),
        MediaInfo = new MediaInfo("https://youtu.be/x", "x", title, "Autor", TimeSpan.FromMinutes(1), null, [format]),
        SelectedFormat = format,
        Kind = DownloadKind.Video,
        TargetDirectory = _targetDir,
        FileNameTemplate = "{title}.{ext}",
        TargetAudioBitrateKbps = 192,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!predicate())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("La condición esperada no se cumplió a tiempo.");
            }
            await Task.Delay(15);
        }
    }

    [Fact]
    public async Task CompletedJob_IsRecordedInHistory_Once()
    {
        var repo = new FakeHistoryRepository();
        var queue = new DownloadQueue(new DownloadService(new FakeVideoSource()), new ConversionService(new FakeMediaProcessor()), maxConcurrency: 1);
        _ = new HistoryService(repo, queue, new AnalyzeUrlService(new FakeVideoSource()));

        var job = BuildJob(Muxed360p);
        queue.Enqueue(job);

        await WaitUntilAsync(() => repo.Records.Count > 0, TimeSpan.FromSeconds(5));
        await Task.Delay(50); // ventana corta para detectar un segundo registro indebido

        var record = Assert.Single(repo.Records);
        Assert.Equal(job.MediaInfo.Url, record.Url);
        Assert.Equal(job.MediaInfo.Title, record.Title);
        Assert.Equal(JobStatus.Completed, record.Status);
        Assert.Equal("18", record.FormatId);
        Assert.True(File.Exists(record.OutputFile));
    }

    [Fact]
    public async Task CompletedJob_RaisesRecordAdded_AfterPersisting()
    {
        var repo = new FakeHistoryRepository();
        var queue = new DownloadQueue(new DownloadService(new FakeVideoSource()), new ConversionService(new FakeMediaProcessor()), maxConcurrency: 1);
        var service = new HistoryService(repo, queue, new AnalyzeUrlService(new FakeVideoSource()));
        HistoryRecord? raised = null;
        service.RecordAdded += r => raised = r;

        var job = BuildJob(Muxed360p);
        queue.Enqueue(job);

        await WaitUntilAsync(() => raised is not null, TimeSpan.FromSeconds(5));

        Assert.Single(repo.Records); // ya persistido cuando se dispara el evento
        Assert.Equal(job.MediaInfo.Title, raised!.Title);
    }

    [Fact]
    public async Task FailedJob_IsAlsoRecorded()
    {
        var repo = new FakeHistoryRepository();
        var fake = new FakeVideoSource { ThrowOnDownload = new InvalidOperationException("boom") };
        var queue = new DownloadQueue(new DownloadService(fake), new ConversionService(new FakeMediaProcessor()), maxConcurrency: 1);
        _ = new HistoryService(repo, queue, new AnalyzeUrlService(fake));

        queue.Enqueue(BuildJob(Muxed360p));

        await WaitUntilAsync(() => repo.Records.Count > 0, TimeSpan.FromSeconds(5));

        Assert.Equal(JobStatus.Failed, repo.Records[0].Status);
    }

    [Fact]
    public async Task RepeatAsync_EnqueuesNewJob_WhenFormatStillAvailable()
    {
        var repo = new FakeHistoryRepository();
        var fake = new FakeVideoSource { AnalyzeResult = BuildMediaInfoFor(Muxed360p) };
        var queue = new DownloadQueue(new DownloadService(fake), new ConversionService(new FakeMediaProcessor()), maxConcurrency: 1);
        var service = new HistoryService(repo, queue, new AnalyzeUrlService(fake));
        var record = BuildHistoryRecord(Muxed360p.FormatId);

        var job = await service.RepeatAsync(record, _targetDir, "{title}.{ext}", 192);

        await WaitUntilAsync(() => job.Status is JobStatus.Completed or JobStatus.Failed, TimeSpan.FromSeconds(5));
        Assert.Equal(JobStatus.Completed, job.Status);
    }

    [Fact]
    public async Task RepeatAsync_Throws_WhenFormatNoLongerAvailable()
    {
        var repo = new FakeHistoryRepository();
        var fake = new FakeVideoSource { AnalyzeResult = BuildMediaInfoFor(VideoOnly1080p) }; // ya no ofrece "18"
        var queue = new DownloadQueue(new DownloadService(fake), new ConversionService(new FakeMediaProcessor()), maxConcurrency: 1);
        var service = new HistoryService(repo, queue, new AnalyzeUrlService(fake));
        var record = BuildHistoryRecord("18");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RepeatAsync(record, _targetDir, "{title}.{ext}", 192));
    }

    [Fact]
    public async Task SearchAsync_WithBlankQuery_DelegatesToGetAll()
    {
        var repo = new FakeHistoryRepository();
        var queue = new DownloadQueue(new DownloadService(new FakeVideoSource()), new ConversionService(new FakeMediaProcessor()), maxConcurrency: 1);
        var service = new HistoryService(repo, queue, new AnalyzeUrlService(new FakeVideoSource()));

        await service.SearchAsync("   ");

        Assert.True(repo.GetAllCalled);
        Assert.False(repo.SearchCalled);
    }

    [Fact]
    public async Task SearchAsync_WithQuery_DelegatesToRepositorySearch()
    {
        var repo = new FakeHistoryRepository();
        var queue = new DownloadQueue(new DownloadService(new FakeVideoSource()), new ConversionService(new FakeMediaProcessor()), maxConcurrency: 1);
        var service = new HistoryService(repo, queue, new AnalyzeUrlService(new FakeVideoSource()));

        await service.SearchAsync("gatos");

        Assert.True(repo.SearchCalled);
        Assert.Equal("gatos", repo.LastSearchQuery);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToRepository()
    {
        var repo = new FakeHistoryRepository();
        var queue = new DownloadQueue(new DownloadService(new FakeVideoSource()), new ConversionService(new FakeMediaProcessor()), maxConcurrency: 1);
        var service = new HistoryService(repo, queue, new AnalyzeUrlService(new FakeVideoSource()));
        var id = Guid.NewGuid();

        await service.DeleteAsync(id);

        Assert.Contains(id, repo.DeletedIds);
    }

    [Fact]
    public async Task SetFavoriteAsync_DelegatesToRepository()
    {
        var repo = new FakeHistoryRepository();
        var queue = new DownloadQueue(new DownloadService(new FakeVideoSource()), new ConversionService(new FakeMediaProcessor()), maxConcurrency: 1);
        var service = new HistoryService(repo, queue, new AnalyzeUrlService(new FakeVideoSource()));
        var record = BuildHistoryRecord("18");
        await repo.AddAsync(record);

        await service.SetFavoriteAsync(record.Id, true);

        Assert.True(repo.Records.Single(r => r.Id == record.Id).IsFavorite);
    }

    private static MediaInfo BuildMediaInfoFor(FormatOption format) =>
        new("https://youtu.be/x", "x", "Video de prueba", "Autor", TimeSpan.FromMinutes(1), null, [format]);

    private static HistoryRecord BuildHistoryRecord(string formatId) => new(
        Guid.NewGuid(), "https://youtu.be/x", "Video de prueba", DateTimeOffset.UtcNow, DownloadKind.Video,
        formatId, "360p", "C:\\old.mp4", JobStatus.Completed, TimeSpan.FromSeconds(10));

    public void Dispose() => Directory.Delete(_targetDir, recursive: true);

    private sealed class FakeHistoryRepository : IHistoryRepository
    {
        public List<HistoryRecord> Records { get; } = [];
        public List<Guid> DeletedIds { get; } = [];
        public bool GetAllCalled { get; private set; }
        public bool SearchCalled { get; private set; }
        public string? LastSearchQuery { get; private set; }

        public Task AddAsync(HistoryRecord record, CancellationToken cancellationToken = default)
        {
            lock (Records) Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<HistoryRecord>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            GetAllCalled = true;
            return Task.FromResult<IReadOnlyList<HistoryRecord>>(Records.ToList());
        }

        public Task<IReadOnlyList<HistoryRecord>> SearchAsync(string query, CancellationToken cancellationToken = default)
        {
            SearchCalled = true;
            LastSearchQuery = query;
            return Task.FromResult<IReadOnlyList<HistoryRecord>>([]);
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            DeletedIds.Add(id);
            return Task.CompletedTask;
        }

        public Task SetFavoriteAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default)
        {
            var index = Records.FindIndex(r => r.Id == id);
            if (index >= 0)
            {
                Records[index] = Records[index] with { IsFavorite = isFavorite };
            }
            return Task.CompletedTask;
        }
    }

    private sealed class FakeVideoSource : IVideoSource
    {
        public Exception? ThrowOnDownload { get; set; }
        public MediaInfo? AnalyzeResult { get; set; }

        public Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<MediaInfo> AnalyzeAsync(string url, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default) =>
            AnalyzeResult is not null ? Task.FromResult(AnalyzeResult) : throw new NotSupportedException();

        public Task DownloadAsync(
            string url, string formatId, string outputFilePath,
            Action<DownloadProgressUpdate>? onProgress = null, Action<string>? onOutputLine = null,
            CancellationToken cancellationToken = default)
        {
            if (ThrowOnDownload is not null)
            {
                throw ThrowOnDownload;
            }

            File.WriteAllText(outputFilePath, "contenido");
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMediaProcessor : IMediaProcessor
    {
        public Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task MuxAsync(string videoFilePath, string audioFilePath, string outputFilePath, Action<TimeSpan>? onProgress = null, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task ExtractAudioAsync(string sourceFilePath, string outputFilePath, int bitrateKbps, Action<TimeSpan>? onProgress = null, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<MediaVerificationResult> VerifyAsync(string filePath, TimeSpan expectedDuration, bool requireVideoStream, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
