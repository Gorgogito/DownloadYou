using System.Text.Json;
using System.Text.Json.Serialization;
using DownloadYou.Application.Abstractions;
using DownloadYou.Domain.Entities;
using Microsoft.Extensions.Options;

namespace DownloadYou.Infrastructure.Configuration;

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;

    public JsonSettingsStore(IOptions<SettingsOptions> options)
    {
        _filePath = Environment.ExpandEnvironmentVariables(options.Value.FilePath);
    }

    public AppSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            return WithDefaults(new AppSettings());
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return WithDefaults(settings ?? new AppSettings());
        }
        catch (JsonException)
        {
            // Un archivo de configuración corrupto o de un formato viejo no debe impedir
            // arrancar la app — se vuelve a valores por defecto en vez de fallar.
            return WithDefaults(new AppSettings());
        }
    }

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    // AppSettings (Domain) no puede depender de rutas del sistema operativo, así que la
    // carpeta de descargas por defecto ("") se resuelve recién acá, en Infrastructure.
    private static AppSettings WithDefaults(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.DownloadFolder))
        {
            settings.DownloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        }

        return settings;
    }
}
