using System;
using System.Collections.Generic;
using System.IO;
using Magnetar_Client.Api;
using Magnetar_Client.Utils;
using static Magnetar_Client.Utils.Magnetar_Logger;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using MelonLoader;
#elif BEPINEX || RELEASE_BEPINEX || ANDROID
using BepInEx.Configuration;
#endif

namespace Magnetar_Client;

public static class Preferences
{
    public interface IPreferenceEntry<T>
    {
        string Key { get; }
        T Value { get; set; }
        void Save();
    }

#if MELONLOADER || RELEASE_MELON
    public static MelonPreferences_Category MagnetarCategory { get; private set; }

    private class MelonEntryWrapper<T> : IPreferenceEntry<T>
    {
        private readonly MelonPreferences_Entry<T> _entry;
        public string Key => _entry.Identifier;
        public T Value
        {
            get => _entry.Value;
            set => _entry.Value = value;
        }

        public MelonEntryWrapper(MelonPreferences_Entry<T> entry) => _entry = entry;
        public void Save() => MagnetarCategory?.SaveToFile();
    }
#elif BEPINEX || RELEASE_BEPINEX || ANDROID
    public static ConfigFile BepInExConfig;

    private class BepInExEntryWrapper<T> : IPreferenceEntry<T>
    {
        private readonly ConfigEntry<T> _entry;
        public string Key => _entry.Definition.Key;
        public T Value
        {
            get => _entry.Value;
            set => _entry.Value = value;
        }

        public BepInExEntryWrapper(ConfigEntry<T> entry) => _entry = entry;
        public void Save() => BepInExConfig?.Save();
    }
#endif

    public static IPreferenceEntry<bool> ShowFloatingIconEntry { get; private set; }
    public static IPreferenceEntry<bool> ShowMobileButtonsEntry { get; private set; }
    public static IPreferenceEntry<string> CurrentProfileEntry { get; private set; }
    public static IPreferenceEntry<string> LanguageEntry { get; private set; }
    public static IPreferenceEntry<KeyCode> ModMenuKeyEntry { get; private set; }

    /// <summary>
    /// API to create new preference entries at runtime without preprocessor directives in client code.
    /// </summary>
    public static IPreferenceEntry<T> CreateEntry<T>(string category, string key, T defaultValue, string displayName = null, string description = null)
    {
#if MELONLOADER || RELEASE_MELON
        MagnetarCategory ??= MelonPreferences.CreateCategory("Magnetar Client", "Magnetar Client");

        var melonEntry = MagnetarCategory.CreateEntry(key, defaultValue, displayName ?? key, description);
        return new MelonEntryWrapper<T>(melonEntry);
#elif BEPINEX || RELEASE_BEPINEX || ANDROID
        if (BepInExConfig == null)
        {
            string configDir = PathsManager.ConfigDir;
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
            string configFilePath = Path.Combine(configDir, "Magnetar_Client.cfg");
            BepInExConfig = new ConfigFile(configFilePath, true);
        }

        var bepEntry = BepInExConfig.Bind(category ?? "General", key, defaultValue, description ?? displayName ?? key);
        return new BepInExEntryWrapper<T>(bepEntry);
#else
        return null;
#endif
    }

    public static void InitializePreferences()
    {
        Actions.OnEarlyInitializePreferences?.Invoke();

        try
        {
#if MELONLOADER || RELEASE_MELON
            MagnetarCategory = MelonPreferences.CreateCategory("Magnetar Client", "Magnetar Client");
#elif BEPINEX || RELEASE_BEPINEX || ANDROID
            string configDir = PathsManager.ConfigDir;
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
            string configFilePath = Path.Combine(configDir, "Magnetar_Client.cfg");
            BepInExConfig = new ConfigFile(configFilePath, true);
#endif

            ShowFloatingIconEntry = CreateEntry("UI", "ShowFloatingIcon",
#if ANDROID
                true,
#else
                false,
#endif
                "Show Floating Icon", "Display floating draggable menu button.");

            if (ShowFloatingIconEntry != null)
                Config.ShowFloatingIcon = ShowFloatingIconEntry.Value;

            ShowMobileButtonsEntry = CreateEntry("UI", "ShowMobileButtons",
#if ANDROID
                true,
#else
                false,
#endif
                "Show Mobile Buttons", "Display top-right close buttons on popup windows.");

            if (ShowMobileButtonsEntry != null)
                Config.ShowMobileButtons = ShowMobileButtonsEntry.Value;

            CurrentProfileEntry = CreateEntry("ProfileManager", "CurrentProfile", Config.DefaultProfile,
                "Active Profile", "The active configuration profile name.");
            LanguageEntry = CreateEntry("Localization", "Language", "English", "Language", "Active client language.");

            ModMenuKeyEntry = CreateEntry("UI", "MenuBind", KeyCode.RightShift, "MenuBind", "KeyCode to open the gui.");

        }
        catch (Exception ex)
        {
            AutoSaveLogger.Error($"Failed initializing preferences: {ex.Message}");
        }

        Actions.OnLateInitializePreferences?.Invoke();
    }
}