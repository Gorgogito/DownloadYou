using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;
using DownloadYou.Infrastructure.Configuration;
using DownloadYou.Infrastructure.History;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace DownloadYou.Infrastructure.Tests;

public class SqliteHistoryRepositoryTests : IDisposable
{
    private readonly string _dbDir = Directory.CreateTempSubdirectory("dy-history-").FullName;

    private SqliteHistoryRepository BuildRepository() =>
        new(Options.Create(new HistoryOptions { DatabasePath = Path.Combine(_dbDir, "history.db") }));

    private static HistoryRecord BuildRecord(string title = "Mi Video", string url = "https://youtu.be/abc") => new(
        Guid.NewGuid(), url, title, DateTimeOffset.UtcNow, DownloadKind.Video, "18", "360p (combinado)",
        @"C:\Videos\mi-video.mp4", JobStatus.Completed, TimeSpan.FromSeconds(42));

    [Fact]
    public void Constructor_CreatesDatabaseFile()
    {
        BuildRepository();

        Assert.True(File.Exists(Path.Combine(_dbDir, "history.db")));
    }

    [Fact]
    public async Task AddAsync_ThenGetAllAsync_RoundTripsAllFields()
    {
        var repo = BuildRepository();
        var record = BuildRecord();

        await repo.AddAsync(record);
        var all = await repo.GetAllAsync();

        var stored = Assert.Single(all);
        Assert.Equal(record.Id, stored.Id);
        Assert.Equal(record.Url, stored.Url);
        Assert.Equal(record.Title, stored.Title);
        Assert.Equal(record.Kind, stored.Kind);
        Assert.Equal(record.FormatId, stored.FormatId);
        Assert.Equal(record.QualityLabel, stored.QualityLabel);
        Assert.Equal(record.OutputFile, stored.OutputFile);
        Assert.Equal(record.Status, stored.Status);
        Assert.Equal(record.ProcessDuration.TotalSeconds, stored.ProcessDuration.TotalSeconds, precision: 3);
        Assert.Equal(record.Date.ToUnixTimeMilliseconds(), stored.Date.ToUnixTimeMilliseconds());
    }

    [Fact]
    public async Task GetAllAsync_OrdersByDateDescending()
    {
        var repo = BuildRepository();
        var older = BuildRecord("Vieja") with { Date = DateTimeOffset.UtcNow.AddDays(-1) };
        var newer = BuildRecord("Nueva") with { Date = DateTimeOffset.UtcNow };

        await repo.AddAsync(older);
        await repo.AddAsync(newer);
        var all = await repo.GetAllAsync();

        Assert.Equal(["Nueva", "Vieja"], all.Select(r => r.Title));
    }

    [Fact]
    public async Task SearchAsync_MatchesTitleOrUrl_CaseInsensitive()
    {
        var repo = BuildRepository();
        await repo.AddAsync(BuildRecord("Tutorial de C#", "https://youtu.be/x1"));
        await repo.AddAsync(BuildRecord("Receta de cocina", "https://youtu.be/x2"));

        var byTitle = await repo.SearchAsync("tutorial");
        var byUrl = await repo.SearchAsync("x2");

        Assert.Single(byTitle);
        Assert.Equal("Tutorial de C#", byTitle[0].Title);
        Assert.Single(byUrl);
        Assert.Equal("Receta de cocina", byUrl[0].Title);
    }

    [Fact]
    public async Task SearchAsync_EscapesLikeWildcards_InUserQuery()
    {
        var repo = BuildRepository();
        await repo.AddAsync(BuildRecord("100% Real"));
        await repo.AddAsync(BuildRecord("Otro video cualquiera"));

        var results = await repo.SearchAsync("100%");

        Assert.Single(results);
        Assert.Equal("100% Real", results[0].Title);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlyTheGivenRecord()
    {
        var repo = BuildRepository();
        var toDelete = BuildRecord("Borrar");
        var toKeep = BuildRecord("Mantener");
        await repo.AddAsync(toDelete);
        await repo.AddAsync(toKeep);

        await repo.DeleteAsync(toDelete.Id);
        var remaining = await repo.GetAllAsync();

        var kept = Assert.Single(remaining);
        Assert.Equal("Mantener", kept.Title);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNotThrow()
    {
        var repo = BuildRepository();
        await repo.DeleteAsync(Guid.NewGuid());
    }

    public void Dispose()
    {
        // Microsoft.Data.Sqlite agrupa conexiones (pooling) y retiene el handle nativo
        // del archivo incluso después de Dispose() en cada "using" — sin esto, borrar
        // el directorio temporal falla con IOException "en uso por otro proceso".
        SqliteConnection.ClearAllPools();
        Directory.Delete(_dbDir, recursive: true);
    }
}
