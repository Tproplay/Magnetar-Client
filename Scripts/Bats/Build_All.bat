@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0..\.."

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

"%PYTHON_EXE%" "%~dp0..\build_and_pack.py" %*
set "EXIT_CODE=%errorlevel%"

if %EXIT_CODE% neq 0 (
    echo.
    echo [ERROR] Build and packaging script failed with exit code %EXIT_CODE%.
)

if /i not "%~1"=="nopause" (
    pause
)

exit /b %EXIT_CODE%
