using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Magnetar_Client.Core;
using Magnetar_Client.Modules;
using static Magnetar_Client.Utils.Magnetar_Logger;

#if MELONLOADER || RELEASE_MELON
using MelonLoader;
using MelonLoader.Utils;
#elif BEPINEX || RELEASE_BEPINEX
using BepInEx;
using BepInEx.Configuration;
#endif

namespace Magnetar_Client.Utils
{
    public static class ProfileManager
    {
        public const string DefaultProfile = "Default";

        /// <summary>
        /// List of all detected profile names.
        /// </summary>
        public static List<string> Profiles { get; private set; } = new List<string> { DefaultProfile };

#if MELONLOADER || RELEASE_MELON
        private static MelonPreferences_Entry<string> prefCurrentProfile;
#elif BEPINEX || RELEASE_BEPINEX
        private static ConfigEntry<string> prefCurrentProfile;
#endif

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

        public static string GetProfilePath(string profileName)
        {
            string safeName = string.Join("_", profileName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(ConfigDir, $"Magnetar_{safeName}.json");
        }

        public static void Init()
        {
            // Resolve and create config directory before initialization
            _ = ConfigDir;

#if MELONLOADER || RELEASE_MELON
            prefCurrentProfile = Prefrences.MagnetarCategory.CreateEntry("CurrentProfile", DefaultProfile, "Active Profile");
#elif BEPINEX || RELEASE_BEPINEX
            try
            {
                if (Prefrences.BepInExConfig != null)
                {
                    prefCurrentProfile = Prefrences.BepInExConfig.Bind("ProfileManager", "CurrentProfile", DefaultProfile, "Active Profile");
                }
            }
            catch (Exception ex)
            {
                AutoSaveLogger.Error($"Failed to bind BepInEx preference: {ex.Message}");
            }
#endif

            RefreshProfiles();

            if (!Profiles.Contains(DefaultProfile, StringComparer.OrdinalIgnoreCase))
            {
                Profiles.Insert(0, DefaultProfile);
            }

            string savedProfile = DefaultProfile;

            if (prefCurrentProfile != null && !string.IsNullOrWhiteSpace(prefCurrentProfile.Value))
            {
                savedProfile = prefCurrentProfile.Value;
            }

            if (Profiles.Contains(savedProfile, StringComparer.OrdinalIgnoreCase))
            {
                Config.CurrentProfile = savedProfile;
            }
            else
            {
                Config.CurrentProfile = DefaultProfile;
                SaveCurrentProfileToPrefrences(DefaultProfile);
            }

            AutoSaveLogger.Msg($"Profile Manager initialized. Directory: '{ConfigDir}', Active profile: '{Config.CurrentProfile}'");
        }

        private static void SaveCurrentProfileToPrefrences(string profileName)
        {
            try
            {
#if MELONLOADER || RELEASE_MELON
                if (prefCurrentProfile != null)
                {
                    prefCurrentProfile.Value = profileName;
                    Prefrences.MagnetarCategory.SaveToFile();
                }
#elif BEPINEX || RELEASE_BEPINEX
                if (prefCurrentProfile != null && Prefrences.BepInExConfig != null)
                {
                    prefCurrentProfile.Value = profileName;
                    Prefrences.BepInExConfig.Save();
                }
#endif
            }
            catch (Exception ex)
            {
                AutoSaveLogger.Error($"Failed to persist profile preference: {ex.Message}");
            }
        }

        /// <summary>
        /// Scans the config directory for all Magnetar_{profile}.json save files.
        /// </summary>
        public static void RefreshProfiles()
        {
            Profiles.Clear();
            Profiles.Add(DefaultProfile);

            try
            {
                if (Directory.Exists(ConfigDir))
                {
                    var files = Directory.GetFiles(ConfigDir, "Magnetar_*.json");
                    foreach (var file in files)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        if (fileName.StartsWith("Magnetar_"))
                        {
                            string profileName = fileName.Substring("Magnetar_".Length);

                            if (!string.IsNullOrWhiteSpace(profileName) &&
                                !string.Equals(profileName, "Config", StringComparison.OrdinalIgnoreCase) &&
                                !Profiles.Contains(profileName, StringComparer.OrdinalIgnoreCase))
                            {
                                Profiles.Add(profileName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AutoSaveLogger.Error($"Error scanning profiles in '{ConfigDir}': {ex.Message}");
            }
        }

        public static void DisableAllModules()
        {
            if (ModuleManager.Modules == null) return;

            foreach (var mod in ModuleManager.Modules)
            {
                if (mod == null) continue;

                if (mod.Active)
                {
                    mod.Active = false;
                    try
                    {
                        mod.OnDisable();
                    }
                    catch (Exception ex)
                    {
                        AutoSaveLogger.Error($"Error disabling module '{mod.Name}': {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Disables all modules and resets all settings across every module to default values.
        /// </summary>
        public static void ResetAllModulesToDefault()
        {
            DisableAllModules();

            if (ModuleManager.Modules == null) return;

            foreach (var mod in ModuleManager.Modules)
            {
                if (mod == null) continue;

                if (mod.KeyBind != null)
                {
                    mod.KeyBind.BindKeys = mod.KeyBind.DefaultKeys != null
                        ? new List<KeyCode>(mod.KeyBind.DefaultKeys)
                        : new List<KeyCode>();
                }
                mod.HoldMode = false;

                if (mod.Settings != null)
                {
                    foreach (var setting in mod.Settings)
                    {
                        if (setting == null) continue;

                        try
                        {
                            if (setting is BoolSetting b) b.Value = b.DefaultValue;
                            else if (setting is FloatSetting f) f.Value = f.DefaultValue;
                            else if (setting is IntSetting i) i.Value = i.DefaultValue;
                            else if (setting is StringSetting s) s.Value = s.DefaultValue;
                            else if (setting is SelectSetting sel) sel.Value = sel.DefaultValue;
                            else if (setting is BindSetting bind)
                            {
                                bind.BindKeys = bind.DefaultKeys != null
                                    ? new List<KeyCode>(bind.DefaultKeys)
                                    : new List<KeyCode>();
                            }
                            else if (setting is MultiSelectSetting ms)
                            {
                                ms.SelectedValues.Clear();
                            }
                            else if (setting is CategorySetting cat)
                            {
                                cat.IsExpanded = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            AutoSaveLogger.Error($"Error resetting setting '{setting.Name}' in module '{mod.Name}': {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new profile, saves current profile state, resets modules to default, and updates preferences.
        /// </summary>
        public static bool CreateProfile(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return false;

            profileName = profileName.Trim();

            if (Profiles.Contains(profileName, StringComparer.OrdinalIgnoreCase))
            {
                AutoSaveLogger.Error($"Profile '{profileName}' already exists.");
                return false;
            }

            SaveLoad.Save(force: true);
            ResetAllModulesToDefault();

            Profiles.Add(profileName);
            Config.CurrentProfile = profileName;

            SaveCurrentProfileToPrefrences(profileName);
            SaveLoad.Save(force: true);
            Config.showgui = true;

            AutoSaveLogger.Msg($"Created and loaded new profile: '{profileName}'");
            return true;
        }

        /// <summary>
        /// Saves current configuration, disables active modules, updates preferences, and loads target profile.
        /// </summary>
        public static void SwitchProfile(string targetProfile)
        {
            if (string.IsNullOrWhiteSpace(targetProfile) || string.Equals(Config.CurrentProfile, targetProfile, StringComparison.OrdinalIgnoreCase)) return;

            SaveLoad.Save(force: true);
            DisableAllModules();

            if (!Profiles.Contains(targetProfile, StringComparer.OrdinalIgnoreCase))
            {
                Profiles.Add(targetProfile);
            }

            Config.CurrentProfile = targetProfile;
            SaveCurrentProfileToPrefrences(targetProfile);

            SaveLoad.Load();
            Config.showgui = true;

            AutoSaveLogger.Msg($"Switched active profile to: '{Config.CurrentProfile}'");
        }

        /// <summary>
        /// Deletes a profile file and removes it from the list. Fallbacks to Default if active.
        /// </summary>
        public static bool DeleteProfile(string profileName)
        {
            if (string.Equals(profileName, DefaultProfile, StringComparison.OrdinalIgnoreCase))
            {
                AutoSaveLogger.Error("Cannot delete the Default profile.");
                return false;
            }

            string targetPath = GetProfilePath(profileName);

            if (File.Exists(targetPath))
            {
                try
                {
                    File.Delete(targetPath);
                }
                catch (Exception ex)
                {
                    AutoSaveLogger.Error($"Failed to delete profile file '{targetPath}': {ex.Message}");
                    return false;
                }
            }

            Profiles.RemoveAll(p => string.Equals(p, profileName, StringComparison.OrdinalIgnoreCase));

            if (string.Equals(Config.CurrentProfile, profileName, StringComparison.OrdinalIgnoreCase))
            {
                DisableAllModules();
                Config.CurrentProfile = DefaultProfile;
                SaveCurrentProfileToPrefrences(DefaultProfile);
                SaveLoad.Load();
            }

            AutoSaveLogger.Warning($"Deleted profile: '{profileName}'");
            return true;
        }
    }
}