using DownloadYou.Application.Abstractions;
using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Application.Tests;

public class DownloadServiceTests : IDisposable
{
    private readonly string _targetDir = Directory.CreateTempSubdirectory("dy-target-").FullName;

    private static readonly FormatOption Muxed360p = new("18", StreamKind.Muxed, "mp4", "avc1", "mp4a", 360, 30, 500, 96, null);
    private static readonly FormatOption VideoOnly1080p = new("299", StreamKind.VideoOnly, "mp4", "avc1", null, 1080, 60, 4500, null, null);
    private static readonly FormatOption AudioOnly160 = new("251", StreamKind.AudioOnly, "webm", null, "opus", null, null, null, 160, null);

    private static MediaInfo BuildMediaInfo(params FormatOption[] formats) =>
        new("https://youtu.be/x", "x", "Título de prueba", "Autor", TimeSpan.FromMinutes(1), null, formats);

    private static DownloadJob BuildJob(
        MediaInfo mediaInfo, FormatOption selected, FormatOption? pairedAudio, string targetDir,
        DownloadKind kind = DownloadKind.Video, ExistingFileBehavior existingFileBehavior = ExistingFileBehavior.Rename) => new()
    {
        Id = Guid.NewGuid(),
        MediaInfo = mediaInfo,
        SelectedFormat = selected,
        PairedAudioFormat = pairedAudio,
        Kind = kind,
        TargetDirectory = targetDir,
        FileNameTemplate = "{title}.{ext}",
        TargetAudioBitrateKbps = 192,
        CreatedAt = DateTimeOffset.UtcNow,
        ExistingFileBehavior = existingFileBehavior
    };

    [Fact]
    public async Task RunAsync_MovesFileAndCompletes_WhenFormatIsAlreadyMuxed()
    {
        var mediaInfo = BuildMediaInfo(Muxed360p);
        var job = BuildJob(mediaInfo, Muxed360p, pairedAudio: null, _targetDir);
        var service = new DownloadService(new FakeVideoSource());

        await service.RunAsync(job);

        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.Equal(100, job.ProgressPercent);
        Assert.NotNull(job.OutputFilePath);
        Assert.True(File.Exists(job.OutputFilePath));
        Assert.Equal("Título de prueba.mp4", Path.GetFileName(job.OutputFilePath));
    }

    [Fact]
    public async Task RunAsync_RenamesOnCollision_WhenDestinationAlreadyExists()
    {
        File.WriteAllText(Path.Combine(_targetDir, "Título de prueba.mp4"), "existente");
        var mediaInfo = BuildMediaInfo(Muxed360p);
        var job = BuildJob(mediaInfo, Muxed360p, pairedAudio: null, _targetDir);
        var service = new DownloadService(new FakeVideoSource());

        await service.RunAsync(job);

        Assert.Equal("Título de prueba (2).mp4", Path.GetFileName(job.OutputFilePath));
    }

    [Fact]
    public async Task RunAsync_OverwritesExistingFile_WhenBehaviorIsOverwrite()
    {
        var existingPath = Path.Combine(_targetDir, "Título de prueba.mp4");
        File.WriteAllText(existingPath, "contenido viejo");
        var mediaInfo = BuildMediaInfo(Muxed360p);
        var job = BuildJob(mediaInfo, Muxed360p, pairedAudio: null, _targetDir, existingFileBehavior: ExistingFileBehavior.Overwrite);
        var service = new DownloadService(new FakeVideoSource());

        await service.RunAsync(job);

        Assert.Equal(existingPath, job.OutputFilePath);
        Assert.Equal("contenido-simulado", File.ReadAllText(existingPath));
    }

    [Fact]
    public async Task RunAsync_SkipsWithoutDownloading_WhenBehaviorIsSkipAndFileAlreadyExists()
    {
        var existingPath = Path.Combine(_targetDir, "Título de prueba.mp4");
        File.WriteAllText(existingPath, "ya descargado antes");
        var mediaInfo = BuildMediaInfo(Muxed360p);
        var job = BuildJob(mediaInfo, Muxed360p, pairedAudio: null, _targetDir, existingFileBehavior: ExistingFileBehavior.Skip);
        // Si DownloadAsync llegara a invocarse, esto lanzaría y el test fallaría —
        // es la forma de probar que el chequeo de Skip corta antes de descargar nada.
        var service = new DownloadService(new FakeVideoSource { ThrowOnDownload = new InvalidOperationException("no debería descargar") });

        await service.RunAsync(job);

        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.Equal(existingPath, job.OutputFilePath);
        Assert.Equal(100, job.ProgressPercent);
        Assert.Equal("ya descargado antes", File.ReadAllText(existingPath));
    }

    [Fact]
    public async Task RunAsync_DownloadsNormally_WhenBehaviorIsSkipButFileDoesNotExistYet()
    {
        var mediaInfo = BuildMediaInfo(Muxed360p);
        var job = BuildJob(mediaInfo, Muxed360p, pairedAudio: null, _targetDir, existingFileBehavior: ExistingFileBehavior.Skip);
        var service = new DownloadService(new FakeVideoSource());

        await service.RunAsync(job);

        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.Equal("contenido-simulado", File.ReadAllText(job.OutputFilePath!));
    }

    [Fact]
    public async Task RunAsync_SkipCheck_AccountsForMp3Extension_WhenKindIsAudioMp3()
    {
        var existingPath = Path.Combine(_targetDir, "Título de prueba.mp3");
        File.WriteAllText(existingPath, "mp3 ya existente");
        var audioOnly = new FormatOption("140", StreamKind.AudioOnly, "m4a", null, "aac", null, null, null, 128, null);
        var mediaInfo = BuildMediaInfo(audioOnly);
        var job = BuildJob(mediaInfo, audioOnly, pairedAudio: null, _targetDir, kind: DownloadKind.AudioMp3, existingFileBehavior: ExistingFileBehavior.Skip);
        var service = new DownloadService(new FakeVideoSource { ThrowOnDownload = new InvalidOperationException("no debería descargar") });

        await service.RunAsync(job);

        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.Equal(existingPath, job.OutputFilePath);
    }

    [Fact]
    public async Task RunAsync_LeavesJobInConverting_WhenVideoAndAudioMustBeMerged()
    {
        var mediaInfo = BuildMediaInfo(VideoOnly1080p, AudioOnly160);
        var job = BuildJob(mediaInfo, VideoOnly1080p, AudioOnly160, _targetDir);
        var service = new DownloadService(new FakeVideoSource());

        await service.RunAsync(job);

        Assert.Equal(JobStatus.Converting, job.Status);
        Assert.Null(job.OutputFilePath);
        Assert.True(File.Exists(job.PrimaryFilePath));
        Assert.True(File.Exists(job.PairedAudioFilePath));
    }

    [Fact]
    public async Task RunAsync_WeighsProgressAcrossBothStreams()
    {
        var mediaInfo = BuildMediaInfo(VideoOnly1080p, AudioOnly160);
        var job = BuildJob(mediaInfo, VideoOnly1080p, AudioOnly160, _targetDir);
        var percentSnapshots = new List<double>();
        var fake = new FakeVideoSource { OnProgressReported = (_, _) => percentSnapshots.Add(job.ProgressPercent) };
        var service = new DownloadService(fake);

        await service.RunAsync(job);

        Assert.Contains(percentSnapshots, p => p is >= 0 and < 50);
        Assert.Contains(percentSnapshots, p => p is >= 50 and <= 100);
    }

    [Fact]
    public async Task RunAsync_MarksFailed_WhenDownloadThrows()
    {
        var mediaInfo = BuildMediaInfo(Muxed360p);
        var job = BuildJob(mediaInfo, Muxed360p, pairedAudio: null, _targetDir);
        var service = new DownloadService(new FakeVideoSource { ThrowOnDownload = new InvalidOperationException("red caída") });

        await service.RunAsync(job);

        Assert.Equal(JobStatus.Failed, job.Status);
        Assert.Equal("red caída", job.ErrorMessage);
    }

    [Fact]
    public async Task RunAsync_MarksCanceled_WhenCancellationRequested()
    {
        var mediaInfo = BuildMediaInfo(Muxed360p);
        var job = BuildJob(mediaInfo, Muxed360p, pairedAudio: null, _targetDir);
        var service = new DownloadService(new FakeVideoSource { ThrowOnDownload = new OperationCanceledException() });

        await service.RunAsync(job);

        Assert.Equal(JobStatus.Canceled, job.Status);
    }

    public void Dispose() => Directory.Delete(_targetDir, recursive: true);

    private sealed class FakeVideoSource : IVideoSource
    {
        public Exception? ThrowOnDownload { get; set; }
        public Action<string, string>? OnProgressReported { get; set; }

        public Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<MediaInfo> AnalyzeAsync(string url, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DownloadAsync(
            string url,
            string formatId,
            string outputFilePath,
            Action<DownloadProgressUpdate>? onProgress = null,
            Action<string>? onOutputLine = null,
            CancellationToken cancellationToken = default)
        {
            if (ThrowOnDownload is not null)
            {
                throw ThrowOnDownload;
            }

            onProgress?.Invoke(new DownloadProgressUpdate("downloading", 50, 100, 1_000_000, TimeSpan.FromSeconds(1)));
            OnProgressReported?.Invoke(formatId, outputFilePath);
            File.WriteAllText(outputFilePath, "contenido-simulado");
            onProgress?.Invoke(new DownloadProgressUpdate("finished", 100, 100, null, TimeSpan.Zero));
            OnProgressReported?.Invoke(formatId, outputFilePath);
            return Task.CompletedTask;
        }
    }
}
