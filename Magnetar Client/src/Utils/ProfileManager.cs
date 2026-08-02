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

        public static string ConfigDir =>
#if MELONLOADER || RELEASE_MELON
            MelonEnvironment.UserDataDirectory;
#elif BEPINEX || RELEASE_BEPINEX
            Paths.ConfigPath;
#endif

        public static string GetProfilePath(string profileName)
        {
            string safeName = string.Join("_", profileName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(ConfigDir, $"Magnetar_{safeName}.json");
        }

        public static void Init()
        {
#if MELONLOADER || RELEASE_MELON
            prefCurrentProfile = Prefrences.MagnetarCategory.CreateEntry("CurrentProfile", DefaultProfile, "Active Profile");
#elif BEPINEX || RELEASE_BEPINEX
            prefCurrentProfile = Prefrences.BepInExConfig.Bind("ProfileManager", "CurrentProfile", DefaultProfile, "Active Profile");
#endif

            RefreshProfiles();

            // Migrate legacy config if needed
            string legacyPath = Path.Combine(ConfigDir, "Magnetar_Config.json");
            string defaultPath = GetProfilePath(DefaultProfile);

            if (File.Exists(legacyPath) && !File.Exists(defaultPath))
            {
                try
                {
                    File.Move(legacyPath, defaultPath);
                    AutoSaveLogger.Msg($"Migrated legacy config to '{defaultPath}'");
                }
                catch (Exception ex)
                {
                    AutoSaveLogger.Error($"Failed to migrate legacy config: {ex.Message}");
                }
            }

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

            AutoSaveLogger.Msg($"Profile Manager initialized. Active profile: '{Config.CurrentProfile}'");
        }

        private static void SaveCurrentProfileToPrefrences(string profileName)
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

        /// <summary>
        /// Scans the config directory for all Magnetar_{profile}.json save files.
        /// </summary>
        public static void RefreshProfiles()
        {
            Profiles.Clear();
            Profiles.Add(DefaultProfile);

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