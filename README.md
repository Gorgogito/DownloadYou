# DownloadYou

Aplicación de escritorio (Windows, .NET 10, WPF) para descarga y conversión de contenido de YouTube. Ver la propuesta de arquitectura completa (comparación de lenguajes, stack, pipeline, seguridad, roadmap) en el documento aprobado del proyecto.

## Estado

- ✅ **Fase 1 — Prototipo técnico.** Solución con las cuatro capas (Domain, Application, Infrastructure, Presentation), inyección de dependencias configurada, y una prueba de concepto que invoca `yt-dlp` y `ffmpeg` como procesos externos y transmite su salida estándar en tiempo real a la interfaz.
- ✅ **Fase 2 — Análisis de URL y metadatos.** `AnalyzeUrlService` valida que la URL sea de YouTube (allowlist de hosts, defensa en profundidad) y delega en `YtDlpVideoSource.AnalyzeAsync`, que invoca `yt-dlp --dump-json` y parsea el resultado (`YtDlpMetadataParser`) a `MediaInfo`/`FormatOption`, clasificando cada stream como video-only, audio-only o combinado. La ventana muestra miniatura, título, duración y la lista de calidades realmente disponibles para ese video.
- ✅ **Fase 3 — Descarga.** `DownloadJobFactory` empareja automáticamente un stream de audio cuando la calidad elegida es video-only (DASH). `DownloadService` descarga cada stream con `YtDlpVideoSource.DownloadAsync` (progreso vía `--progress-template` de yt-dlp — campos numéricos crudos, no texto parseado — mucho más robusto) y, si el formato ya viene combinado, mueve el archivo final con nombre saneado; si hace falta unir video+audio o convertir a MP3, deja el job listo para la Fase 4. Sin cola todavía (una descarga secuencial por vez): la ventana ya muestra progreso real (velocidad, MB descargados/total, ETA) y permite cancelar.
- ✅ **Fase 4 — Conversión.** `ConversionService` retoma el job en estado `Converting`: `FfmpegMediaProcessor.MuxAsync` remuxea video+audio con `-c copy` (rápido, sin pérdida) y si falla reintenta transcodificando solo el audio a un códec compatible con el contenedor de salida (AAC para mp4, Opus para webm); `ExtractAudioAsync` convierte a MP3 acotando el bitrate al real de la fuente (nunca anuncia más calidad de la que hay); `VerifyAsync` usa ffprobe para confirmar duración y pistas antes de mover el archivo a su destino final y marcar el job `Completed`. El progreso de FFmpeg (`-progress pipe:1`) también se refleja en la barra, y la ventana muestra el indicador de etapas (✓ Analizando ✓ Descargando ● Convirtiendo ○ Verificando ○ Finalizado) que pedía la propuesta original. `DownloadJobFactory` ahora prioriza emparejar audio de la misma familia de contenedor que el video, para que el remux directo sea el camino común.
- ✅ **Fase 5 — Interfaz.** Rediseño visual completo sobre WPF-UI (Fluent, Mica, tema claro/oscuro automático según Windows): la ventana pasó de formulario plano a tarjetas (`CardControl`/`CardExpander`) con iconografía Fluent, un flujo narrativo que revela Información/Formato/Descarga/Progreso a medida que avanza el proceso, lista de formatos como tabla (`ListView` + `GridView`), barra de progreso propia, indicador de etapas con íconos de estado (hecho/actual/error) y un `InfoBar` con severidad según el resultado. El panel técnico (diagnóstico + log crudo) quedó plegado por defecto para no distraer del flujo principal.

## Estructura

```
src/
  DownloadYou.Domain          Entidades y enums, sin dependencias externas
  DownloadYou.Application     Casos de uso + abstracciones (IVideoSource, IMediaProcessor, IExternalToolLocator)
  DownloadYou.Infrastructure  Adaptadores: yt-dlp (versión + análisis + descarga), ffmpeg/ffprobe (mux, MP3, verificación), orquestación de procesos (CliWrap)
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

La ventana sigue un flujo narrativo de arriba hacia abajo:

1. **Analizar** — URL + botón, ejecuta `yt-dlp --dump-json` sobre el video.
2. **Información** — miniatura, título y autor (aparece tras analizar).
3. **Formato y calidad** — tabla con las calidades realmente disponibles (video-only, audio-only, combinadas).
4. **Descarga** — Video/Audio MP3, carpeta de destino, botón Descargar/Cancelar; ejecuta el pipeline completo (descarga → conversión si hace falta → verificación).
5. **Progreso** — barra propia, velocidad/tamaño/ETA, indicador de etapas y resultado final.
6. **Diagnóstico y registro técnico** (plegado) — "Probar motores" y el log crudo de yt-dlp/ffmpeg, para quien quiera ver el detalle.

## Pruebas

```powershell
# Solo unitarias, sin depender de binarios externos (seguro para CI)
dotnet test --filter "Category!=Integration"

# Todo, incluida la prueba de integración contra yt-dlp/ffmpeg reales en tools/
dotnet test
```
