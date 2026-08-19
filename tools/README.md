# Herramientas externas (no versionadas)

Coloca aquí los ejecutables que el prototipo invoca como procesos externos:

- `yt-dlp.exe` — https://github.com/yt-dlp/yt-dlp/releases (asset `yt-dlp.exe`)
- `ffmpeg.exe` y `ffprobe.exe` — build **LGPL** de https://github.com/BtbN/FFmpeg-Builds/releases (asset `ffmpeg-master-latest-win64-lgpl.zip`, carpeta `bin/`)

  gyan.dev (la fuente históricamente sugerida acá) dejó de publicar builds LGPL de ffmpeg/ffprobe — todos sus builds actuales son GPLv3 (`--enable-gpl`, con libx264/libx265). No los uses para el instalador redistribuible: la app solo necesita `-c copy` (remux) y `libmp3lame` (extracción a MP3), ninguno de los dos requiere codificadores GPL, así que el build LGPL de BtbN cubre todo lo que este proyecto usa sin arrastrar la licencia GPL a la distribución completa (ver §16 del documento de arquitectura).

`ExternalToolLocator` busca en esta carpeta (subiendo desde el directorio de salida del build hasta la raíz del repo) y, si no encuentra el ejecutable, en el `PATH` del sistema. No es necesario copiar nada al directorio `bin/` del proyecto.

Verifica el checksum SHA-256 de cada binario contra el publicado en la página oficial de descargas antes de usarlo.
