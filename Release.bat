@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

set "PYTHON_EXE="

if exist "%LocalAppData%\Programs\Python\Python312\python.exe" set "PYTHON_EXE=%LocalAppData%\Programs\Python\Python312\python.exe"& goto :found
if exist "%LocalAppData%\Programs\Python\Python311\python.exe" set "PYTHON_EXE=%LocalAppData%\Programs\Python\Python311\python.exe"& goto :found
if exist "%LocalAppData%\Programs\Python\Python310\python.exe" set "PYTHON_EXE=%LocalAppData%\Programs\Python\Python310\python.exe"& goto :found

where py >nul 2>nul
if %errorlevel% equ 0 set "PYTHON_EXE=py"& goto :found

where python >nul 2>nul
if %errorlevel% equ 0 set "PYTHON_EXE=python"& goto :found

:found
if "%PYTHON_EXE%"=="" (
    echo [ERROR] Python 3 was not found on your system.
    echo Please install Python 3 or add it to your PATH.
    pause
    exit /b 1
)

echo.
echo ============================================================
echo   Magnetar Client Release Pipeline
echo   Make sure you have:
echo     1. Run Build_All.bat first
echo     2. Filled in Changelog.md
echo     3. Authenticated: gh auth login
echo ============================================================
echo.

"%PYTHON_EXE%" "%~dp0Scripts\release.py" %*
set "EXIT_CODE=%errorlevel%"

if %EXIT_CODE% neq 0 (
    echo.
    echo [ERROR] Release script failed with exit code %EXIT_CODE%.
)

if /i not "%~1"=="nopause" (
    pause
)

exit /b %EXIT_CODE%
