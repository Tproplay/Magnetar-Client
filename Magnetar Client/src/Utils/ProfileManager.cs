using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Magnetar_Client.Api.PathsManager;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils;

/// <summary>
/// Manages the profile creation and handeling.
/// </summary>
public static class ProfileManager
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public const string DefaultProfile = "Default";
    public static List<string> Profiles { get; private set; } = new() { ProfileManager.DefaultProfile };
    public static string CurrentProfile { get; set; } = "Default";
    public const string SaveFilePrefix = "Magnetar_";

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

    /// <summary>
    /// Reloads the profile files from the Profile's directory
    /// </summary>
    public static void RefreshProfiles()
    {
        Profiles.Clear();
        Profiles.Add(ProfileManager.DefaultProfile);

        try
        {
            if (Directory.Exists(ConfigDir))
            {
                var files = Directory.GetFiles(ConfigDir, SaveFilePrefix + "*.json");
                foreach (var file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName.StartsWith(SaveFilePrefix))
                    {
                        string profileName = fileName[SaveFilePrefix.Length..];

                        if (!string.IsNullOrWhiteSpace(profileName) &&
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

        Api.Actions.Invoke_OnProfileChanged();
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
            ProfileManager.CurrentProfile = ProfileManager.DefaultProfile;
            SaveCurrentProfileToPreferences(ProfileManager.DefaultProfile);
            SaveLoad.Load();
            Api.Actions.Invoke_OnProfileChanged();
        }

        AutoSaveLogger.Warning($"Deleted profile: '{profileName}'");
        return true;
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}