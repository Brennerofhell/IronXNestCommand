@echo off
setlocal enabledelayedexpansion
title IronXNestCommand // Erweiterte Deinstallation

color 0C
echo ==============================================================================
echo   IRON X NEST COMMAND // DEINSTALLATION ^& BEREINIGUNG
echo ==============================================================================
echo.

set "SOURCE_DIR=%~dp0"
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
if exist "G:\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator\Iron Nest Heavy Turret Simulator.exe" (
    set "GAME_PATH=G:\SteamLibrary\steamapps\common\Iron Nest Heavy Turret Simulator"
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

set "REMOVED=0"

:: 1. BepInEx Plugins entfernen
if exist "%GAME_PATH%\BepInEx\plugins\IronXNestCommand.dll" (
    del /F /Q "%GAME_PATH%\BepInEx\plugins\IronXNestCommand.dll" >nul 2>nul
    echo  [-] BepInEx Plugin entfernt: IronXNestCommand.dll
    set /a REMOVED+=1
)
if exist "%GAME_PATH%\BepInEx\plugins\IronXNestCommand.Core.dll" (
    del /F /Q "%GAME_PATH%\BepInEx\plugins\IronXNestCommand.Core.dll" >nul 2>nul
    echo  [-] BepInEx Core entfernt: IronXNestCommand.Core.dll
    set /a REMOVED+=1
)

:: 2. MelonLoader Mods entfernen
if exist "%GAME_PATH%\Mods\IronXNestCommand.dll" (
    del /F /Q "%GAME_PATH%\Mods\IronXNestCommand.dll" >nul 2>nul
    echo  [-] MelonLoader Mod entfernt: IronXNestCommand.dll
    set /a REMOVED+=1
)

:: 3. UserData / Configs optional abfragen
if exist "%GAME_PATH%\UserData\IronXNestCommand" (
    echo.
    echo Moechtest du auch gespeicherte Einstellungen und Daten loeschen?
    echo Pfad: "%GAME_PATH%\UserData\IronXNestCommand"
    set /p "DEL_USERDATA=[J/N] > "
    if /I "!DEL_USERDATA!"=="J" (
        rmdir /S /Q "%GAME_PATH%\UserData\IronXNestCommand" >nul 2>nul
        echo  [-] UserData/IronXNestCommand wurde vollstaendig geloescht.
        set /a REMOVED+=1
    ) else (
        echo  [i] Einstellungen und Speicherdaten wurden beibehalten.
    )
)

echo.
echo ==============================================================================
if %REMOVED% GTR 0 (
    echo   [ERFOLG] Deinstallation abgeschlossen (%REMOVED% Elemente bereinigt).
) else (
    echo   [INFO] Keine installierten IronXNestCommand Mod-Dateien gefunden.
)
echo ==============================================================================
echo.
pause
exit /b 0
