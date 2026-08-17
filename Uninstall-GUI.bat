@echo off
setlocal
title Iron Nest // Mod Manager ^& Uninstaller GUI
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\ModManagerGUI.ps1"
if %ERRORLEVEL% NEQ 0 (
    pause
)
