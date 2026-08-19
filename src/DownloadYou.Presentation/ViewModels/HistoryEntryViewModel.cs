using CommunityToolkit.Mvvm.ComponentModel;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Presentation.ViewModels;

/// <summary>Envoltorio de un HistoryRecord para una fila de la lista de historial o biblioteca.</summary>
public sealed partial class HistoryEntryViewModel : ObservableObject
{
    public HistoryEntryViewModel(HistoryRecord record)
    {
        Record = record;
        _isFavorite = record.IsFavorite;
    }

    public HistoryRecord Record { get; }

    public Guid Id => Record.Id;
    public string Title => Record.Title;
    public string DateLabel => Record.Date.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    public string KindLabel => Record.Kind == DownloadKind.AudioMp3 ? "Audio MP3" : "Video";
    public string QualityLabel => Record.QualityLabel;
    public bool IsCompleted => Record.Status == JobStatus.Completed;
    public string StatusLabel => Record.Status switch
    {
        JobStatus.Completed => "Completado",
        JobStatus.Failed => "Con error",
        JobStatus.Canceled => "Cancelado",
        _ => Record.Status.ToString()
    };

    [ObservableProperty]
    private bool _isFavorite;
}
