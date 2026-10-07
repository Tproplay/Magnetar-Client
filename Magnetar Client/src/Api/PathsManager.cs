using Magnetar_Client.Utils;
using MelonLoader.Utils;
using System;
using System.IO;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Api;

public static class PathsManager
{
    static string _cachedModsDir;
    public static string ModsDir
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

    public static string AddonsDir => GetAddonsDir();

    static string GetAddonsDir()
    {
        var path = Path.Combine(ConfigDir, "Magnetar Profiles");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }

    private static string _cachedConfigDir;

    public static string ConfigDir
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

    public static string ProfilesDir => GetProfilesDir();
    static string GetProfilesDir()
    {
        var path = Path.Combine(ConfigDir, "Magnetar Profiles");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }

    public static string GetProfilePath(string profileName)
    {
        string safeName = string.Join("_", profileName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(ProfilesDir, $"Magnetar_{safeName}.json");
    }
    public static string GetSafeProfilePath(string profileName)
    {
        string path = GetProfilePath(profileName);
        return path;
    }

    public static string TranslationRootDir => GetSafeTranslationRootDir();

    static string GetSafeTranslationRootDir()
    {
        var path = Path.Combine(ModsDir, "Magnetar Translation");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }

    public static string GetLanguageDir(string targetLanguage) => Path.Combine(TranslationRootDir, targetLanguage);

    public static string DataDir => GetSafeDataDir();
    static string GetSafeDataDir()
    {
        var path = Path.Combine(ModsDir, "Magnetar Data");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }

    public static string TextureDataPath => GetSafeTexturePath();
    static string GetSafeTexturePath()
    {
        if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);
        return Path.Combine(DataDir, "TextureData.json");
    }

    public static string ThemesDir => GetSafeThemesDir();
    static string GetSafeThemesDir()
    {
        var path = Path.Combine(ModsDir, "Magnetar Data", "Themes");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }
}
