using Magnetar_Client.Utils;
using MelonLoader.Utils;
using System.IO;
using System;
using static Magnetar_Client.Utils.Magnetar_Logger;
using UnityEngine;

namespace Magnetar_Client.Api;

internal static class PathsManager
{
    static string _cachedModsDir;
    internal static string ModsDir
    {
        get
        {
            if (!string.IsNullOrEmpty(_cachedModsDir) && Directory.Exists(_cachedModsDir))
            {
                return _cachedModsDir;
            }

            string targetDir = null;

#if ANDROID
            string mobilePlugins = "/storage/emulated/0/PVZRH_Launcher/com.LanPiaoPiao.PlantsVsZombiesRH/BepInEx/plugins";

            try
            {
                if (Directory.Exists("/storage/emulated/0/PVZRH_Launcher/com.LanPiaoPiao.PlantsVsZombiesRH/BepInEx"))
                {
                    targetDir = mobilePlugins;
                }
            }
            catch { }

            if (string.IsNullOrEmpty(targetDir))
            {
                try
                {
                    if (!string.IsNullOrEmpty(Paths.PluginPath)) targetDir = Paths.PluginPath;
                }
                catch { }
            }

            if (string.IsNullOrEmpty(targetDir))
            {
                targetDir = Path.Combine(Application.persistentDataPath, "Magnetar", "Plugins");
            }
#elif MELONLOADER || RELEASE_MELON
            targetDir = MelonEnvironment.ModsDirectory;
#elif BEPINEX || RELEASE_BEPINEX
            targetDir = Paths.PluginPath;
#else
            targetDir = Path.Combine(Application.persistentDataPath, "Magnetar", "Plugins");
#endif

            try
            {
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                _cachedModsDir = targetDir;
            }
            catch
            {
                _cachedModsDir = ConfigDir;
                return _cachedModsDir;
            }

            return targetDir;
        }
    }

    internal static string AddonsDir => Path.Combine(ModsDir, "Magnetar Addon");

    private static string _cachedConfigDir;

    internal static string ConfigDir
    {
        get
        {
            if (!string.IsNullOrEmpty(_cachedConfigDir) && Directory.Exists(_cachedConfigDir))
            {
                return _cachedConfigDir;
            }

            string targetDir = null;

#if ANDROID
            // 1. Mobile BepInEx path
            string mobileBepInExConfig = "/storage/emulated/0/PVZRH_Launcher/com.LanPiaoPiao.PlantsVsZombiesRH/BepInEx/config";

            try
            {
                if (Directory.Exists("/storage/emulated/0/PVZRH_Launcher/com.LanPiaoPiao.PlantsVsZombiesRH/BepInEx"))
                {
                    targetDir = mobileBepInExConfig;
                }
            }
            catch { }

            // 2. Fallback to BepInEx Paths.ConfigPath if available
            if (string.IsNullOrEmpty(targetDir))
            {
                try
                {
#if BEPINEX || RELEASE_BEPINEX
                    if (!string.IsNullOrEmpty(Paths.ConfigPath))
                    {
                        targetDir = Paths.ConfigPath;
                    }
#endif
                }
                catch { }
            }

            // 3. Fallback to internal app sandbox storage
            if (string.IsNullOrEmpty(targetDir))
            {
                targetDir = Path.Combine(Application.persistentDataPath, "Magnetar", "Config");
            }
#elif MELONLOADER || RELEASE_MELON
            targetDir = MelonEnvironment.UserDataDirectory;
#elif BEPINEX || RELEASE_BEPINEX
            targetDir = Paths.ConfigPath;
#else
            targetDir = Path.Combine(Application.persistentDataPath, "Magnetar", "Config");
#endif

            try
            {
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
                _cachedConfigDir = targetDir;
            }
            catch (Exception ex)
            {
                AutoSaveLogger.Error($"[ProfileManager] Failed to create config dir '{targetDir}': {ex.Message}");
                _cachedConfigDir = Application.persistentDataPath;
                return _cachedConfigDir;
            }

            return targetDir;
        }
    }

    internal static string GetProfilePath(string profileName)
    {
        string safeName = string.Join("_", profileName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(ConfigDir, $"Magnetar_{safeName}.json");
    }

    internal static string TranslationRootDir => Path.Combine(ModsDir, "Magnetar Translation");

    internal static string GetLanguageDir(string targetLanguage) => Path.Combine(TranslationRootDir, targetLanguage);

    internal static string DataDir => Path.Combine(ModsDir, "Magnetar Data");

}
