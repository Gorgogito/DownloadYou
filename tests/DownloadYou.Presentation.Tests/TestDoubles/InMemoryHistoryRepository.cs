using DownloadYou.Application.Abstractions;
using DownloadYou.Domain.Entities;

namespace DownloadYou.Presentation.Tests.TestDoubles;

public sealed class InMemoryHistoryRepository : IHistoryRepository
{
    private readonly List<HistoryRecord> _records = [];

    public Task AddAsync(HistoryRecord record, CancellationToken cancellationToken = default)
    {
        _records.Add(record);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<HistoryRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<HistoryRecord>>(_records.ToList());

    public Task<IReadOnlyList<HistoryRecord>> SearchAsync(string query, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<HistoryRecord>>(_records
            .Where(r => r.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || r.Url.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList());

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _records.RemoveAll(r => r.Id == id);
        return Task.CompletedTask;
    }

    public Task SetFavoriteAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default)
    {
        var index = _records.FindIndex(r => r.Id == id);
        if (index >= 0)
        {
            _records[index] = _records[index] with { IsFavorite = isFavorite };
        }
        return Task.CompletedTask;
    }
}
