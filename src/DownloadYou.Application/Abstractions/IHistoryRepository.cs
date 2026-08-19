using DownloadYou.Domain.Entities;

namespace DownloadYou.Application.Abstractions;

public interface IHistoryRepository
{
    Task AddAsync(HistoryRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HistoryRecord>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Coincidencia por subcadena (sin distinguir mayúsculas) sobre título y URL.</summary>
    Task<IReadOnlyList<HistoryRecord>> SearchAsync(string query, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task SetFavoriteAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default);
}
