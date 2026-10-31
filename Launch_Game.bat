@echo off
title PvZ RH - Magnetar Launcher
cd /d "%~dp0"

set "EXE=PlantsVsZombiesRH.exe"
set "MODS=Mods"
set "DLL=%MODS%\Magnetar Client.dll"

if not exist "%EXE%" (
    echo %EXE% not found. Place this .bat in the game folder.
    pause
    exit /b 1
)

:: Close the game so files aren't locked
taskkill /F /IM "%EXE%" >nul 2>&1

if not exist "%MODS%" mkdir "%MODS%"

:: Apply updates: staged .update file, or a loose DLL next to this .bat
if exist "%DLL%.update" move /Y "%DLL%.update" "%DLL%" >nul && echo Applied staged update.
if exist "Magnetar Client.dll" move /Y "Magnetar Client.dll" "%DLL%" >nul && echo Installed new DLL.

:: Extract any downloaded update zip into Mods
for %%Z in (MagnetarClient*.zip update*.zip) do (
    echo Extracting %%Z...
    powershell -NoProfile -Command "Expand-Archive -LiteralPath '%%Z' -DestinationPath '%MODS%' -Force" && del /F /Q "%%Z"
)

if not exist "%DLL%" (
    echo Magnetar Client.dll is not in the Mods folder.
    echo Put it there ^(with 'Magnetar Data'^), or drop the MagnetarClient zip here and rerun.
    pause
    exit /b 1
)

start "" "%EXE%"
