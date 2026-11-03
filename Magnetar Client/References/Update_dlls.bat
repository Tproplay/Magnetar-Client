@echo off

py update_dll.py "C:\Users\Tproplay\Games\Pvz\Fusion RH\Melonloader 4.1\MelonLoader\Il2CppAssemblies" "C:\Users\Tproplay\Desktop\Visual studio\Magnetar Client\Magnetar Client\References\Melonloader\Il2Cpp"

py update_dll.py "C:\Users\Tproplay\Games\Pvz\Fusion RH\Melonloader 4.1\MelonLoader\net6" "C:\Users\Tproplay\Desktop\Visual studio\Magnetar Client\Magnetar Client\References\Melonloader"

py update_dll.py "C:\Users\Tproplay\Games\Pvz\Fusion RH\BepInEx 4.1\BepInEx\core" "C:\Users\Tproplay\Desktop\Visual studio\Magnetar Client\Magnetar Client\References\Bepinex"

py update_dll.py "C:\Users\Tproplay\Games\Pvz\Fusion RH\BepInEx 4.1\BepInEx\interop" "C:\Users\Tproplay\Desktop\Visual studio\Magnetar Client\Magnetar Client\References\Bepinex\Il2Cpp"

echo All updates completed.
pause
