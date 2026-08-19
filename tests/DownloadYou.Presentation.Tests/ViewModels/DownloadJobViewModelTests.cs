using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;
using DownloadYou.Presentation.Models;
using DownloadYou.Presentation.Tests.TestDoubles;
using DownloadYou.Presentation.ViewModels;

namespace DownloadYou.Presentation.Tests.ViewModels;

public class DownloadJobViewModelTests
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

    private static DownloadJob BuildJob(JobStatus status, string? primaryFilePath = null, FormatOption? pairedAudio = null, string? pairedAudioFilePath = null) => new()
    {
        Id = Guid.NewGuid(),
        MediaInfo = Info,
        SelectedFormat = Muxed,
        PairedAudioFormat = pairedAudio,
        Kind = DownloadKind.Video,
        TargetDirectory = @"C:\Videos",
        FileNameTemplate = "{title}.{ext}",
        TargetAudioBitrateKbps = 192,
        CreatedAt = DateTimeOffset.UtcNow,
        Status = status,
        PrimaryFilePath = primaryFilePath,
        PairedAudioFilePath = pairedAudioFilePath
    };

    [Fact]
    public void Constructor_CopiesStaticLabelsFromJob()
    {
        var job = BuildJob(JobStatus.Queued);
        var vm = new DownloadJobViewModel(job, BuildQueue());

        Assert.Equal("Mi Video", vm.Title);
        Assert.Equal(Muxed.DisplayLabel, vm.QualityLabel);
        Assert.Equal("Video", vm.KindLabel);
    }

    [Theory]
    [InlineData(JobStatus.Queued, true, false, true)]
    [InlineData(JobStatus.Downloading, true, false, true)]
    [InlineData(JobStatus.Converting, true, false, true)]
    [InlineData(JobStatus.Verifying, true, false, true)]
    [InlineData(JobStatus.Paused, false, true, true)]
    [InlineData(JobStatus.Completed, false, false, false)]
    [InlineData(JobStatus.Failed, false, false, false)]
    [InlineData(JobStatus.Canceled, false, false, false)]
    public void Refresh_SetsCommandAvailability_BasedOnStatus(JobStatus status, bool canPause, bool canResume, bool canCancel)
    {
        var vm = new DownloadJobViewModel(BuildJob(status), BuildQueue());

        Assert.Equal(canPause, vm.CanPause);
        Assert.Equal(canResume, vm.CanResume);
        Assert.Equal(canCancel, vm.CanCancel);
    }

    [Fact]
    public void Refresh_Completed_MarksAllPipelineStepsDone()
    {
        var vm = new DownloadJobViewModel(BuildJob(JobStatus.Completed), BuildQueue());

        Assert.Equal(5, vm.PipelineSteps.Count);
        Assert.All(vm.PipelineSteps, step => Assert.Equal(PipelineStepState.Done, step.State));
    }

    [Theory]
    [InlineData(JobStatus.Queued, 0)]
    [InlineData(JobStatus.Downloading, 1)]
    [InlineData(JobStatus.Converting, 2)]
    [InlineData(JobStatus.Verifying, 3)]
    public void Refresh_InProgress_MarksStepsBeforeCurrentAsDoneAndCurrentAsCurrent(JobStatus status, int expectedCurrentIndex)
    {
        var vm = new DownloadJobViewModel(BuildJob(status), BuildQueue());

        for (var i = 0; i < vm.PipelineSteps.Count; i++)
        {
            var expected = i < expectedCurrentIndex ? PipelineStepState.Done
                : i == expectedCurrentIndex ? PipelineStepState.Current
                : PipelineStepState.Pending;
            Assert.Equal(expected, vm.PipelineSteps[i].State);
        }
    }

    [Fact]
    public void Refresh_Failed_MarksStoppedStepAsError_NotPending()
    {
        // Sin PrimaryFilePath todavia => se infiere que se detuvo descargando (índice 1).
        var vm = new DownloadJobViewModel(BuildJob(JobStatus.Failed), BuildQueue());

        Assert.Equal(PipelineStepState.Done, vm.PipelineSteps[0].State);
        Assert.Equal(PipelineStepState.Error, vm.PipelineSteps[1].State);
        Assert.Equal(PipelineStepState.Pending, vm.PipelineSteps[2].State);
    }

    [Fact]
    public void Refresh_Canceled_WithPrimaryFileAlreadyDownloaded_InfersStoppedWhileConverting()
    {
        var job = BuildJob(JobStatus.Canceled, primaryFilePath: @"C:\Videos\temp.mp4");
        var vm = new DownloadJobViewModel(job, BuildQueue());

        // Descarga (índice 1) ya se dio por Done porque el archivo primario existe; se detuvo en Convirtiendo (índice 2).
        Assert.Equal(PipelineStepState.Done, vm.PipelineSteps[1].State);
        Assert.Equal(PipelineStepState.Pending, vm.PipelineSteps[2].State);
    }

    [Fact]
    public void Refresh_Canceled_WaitingOnPairedAudio_StillCountsAsStoppedWhileDownloading()
    {
        var pairedAudio = new FormatOption("140", StreamKind.AudioOnly, "m4a", null, "aac", null, null, null, 128, null);
        var job = BuildJob(JobStatus.Canceled, primaryFilePath: @"C:\Videos\temp.mp4", pairedAudio: pairedAudio, pairedAudioFilePath: null);
        var vm = new DownloadJobViewModel(job, BuildQueue());

        // El video ya bajó pero falta el audio emparejado => la descarga en sí no terminó (índice 1), no llegó a convertir.
        Assert.Equal(PipelineStepState.Pending, vm.PipelineSteps[1].State);
    }

    [Theory]
    [InlineData(JobStatus.Canceled, "Cancelado.")]
    [InlineData(JobStatus.Paused, "Pausado — se puede reanudar.")]
    [InlineData(JobStatus.Queued, "En cola, esperando turno...")]
    [InlineData(JobStatus.Analyzing, "Analizando...")]
    [InlineData(JobStatus.Downloading, "Descargando...")]
    [InlineData(JobStatus.Converting, "Convirtiendo...")]
    [InlineData(JobStatus.Verifying, "Verificando el archivo final...")]
    public void Refresh_SetsStatusLabel_InSpanish_ForEveryNonTerminalOrPausedState(JobStatus status, string expected)
    {
        var vm = new DownloadJobViewModel(BuildJob(status), BuildQueue());

        Assert.Equal(expected, vm.StatusLabel);
    }

    [Fact]
    public void Refresh_Failed_StatusLabelIncludesErrorMessage()
    {
        var job = BuildJob(JobStatus.Failed);
        job.ErrorMessage = "red caída";
        var vm = new DownloadJobViewModel(job, BuildQueue());

        Assert.Equal("Error: red caída", vm.StatusLabel);
    }

    [Fact]
    public void Refresh_Completed_StatusLabelIncludesOutputPath()
    {
        var job = BuildJob(JobStatus.Completed);
        job.OutputFilePath = @"C:\Videos\Mi Video.mp4";
        var vm = new DownloadJobViewModel(job, BuildQueue());

        Assert.Equal(@"Completado: C:\Videos\Mi Video.mp4", vm.StatusLabel);
    }
}
