using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DownloadYou.Application.Services;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;
using DownloadYou.Presentation.Formatting;
using DownloadYou.Presentation.Models;

namespace DownloadYou.Presentation.ViewModels;

/// <summary>Envoltorio observable de un DownloadJob para una fila de la cola en la UI.</summary>
public sealed partial class DownloadJobViewModel : ObservableObject
{
    private static readonly string[] PipelineStageNames = ["Analizando", "Descargando", "Convirtiendo", "Verificando", "Finalizado"];

    private readonly DownloadJob _job;
    private readonly DownloadQueue _queue;

    public DownloadJobViewModel(DownloadJob job, DownloadQueue queue)
    {
        _job = job;
        _queue = queue;
        Title = job.MediaInfo.Title;
        QualityLabel = job.SelectedFormat.DisplayLabel;
        KindLabel = job.Kind == DownloadKind.AudioMp3 ? "Audio MP3" : "Video";
        Refresh();
    }

    public Guid Id => _job.Id;
    public string Title { get; }
    public string QualityLabel { get; }
    public string KindLabel { get; }

    public ObservableCollection<PipelineStepViewModel> PipelineSteps { get; } = [];

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _speedLabel = string.Empty;

    [ObservableProperty]
    private string _sizeLabel = string.Empty;

    [ObservableProperty]
    private string _etaLabel = string.Empty;

    [ObservableProperty]
    private string _statusLabel = string.Empty;

    [ObservableProperty]
    private JobStatus _status;

    [ObservableProperty]
    private bool _canPause;

    [ObservableProperty]
    private bool _canResume;

    [ObservableProperty]
    private bool _canCancel;

    [RelayCommand]
    private void Pause() => _queue.Pause(_job);

    [RelayCommand]
    private void Resume() => _queue.Resume(_job);

    [RelayCommand]
    private void Cancel() => _queue.Cancel(_job);

    /// <summary>Vuelve a leer el estado del DownloadJob subyacente. Llamar desde el hilo de UI.</summary>
    public void Refresh()
    {
        ProgressPercent = _job.ProgressPercent;
        SpeedLabel = DisplayFormat.Speed(_job.SpeedBytesPerSecond);
        SizeLabel = DisplayFormat.Size(_job.DownloadedBytes, _job.TotalBytes);
        EtaLabel = DisplayFormat.Eta(_job.Eta);
        Status = _job.Status;

        StatusLabel = _job.Status switch
        {
            JobStatus.Completed => $"Completado: {_job.OutputFilePath}",
            JobStatus.Failed => $"Error: {_job.ErrorMessage}",
            JobStatus.Canceled => "Cancelado.",
            JobStatus.Paused => "Pausado — se puede reanudar.",
            JobStatus.Queued => "En cola, esperando turno...",
            JobStatus.Analyzing => "Analizando...",
            JobStatus.Downloading => "Descargando...",
            JobStatus.Converting => "Convirtiendo...",
            JobStatus.Verifying => "Verificando el archivo final...",
            _ => _job.Status.ToString()
        };

        var active = _job.Status is JobStatus.Queued or JobStatus.Downloading or JobStatus.Converting or JobStatus.Verifying;
        CanPause = active;
        CanResume = _job.Status == JobStatus.Paused;
        CanCancel = active || _job.Status == JobStatus.Paused;

        RebuildPipelineSteps(_job.Status);
    }

    private void RebuildPipelineSteps(JobStatus status)
    {
        PipelineSteps.Clear();

        if (status is JobStatus.Failed or JobStatus.Canceled or JobStatus.Paused)
        {
            // DownloadService/ConversionService colapsan ambos a "Canceled" al cortar
            // (la cola recién decide después si era Pause o Cancel), así que ya no sabemos
            // en qué etapa exacta se detuvo por el status solo — se infiere de qué archivos
            // existen: si el/los stream(s) de origen ya están, se detuvo convirtiendo.
            var stoppedIndex = InferStoppedIndex();
            var stoppedState = status == JobStatus.Failed ? PipelineStepState.Error : PipelineStepState.Pending;
            for (var i = 0; i < PipelineStageNames.Length; i++)
            {
                var state = i < stoppedIndex ? PipelineStepState.Done : i == stoppedIndex ? stoppedState : PipelineStepState.Pending;
                PipelineSteps.Add(new PipelineStepViewModel(PipelineStageNames[i], state));
            }
            return;
        }

        if (status == JobStatus.Completed)
        {
            foreach (var name in PipelineStageNames)
            {
                PipelineSteps.Add(new PipelineStepViewModel(name, PipelineStepState.Done));
            }
            return;
        }

        var currentIndex = CurrentPipelineIndex(status);
        for (var i = 0; i < PipelineStageNames.Length; i++)
        {
            var state = i < currentIndex ? PipelineStepState.Done : i == currentIndex ? PipelineStepState.Current : PipelineStepState.Pending;
            PipelineSteps.Add(new PipelineStepViewModel(PipelineStageNames[i], state));
        }
    }

    private static int CurrentPipelineIndex(JobStatus status) => status switch
    {
        JobStatus.Queued or JobStatus.Analyzing => 0,
        JobStatus.Downloading => 1,
        JobStatus.Converting => 2,
        JobStatus.Verifying => 3,
        _ => 0
    };

    private int InferStoppedIndex()
    {
        var downloadComplete = _job.PrimaryFilePath is not null && (_job.PairedAudioFormat is null || _job.PairedAudioFilePath is not null);
        return downloadComplete ? 2 : 1;
    }
}
