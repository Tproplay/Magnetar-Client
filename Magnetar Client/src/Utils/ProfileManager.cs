using Magnetar_Client.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Magnetar_Client.Api.PathsManager;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils;

public static class ProfileManager
{
    public const string DefaultProfile = "Default";
    public static List<string> Profiles { get; private set; } = new() { ProfileManager.DefaultProfile };
    public static string CurrentProfile { get; set; } = "Default";

    public static void Init()
    {
        RefreshProfiles();

        if (!Profiles.Contains(ProfileManager.DefaultProfile, StringComparer.OrdinalIgnoreCase))
        {
            Profiles.Insert(0, ProfileManager.DefaultProfile);
        }

        string savedProfile = ProfileManager.DefaultProfile;

        if (PreferenceManager.CurrentProfileEntry != null && !string.IsNullOrWhiteSpace(PreferenceManager.CurrentProfileEntry.Value))
        {
            savedProfile = PreferenceManager.CurrentProfileEntry.Value;
        }

        if (Profiles.Contains(savedProfile, StringComparer.OrdinalIgnoreCase))
        {
            ProfileManager.CurrentProfile = savedProfile;
        }
        else
        {
            ProfileManager.CurrentProfile = ProfileManager.DefaultProfile;
            SaveCurrentProfileToPreferences(ProfileManager.DefaultProfile);
        }

        AutoSaveLogger.Msg($"Profile Manager initialized. Directory: '{ConfigDir}', Active profile: '{ProfileManager.CurrentProfile}'");
    }

    private static void SaveCurrentProfileToPreferences(string profileName)
    {
        try
        {
            if (PreferenceManager.CurrentProfileEntry != null)
            {
                PreferenceManager.CurrentProfileEntry.Value = profileName;
                PreferenceManager.CurrentProfileEntry.Save();
            }
        }
        catch (Exception ex)
        {
            AutoSaveLogger.Error($"Failed to persist profile preference: {ex.Message}");
        }
    }

    public static void RefreshProfiles()
    {
        Profiles.Clear();
        Profiles.Add(ProfileManager.DefaultProfile);

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
                            !string.Equals(profileName, "ClientConfig", StringComparison.OrdinalIgnoreCase) &&
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

    public static bool CreateProfile(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName)) return false;
        profileName = profileName.Trim();

        if (Profiles.Contains(profileName, StringComparer.OrdinalIgnoreCase))
            return false;

        SaveLoad.Save(force: true);
        SaveLoad.ResetAll();

        Profiles.Add(profileName);
        ProfileManager.CurrentProfile = profileName;

        SaveCurrentProfileToPreferences(profileName);
        SaveLoad.Save(force: true);
        ClientConfig.showgui = true;

        AutoSaveLogger.Msg($"Created and loaded new profile: '{profileName}'");
        return true;
    }

    public static void SwitchProfile(string targetProfile)
    {
        if (string.IsNullOrWhiteSpace(targetProfile) || string.Equals(ProfileManager.CurrentProfile, targetProfile, StringComparison.OrdinalIgnoreCase)) return;

        SaveLoad.Save(force: true);
        SaveLoad.ResetAll();

        if (!Profiles.Contains(targetProfile, StringComparer.OrdinalIgnoreCase))
        {
            Profiles.Add(targetProfile);
        }
        ProfileManager.CurrentProfile = targetProfile;
        SaveCurrentProfileToPreferences(targetProfile);

        SaveLoad.Load();
        ClientConfig.showgui = true;

        AutoSaveLogger.Msg($"Switched active profile to: '{ProfileManager.CurrentProfile}'");
    }

    public static bool DeleteProfile(string profileName)
    {
        if (string.Equals(profileName, ProfileManager.DefaultProfile, StringComparison.OrdinalIgnoreCase))
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

        if (string.Equals(ProfileManager.CurrentProfile, profileName, StringComparison.OrdinalIgnoreCase))
        {
            ResetAllModulesToDefault();
            ProfileManager.CurrentProfile = ProfileManager.DefaultProfile;
            SaveCurrentProfileToPreferences(ProfileManager.DefaultProfile);
            SaveLoad.Load();
        }

        AutoSaveLogger.Warning($"Deleted profile: '{profileName}'");
        return true;
    }
}