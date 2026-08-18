# Build IronXNestCommand and copy plugin DLLs into the game's BepInEx plugins folder.
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$game = 'C:\Program Files (x86)\Steam\steamapps\common\Iron Nest Heavy Turret Simulator'
$plugins = Join-Path $game 'BepInEx\plugins'
$solution = Join-Path $root 'IronXNestCommand.sln'

$dotnet = $null
foreach ($candidate in @(
        (Join-Path $root 'tools\dotnet-sdk\dotnet.exe'),
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
    throw 'No .NET SDK found. Please install .NET SDK (https://aka.ms/dotnet/8.0/dotnet-sdk-win-x64.exe) or place a portable SDK in tools\dotnet-sdk\.'
}

Write-Host "Using $dotnet"
& $dotnet --list-sdks
& $dotnet build $solution -c Release
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $plugins)) {
    New-Item -ItemType Directory -Path $plugins -Force | Out-Null
}

$hostOut = Join-Path $root 'IronXNestCommand.Host.BepInEx\bin\Release'
$coreOut = Join-Path $root 'IronXNestCommand.Core\bin\Release'
Copy-Item (Join-Path $hostOut 'IronXNestCommand.dll') $plugins -Force
Copy-Item (Join-Path $coreOut 'IronXNestCommand.Core.dll') $plugins -Force

$coopDll = Join-Path $root 'tools\extracted_coop\IronNestCoop.Core.dll'
if (Test-Path $coopDll) {
    Copy-Item $coopDll $plugins -Force
}

Write-Host "Deployed to $plugins"
Get-ChildItem $plugins | Format-Table Name, Length, LastWriteTime -AutoSize
