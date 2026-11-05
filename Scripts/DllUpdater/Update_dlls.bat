@echo off
pushd "%~dp0"

set "BASE_REF=..\..\References"

py update_dll.py "C:\Users\Tproplay\Games\Pvz\Fusion RH\Melonloader 4.1\MelonLoader\Il2CppAssemblies" "%BASE_REF%\Melonloader\Il2Cpp"

py update_dll.py "C:\Users\Tproplay\Games\Pvz\Fusion RH\Melonloader 4.1\MelonLoader\net6" "%BASE_REF%\Melonloader"

py update_dll.py "C:\Users\Tproplay\Games\Pvz\Fusion RH\BepInEx 4.1\BepInEx\core" "%BASE_REF%\Bepinex"

py update_dll.py "C:\Users\Tproplay\Games\Pvz\Fusion RH\BepInEx 4.1\BepInEx\interop" "%BASE_REF%\Bepinex\Il2Cpp"

echo All updates completed.
popd
pause
