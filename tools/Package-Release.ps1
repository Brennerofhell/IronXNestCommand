<#
.SYNOPSIS
    Baut das IronXNestCommand Projekt und erstellt ein fertiges Release-Paket (ZIP + optional Inno Setup .exe).
#>

[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$distDir  = Join-Path $repoRoot "dist"
$tempDir  = Join-Path $distDir "temp_payload"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  IRON X NEST COMMAND // RELEASE BUILDER" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# 1. dotnet SDK ermitteln
$dotnet = "dotnet"
$localDotNet = Join-Path $repoRoot "tools\dotnet-sdk\dotnet.exe"
if (Test-Path $localDotNet) { $dotnet = $localDotNet }

Write-Host "[1/4] Baue Projekt in $Configuration Konfiguration..." -ForegroundColor Yellow
$slnPath = Join-Path $repoRoot "IronXNestCommand.sln"
& $dotnet build $slnPath -c $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build fehlgeschlagen."
    exit 1
}

# 2. Dist-Verzeichnis vorbereiten
if (Test-Path $tempDir) { Remove-Item -Path $tempDir -Recurse -Force }
if (-not (Test-Path $distDir)) { New-Item -ItemType Directory -Path $distDir -Force | Out-Null }
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $tempDir "tools") -Force | Out-Null

$version = "0.1.0"
$modInfoPath = Join-Path $repoRoot "IronXNestCommand.Core\ModInfo.cs"
if (Test-Path $modInfoPath) {
    $content = Get-Content $modInfoPath -Raw
    if ($content -match 'Version\s*=\s*"([^"]+)"') {
        $version = $matches[1]
    }
}

Write-Host "[2/4] Kopiere Release-Dateien..." -ForegroundColor Yellow

# DLLs kopieren
Copy-Item (Join-Path $repoRoot "IronXNestCommand.Host.BepInEx\bin\$Configuration\IronXNestCommand.dll") -Destination $tempDir
Copy-Item (Join-Path $repoRoot "IronXNestCommand.Core\bin\$Configuration\IronXNestCommand.Core.dll") -Destination $tempDir

# Skripte kopieren
Copy-Item (Join-Path $repoRoot "Install-Mod.bat") -Destination $tempDir
Copy-Item (Join-Path $repoRoot "Deinstall-Mod.bat") -Destination $tempDir
Copy-Item (Join-Path $repoRoot "Uninstall-GUI.bat") -Destination $tempDir
Copy-Item (Join-Path $repoRoot "README.md") -Destination $tempDir
Copy-Item (Join-Path $repoRoot "tools\ModManagerGUI.ps1") -Destination (Join-Path $tempDir "tools")

# 3. ZIP-Archiv schnüren
Write-Host "[3/4] Erstelle Standalone-ZIP-Paket..." -ForegroundColor Yellow
$zipOutput = Join-Path $distDir "IronXNestCommand_v$version.zip"
if (Test-Path $zipOutput) { Remove-Item $zipOutput -Force }

Compress-Archive -Path "$tempDir\*" -DestinationPath $zipOutput -CompressionLevel Optimal
Remove-Item -Path $tempDir -Recurse -Force
Write-Host "  -> ZIP erstellt: $zipOutput" -ForegroundColor Green

# 4. Standalone Single-File .exe Installer erstellen
Write-Host "[4/5] Erstelle Standalone Single-File .exe Installer..." -ForegroundColor Yellow
$buildExeScript = Join-Path $PSScriptRoot "Build-Standalone-Exe.ps1"
if (Test-Path $buildExeScript) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $buildExeScript -Configuration $Configuration
}

# 5. Inno Setup Compiler pruefen (Optional)
Write-Host "[5/5] Pruefe Inno Setup Compiler (ISCC)..." -ForegroundColor Yellow
$isccCandidates = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 5\ISCC.exe",
    "C:\Program Files\Inno Setup 5\ISCC.exe"
)

$isccPath = $null
foreach ($cand in $isccCandidates) {
    if (Test-Path $cand) {
        $isccPath = $cand
        break
    }
}

if (-not $isccPath) {
    $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($cmd) { $isccPath = $cmd.Source }
}

if ($isccPath) {
    Write-Host "  ISCC gefunden unter: $isccPath" -ForegroundColor DarkCyan
    Write-Host "  Kompiliere Setup.exe..." -ForegroundColor Yellow
    $issFile = Join-Path $repoRoot "tools\Installer.iss"
    & $isccPath $issFile
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  -> Setup.exe erfolgreich erstellt in 'dist/'!" -ForegroundColor Green
    } else {
        Write-Warning "Inno Setup Kompilierung hat Warnungen oder Fehler zurueckgegeben."
    }
} else {
    Write-Host "  [i] Inno Setup (ISCC.exe) ist nicht installiert." -ForegroundColor DarkGray
    Write-Host "      Um eine Setup.exe zu bauen: Lade Inno Setup 6 (https://jrsoftware.org/isdl.php) herunter." -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  BUILD & PACKAGING ERFOLGREICH BEENDET" -ForegroundColor Green
Write-Host "  Dateien in: $distDir" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
