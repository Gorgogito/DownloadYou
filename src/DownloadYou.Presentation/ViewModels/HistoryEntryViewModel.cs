using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Presentation.ViewModels;

/// <summary>Envoltorio de solo lectura de un HistoryRecord para una fila de la lista de historial.</summary>
public sealed class HistoryEntryViewModel(HistoryRecord record)
{
    public HistoryRecord Record { get; } = record;

    public Guid Id => Record.Id;
    public string Title => Record.Title;
    public string DateLabel => Record.Date.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    public string KindLabel => Record.Kind == DownloadKind.AudioMp3 ? "Audio MP3" : "Video";
    public string QualityLabel => Record.QualityLabel;
    public string StatusLabel => Record.Status switch
    {
        JobStatus.Completed => "Completado",
        JobStatus.Failed => "Con error",
        JobStatus.Canceled => "Cancelado",
        _ => Record.Status.ToString()
    };
}
