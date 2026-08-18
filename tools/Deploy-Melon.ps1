# Build IronXNestCommand and copy mod DLL into the game's MelonLoader Mods folder.
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$game = 'C:\Program Files (x86)\Steam\steamapps\common\Iron Nest Heavy Turret Simulator'
$modsFolder = Join-Path $game 'Mods'
$projectPath = Join-Path $root 'IronXNestCommand.MelonLoader\IronXNestCommand.csproj'

# 1. Check game folder
if (-not (Test-Path $game)) {
    throw "Game folder not found at: $game"
}

if (-not (Test-Path $modsFolder)) {
    New-Item -ItemType Directory -Path $modsFolder -Force | Out-Null
}

# 2. Locate dotnet SDK
$dotnet = $null
foreach ($candidate in @(
        'C:\Program Files\dotnet\dotnet.exe',
        (Join-Path $env:LOCALAPPDATA 'dotnet\dotnet.exe'),
        'dotnet'
    )) {
    if ($candidate -eq 'dotnet') {
        $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
        if ($cmd) { $dotnet = $cmd.Source; break }
    }
    elseif (Test-Path $candidate) {
        $dotnet = $candidate
        break
    }
}

if (-not $dotnet) {
    throw 'No dotnet executable found. Please install .NET SDK: https://aka.ms/dotnet/8.0/dotnet-sdk-win-x64.exe'
}

# MelonLoader Runtime sicherstellen
$melonVendor = Join-Path $root 'tools\vendor\MelonLoader-extracted'
if (Test-Path $melonVendor) {
    if (-not (Test-Path (Join-Path $game 'version.dll'))) {
        Copy-Item "$melonVendor\*" $game -Recurse -Force
        Write-Host "[DEPLOY] Installed MelonLoader 0.7.3 Runtime to $game" -ForegroundColor Green
    }
}

Write-Host "[DEPLOY] Using dotnet at: $dotnet"
Write-Host "[DEPLOY] Building IronXNestCommand (Release)..."

& $dotnet build $projectPath -c Release -p:GameFolder="$game"

if ($LASTEXITCODE -ne 0) {
    Write-Warning "Build fehlgeschlagen oder fehlende Interop-Assemblies. Verwende existierende Release-Binaries."
}

# 3. Copy DLL to Mods
$outputDll = Join-Path $root 'IronXNestCommand.MelonLoader\bin\Release\net6.0\IronXNestCommand.dll'
if (-not (Test-Path $outputDll)) {
    # Alternative path if AppendTargetFrameworkToOutputPath is false
    $outputDll = Join-Path $root 'IronXNestCommand.MelonLoader\bin\Release\IronXNestCommand.dll'
}

if (Test-Path $outputDll) {
    Copy-Item $outputDll $modsFolder -Force
    Write-Host "[DEPLOY] SUCCESS! Copied IronXNestCommand.dll to $modsFolder" -ForegroundColor Green
    Get-Item (Join-Path $modsFolder 'IronXNestCommand.dll') | Format-Table Name, Length, LastWriteTime -AutoSize
} else {
    Write-Warning "Could not locate output DLL at: $outputDll"
}

$coopSource = Join-Path $root 'tools\extracted_coop\IronNestCoop.Core.dll'
if (Test-Path $coopSource) {
    Copy-Item $coopSource $modsFolder -Force
    $userLibs = Join-Path $game 'UserLibs'
    if (-not (Test-Path $userLibs)) { New-Item -ItemType Directory -Path $userLibs -Force | Out-Null }
    Copy-Item $coopSource $userLibs -Force
    Write-Host "[DEPLOY] Copied IronNestCoop.Core.dll to Mods/ and UserLibs/" -ForegroundColor Green
}
