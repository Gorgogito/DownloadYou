#Requires -Version 5.1
<#
    Genera los dos artefactos de distribucion de la Fase 11 (Instalador):
      1. dist\DownloadYou-Setup-<version>.exe  -- instalador (Inno Setup)
      2. dist\DownloadYou-Portable-<version>.zip -- ZIP portable, sin instalacion

    Requiere:
      - dotnet SDK (para el publish self-contained/single-file)
      - Inno Setup 6 instalado (ISCC.exe) -- se busca en el PATH y en la ruta
        de instalacion por defecto de winget/el instalador oficial
      - tools\ffmpeg.exe / ffprobe.exe / yt-dlp.exe presentes (ver tools\README.md;
        ffmpeg/ffprobe deben ser el build LGPL, nunca uno GPL, para poder
        redistribuirlos en el instalador)

    Uso:
      powershell -File build\publish.ps1
      powershell -File build\publish.ps1 -SkipInstaller   # solo el ZIP portable
      powershell -File build\publish.ps1 -SkipZip         # solo el instalador
#>
param(
    [switch]$SkipInstaller,
    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$presentationProj = Join-Path $repoRoot "src\DownloadYou.Presentation"
$toolsDir = Join-Path $repoRoot "tools"
$publishDir = Join-Path $presentationProj "bin\Release\net10.0-windows\win-x64\publish"
$distDir = Join-Path $repoRoot "dist"
$version = "1.0.0"

Write-Host "== DownloadYou -- build de distribucion ($version) ==" -ForegroundColor Cyan

# --- Verificar que las herramientas externas reales esten presentes ---
$requiredTools = "yt-dlp.exe", "ffmpeg.exe", "ffprobe.exe", "deno.exe"
foreach ($tool in $requiredTools) {
    $toolPath = Join-Path $toolsDir $tool
    if (-not (Test-Path $toolPath)) {
        throw "Falta $toolPath. Ver tools\README.md -- ffmpeg/ffprobe deben ser el build LGPL de BtbN/FFmpeg-Builds (nunca un build GPL); deno.exe es opcional para la app pero requerido para que este script arme la distribucion completa."
    }
}

New-Item -ItemType Directory -Path $distDir -Force | Out-Null

# --- 1. Publish self-contained single-file ---
Write-Host "`n-- Publicando self-contained single-file (win-x64) --" -ForegroundColor Yellow
& dotnet publish $presentationProj -c Release -p:PublishProfile=win-x64
if ($LASTEXITCODE -ne 0) { throw "dotnet publish fallo (exit code $LASTEXITCODE)." }

$exePath = Join-Path $publishDir "DownloadYou.exe"
if (-not (Test-Path $exePath)) { throw "No se encontro $exePath tras el publish." }

# --- 2. ZIP portable ---
if (-not $SkipZip) {
    Write-Host "`n-- Armando ZIP portable --" -ForegroundColor Yellow
    $stagingDir = Join-Path $distDir "staging-portable"
    if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }
    New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $stagingDir "tools") -Force | Out-Null

    Copy-Item $exePath -Destination $stagingDir
    foreach ($tool in $requiredTools) {
        Copy-Item (Join-Path $toolsDir $tool) -Destination (Join-Path $stagingDir "tools")
    }
    Copy-Item (Join-Path $repoRoot "installer\THIRD-PARTY-NOTICES.txt") -Destination $stagingDir
    Copy-Item (Join-Path $repoRoot "installer\uso-responsable.txt") -Destination $stagingDir

    $zipPath = Join-Path $distDir "DownloadYou-Portable-$version.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path (Join-Path $stagingDir "*") -DestinationPath $zipPath -CompressionLevel Optimal
    Remove-Item $stagingDir -Recurse -Force

    $zipSizeMb = [Math]::Round((Get-Item $zipPath).Length / 1MB, 1)
    Write-Host "ZIP portable: $zipPath ($zipSizeMb MB)" -ForegroundColor Green
}

# --- 3. Instalador (Inno Setup) ---
if (-not $SkipInstaller) {
    Write-Host "`n-- Compilando instalador con Inno Setup --" -ForegroundColor Yellow

    $iscc = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($iscc) {
        $isccPath = $iscc.Source
    } else {
        $candidates = @(
            "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
            "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
        )
        $isccPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    }

    if (-not $isccPath) {
        throw "No se encontro ISCC.exe (Inno Setup). Instalalo con: winget install JRSoftware.InnoSetup"
    }

    $issPath = Join-Path $repoRoot "installer\DownloadYou.iss"
    & $isccPath $issPath
    if ($LASTEXITCODE -ne 0) { throw "ISCC.exe fallo (exit code $LASTEXITCODE)." }

    $setupExe = Get-ChildItem $distDir -Filter "DownloadYou-Setup-*.exe" | Select-Object -First 1
    if ($setupExe) {
        $setupSizeMb = [Math]::Round($setupExe.Length / 1MB, 1)
        Write-Host "Instalador: $($setupExe.FullName) ($setupSizeMb MB)" -ForegroundColor Green
    }
}

Write-Host "`nListo." -ForegroundColor Cyan
