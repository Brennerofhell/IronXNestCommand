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
    throw 'No .NET SDK found. Install .NET 6 SDK or place a portable SDK in tools\dotnet-sdk\.'
}

Write-Host "Using $dotnet"
& $dotnet --list-sdks
& $dotnet build $solution -c Release
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $plugins)) {
    throw "BepInEx plugins folder missing: $plugins"
}

$hostOut = Join-Path $root 'IronXNestCommand.Host.BepInEx\bin\Release'
$coreOut = Join-Path $root 'IronXNestCommand.Core\bin\Release'
Copy-Item (Join-Path $hostOut 'IronXNestCommand.dll') $plugins -Force
Copy-Item (Join-Path $coreOut 'IronXNestCommand.Core.dll') $plugins -Force

Write-Host "Deployed to $plugins"
Get-Item (Join-Path $plugins 'IronXNestCommand.dll'), (Join-Path $plugins 'IronXNestCommand.Core.dll') |
    Format-Table Name, Length, LastWriteTime -AutoSize
