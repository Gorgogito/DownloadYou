# Herramientas externas (no versionadas)

Coloca aquí los ejecutables que el prototipo invoca como procesos externos:

- `yt-dlp.exe` — https://github.com/yt-dlp/yt-dlp/releases (asset `yt-dlp.exe`)
- `ffmpeg.exe` y `ffprobe.exe` — build LGPL de https://www.gyan.dev/ffmpeg/builds/ (carpeta `bin/`)

`ExternalToolLocator` busca en esta carpeta (subiendo desde el directorio de salida del build hasta la raíz del repo) y, si no encuentra el ejecutable, en el `PATH` del sistema. No es necesario copiar nada al directorio `bin/` del proyecto.

Verifica el checksum SHA-256 de cada binario contra el publicado en la página oficial de descargas antes de usarlo.
