@echo off
setlocal enabledelayedexpansion
title IronXNestCommand // Deinstallation

echo ==============================================================================
echo   IRON X NEST COMMAND // DEINSTALLATION (BepInEx 6 IL2CPP)
echo ==============================================================================
echo.

set "GAME_PATH=C:\Program Files (x86)\Steam\steamapps\common\Iron Nest Heavy Turret Simulator"
set "PLUGINS_DIR=%GAME_PATH%\BepInEx\plugins"

if exist "%PLUGINS_DIR%\IronXNestCommand.dll" (
    del /F /Q "%PLUGINS_DIR%\IronXNestCommand.dll" >nul 2>nul
    del /F /Q "%PLUGINS_DIR%\IronXNestCommand.Core.dll" >nul 2>nul
    echo [ERFOLG] IronXNestCommand wurde aus %PLUGINS_DIR% entfernt.
) else (
    echo [INFO] IronXNestCommand war nicht in %PLUGINS_DIR% vorhanden.
)

echo.
pause
exit /b 0
