@echo off
rem Double-click: local server and two game windows. See play-local.ps1.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0play-local.ps1" %*
if errorlevel 1 pause
