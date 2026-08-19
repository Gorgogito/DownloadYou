using DownloadYou.Domain.Entities;

namespace DownloadYou.Application.Abstractions;

/// <summary>
/// Lectura/escritura sincrónica a propósito: la configuración es un archivo local
/// pequeño de lectura/escritura infrecuente (§9 del documento de arquitectura), y
/// SettingsService la carga en su constructor — usar async ahí arriesgaría un
/// deadlock clásico de WPF (sync-over-async sobre el SynchronizationContext del hilo
/// de UI) sin ganar nada a cambio.
/// </summary>
public interface ISettingsStore
{
    AppSettings Load();

    void Save(AppSettings settings);
}
