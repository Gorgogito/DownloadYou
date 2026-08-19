using System.Collections.Concurrent;
using DownloadYou.Application.Abstractions;
using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Application.Tests;

public class DownloadQueueTests : IDisposable
{
    private readonly string _targetDir = Directory.CreateTempSubdirectory("dy-queue-target-").FullName;

    private static readonly FormatOption Muxed360p = new("18", StreamKind.Muxed, "mp4", "avc1", "mp4a", 360, 30, 500, 96, null);

    // Cada job usa un título distinto a propósito: si compartieran nombre de archivo final,
    // dos jobs corriendo en paralelo podrían pisarse en DestinationPathResolver (carrera al
    // resolver colisiones) — eso es un problema aparte de lo que esta suite quiere probar.
    private DownloadJob BuildJob() => new()
    {
        Id = Guid.NewGuid(),
        MediaInfo = new MediaInfo("https://youtu.be/x", "x", $"Título {Guid.NewGuid():N}", "Autor", TimeSpan.FromMinutes(1), null, [Muxed360p]),
        SelectedFormat = Muxed360p,
        Kind = DownloadKind.Video,
        TargetDirectory = _targetDir,
        FileNameTemplate = "{title}-{quality}.{ext}",
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
    public async Task Enqueue_RunsJob_ToCompletion()
    {
        var queue = new DownloadQueue(new DownloadService(new FakeVideoSource()), new ConversionService(new FakeMediaProcessor()), maxConcurrency: 1);
        var job = BuildJob();
        var enqueuedFired = false;
        queue.JobEnqueued += _ => enqueuedFired = true;

        queue.Enqueue(job);

        await WaitUntilAsync(() => job.Status is JobStatus.Completed or JobStatus.Failed, TimeSpan.FromSeconds(5));

        Assert.True(enqueuedFired);
        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.True(File.Exists(job.OutputFilePath));
    }

    [Fact]
    public async Task Queue_NeverExceedsMaxConcurrency()
    {
        var fake = new FakeVideoSource { DownloadDelay = TimeSpan.FromMilliseconds(80) };
        var queue = new DownloadQueue(new DownloadService(fake), new ConversionService(new FakeMediaProcessor()), maxConcurrency: 2);
        var jobs = Enumerable.Range(0, 6).Select(_ => BuildJob()).ToList();

        foreach (var job in jobs)
        {
            queue.Enqueue(job);
        }

        await WaitUntilAsync(() => jobs.All(j => j.Status is JobStatus.Completed or JobStatus.Failed), TimeSpan.FromSeconds(10));

        Assert.All(jobs, j => Assert.Equal(JobStatus.Completed, j.Status));
        Assert.True(fake.MaxObservedConcurrent <= 2, $"Concurrencia observada: {fake.MaxObservedConcurrent}");
        Assert.Equal(2, fake.MaxObservedConcurrent);
    }

    [Fact]
    public async Task Cancel_MarksJobCanceled_AndCleansStaging()
    {
        var fake = new FakeVideoSource { BlockOnCallNumber = 1 };
        var queue = new DownloadQueue(new DownloadService(fake), new ConversionService(new FakeMediaProcessor()), maxConcurrency: 1);
        var job = BuildJob();

        queue.Enqueue(job);
        await WaitUntilAsync(() => job.Status == JobStatus.Downloading, TimeSpan.FromSeconds(2));

        queue.Cancel(job);
        await WaitUntilAsync(() => job.Status == JobStatus.Canceled, TimeSpan.FromSeconds(2));

        Assert.False(Directory.Exists(JobStagingPath.For(job.Id)));
    }

    [Fact]
    public async Task Pause_PreservesStaging_AndResume_CompletesJob()
    {
        var fake = new FakeVideoSource { BlockOnCallNumber = 1 };
        var queue = new DownloadQueue(new DownloadService(fake), new ConversionService(new FakeMediaProcessor()), maxConcurrency: 1);
        var job = BuildJob();

        queue.Enqueue(job);
        await WaitUntilAsync(() => job.Status == JobStatus.Downloading, TimeSpan.FromSeconds(2));

        queue.Pause(job);
        await WaitUntilAsync(() => job.Status == JobStatus.Paused, TimeSpan.FromSeconds(2));

        Assert.True(Directory.Exists(JobStagingPath.For(job.Id)));

        queue.Resume(job);
        await WaitUntilAsync(() => job.Status is JobStatus.Completed or JobStatus.Failed, TimeSpan.FromSeconds(5));

        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.Equal(2, fake.CallCount);
    }

    [Fact]
    public async Task Resume_IsNoOp_WhenJobIsNotPaused()
    {
        var queue = new DownloadQueue(new DownloadService(new FakeVideoSource()), new ConversionService(new FakeMediaProcessor()), maxConcurrency: 1);
        var job = BuildJob();
        queue.Enqueue(job);
        await WaitUntilAsync(() => job.Status == JobStatus.Completed, TimeSpan.FromSeconds(5));

        queue.Resume(job); // ya está Completed, no debería re-encolar

        await Task.Delay(100);
        Assert.Equal(JobStatus.Completed, job.Status);
    }

    public void Dispose() => Directory.Delete(_targetDir, recursive: true);

    private sealed class FakeVideoSource : IVideoSource
    {
        private int _callCount;
        public int CallCount => _callCount;
        public int BlockOnCallNumber { get; set; } = -1;
        public TimeSpan DownloadDelay { get; set; } = TimeSpan.Zero;

        private int _concurrentCalls;
        public int MaxObservedConcurrent { get; private set; }
        private readonly object _lock = new();

        public Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<MediaInfo> AnalyzeAsync(string url, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public async Task DownloadAsync(
            string url, string formatId, string outputFilePath,
            Action<DownloadProgressUpdate>? onProgress = null, Action<string>? onOutputLine = null,
            CancellationToken cancellationToken = default)
        {
            var call = Interlocked.Increment(ref _callCount);

            lock (_lock)
            {
                _concurrentCalls++;
                MaxObservedConcurrent = Math.Max(MaxObservedConcurrent, _concurrentCalls);
            }

            try
            {
                if (call == BlockOnCallNumber)
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }

                if (DownloadDelay > TimeSpan.Zero)
                {
                    await Task.Delay(DownloadDelay, cancellationToken);
                }

                File.WriteAllText(outputFilePath, "contenido");
            }
            finally
            {
                lock (_lock)
                {
                    _concurrentCalls--;
                }
            }
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
