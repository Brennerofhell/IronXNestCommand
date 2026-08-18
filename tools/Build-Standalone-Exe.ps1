<#
.SYNOPSIS
    Kompiliert einen echten, eigenständigen Single-File .exe Installer mit csc.exe.
#>

[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$distDir  = Join-Path $repoRoot "dist"
if (-not (Test-Path $distDir)) { New-Item -ItemType Directory -Path $distDir -Force | Out-Null }

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  IRON X NEST COMMAND // STANDALONE EXE INSTALLER BUILDER" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Erst die Mod DLLs bauen falls noch nicht vorhanden
$bepDll   = Join-Path $repoRoot "IronXNestCommand.Host.BepInEx\bin\$Configuration\IronXNestCommand.dll"
$coreDll  = Join-Path $repoRoot "IronXNestCommand.Core\bin\$Configuration\IronXNestCommand.Core.dll"
$melonDll = Join-Path $repoRoot "IronXNestCommand.MelonLoader\bin\$Configuration\IronXNestCommand.dll"

if (-not (Test-Path $bepDll) -or -not (Test-Path $coreDll)) {
    Write-Host "[1/3] Baue Solution..." -ForegroundColor Yellow
    dotnet build (Join-Path $repoRoot "IronXNestCommand.sln") -c $Configuration
}

# 2. csc.exe suchen
Write-Host "[2/3] Suche C# Compiler (csc.exe)..." -ForegroundColor Yellow
$cscCandidates = @(
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

$csc = $null
foreach ($cand in $cscCandidates) {
    if (Test-Path $cand) { $csc = $cand; break }
}

if (-not $csc) {
    Write-Error "csc.exe nicht gefunden."
    exit 1
}

# 3. Exe kompilieren mit eingebetteten DLLs
$outputExe = Join-Path $distDir "IronXNestCommand-Installer.exe"
$sourceFile = Join-Path $repoRoot "tools\StandaloneInstaller\Program.cs"

Write-Host "[3/3] Kompiliere eigenständige Single-File Setup.exe..." -ForegroundColor Yellow

$argsList = @(
    "/target:winexe",
    "/optimize+",
    "/platform:anycpu",
    "/out:$outputExe",
    "/reference:System.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Windows.Forms.dll",
    "/resource:$bepDll,IronXNestCommand.dll",
    "/resource:$coreDll,IronXNestCommand.Core.dll"
)

if (Test-Path $melonDll) {
    $argsList += "/resource:$melonDll,IronXNestCommand_Melon.dll"
}

$argsList += $sourceFile

& $csc $argsList
if ($LASTEXITCODE -eq 0) {
    $size = (Get-Item $outputExe).Length / 1KB
    Write-Host ""
    Write-Host "  -> ERFOLG! Single-File Installer erstellt:" -ForegroundColor Green
    Write-Host "     Pfad: $outputExe" -ForegroundColor Cyan
    Write-Host "     Groesse: $([Math]::Round($size, 1)) KB" -ForegroundColor Green
    Write-Host ""
    Write-Host "Diese einzelne .exe-Datei kann nun direkt an jeden weitergegeben werden!" -ForegroundColor Green
} else {
    Write-Error "Kompilierung des Installers fehlgeschlagen."
}
