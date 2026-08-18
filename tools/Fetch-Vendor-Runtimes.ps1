<#
.SYNOPSIS
    Laedt den gepinnten BepInEx 6 IL2CPP-Runtime-Build herunter und entpackt ihn nach tools/vendor/,
    damit Package-Release.ps1 ihn in ZIP/Standalone-exe/Setup.exe einbetten kann.

.NOTES
    BepInEx wird unter einer Lizenz vertrieben, die das Bundling kompilierter Binaries in einem
    Drittanbieter-Installer erlaubt (LGPL-2.1) -- siehe THIRD-PARTY-LICENSES.md im Repo-Root.
    Downloads werden unter tools/vendor/ gecacht (gitignored, grosse Binaries) und nur bei Bedarf
    erneut geholt/entpackt.

    MelonLoader wird ab dieser Version nicht mehr released (siehe CLAUDE.md) -- der Fetch-Aufruf
    dafuer wurde entfernt. Der MelonLoader-Quellcode (IronXNestCommand.MelonLoader) bleibt im Repo,
    wird aber nicht mehr gebaut/gebundlet.

    Versions-Pin (bei Bedarf hier aktualisieren):
      - BepInEx 6 IL2CPP Bleeding-Edge Build 785 (2026-06-28)
#>

[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$repoRoot  = Split-Path -Parent $PSScriptRoot
$vendorDir = Join-Path $repoRoot "tools\vendor"
$dlDir     = Join-Path $vendorDir "downloads"

$BepInExUrl      = "https://builds.bepinex.dev/projects/bepinex_be/785/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.785+6abdba4.zip"
$BepInExZipName  = "BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.785.zip"
$BepInExExtractDir = Join-Path $vendorDir "BepInEx-extracted"

New-Item -ItemType Directory -Force -Path $dlDir | Out-Null

function Get-VendoredRuntime {
    param(
        [string]$Name,
        [string]$Url,
        [string]$ZipFileName,
        [string]$ExtractDir
    )

    $zipPath = Join-Path $dlDir $ZipFileName

    if ($Force -and (Test-Path $zipPath)) { Remove-Item $zipPath -Force }
    if ($Force -and (Test-Path $ExtractDir)) { Remove-Item $ExtractDir -Recurse -Force }

    if (-not (Test-Path $zipPath)) {
        Write-Host "  [$Name] Lade herunter: $Url" -ForegroundColor Yellow
        Invoke-WebRequest -Uri $Url -OutFile $zipPath -UseBasicParsing
    } else {
        Write-Host "  [$Name] Bereits gecacht: $zipPath" -ForegroundColor DarkGray
    }

    if (-not (Test-Path $ExtractDir) -or (Get-ChildItem $ExtractDir -ErrorAction SilentlyContinue | Measure-Object).Count -eq 0) {
        Write-Host "  [$Name] Entpacke nach $ExtractDir ..." -ForegroundColor Yellow
        New-Item -ItemType Directory -Force -Path $ExtractDir | Out-Null
        Expand-Archive -Path $zipPath -DestinationPath $ExtractDir -Force
    } else {
        Write-Host "  [$Name] Bereits entpackt: $ExtractDir" -ForegroundColor DarkGray
    }
}

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  VENDOR RUNTIMES // BepInEx 6 IL2CPP" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

Get-VendoredRuntime -Name "BepInEx" -Url $BepInExUrl -ZipFileName $BepInExZipName -ExtractDir $BepInExExtractDir

Write-Host ""
Write-Host "  -> Vendored Runtime bereit:" -ForegroundColor Green
Write-Host "     $BepInExExtractDir" -ForegroundColor Green
