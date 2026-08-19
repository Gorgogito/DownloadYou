using DownloadYou.Application.Abstractions;
using DownloadYou.Domain.Entities;

namespace DownloadYou.Application.Services;

/// <summary>
/// Fuente única de la configuración actual en memoria. Se carga una vez al construirse
/// (vía DI, temprano en el arranque) y se actualiza explícitamente con Save.
/// </summary>
public sealed class SettingsService
{
    private readonly ISettingsStore _store;

    public SettingsService(ISettingsStore store)
    {
        _store = store;
        Current = _store.Load();
    }

    public AppSettings Current { get; private set; }

    public event Action<AppSettings>? SettingsChanged;

    public void Save(AppSettings settings)
    {
        _store.Save(settings);
        Current = settings;
        SettingsChanged?.Invoke(settings);
    }
}
