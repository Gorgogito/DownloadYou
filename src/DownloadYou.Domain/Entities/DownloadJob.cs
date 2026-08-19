using DownloadYou.Domain.Enums;

namespace DownloadYou.Domain.Entities;

public sealed class DownloadJob
{
    public required Guid Id { get; init; }
    public required MediaInfo MediaInfo { get; init; }
    public required FormatOption SelectedFormat { get; init; }

    /// <summary>Stream de audio a combinar cuando <see cref="SelectedFormat"/> es video-only (DASH).</summary>
    public FormatOption? PairedAudioFormat { get; init; }

    public required DownloadKind Kind { get; init; }
    public required string TargetDirectory { get; init; }
    public required string FileNameTemplate { get; init; }
    public required int TargetAudioBitrateKbps { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Qué hacer si el archivo final ya existe; tomado de AppSettings al crear el job.</summary>
    public ExistingFileBehavior ExistingFileBehavior { get; init; } = ExistingFileBehavior.Rename;

    public JobStatus Status { get; set; } = JobStatus.Queued;
    public double ProgressPercent { get; set; }
    public double? SpeedBytesPerSecond { get; set; }
    public long? DownloadedBytes { get; set; }
    public long? TotalBytes { get; set; }
    public TimeSpan? Eta { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OutputFilePath { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Archivo descargado para <see cref="SelectedFormat"/>, antes de conversión/mux (Fase 4).</summary>
    public string? PrimaryFilePath { get; set; }

    /// <summary>Archivo descargado para <see cref="PairedAudioFormat"/>, cuando aplica.</summary>
    public string? PairedAudioFilePath { get; set; }

    public bool RequiresConversion => Kind == DownloadKind.AudioMp3 || PairedAudioFormat is not null;
}
