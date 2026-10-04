using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Magnetar_Client.Core;
using static Magnetar_Client.Utils.Magnetar_Logger;
using static Magnetar_Client.Api.MagnetarApi;
using static Magnetar_Client.Preferences;

#if MELONLOADER || RELEASE_MELON
using MelonLoader;
using MelonLoader.Utils;
#elif BEPINEX || RELEASE_BEPINEX
using BepInEx;
using BepInEx.Configuration;
#endif

namespace Magnetar_Client.Utils;

public static class ProfileManager
{

    /// <summary>
    /// List of all detected profile names.
    /// </summary>
    public static List<string> Profiles { get; private set; } = new List<string> { Config.DefaultProfile };



    public static void Init()
    {
        _ = ConfigDir;

        RefreshProfiles();

        if (!Profiles.Contains(Config.DefaultProfile, StringComparer.OrdinalIgnoreCase))
        {
            Profiles.Insert(0, Config.DefaultProfile);
        }

        string savedProfile = Config.DefaultProfile;

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
            Config.CurrentProfile = Config.DefaultProfile;
            SaveCurrentProfileToPreferences(Config.DefaultProfile);
        }

        AutoSaveLogger.Msg($"Profile Manager initialized. Directory: '{ConfigDir}', Active profile: '{Config.CurrentProfile}'");
    }

    private static void SaveCurrentProfileToPreferences(string profileName)
    {
        try
        {
#if MELONLOADER || RELEASE_MELON
            if (prefCurrentProfile != null)
            {
                prefCurrentProfile.Value = profileName;
                Preferences.MagnetarCategory.SaveToFile();
            }
#elif BEPINEX || RELEASE_BEPINEX
            if (prefCurrentProfile != null && Preferences.BepInExConfig != null)
            {
                prefCurrentProfile.Value = profileName;
                Preferences.BepInExConfig.Save();
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
        Profiles.Add(Config.DefaultProfile);

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
    /// Disables all modules and resets every setting polymorphically to default values.
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
                mod.KeyBind.Reset();
            }

            mod.HoldMode = mod.defaultHoldMode;
            if (mod.Active != mod.defaultActive)
            {
                mod.Toggle();
            }

            if (mod.Settings != null)
            {
                foreach (var setting in mod.Settings)
                {
                    if (setting == null) continue;

                    try
                    {
                        setting.Reset();
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

        SaveCurrentProfileToPreferences(profileName);
        SaveLoad.Save(force: true);
        Config.showgui = true;

        AutoSaveLogger.Msg($"Created and loaded new profile: '{profileName}'");
        return true;
    }

    /// <summary>
    /// Saves current profile, resets modules to clean defaults, and loads the target profile.
    /// </summary>
    public static void SwitchProfile(string targetProfile)
    {
        if (string.IsNullOrWhiteSpace(targetProfile) || string.Equals(Config.CurrentProfile, targetProfile, StringComparison.OrdinalIgnoreCase)) return;

        SaveLoad.Save(force: true);
        ResetAllModulesToDefault();

        if (!Profiles.Contains(targetProfile, StringComparer.OrdinalIgnoreCase))
        {
            Profiles.Add(targetProfile);
        }

        Config.CurrentProfile = targetProfile;
        SaveCurrentProfileToPreferences(targetProfile);

        SaveLoad.Load();
        Config.showgui = true;

        AutoSaveLogger.Msg($"Switched active profile to: '{Config.CurrentProfile}'");
    }

    /// <summary>
    /// Deletes a profile file and removes it from the list. Fallbacks to Default if active.
    /// </summary>
    public static bool DeleteProfile(string profileName)
    {
        if (string.Equals(profileName, Config.DefaultProfile, StringComparison.OrdinalIgnoreCase))
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
            ResetAllModulesToDefault();
            Config.CurrentProfile = Config.DefaultProfile;
            SaveCurrentProfileToPreferences(Config.DefaultProfile);
            SaveLoad.Load();
        }

        AutoSaveLogger.Warning($"Deleted profile: '{profileName}'");
        return true;
    }
}