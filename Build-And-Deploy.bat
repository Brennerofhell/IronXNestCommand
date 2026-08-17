@echo off
setlocal enabledelayedexpansion
title IronXNestCommand - Auto Build & Deploy

echo ========================================================
echo         IRON X NEST COMMAND - BUILD & DEPLOY
echo ========================================================
echo.

set "GAME_DIR=C:\Program Files (x86)\Steam\steamapps\common\Iron Nest Heavy Turret Simulator"
set "MODS_DIR=%GAME_DIR%\Mods"
set "PROJECT_FILE=%~dp0IronXNestCommand.MelonLoader\IronXNestCommand.csproj"
set "OUTPUT_DLL=%~dp0IronXNestCommand.MelonLoader\bin\Release\net6.0\IronXNestCommand.dll"

:: 1. Check if Game directory exists
if not exist "%GAME_DIR%" (
    echo [FEHLER] Spielverzeichnis nicht gefunden unter:
    echo "%GAME_DIR%"
    echo Bitte passe den Pfad in dieser Batch-Datei an.
    goto :error
)

:: Ensure Mods folder exists
if not exist "%MODS_DIR%" (
    mkdir "%MODS_DIR%"
)

:: 2. Locate dotnet.exe
set "DOTNET_CMD="
where dotnet >nul 2>nul
if %errorlevel% equ 0 (
    set "DOTNET_CMD=dotnet"
) else if exist "C:\Program Files\dotnet\dotnet.exe" (
    set "DOTNET_CMD=C:\Program Files\dotnet\dotnet.exe"
) else if exist "%LOCALAPPDATA%\dotnet\dotnet.exe" (
    set "DOTNET_CMD=%LOCALAPPDATA%\dotnet\dotnet.exe"
)

if "%DOTNET_CMD%"=="" (
    echo [FEHLER] .NET 6 SDK wurde nicht gefunden!
    echo.
    echo Bitte lade das kostenlose .NET 6 SDK von Microsoft herunter:
    echo https://dotnet.microsoft.com/download/dotnet/6.0
    echo.
    goto :error
)

echo [INFO] Nutze dotnet: %DOTNET_CMD%
echo [INFO] Kompiliere IronXNestCommand (Release)...
echo.

"%DOTNET_CMD%" build "%PROJECT_FILE%" -c Release -p:GameFolder="%GAME_DIR%"

if %errorlevel% neq 0 (
    echo.
    echo [FEHLER] Kompilierung fehlgeschlagen!
    goto :error
)

echo.
echo [INFO] Kopiere DLL in den Mods-Ordner...

if exist "%OUTPUT_DLL%" (
    copy /Y "%OUTPUT_DLL%" "%MODS_DIR%\IronXNestCommand.dll" >nul
) else (
    :: Fallback search if path differs
    if exist "%~dp0IronXNestCommand.MelonLoader\bin\Release\IronXNestCommand.dll" (
        copy /Y "%~dp0IronXNestCommand.MelonLoader\bin\Release\IronXNestCommand.dll" "%MODS_DIR%\IronXNestCommand.dll" >nul
    ) else (
        echo [FEHLER] Konnte die erstellte IronXNestCommand.dll nicht finden!
        goto :error
    )
)

echo.
echo ========================================================
echo   [ERFOLG] Die Mod wurde erfolgreich gebaut und installiert!
echo   Ziel: %MODS_DIR%\IronXNestCommand.dll
echo ========================================================
echo.
echo Du kannst das Spiel jetzt einfach ueber Steam starten!
echo Druecke im Spiel [F8] fuer das Overlay.
echo.
pause
exit /b 0

:error
echo.
echo ========================================================
echo   Der Vorgang wurde mit Fehlern abgebrochen.
echo ========================================================
echo.
pause
exit /b 1
