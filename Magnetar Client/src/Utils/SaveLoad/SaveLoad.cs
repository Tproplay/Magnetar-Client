using Magnetar_Client.Core;
using Magnetar_Client.Modules;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;
using static Magnetar_Client.Utils.SaveLoadData;
using static Magnetar_Client.Api.MagnetarApi;

namespace Magnetar_Client.Utils;

public static class SaveLoad
{
    private static float LastSaved;
    private static readonly object _fileLock = new();

    public static string ProfilesDir => SaveLoadPlatform.ProfilesDir;

    private static string TexturePath => Path.Combine(DataDir, "TextureData.json");

    public static void Save(bool force = false)
    {
        if (!force)
        {
            if (LastSaved == 0)
            {
                LastSaved = Time.realtimeSinceStartup;
            }
            else if (LastSaved + Config.MinTimeBetweenSaves >= Time.realtimeSinceStartup)
                return;
        }

        LastSaved = Time.realtimeSinceStartup;

        List<int> safeHudElements = new();
        if (HUDRenderer.HudToggles != null && HUDRenderer.HudToggles.SelectedValues != null)
            safeHudElements = new List<int>(HUDRenderer.HudToggles.SelectedValues);

        // ==========================================
        // 1. SAVE MAIN CONFIG
        // ==========================================
        MagnetarSaveData data = new()
        {
            ShowGui = Config.showgui,
            HudEnabled = HUDManager.Enabled,
            ShowBackground = HUDManager.showBackground,
            SelectedHudElements = safeHudElements,
            HudPositions = new Dictionary<string, SimpleRect>(),
            CategoryPositions = new Dictionary<string, SimpleRect>(),
            Modules = new Dictionary<string, ModuleSaveData>(),
            Language = Config.Language,
            Theme = Config.Theme,
            GUIScale = Config.GUIScale,
            ElementScale = Config.ElementScale,
            ShowFloatingIcon = Config.ShowFloatingIcon,
            ShowMainMenuCredits = Config.ShowMainMenuCredits,
        };

        if (HUDRenderer.Elements != null)
        {
            foreach (var element in HUDRenderer.Elements)
                data.HudPositions[element.Name] = element.Bounds;
        }

        if (ModuleManager.windowPositions != null)
        {
            foreach (var kvp in ModuleManager.windowPositions)
                data.CategoryPositions[kvp.Key.ToString()] = kvp.Value;
        }

        if (ModuleManager.Modules != null)
        {
            foreach (var mod in ModuleManager.Modules)
            {
                if (mod == null || !mod.SaveData) continue;

                ModuleSaveData modData = new()
                {
                    Active = mod.Active,
                    HoldMode = mod.HoldMode,
                    KeyBinds = mod.BindKeys != null ? new List<KeyCode>(mod.BindKeys) : new List<KeyCode>()
                };

                if (mod.Settings != null)
                {
                    foreach (var setting in mod.Settings)
                    {
                        if (setting == null || string.IsNullOrEmpty(setting.Name)) continue;
                        string saveKey = setting is CategorySetting ? setting.Name + "_Category" : setting.Name;

                        object serializedValue = SerializeSettingValue(setting);
                        if (serializedValue != null)
                        {
                            modData.Settings[saveKey] = serializedValue;
                        }
                    }
                }
                data.Modules[mod.Name] = modData;
            }
        }

        string savePath = SaveLoadPlatform.GetSafeProfilePath(Config.CurrentProfile);
        string dir = Path.GetDirectoryName(savePath);

        try
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(savePath, JsonConvert.SerializeObject(data, Formatting.Indented));
        }
        catch (Exception ex)
        {
            AutoSaveLogger.Error($"Failed to write save file: {ex.Message}");
        }

        // ==========================================
        // 2. SAVE TEXTURE OVERRIDES
        // ==========================================
        try
        {
            if (File.Exists(TexturePath))
            {
                try
                {
                    string existingJson = File.ReadAllText(TexturePath);
                    TextureSaveData existingData = JsonConvert.DeserializeObject<TextureSaveData>(existingJson);
                    if (existingData != null)
                    {
                        if (existingData.PlantTextureOverrides != null)
                        {
                            foreach (var kvp in existingData.PlantTextureOverrides)
                                TextureLoader.PlantTextureOverrides[kvp.Key] = kvp.Value;
                        }
                        if (existingData.ZombieTextureOverrides != null)
                        {
                            foreach (var kvp in existingData.ZombieTextureOverrides)
                                TextureLoader.ZombieTextureOverrides[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (Exception e) { TranslatorLogger.Error("Failed to read Texture Data: " + e); }
            }

            TextureSaveData texData = new()
            {
                PlantTextureOverrides = TextureLoader.PlantTextureOverrides ?? new Dictionary<int, string>(),
                ZombieTextureOverrides = TextureLoader.ZombieTextureOverrides ?? new Dictionary<int, string>()
            };

            string texDirectory = Path.GetDirectoryName(TexturePath);
            if (!Directory.Exists(texDirectory)) Directory.CreateDirectory(texDirectory);

            File.WriteAllText(TexturePath, JsonConvert.SerializeObject(texData, Formatting.Indented));
        }
        catch (Exception e)
        {
            AutoSaveLogger.Error($"Failed to save TextureData: {e.Message}");
        }

        if (!force) AutoSaveLogger.Msg("Saved the current Config Data");
    }

    public static void Load()
    {
        string loadPath = SaveLoadPlatform.GetSafeProfilePath(Config.CurrentProfile);

        lock (_fileLock)
        {
            bool exists = false;
            try
            {
                exists = !string.IsNullOrEmpty(loadPath) && File.Exists(loadPath);
            }
            catch { exists = false; }

            if (exists)
            {
                try
                {
                    string json = File.ReadAllText(loadPath);
                    MagnetarSaveData data = JsonConvert.DeserializeObject<MagnetarSaveData>(json);
                    if (data != null)
                    {
                        Config.showgui = data.ShowGui;
                        Config.ShowFloatingIcon = data.ShowFloatingIcon;
                        Config.ShowMainMenuCredits = data.ShowMainMenuCredits;

                        if (!string.IsNullOrEmpty(data.Theme))
                        {
                            Config.Theme = data.Theme;
                        }

                        // 1. Language Setting
                        Config.Language = data.Language;
                        if (GUIManager.LanguageSetting != null && GUIManager.LanguageSetting.Options != null)
                        {
                            foreach (var key in GUIManager.LanguageSetting.Options)
                            {
                                GUIManager.LanguageSetting.Deselect(0);
                                if (key.Value == data.Language)
                                {
                                    GUIManager.LanguageSetting.Select(key.Key);
                                    break;
                                }
                            }
                        }

                        // 1.1 Theme Options
                        if (GUIManager.ThemeSetting != null)
                        {
                            GUIManager.RefreshThemeOptions();
                        }

                        // 2. Category Window Positions
                        if (data.CategoryPositions != null && ModuleManager.windowPositions != null)
                        {
                            foreach (var entry in data.CategoryPositions)
                            {
                                if (ModuleCategory.TryGet(entry.Key, out ModuleCategory category))
                                {
                                    ModuleManager.windowPositions[category] = new Rect(entry.Value.x, entry.Value.y, entry.Value.w, entry.Value.h);
                                }
                            }
                        }

                        // 3. HUD Settings & Elements
                        HUDManager.Enabled = data.HudEnabled;
                        HUDManager.showBackground = data.ShowBackground;

                        if (HUDRenderer.HudToggles != null && data.SelectedHudElements != null)
                        {
                            HUDRenderer.HudToggles.SelectedValues = new HashSet<int>(data.SelectedHudElements);
                        }

                        if (data.HudPositions != null && HUDRenderer.Elements != null)
                        {
                            foreach (var element in HUDRenderer.Elements)
                            {
                                if (element != null && !string.IsNullOrEmpty(element.Name))
                                {
                                    if (data.HudPositions.TryGetValue(element.Name, out SimpleRect savedPos))
                                    {
                                        element.Bounds = savedPos;
                                    }
                                }
                            }
                        }

                        // 4. Modules
                        if (data.Modules != null && ModuleManager.Modules != null)
                        {
                            foreach (var mod in ModuleManager.Modules)
                            {
                                if (mod == null || !mod.SaveData || string.IsNullOrEmpty(mod.Name)) continue;

                                if (data.Modules.TryGetValue(mod.Name, out ModuleSaveData modData))
                                {
                                    RestoreModuleSettings(mod, modData);
                                }
                            }
                        }

                        // 5. Scales
                        if (data.GUIScale > 0.1f) Config.GUIScale = data.GUIScale;
                        if (data.ElementScale > 0.1f) Config.ElementScale = data.ElementScale;

                        ThemeManager.Rescale();
                        AutoSaveLogger.Msg($"Loaded Magnetar Profile '{Config.CurrentProfile}'");
                    }
                }
                catch (Exception e)
                {
                    AutoSaveLogger.Error($"Main SaveLoad Error: {e.Message}\nStack: {e.StackTrace}");
                }
            }
            else
            {
                AutoSaveLogger.Msg($"No profile found at '{loadPath}'. Initializing defaults.");
                if (ModuleManager.IsInitialized)
                {
                    Save(true);
                }
            }

            // 6. Texture Overrides Data
            string texPath = SaveLoadPlatform.GetSafeTexturePath();
            if (File.Exists(texPath))
            {
                try
                {
                    string texJson = File.ReadAllText(texPath);
                    TextureSaveData texData = JsonConvert.DeserializeObject<TextureSaveData>(texJson);
                    if (texData != null)
                    {
                        TextureLoader.PlantTextureOverrides = texData.PlantTextureOverrides ?? new Dictionary<int, string>();
                        TextureLoader.ZombieTextureOverrides = texData.ZombieTextureOverrides ?? new Dictionary<int, string>();
                    }
                }
                catch (Exception e)
                {
                    AutoSaveLogger.Error($"Texture Load Error: {e.Message}");
                }
            }
            else
            {
                try
                {
                    string texDirectory = Path.GetDirectoryName(texPath);
                    if (!string.IsNullOrEmpty(texDirectory) && !Directory.Exists(texDirectory))
                    {
                        Directory.CreateDirectory(texDirectory);
                    }
                }
                catch (Exception ex)
                {
                    AutoSaveLogger.Error($"Failed to create texture data folder: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Restores keybinds, hold mode, active state, and setting values for a specific module.
    /// Respects mod.SaveData flag.
    /// </summary>
    public static void RestoreModuleSettings(Module mod, ModuleSaveData modData)
    {
        if (mod == null || !mod.SaveData || modData == null) return;

        // 1. Keybinds & Hold Mode
        if (modData.KeyBinds != null && mod.KeyBind != null)
        {
            mod.KeyBind.BindKeys = new List<KeyCode>(modData.KeyBinds);
        }
        mod.HoldMode = modData.HoldMode;

        // 2. Settings restoration
        if (modData.Settings != null && mod.Settings != null)
        {
            foreach (var setting in mod.Settings)
            {
                if (setting == null || string.IsNullOrEmpty(setting.Name)) continue;

                string loadKey = setting is CategorySetting ? setting.Name + "_Category" : setting.Name;
                if (modData.Settings.TryGetValue(loadKey, out object rawValue) ||
                    modData.Settings.TryGetValue(setting.Name, out rawValue))
                {
                    SaveLoadData.RestoreSettingValue(setting, rawValue);
                }
            }
        }

        // 3. Active toggle state
        if (mod.Active != modData.Active)
        {
            mod.Toggle();
        }
    }
}