using System.Globalization;
using DownloadYou.Application.Abstractions;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;
using DownloadYou.Infrastructure.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace DownloadYou.Infrastructure.History;

public sealed class SqliteHistoryRepository : IHistoryRepository
{
    private readonly string _connectionString;

    public SqliteHistoryRepository(IOptions<HistoryOptions> options)
    {
        var dbPath = Environment.ExpandEnvironmentVariables(options.Value.DatabasePath);
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        EnsureSchema();
    }

    public async Task AddAsync(HistoryRecord record, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO History (Id, Url, Title, Date, Kind, FormatId, QualityLabel, OutputFile, Status, ProcessDurationSeconds, IsFavorite)
            VALUES ($id, $url, $title, $date, $kind, $formatId, $quality, $output, $status, $duration, $favorite);
            """;
        command.Parameters.AddWithValue("$id", record.Id.ToString());
        command.Parameters.AddWithValue("$url", record.Url);
        command.Parameters.AddWithValue("$title", record.Title);
        command.Parameters.AddWithValue("$date", record.Date.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$kind", record.Kind.ToString());
        command.Parameters.AddWithValue("$formatId", record.FormatId);
        command.Parameters.AddWithValue("$quality", record.QualityLabel);
        command.Parameters.AddWithValue("$output", record.OutputFile);
        command.Parameters.AddWithValue("$status", record.Status.ToString());
        command.Parameters.AddWithValue("$duration", record.ProcessDuration.TotalSeconds);
        command.Parameters.AddWithValue("$favorite", record.IsFavorite ? 1 : 0);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HistoryRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT Id, Url, Title, Date, Kind, FormatId, QualityLabel, OutputFile, Status, ProcessDurationSeconds, IsFavorite " +
            "FROM History ORDER BY Date DESC;";

        return await ReadAllAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<HistoryRecord>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Url, Title, Date, Kind, FormatId, QualityLabel, OutputFile, Status, ProcessDurationSeconds, IsFavorite
            FROM History
            WHERE Title LIKE $pattern ESCAPE '\' OR Url LIKE $pattern ESCAPE '\'
            ORDER BY Date DESC;
            """;
        command.Parameters.AddWithValue("$pattern", $"%{EscapeLike(query)}%");

        return await ReadAllAsync(command, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM History WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetFavoriteAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE History SET IsFavorite = $favorite WHERE Id = $id;";
        command.Parameters.AddWithValue("$favorite", isFavorite ? 1 : 0);
        command.Parameters.AddWithValue("$id", id.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private void EnsureSchema()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using (var create = connection.CreateCommand())
        {
            create.CommandText = """
                CREATE TABLE IF NOT EXISTS History (
                    Id TEXT PRIMARY KEY,
                    Url TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    Kind TEXT NOT NULL,
                    FormatId TEXT NOT NULL,
                    QualityLabel TEXT NOT NULL,
                    OutputFile TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    ProcessDurationSeconds REAL NOT NULL,
                    IsFavorite INTEGER NOT NULL DEFAULT 0
                );
                """;
            create.ExecuteNonQuery();
        }

        // Migración liviana: una base creada por una versión anterior (Fase 7) ya tiene
        // la tabla sin esta columna, así que el CREATE TABLE IF NOT EXISTS de arriba no
        // la agrega — hay que revisarlo aparte y sumarla con ALTER TABLE si falta.
        if (!ColumnExists(connection, "IsFavorite"))
        {
            using var alter = connection.CreateCommand();
            alter.CommandText = "ALTER TABLE History ADD COLUMN IsFavorite INTEGER NOT NULL DEFAULT 0;";
            alter.ExecuteNonQuery();
        }
    }

    private static bool ColumnExists(SqliteConnection connection, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(History);";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            // En PRAGMA table_info, la columna "name" de cada fila es el índice 1.
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<IReadOnlyList<HistoryRecord>> ReadAllAsync(SqliteCommand command, CancellationToken cancellationToken)
    {
        var results = new List<HistoryRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new HistoryRecord(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
                Enum.Parse<DownloadKind>(reader.GetString(4)),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                Enum.Parse<JobStatus>(reader.GetString(8)),
                TimeSpan.FromSeconds(reader.GetDouble(9)),
                reader.GetInt64(10) != 0));
        }

        return results;
    }

    // Escapa los comodines de LIKE en el texto buscado por el usuario, para que un
    // título que contenga literalmente "%" o "_" no altere el patrón de búsqueda.
    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
