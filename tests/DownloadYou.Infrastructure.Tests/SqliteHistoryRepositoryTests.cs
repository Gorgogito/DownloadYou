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

    [Fact]
    public async Task AddAsync_DefaultsIsFavorite_ToFalse()
    {
        var repo = BuildRepository();
        await repo.AddAsync(BuildRecord());

        var stored = Assert.Single(await repo.GetAllAsync());

        Assert.False(stored.IsFavorite);
    }

    [Fact]
    public async Task SetFavoriteAsync_TogglesFlag_AndOnlyForTheGivenRecord()
    {
        var repo = BuildRepository();
        var favorite = BuildRecord("Favorito");
        var other = BuildRecord("Otro");
        await repo.AddAsync(favorite);
        await repo.AddAsync(other);

        await repo.SetFavoriteAsync(favorite.Id, true);
        var all = await repo.GetAllAsync();

        Assert.True(all.Single(r => r.Id == favorite.Id).IsFavorite);
        Assert.False(all.Single(r => r.Id == other.Id).IsFavorite);
    }

    [Fact]
    public async Task SetFavoriteAsync_CanUnsetFavorite()
    {
        var repo = BuildRepository();
        var record = BuildRecord();
        await repo.AddAsync(record);
        await repo.SetFavoriteAsync(record.Id, true);

        await repo.SetFavoriteAsync(record.Id, false);
        var stored = Assert.Single(await repo.GetAllAsync());

        Assert.False(stored.IsFavorite);
    }

    [Fact]
    public async Task Constructor_MigratesDatabase_CreatedByPreFavoritesVersion()
    {
        var dbPath = Path.Combine(_dbDir, "legacy.db");
        var existingId = Guid.NewGuid();
        CreatePreFavoritesSchemaWithOneRow(dbPath, existingId);

        // Construir el repositorio sobre una base que ya existe sin la columna IsFavorite
        // no debe lanzar, y debe poder seguir operando sobre las filas ya guardadas.
        var repo = new SqliteHistoryRepository(Options.Create(new HistoryOptions { DatabasePath = dbPath }));
        var all = await repo.GetAllAsync();
        var migrated = Assert.Single(all);

        Assert.Equal(existingId, migrated.Id);
        Assert.False(migrated.IsFavorite);

        await repo.SetFavoriteAsync(existingId, true);
        var updated = Assert.Single(await repo.GetAllAsync());
        Assert.True(updated.IsFavorite);
    }

    private static void CreatePreFavoritesSchemaWithOneRow(string dbPath, Guid id)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString());
        connection.Open();

        using (var create = connection.CreateCommand())
        {
            create.CommandText = """
                CREATE TABLE History (
                    Id TEXT PRIMARY KEY,
                    Url TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    Kind TEXT NOT NULL,
                    FormatId TEXT NOT NULL,
                    QualityLabel TEXT NOT NULL,
                    OutputFile TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    ProcessDurationSeconds REAL NOT NULL
                );
                """;
            create.ExecuteNonQuery();
        }

        using var insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO History (Id, Url, Title, Date, Kind, FormatId, QualityLabel, OutputFile, Status, ProcessDurationSeconds)
            VALUES ($id, 'https://youtu.be/legacy', 'Video antiguo', $date, 'Video', '18', '360p', 'C:\old.mp4', 'Completed', 10);
            """;
        insert.Parameters.AddWithValue("$id", id.ToString());
        insert.Parameters.AddWithValue("$date", DateTimeOffset.UtcNow.ToString("O"));
        insert.ExecuteNonQuery();
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
