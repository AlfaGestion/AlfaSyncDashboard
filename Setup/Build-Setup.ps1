#Requires -Version 5.1
<#
.SYNOPSIS
    Publica AlfaSyncDashboard y genera el instalador con Inno Setup.
.DESCRIPTION
    1. Ejecuta dotnet publish en modo Release (win-x64, self-contained)
    2. Compila el script Inno Setup y genera el .exe instalador
    El instalador queda en Setup\Output\
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Rutas
$ScriptDir  = $PSScriptRoot
$RepoRoot   = Split-Path $ScriptDir -Parent
$CsprojPath = Join-Path $RepoRoot "AlfaSyncDashboard\AlfaSyncDashboard.csproj"
$IssPath    = Join-Path $ScriptDir "AlfaSyncDashboard.iss"
$OutputDir  = Join-Path $ScriptDir "Output"

# Buscar ISCC.exe (compilador de Inno Setup)
$IssccCandidates = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    "C:\Program Files\Inno Setup 6\ISCC.exe"
    "C:\Program Files (x86)\Inno Setup 5\ISCC.exe"
)
$IsccPath = $IssccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $IsccPath) {
    Write-Error @"
No se encontro ISCC.exe (compilador de Inno Setup).
Instale Inno Setup 6 desde: https://jrsoftware.org/isdl.php
O agregue la ruta manualmente en la variable `$IssccCandidates de este script.
"@
}

# Validar que existan los archivos necesarios
foreach ($path in @($CsprojPath, $IssPath)) {
    if (-not (Test-Path $path)) {
        Write-Error "No se encontro: $path"
    }
}

# Banner
Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "  Alfa Sync Dashboard - Generacion de Setup"     -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Paso 1: dotnet publish
Write-Host '[1/2] Publicando aplicacion (Release, win-x64, self-contained)...' -ForegroundColor Yellow

$PublishArgs = @(
    "publish"
    $CsprojPath
    "--configuration", "Release"
    "--runtime",       "win-x64"
    "--self-contained", "true"
    "--nologo"
)

& dotnet @PublishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish fallo con codigo $LASTEXITCODE"
}

Write-Host '    OK - Publicacion completada.' -ForegroundColor Green
Write-Host ""

# Paso 2: Inno Setup
Write-Host '[2/2] Compilando instalador con Inno Setup...' -ForegroundColor Yellow
Write-Host "    $IsccPath"
Write-Host ""

& $IsccPath $IssPath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Inno Setup fallo con codigo $LASTEXITCODE"
}

# Resultado
$Installer = Get-ChildItem -Path $OutputDir -Filter "*.exe" |
             Sort-Object LastWriteTime -Descending |
             Select-Object -First 1

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Setup generado correctamente:"                  -ForegroundColor Green
Write-Host "  $($Installer.FullName)"                         -ForegroundColor White
Write-Host "  Tamanio: $([math]::Round($Installer.Length / 1MB, 1)) MB" -ForegroundColor White
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
