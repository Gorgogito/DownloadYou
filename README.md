# DownloadYou

Aplicación de escritorio (Windows, .NET 10, WPF) para descarga y conversión de contenido de YouTube. Ver la propuesta de arquitectura completa (comparación de lenguajes, stack, pipeline, seguridad, roadmap) en el documento aprobado del proyecto.

## Estado

- ✅ **Fase 1 — Prototipo técnico.** Solución con las cuatro capas (Domain, Application, Infrastructure, Presentation), inyección de dependencias configurada, y una prueba de concepto que invoca `yt-dlp` y `ffmpeg` como procesos externos y transmite su salida estándar en tiempo real a la interfaz.
- ✅ **Fase 2 — Análisis de URL y metadatos.** `AnalyzeUrlService` valida que la URL sea de YouTube (allowlist de hosts, defensa en profundidad) y delega en `YtDlpVideoSource.AnalyzeAsync`, que invoca `yt-dlp --dump-json` y parsea el resultado (`YtDlpMetadataParser`) a `MediaInfo`/`FormatOption`, clasificando cada stream como video-only, audio-only o combinado. La ventana muestra miniatura, título, duración y la lista de calidades realmente disponibles para ese video.
- ✅ **Fase 3 — Descarga.** `DownloadJobFactory` empareja automáticamente un stream de audio cuando la calidad elegida es video-only (DASH). `DownloadService` descarga cada stream con `YtDlpVideoSource.DownloadAsync` (progreso vía `--progress-template` de yt-dlp — campos numéricos crudos, no texto parseado — mucho más robusto) y, si el formato ya viene combinado, mueve el archivo final con nombre saneado; si hace falta unir video+audio o convertir a MP3, deja el job listo para la Fase 4. Sin cola todavía (una descarga secuencial por vez): la ventana ya muestra progreso real (velocidad, MB descargados/total, ETA) y permite cancelar.
- ✅ **Fase 4 — Conversión.** `ConversionService` retoma el job en estado `Converting`: `FfmpegMediaProcessor.MuxAsync` remuxea video+audio con `-c copy` (rápido, sin pérdida) y si falla reintenta transcodificando solo el audio a un códec compatible con el contenedor de salida (AAC para mp4, Opus para webm); `ExtractAudioAsync` convierte a MP3 acotando el bitrate al real de la fuente (nunca anuncia más calidad de la que hay); `VerifyAsync` usa ffprobe para confirmar duración y pistas antes de mover el archivo a su destino final y marcar el job `Completed`. El progreso de FFmpeg (`-progress pipe:1`) también se refleja en la barra, y la ventana muestra el indicador de etapas (✓ Analizando ✓ Descargando ● Convirtiendo ○ Verificando ○ Finalizado) que pedía la propuesta original. `DownloadJobFactory` ahora prioriza emparejar audio de la misma familia de contenedor que el video, para que el remux directo sea el camino común.
- ✅ **Fase 5 — Interfaz.** Rediseño visual completo sobre WPF-UI (Fluent, Mica, tema claro/oscuro automático según Windows): la ventana pasó de formulario plano a tarjetas (`CardControl`/`CardExpander`) con iconografía Fluent, un flujo narrativo que revela Información/Formato/Descarga/Progreso a medida que avanza el proceso, lista de formatos como tabla (`ListView` + `GridView`), barra de progreso propia, indicador de etapas con íconos de estado (hecho/actual/error) y un `InfoBar` con severidad según el resultado. El panel técnico (diagnóstico + log crudo) quedó plegado por defecto para no distraer del flujo principal.
- ✅ **Fase 6 — Cola de descargas.** `DownloadQueue` reemplaza la descarga única por una cola real: un `Channel<DownloadJob>` alimenta N workers concurrentes (configurable, 3 por defecto). Cada job tiene su propio `CancellationTokenSource`; **Pausar** cancela cooperativamente conservando el `.part` de yt-dlp y los streams ya descargados, **Reanudar** vuelve a encolar el mismo job — yt-dlp retoma cada stream desde donde quedó, y si la conversión ya se había iniciado, solo esa etapa se rehace (FFmpeg no puede reanudar un mux a medias). **Cancelar** sí limpia los archivos temporales. `YtDlpVideoSource.DownloadAsync` ahora reintenta con Polly (backoff exponencial, hasta 3 veces) solo ante errores de red transitorios — un video privado o eliminado falla de inmediato, sin reintentar. La ventana muestra una tarjeta por job en la cola, cada una con su propio progreso, indicador de etapas y botones Pausar/Reanudar/Cancelar independientes.
- ✅ **Fase 7 — Historial.** `HistoryService` escucha `DownloadQueue` y registra automáticamente cada job que llega a un estado terminal (Completado, Con error o Cancelado) en SQLite (`SqliteHistoryRepository`, vía Microsoft.Data.Sqlite, en `%AppData%\DownloadYou\history.db`). La ventana muestra una tarjeta de Historial con buscador (por título o URL) y, por registro, botones **Abrir carpeta** (selecciona el archivo en el Explorador, o abre la carpeta si el archivo ya no existe), **Repetir** (vuelve a analizar la URL y reencola exactamente el mismo `FormatId` — si ya no está disponible, avisa en vez de descargar algo distinto) y **Eliminar**. Las descargas nuevas aparecen en el historial sin recargar la lista.
- ✅ **Fase 8 — Biblioteca.** Vistas **Recientes / Videos / Audio / Favoritos** sobre el mismo historial (sin una fuente de datos separada): filtran en memoria los registros ya `Completed` cuyo archivo todavía existe en disco. Se agregó `IsFavorite` a `HistoryRecord` — con migración liviana (`ALTER TABLE`) para bases creadas en la Fase 7, que no tenían esa columna — y un botón de estrella (compartido entre las filas de Historial y Biblioteca, misma instancia de `HistoryEntryViewModel`, así que marcar/desmarcar se refleja al instante en ambos lados).
- ✅ **Fase 9 — Configuración.** `AppSettings` se persiste como JSON legible en `%AppData%\DownloadYou\settings.json` (`JsonSettingsStore`) y queda genuinamente conectado al resto de la app vía `SettingsService`, no solo expuesto en la UI: carpeta de descargas, tipo y bitrate de audio por defecto, plantilla de nombre de archivo y comportamiento ante archivo existente ahora los lee `MainViewModel` de `SettingsService.Current` en cada `Enqueue`/`Repetir`, en vez de constantes fijas. De paso se cerró un hueco real que venía desde la Fase 3: la opción **Omitir** (Skip) nunca se ejecutaba de verdad — `DownloadService` la trataba como Renombrar porque Configuración todavía no existía para exponerla; ahora hay un chequeo previo que detecta el archivo ya existente y evita la descarga por completo. El tema (Sistema/Claro/Oscuro) tiene vista previa instantánea al elegirlo y, al guardar, la preferencia se respeta también al reabrir la app (antes, `App.xaml.cs` siempre forzaba el tema del sistema al arrancar, ignorando cualquier elección previa). **Limitaciones conocidas, documentadas en la propia UI:** la cantidad de descargas simultáneas se aplica recién la próxima vez que se abre la app (la cola no redimensiona su pool de workers en caliente), y el campo Idioma queda guardado para compatibilidad futura pero no hay todavía infraestructura de localización — toda la interfaz sigue en español fijo, así que no se ofrece un selector que no haría nada.
- ✅ **Fase 10 — Pruebas.** El mayor hueco de cobertura real era la capa de Presentación: cero tests desde el inicio del proyecto pese a tener lógica no trivial (motor de estados del indicador de etapas de cada descarga, formateo de velocidad/tamaño/ETA, conmutación mutuamente excluyente de Video/Audio, previsualización en vivo de la plantilla de nombre). Se agregó el proyecto `DownloadYou.Presentation.Tests` (95 pruebas: ViewModels, converters de WPF y `DisplayFormat`, con dobles de prueba propios para `IVideoSource`/`IMediaProcessor`/`IHistoryRepository` en vez de tocar yt-dlp/ffmpeg/SQLite reales) y `DestinationPathResolverTests` en `DownloadYou.Application.Tests`, que hasta ahora solo se ejercitaba indirectamente a través de `DownloadService`. De paso se encontró y corrigió un deadlock real: un test que encolaba una descarga de verdad hacía que el worker en background de `DownloadQueue` llamara a `Dispatcher.Invoke` desde otro hilo hacia un `Dispatcher` de prueba sin message loop corriendo — se colgaba para siempre y, según el orden de ejecución, podía trabar toda la corrida de `dotnet test` de la solución. El doble de prueba de `IVideoSource` ahora nunca completa la descarga (en vez de lanzar de inmediato), así el worker se queda esperando sin volver a cruzar hilos hacia el `Dispatcher` capturado en el test.

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
4. **Descarga** — Video/Audio MP3, carpeta de destino, botón "Agregar a la cola".
5. **Cola de descargas** — una tarjeta por job encolado, cada una con su propio progreso, velocidad/tamaño/ETA, indicador de etapas y botones Pausar/Reanudar/Cancelar; varios jobs corren en paralelo (hasta 3 por defecto).
6. **Historial** — buscador + lista de descargas pasadas, con favorito / Abrir carpeta / Repetir / Eliminar por registro.
7. **Biblioteca** — Recientes / Videos / Audio / Favoritos: las mismas descargas completadas, filtradas.
8. **Configuración** (plegado) — carpeta de descargas, tipo y bitrate de audio por defecto, descargas simultáneas, comportamiento ante archivo existente, plantilla de nombre de archivo, tema y aviso de uso responsable.
9. **Diagnóstico y registro técnico** (plegado) — "Probar motores" y el log crudo de yt-dlp/ffmpeg, para quien quiera ver el detalle.

## Pruebas

```powershell
# Solo unitarias, sin depender de binarios externos (seguro para CI)
dotnet test --filter "Category!=Integration"

# Todo, incluida la prueba de integración contra yt-dlp/ffmpeg reales en tools/
dotnet test
```
