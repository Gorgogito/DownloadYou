using DownloadYou.Application.Abstractions;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Application.Services;

/// <summary>
/// Escucha DownloadQueue y registra automáticamente cada job que llega a un estado
/// terminal (Completed/Failed/Canceled). También expone buscar, listar, eliminar y
/// "repetir descarga" a partir de un registro histórico.
/// </summary>
public sealed class HistoryService
{
    private readonly IHistoryRepository _repository;
    private readonly DownloadQueue _queue;
    private readonly AnalyzeUrlService _analyzeUrlService;
    private readonly HashSet<Guid> _recordedJobIds = [];
    private readonly Lock _recordedJobIdsLock = new();

    /// <summary>Se dispara tras persistir un registro nuevo (ya escrito en SQLite).</summary>
    public event Action<HistoryRecord>? RecordAdded;

    public HistoryService(IHistoryRepository repository, DownloadQueue queue, AnalyzeUrlService analyzeUrlService)
    {
        _repository = repository;
        _queue = queue;
        _analyzeUrlService = analyzeUrlService;
        _queue.JobUpdated += OnJobUpdated;
    }

    public Task<IReadOnlyList<HistoryRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<HistoryRecord>> SearchAsync(string query, CancellationToken cancellationToken = default) =>
        string.IsNullOrWhiteSpace(query) ? _repository.GetAllAsync(cancellationToken) : _repository.SearchAsync(query, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        _repository.DeleteAsync(id, cancellationToken);

    public Task SetFavoriteAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default) =>
        _repository.SetFavoriteAsync(id, isFavorite, cancellationToken);

    /// <exception cref="InvalidOperationException">El formato original ya no está disponible para este video.</exception>
    public async Task<DownloadJob> RepeatAsync(
        HistoryRecord record,
        string targetDirectory,
        string fileNameTemplate,
        int targetAudioBitrateKbps,
        Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default)
    {
        var mediaInfo = await _analyzeUrlService.AnalyzeAsync(record.Url, onOutputLine, cancellationToken);

        var format = mediaInfo.AvailableFormats.FirstOrDefault(f => f.FormatId == record.FormatId)
            ?? throw new InvalidOperationException(
                $"El formato '{record.FormatId}' ({record.QualityLabel}) ya no está disponible para este video. Analízalo de nuevo y elige otra calidad.");

        var job = DownloadJobFactory.Create(mediaInfo, format, record.Kind, targetDirectory, fileNameTemplate, targetAudioBitrateKbps);
        _queue.Enqueue(job);
        return job;
    }

    private async void OnJobUpdated(DownloadJob job)
    {
        if (job.Status is not (JobStatus.Completed or JobStatus.Failed or JobStatus.Canceled))
        {
            return;
        }

        lock (_recordedJobIdsLock)
        {
            if (!_recordedJobIds.Add(job.Id))
            {
                return;
            }
        }

        try
        {
            var record = BuildRecord(job);
            await _repository.AddAsync(record);
            RecordAdded?.Invoke(record);
        }
        catch
        {
            // Guardar el historial es best-effort: un fallo de E/S en SQLite no debe
            // afectar la descarga ya terminada ni al resto de la cola.
        }
    }

    private static HistoryRecord BuildRecord(DownloadJob job) => new(
        Guid.NewGuid(),
        job.MediaInfo.Url,
        job.MediaInfo.Title,
        DateTimeOffset.UtcNow,
        job.Kind,
        job.SelectedFormat.FormatId,
        job.SelectedFormat.DisplayLabel,
        job.OutputFilePath ?? string.Empty,
        job.Status,
        (job.CompletedAt ?? DateTimeOffset.UtcNow) - job.CreatedAt);
}
