using DownloadYou.Application.Abstractions;
using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Application.Tests;

public class ConversionServiceTests : IDisposable
{
    private readonly string _targetDir = Directory.CreateTempSubdirectory("dy-conv-target-").FullName;
    private readonly string _stagingDir = Directory.CreateTempSubdirectory("dy-conv-staging-").FullName;

    private static readonly FormatOption VideoOnly1080p = new("299", StreamKind.VideoOnly, "mp4", "avc1", null, 1080, 60, 4500, null, null);
    private static readonly FormatOption AudioOnly160 = new("251", StreamKind.AudioOnly, "webm", null, "opus", null, null, null, 160, null);

    private DownloadJob BuildJobReadyForConversion(DownloadKind kind, FormatOption? pairedAudio, int targetAudioBitrateKbps = 192)
    {
        var mediaInfo = new MediaInfo("https://youtu.be/x", "x", "Título", "Autor", TimeSpan.FromSeconds(19), null, [VideoOnly1080p, AudioOnly160]);
        var primaryPath = Path.Combine(_stagingDir, "primary.mp4");
        File.WriteAllText(primaryPath, "video-crudo");

        var job = new DownloadJob
        {
            Id = Guid.NewGuid(),
            MediaInfo = mediaInfo,
            SelectedFormat = kind == DownloadKind.AudioMp3 ? AudioOnly160 : VideoOnly1080p,
            PairedAudioFormat = pairedAudio,
            Kind = kind,
            TargetDirectory = _targetDir,
            FileNameTemplate = "{title}.{ext}",
            TargetAudioBitrateKbps = targetAudioBitrateKbps,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = JobStatus.Converting,
            PrimaryFilePath = primaryPath
        };

        if (pairedAudio is not null)
        {
            var audioPath = Path.Combine(_stagingDir, "audio.webm");
            File.WriteAllText(audioPath, "audio-crudo");
            job.PairedAudioFilePath = audioPath;
        }

        return job;
    }

    [Fact]
    public async Task RunAsync_Throws_WhenJobIsNotInConvertingState()
    {
        var job = BuildJobReadyForConversion(DownloadKind.Video, AudioOnly160);
        job.Status = JobStatus.Downloading;
        var service = new ConversionService(new FakeMediaProcessor());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunAsync(job));
    }

    [Fact]
    public async Task RunAsync_UpdatesProgressPercent_FromRealMuxElapsedTime()
    {
        // Antes, ProgressPercent quedaba congelado en lo que dejó la descarga (típicamente 100)
        // durante toda la conversión: FFmpeg reporta avance real vía -progress pipe:1, pero se
        // descartaba. La duración del job de prueba es 19s (ver BuildJobReadyForConversion).
        var job = BuildJobReadyForConversion(DownloadKind.Video, AudioOnly160);
        job.ProgressPercent = 100; // como lo deja DownloadService al terminar de bajar los streams
        var processor = new FakeMediaProcessor { ProgressElapsedToReport = TimeSpan.FromSeconds(9.5) };
        var service = new ConversionService(processor);

        await service.RunAsync(job);

        Assert.Equal(50, job.ProgressPercent, precision: 3);
    }

    [Fact]
    public async Task RunAsync_ClampsProgressPercent_WhenFfmpegReportsBeyondExpectedDuration()
    {
        var job = BuildJobReadyForConversion(DownloadKind.Video, AudioOnly160);
        var processor = new FakeMediaProcessor { ProgressElapsedToReport = TimeSpan.FromSeconds(50) };
        var service = new ConversionService(processor);

        await service.RunAsync(job);

        Assert.Equal(100, job.ProgressPercent);
    }

    [Fact]
    public async Task RunAsync_MuxesVideoAndAudio_ThenCompletesJob()
    {
        var job = BuildJobReadyForConversion(DownloadKind.Video, AudioOnly160);
        var processor = new FakeMediaProcessor();
        var service = new ConversionService(processor);

        await service.RunAsync(job);

        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.True(processor.MuxCalled);
        Assert.False(processor.ExtractAudioCalled);
        Assert.True(processor.VerifyRequiredVideoStream);
        Assert.True(File.Exists(job.OutputFilePath));
        Assert.EndsWith(".mp4", job.OutputFilePath);
        Assert.False(Directory.Exists(_stagingDir));
    }

    [Fact]
    public async Task RunAsync_ExtractsMp3_ThenCompletesJob()
    {
        var job = BuildJobReadyForConversion(DownloadKind.AudioMp3, pairedAudio: null);
        var processor = new FakeMediaProcessor();
        var service = new ConversionService(processor);

        await service.RunAsync(job);

        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.True(processor.ExtractAudioCalled);
        Assert.False(processor.MuxCalled);
        Assert.False(processor.VerifyRequiredVideoStream);
        Assert.EndsWith(".mp3", job.OutputFilePath);
    }

    [Fact]
    public async Task RunAsync_CapsMp3Bitrate_ToSourceBitrate_WhenTargetIsHigher()
    {
        // la fuente (AudioOnly160) solo tiene 160 kbps reales
        var job = BuildJobReadyForConversion(DownloadKind.AudioMp3, pairedAudio: null, targetAudioBitrateKbps: 320);
        var processor = new FakeMediaProcessor();
        var service = new ConversionService(processor);

        await service.RunAsync(job);

        Assert.Equal(160, processor.RequestedBitrateKbps);
    }

    [Fact]
    public async Task RunAsync_UsesTargetBitrate_WhenLowerThanSource()
    {
        var job = BuildJobReadyForConversion(DownloadKind.AudioMp3, pairedAudio: null, targetAudioBitrateKbps: 128);
        var processor = new FakeMediaProcessor();
        var service = new ConversionService(processor);

        await service.RunAsync(job);

        Assert.Equal(128, processor.RequestedBitrateKbps);
    }

    [Fact]
    public async Task RunAsync_MarksFailed_AndDeletesOutput_WhenVerificationFails()
    {
        var job = BuildJobReadyForConversion(DownloadKind.Video, AudioOnly160);
        var processor = new FakeMediaProcessor { VerificationResult = MediaVerificationResult.Invalid("duración incorrecta") };
        var service = new ConversionService(processor);

        await service.RunAsync(job);

        Assert.Equal(JobStatus.Failed, job.Status);
        Assert.Equal("duración incorrecta", job.ErrorMessage);
        Assert.Null(job.OutputFilePath);
    }

    [Fact]
    public async Task RunAsync_MarksFailed_WhenFfmpegThrows()
    {
        var job = BuildJobReadyForConversion(DownloadKind.Video, AudioOnly160);
        var processor = new FakeMediaProcessor { ThrowOnMux = new InvalidOperationException("ffmpeg explotó") };
        var service = new ConversionService(processor);

        await service.RunAsync(job);

        Assert.Equal(JobStatus.Failed, job.Status);
        Assert.Equal("ffmpeg explotó", job.ErrorMessage);
    }

    [Fact]
    public async Task RunAsync_MarksCanceled_WhenCancellationRequested()
    {
        var job = BuildJobReadyForConversion(DownloadKind.Video, AudioOnly160);
        var processor = new FakeMediaProcessor { ThrowOnMux = new OperationCanceledException() };
        var service = new ConversionService(processor);

        await service.RunAsync(job);

        Assert.Equal(JobStatus.Canceled, job.Status);
    }

    public void Dispose()
    {
        if (Directory.Exists(_targetDir)) Directory.Delete(_targetDir, recursive: true);
        if (Directory.Exists(_stagingDir)) Directory.Delete(_stagingDir, recursive: true);
    }

    private sealed class FakeMediaProcessor : IMediaProcessor
    {
        public bool MuxCalled { get; private set; }
        public bool ExtractAudioCalled { get; private set; }
        public bool VerifyRequiredVideoStream { get; private set; }
        public int RequestedBitrateKbps { get; private set; }
        public Exception? ThrowOnMux { get; set; }
        public MediaVerificationResult VerificationResult { get; set; } = MediaVerificationResult.Valid(TimeSpan.FromSeconds(19));
        public TimeSpan? ProgressElapsedToReport { get; set; }

        public Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task MuxAsync(
            string videoFilePath, string audioFilePath, string outputFilePath,
            Action<TimeSpan>? onProgress = null, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
        {
            if (ThrowOnMux is not null) throw ThrowOnMux;
            MuxCalled = true;
            if (ProgressElapsedToReport is { } elapsed) onProgress?.Invoke(elapsed);
            File.WriteAllText(outputFilePath, "muxed");
            return Task.CompletedTask;
        }

        public Task ExtractAudioAsync(
            string sourceFilePath, string outputFilePath, int bitrateKbps,
            Action<TimeSpan>? onProgress = null, Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
        {
            ExtractAudioCalled = true;
            RequestedBitrateKbps = bitrateKbps;
            if (ProgressElapsedToReport is { } elapsed) onProgress?.Invoke(elapsed);
            File.WriteAllText(outputFilePath, "mp3");
            return Task.CompletedTask;
        }

        public Task<MediaVerificationResult> VerifyAsync(
            string filePath, TimeSpan expectedDuration, bool requireVideoStream,
            Action<string>? onOutputLine = null, CancellationToken cancellationToken = default)
        {
            VerifyRequiredVideoStream = requireVideoStream;
            return Task.FromResult(VerificationResult);
        }
    }
}
