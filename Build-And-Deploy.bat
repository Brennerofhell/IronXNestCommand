@echo off
title IronXNestCommand - MelonLoader ^& BepInEx Build ^& Deploy

echo ==============================================================================
echo   IRON X NEST COMMAND - DUAL LOADER BUILD ^& DEPLOY
echo   (Non-Standalone / MelonLoader in Mods/ ^& BepInEx in BepInEx/plugins/)
echo ==============================================================================
echo.

set GAME_DIR=C:\Program Files (x86)\Steam\steamapps\common\Iron Nest Heavy Turret Simulator
set MODS_DIR=C:\Program Files (x86)\Steam\steamapps\common\Iron Nest Heavy Turret Simulator\Mods
set PLUGINS_DIR=C:\Program Files (x86)\Steam\steamapps\common\Iron Nest Heavy Turret Simulator\BepInEx\plugins

set SOLUTION_PATH=%~dp0IronXNestCommand.sln
set MELON_DLL=%~dp0IronXNestCommand.MelonLoader\bin\Release\IronXNestCommand.dll
set BEPINEX_DLL=%~dp0IronXNestCommand.Host.BepInEx\bin\Release\IronXNestCommand.dll
set CORE_DLL=%~dp0IronXNestCommand.Core\bin\Release\IronXNestCommand.Core.dll

:: 1. Build Solution (MelonLoader + BepInEx + Core)
echo [1/2] Kompiliere alle Projekte in IronXNestCommand.sln (Release)...
dotnet build "%SOLUTION_PATH%" -c Release

if errorlevel 1 goto :BuildError

:: 2. Deploy to both Mods/ (MelonLoader) and BepInEx/plugins/ (BepInEx)
echo.
echo [2/2] Kopiere Mod-DLLs in Spielverzeichnisse...

powershell -NoProfile -Command "New-Item -ItemType Directory -Path '%MODS_DIR%' -Force | Out-Null; Copy-Item '%MELON_DLL%' '%MODS_DIR%\IronXNestCommand.dll' -Force; Write-Host '  [+] MelonLoader: %MODS_DIR%\IronXNestCommand.dll'; New-Item -ItemType Directory -Path '%PLUGINS_DIR%' -Force | Out-Null; Copy-Item '%BEPINEX_DLL%' '%PLUGINS_DIR%\IronXNestCommand.dll' -Force; Copy-Item '%CORE_DLL%' '%PLUGINS_DIR%\IronXNestCommand.Core.dll' -Force; Write-Host '  [+] BepInEx:     %PLUGINS_DIR%\IronXNestCommand.dll'"

echo.
echo ==============================================================================
echo   [ERFOLG] IronXNestCommand fuer MelonLoader UND BepInEx installiert!
echo ==============================================================================
echo.
pause
exit /b 0

:BuildError
echo.
echo [FEHLER] Build fehlgeschlagen!
pause
exit /b 1
