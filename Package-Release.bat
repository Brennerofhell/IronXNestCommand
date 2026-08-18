@echo off
setlocal
title IronXNestCommand // Release Packager

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Package-Release.ps1"

echo.
pause
