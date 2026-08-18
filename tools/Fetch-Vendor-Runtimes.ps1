<#
.SYNOPSIS
    Laedt die gepinnten BepInEx 6 IL2CPP- und MelonLoader-Runtime-Builds herunter und entpackt sie
    nach tools/vendor/, damit Package-Release.ps1 sie in ZIP/Standalone-exe/Setup.exe einbetten kann.

.NOTES
    Beide Loader werden unter einer Lizenz vertrieben, die das Bundling kompilierter Binaries in
    einem Drittanbieter-Installer erlaubt (BepInEx: LGPL-2.1, MelonLoader: Apache-2.0) -- siehe
    THIRD-PARTY-LICENSES.md im Repo-Root. Downloads werden unter tools/vendor/ gecacht (gitignored,
    grosse Binaries) und nur bei Bedarf erneut geholt/entpackt.

    Versions-Pins (bei Bedarf hier aktualisieren):
      - BepInEx 6 IL2CPP Bleeding-Edge Build 785 (2026-06-28)
      - MelonLoader v0.7.3 (Asset: MelonLoader.x64.zip)
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

$MelonLoaderUrl      = "https://github.com/LavaGang/MelonLoader/releases/download/v0.7.3/MelonLoader.x64.zip"
$MelonLoaderZipName  = "MelonLoader.x64.v0.7.3.zip"
$MelonLoaderExtractDir = Join-Path $vendorDir "MelonLoader-extracted"

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
Write-Host "  VENDOR RUNTIMES // BepInEx 6 IL2CPP + MelonLoader 0.7.3" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

Get-VendoredRuntime -Name "BepInEx"    -Url $BepInExUrl      -ZipFileName $BepInExZipName      -ExtractDir $BepInExExtractDir
Get-VendoredRuntime -Name "MelonLoader" -Url $MelonLoaderUrl -ZipFileName $MelonLoaderZipName   -ExtractDir $MelonLoaderExtractDir

Write-Host ""
Write-Host "  -> Vendored Runtimes bereit:" -ForegroundColor Green
Write-Host "     $BepInExExtractDir" -ForegroundColor Green
Write-Host "     $MelonLoaderExtractDir" -ForegroundColor Green
