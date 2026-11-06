@echo off
setlocal enabledelayedexpansion
pushd "%~dp0"

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
    pause
    exit /b 1
)

set "BASE_REF=..\..\References"

"%PYTHON_EXE%" "%~dp0update_dll.py" "C:\Users\Tproplay\Games\Pvz\Fusion RH\Melonloader 4.1\MelonLoader\Il2CppAssemblies" "%BASE_REF%\Melonloader\Il2Cpp"

"%PYTHON_EXE%" "%~dp0update_dll.py" "C:\Users\Tproplay\Games\Pvz\Fusion RH\Melonloader 4.1\MelonLoader\net6" "%BASE_REF%\Melonloader"

"%PYTHON_EXE%" "%~dp0update_dll.py" "C:\Users\Tproplay\Games\Pvz\Fusion RH\BepInEx 4.1\BepInEx\core" "%BASE_REF%\Bepinex"

"%PYTHON_EXE%" "%~dp0update_dll.py" "C:\Users\Tproplay\Games\Pvz\Fusion RH\BepInEx 4.1\BepInEx\interop" "%BASE_REF%\Bepinex\Il2Cpp"

echo All updates completed.
popd
pause
