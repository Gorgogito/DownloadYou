; Script de Inno Setup para DownloadYou (Fase 11 del roadmap — Instalador).
;
; Requiere que antes de compilar este script ya exista la publicación
; self-contained/single-file (ver installer\README.md o build\publish.ps1,
; que hace ambos pasos en orden) y que tools\ tenga los binarios reales de
; yt-dlp/ffmpeg/ffprobe (ver tools\README.md — deben ser el build LGPL de
; FFmpeg, nunca uno GPL, para poder redistribuirlo).
;
; Instalación por usuario (sin privilegios de administrador, sin UAC),
; consistente con cómo se distribuyen hoy herramientas de escritorio
; livianas modernas (VS Code, Discord, etc.).

#define MyAppName "DownloadYou"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "DownloadYou"
#define MyAppExeName "DownloadYou.exe"
#define PublishDir "..\src\DownloadYou.Presentation\bin\Release\net10.0-windows\win-x64\publish"
#define ToolsDir "..\tools"

[Setup]
AppId={{49277C0E-79C6-49ED-96B8-6F11283FD5E7}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=DownloadYou-Setup-{#MyAppVersion}
SetupIconFile=..\src\DownloadYou.Presentation\Assets\icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
LicenseFile=uso-responsable.txt
InfoBeforeFile=THIRD-PARTY-NOTICES.txt

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el Escritorio"; GroupDescription: "Accesos directos adicionales:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#ToolsDir}\yt-dlp.exe"; DestDir: "{app}\tools"; Flags: ignoreversion
Source: "{#ToolsDir}\ffmpeg.exe"; DestDir: "{app}\tools"; Flags: ignoreversion
Source: "{#ToolsDir}\ffprobe.exe"; DestDir: "{app}\tools"; Flags: ignoreversion
Source: "THIRD-PARTY-NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName}"; Flags: nowait postinstall skipifsilent

; ---------------------------------------------------------------------------
; Firma de código: no incluida en este script porque el proyecto todavía no
; cuenta con un certificado de firma de código. Cuando exista uno, agregar
; en [Setup]:
;   SignTool=signtool /f "$qruta\al\certificado.pfx$q" /p $p /tr http://timestamp.digicert.com /td sha256 /fd sha256 $f
; y compilar con ISCC /Ssigntool$p=<password> DownloadYou.iss
; (ver §14 y §16 del documento de arquitectura — reduce falsos positivos de
; antivirus, un riesgo medio ya identificado ahí).
; ---------------------------------------------------------------------------
