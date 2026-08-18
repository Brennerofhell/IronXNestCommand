@echo off
setlocal enabledelayedexpansion
title IronXNestCommand // BepInEx Installer

color 06
echo ==============================================================================
echo   IRON X NEST COMMAND // INSTALLATION
echo   Co-op Lobby Overlay, Feind-Schutz ^& Lochkarten-Sync (BepInEx 6 IL2CPP)
echo ==============================================================================
echo.

set "SOURCE_DIR=%~dp0"
set "HOST_DLL=%SOURCE_DIR%IronXNestCommand.Host.BepInEx\bin\Release\IronXNestCommand.dll"
set "CORE_DLL=%SOURCE_DIR%IronXNestCommand.Core\bin\Release\IronXNestCommand.Core.dll"

if not exist "%HOST_DLL%" (
    if exist "%SOURCE_DIR%IronXNestCommand.dll" (
        set "HOST_DLL=%SOURCE_DIR%IronXNestCommand.dll"
    )
)

if not exist "%CORE_DLL%" (
    if exist "%SOURCE_DIR%IronXNestCommand.Core.dll" (
        set "CORE_DLL=%SOURCE_DIR%IronXNestCommand.Core.dll"
    )
)

:: Suche nach dem Steam-Spielverzeichnis
set "GAME_PATH="

if exist "%SOURCE_DIR%Iron Nest Heavy Turret Simulator.exe" (
    set "GAME_PATH=%SOURCE_DIR%"
    goto :FoundGame
)

if exist "C:\Program Files (x86)\Steam\steamapps\common\Iron Nest Heavy Turret Simulator\Iron Nest Heavy Turret Simulator.exe" (
    set "GAME_PATH=C:\Program Files (x86)\Steam\steamapps\common\Iron Nest Heavy Turret Simulator"
    goto :FoundGame
)
if exist "C:\Program Files\Steam\steamapps\common\Iron Nest Heavy Turret Simulator\Iron Nest Heavy Turret Simulator.exe" (
    set "GAME_PATH=C:\Program Files\Steam\steamapps\common\Iron Nest Heavy Turret Simulator"
    goto :FoundGame
)
if exist "D:\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator\Iron Nest Heavy Turret Simulator.exe" (
    set "GAME_PATH=D:\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator"
    goto :FoundGame
)
if exist "E:\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator\Iron Nest Heavy Turret Simulator.exe" (
    set "GAME_PATH=E:\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator"
    goto :FoundGame
)
if exist "F:\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator\Iron Nest Heavy Turret Simulator.exe" (
    set "GAME_PATH=F:\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator"
    goto :FoundGame
)

echo [!] Spielverzeichnis konnte nicht automatisch gefunden werden.
echo Bitte gib den vollen Pfad zu deinem Spielordner ein:
set /p "USER_INPUT_PATH=> "
set "USER_INPUT_PATH=%USER_INPUT_PATH:"=%"

if exist "%USER_INPUT_PATH%\Iron Nest Heavy Turret Simulator.exe" (
    set "GAME_PATH=%USER_INPUT_PATH%"
    goto :FoundGame
)

echo [FEHLER] Ungueltiges Spielverzeichnis.
pause
exit /b 1

:FoundGame
echo [i] Spiel gefunden unter: "%GAME_PATH%"
echo.

set "PLUGINS_DIR=%GAME_PATH%\BepInEx\plugins"
if not exist "%PLUGINS_DIR%" (
    mkdir "%PLUGINS_DIR%" 2>nul
)

if exist "%HOST_DLL%" (
    copy /Y "%HOST_DLL%" "%PLUGINS_DIR%\IronXNestCommand.dll" >nul
    if exist "%CORE_DLL%" copy /Y "%CORE_DLL%" "%PLUGINS_DIR%\IronXNestCommand.Core.dll" >nul
    if exist "%SOURCE_DIR%tools\extracted_coop\IronNestCoop.Core.dll" (
        copy /Y "%SOURCE_DIR%tools\extracted_coop\IronNestCoop.Core.dll" "%PLUGINS_DIR%\IronNestCoop.Core.dll" >nul
    )
    echo ==============================================================================
    echo   [ERFOLG] IronXNestCommand wurde in BepInEx/plugins/ installiert!
    echo   Pfad: %PLUGINS_DIR%\IronXNestCommand.dll
    echo ==============================================================================
    echo.
    echo Du kannst das Spiel jetzt einfach ueber Steam starten.
    echo Im Spiel oeffnet [F8] das Co-op Lobby-Menue.
    echo.
) else (
    echo [FEHLER] Mod-DLL nicht gefunden. Bitte zuerst Build-And-Deploy.bat ausfuehren!
)

pause
exit /b 0
