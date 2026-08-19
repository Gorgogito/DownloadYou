# DownloadYou

Aplicación de escritorio (Windows, .NET 10, WPF) para descarga y conversión de contenido de YouTube. Ver la propuesta de arquitectura completa (comparación de lenguajes, stack, pipeline, seguridad, roadmap) en el documento aprobado del proyecto.

## Estado

- ✅ **Fase 1 — Prototipo técnico.** Solución con las cuatro capas (Domain, Application, Infrastructure, Presentation), inyección de dependencias configurada, y una prueba de concepto que invoca `yt-dlp` y `ffmpeg` como procesos externos y transmite su salida estándar en tiempo real a la interfaz.
- ✅ **Fase 2 — Análisis de URL y metadatos.** `AnalyzeUrlService` valida que la URL sea de YouTube (allowlist de hosts, defensa en profundidad) y delega en `YtDlpVideoSource.AnalyzeAsync`, que invoca `yt-dlp --dump-json` y parsea el resultado (`YtDlpMetadataParser`) a `MediaInfo`/`FormatOption`, clasificando cada stream como video-only, audio-only o combinado. La ventana muestra miniatura, título, duración y la lista de calidades realmente disponibles para ese video.

## Estructura

```
src/
  DownloadYou.Domain          Entidades y enums, sin dependencias externas
  DownloadYou.Application     Casos de uso + abstracciones (IVideoSource, IMediaProcessor, IExternalToolLocator)
  DownloadYou.Infrastructure  Adaptadores: yt-dlp (versión + análisis), ffmpeg, orquestación de procesos (CliWrap)
  DownloadYou.Presentation    WPF + MVVM (CommunityToolkit.Mvvm) + Generic Host
tests/
  DownloadYou.Domain.Tests
  DownloadYou.Application.Tests
  DownloadYou.Infrastructure.Tests
tools/                        yt-dlp.exe / ffmpeg.exe / ffprobe.exe (no versionados, ver tools/README.md)
```

## Requisitos

- .NET SDK 10
- `yt-dlp.exe`, `ffmpeg.exe` y `ffprobe.exe` en `tools/` (ver `tools/README.md`) o en el `PATH` del sistema

## Ejecutar

```powershell
dotnet build
dotnet run --project src/DownloadYou.Presentation
```

La ventana tiene dos zonas:

1. "Probar motores (yt-dlp / ffmpeg)" resuelve ambos ejecutables, los invoca con `--version` / `-version` y muestra su salida línea por línea en tiempo real — la prueba de concepto que valida el patrón de invocación de procesos externos antes de construir el resto del pipeline sobre él.
2. Un campo de URL + botón "Analizar" que ejecuta `yt-dlp --dump-json` sobre el video y muestra su miniatura, estado y la lista de formatos disponibles (video-only, audio-only, combinados) con su etiqueta de calidad real.

## Pruebas

```powershell
# Solo unitarias, sin depender de binarios externos (seguro para CI)
dotnet test --filter "Category!=Integration"

# Todo, incluida la prueba de integración contra yt-dlp/ffmpeg reales en tools/
dotnet test
```
