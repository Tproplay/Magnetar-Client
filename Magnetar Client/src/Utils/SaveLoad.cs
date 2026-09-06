using Magnetar_Client.Core;
using Magnetar_Client.Modules;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
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
    public static class SaveLoad
    {
        #region Data Definition
        public class MagnetarSaveData
        {
            public bool ShowGui = false;
            public int Language;

            public bool HudEnabled = true;
            public bool ShowBackground = false;
            public List<int> SelectedHudElements = new List<int>();
            public Dictionary<string, SimpleRect> HudPositions = new Dictionary<string, SimpleRect>();
            public Dictionary<string, SimpleRect> CategoryPositions = new Dictionary<string, SimpleRect>();
            public Dictionary<string, ModuleSaveData> Modules = new Dictionary<string, ModuleSaveData>();
        }

        public class TextureSaveData
        {
            public Dictionary<int, string> PlantTextureOverrides = new Dictionary<int, string>();
            public Dictionary<int, string> ZombieTextureOverrides = new Dictionary<int, string>();
        }

        public class ModuleSaveData
        {
            public bool Active;
            public bool HoldMode;
            public List<KeyCode> KeyBinds = new List<KeyCode>();
            public Dictionary<string, object> Settings = new Dictionary<string, object>();
        }

        public class SimpleRect
        {
            public float x, y, w, h;
            public static implicit operator Rect(SimpleRect s) => new Rect(s.x, s.y, s.w, s.h);
            public static implicit operator SimpleRect(Rect r) => new SimpleRect { x = r.x, y = r.y, w = r.width, h = r.height };
        }

        public class MultiSelectSaveData
        {
            public List<int> SelectedValues;
        }
        #endregion

        private static string _cachedModsDir;
        private static string ModsDir
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
                    _cachedModsDir = ProfileManager.ConfigDir;
                    return _cachedModsDir;
                }

                return targetDir;
            }
        }

        private static string ProfilePath => ProfileManager.GetProfilePath(Config.CurrentProfile);
        private static string TexturePath => System.IO.Path.Combine(ModsDir, "Magnetar Data", "TextureData.json");

        static float LastSaved;

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

            List<int> safeHudElements = new List<int>();
            if (HUDRenderer.HudToggles != null && HUDRenderer.HudToggles.SelectedValues != null)
                safeHudElements = new List<int>(HUDRenderer.HudToggles.SelectedValues);

            int safeLanguage = 0;
            if (GUIManager.LanguageSetting != null && GUIManager.LanguageSetting.SelectedValues != null && GUIManager.LanguageSetting.SelectedValues.Count > 0)
                safeLanguage = GUIManager.LanguageSetting.SelectedValues.First();

            // ==========================================
            // 1. SAVE MAIN MAGNETAR CONFIG
            // ==========================================
            MagnetarSaveData data = new MagnetarSaveData
            {
                ShowGui = Config.showgui,
                HudEnabled = HUDManager.Enabled,
                ShowBackground = HUDManager.showBackground,
                SelectedHudElements = safeHudElements,
                HudPositions = new Dictionary<string, SimpleRect>(),
                CategoryPositions = new Dictionary<string, SimpleRect>(),
                Modules = new Dictionary<string, ModuleSaveData>(),
                Language = safeLanguage,
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
                    ModuleSaveData modData = new ModuleSaveData
                    {
                        Active = mod.Active,
                        HoldMode = mod.HoldMode,
                        KeyBinds = mod.BindKeys != null ? new List<KeyCode>(mod.BindKeys) : new List<KeyCode>()
                    };

                    if (mod.Settings != null)
                    {
                        foreach (var setting in mod.Settings)
                        {
                            if (string.IsNullOrEmpty(setting.Name)) continue;
                            string saveKey = setting is CategorySetting ? setting.Name + "_Category" : setting.Name;

                            if (setting is CategorySetting cat) modData.Settings[saveKey] = cat.IsExpanded;
                            else if (setting is MultiSelectSetting ms)
                            {
                                modData.Settings[saveKey] = new MultiSelectSaveData { SelectedValues = new List<int>(ms.SelectedValues) };
                            }
                            else if (setting is BindSetting bind) modData.Settings[bind.Name] = bind.BindKeys;
                            else if (setting is SelectSetting sel) modData.Settings[sel.Name] = sel.Value;
                            else if (setting is StringSetting str) modData.Settings[str.Name] = str.Value;
                            else if (setting is BoolSetting b) modData.Settings[b.Name] = b.Value;
                            else if (setting is FloatSetting f) modData.Settings[f.Name] = f.Value;
                            else if (setting is IntSetting i) modData.Settings[i.Name] = i.Value;
                        }
                    }
                    data.Modules[mod.Name] = modData;
                }
            }

            string savePath = ProfilePath;
            string dir = System.IO.Path.GetDirectoryName(savePath);

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
            // 2. SAVE TEXTURE LOADER DATA
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

                TextureSaveData texData = new TextureSaveData
                {
                    PlantTextureOverrides = TextureLoader.PlantTextureOverrides ?? new Dictionary<int, string>(),
                    ZombieTextureOverrides = TextureLoader.ZombieTextureOverrides ?? new Dictionary<int, string>()
                };

                string texDirectory = System.IO.Path.GetDirectoryName(TexturePath);
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
            string loadPath = ProfilePath;

            if (File.Exists(loadPath))
            {
                try
                {
                    string json = File.ReadAllText(loadPath);
                    MagnetarSaveData data = JsonConvert.DeserializeObject<MagnetarSaveData>(json);
                    if (data != null)
                    {
                        Config.showgui = data.ShowGui;

                        if (GUIManager.LanguageSetting != null)
                        {
                            GUIManager.LanguageSetting.Deselect(0);
                            GUIManager.LanguageSetting.Select(data.Language);
                        }

                        foreach (var entry in data.CategoryPositions)
                        {
                            if (Enum.TryParse(entry.Key, out ModuleCategory category))
                                ModuleManager.windowPositions[category] = new Rect(entry.Value.x, entry.Value.y, entry.Value.w, entry.Value.h);
                        }

                        HUDManager.Enabled = data.HudEnabled;
                        HUDManager.showBackground = data.ShowBackground;

                        if (data.HudPositions != null && HUDRenderer.Elements != null)
                        {
                            if (HUDRenderer.HudToggles != null)
                                HUDRenderer.HudToggles.SelectedValues = new HashSet<int>(data.SelectedHudElements);

                            foreach (var element in HUDRenderer.Elements)
                            {
                                if (data.HudPositions.TryGetValue(element.Name, out SimpleRect savedPos))
                                    element.Bounds = savedPos;
                            }
                        }

                        if (data.Modules != null && ModuleManager.Modules != null)
                        {
                            foreach (var mod in ModuleManager.Modules)
                            {
                                if (string.IsNullOrEmpty(mod.Name)) continue;
                                if (data.Modules.TryGetValue(mod.Name, out ModuleSaveData modData))
                                {
                                    if (modData.KeyBinds != null && mod.KeyBind != null) mod.KeyBind.BindKeys = modData.KeyBinds;
                                    mod.HoldMode = modData.HoldMode;

                                    if (modData.Settings != null && mod.Settings != null)
                                    {
                                        foreach (var setting in mod.Settings)
                                        {
                                            if (string.IsNullOrEmpty(setting.Name)) continue;
                                            string loadKey = setting is CategorySetting ? setting.Name + "_Category" : setting.Name;
                                            if (modData.Settings.TryGetValue(loadKey, out object rawValue) ||
                                                modData.Settings.TryGetValue(setting.Name, out rawValue))
                                            {
                                                RestoreSettingValue(setting, rawValue);
                                            }
                                        }
                                    }
                                    if (mod.Active != modData.Active) mod.Toggle();
                                }
                            }
                        }

                        AutoSaveLogger.Msg($"Loaded Magnetar Profile '{Config.CurrentProfile}'");
                    }
                }
                catch (Exception e) { AutoSaveLogger.Error($"Main SaveLoad Error: {e.Message}"); }
            }

            if (File.Exists(TexturePath))
            {
                try
                {
                    string texJson = File.ReadAllText(TexturePath);
                    TextureSaveData texData = JsonConvert.DeserializeObject<TextureSaveData>(texJson);
                    if (texData != null)
                    {
                        TextureLoader.PlantTextureOverrides = texData.PlantTextureOverrides ?? new Dictionary<int, string>();
                        TextureLoader.ZombieTextureOverrides = texData.ZombieTextureOverrides ?? new Dictionary<int, string>();
                    }
                }
                catch (Exception e) { AutoSaveLogger.Error($"Texture Load Error: {e.Message}"); }
            }
            else
            {
                string texDirectory = System.IO.Path.GetDirectoryName(TexturePath);
                if (!Directory.Exists(texDirectory)) Directory.CreateDirectory(texDirectory);
            }
        }

        private static void RestoreSettingValue(Setting setting, object rawValue)
        {
            try
            {
                if (setting is CategorySetting cat) cat.IsExpanded = Convert.ToBoolean(rawValue);
                else if (setting is MultiSelectSetting ms)
                {
                    string jsonStr = JsonConvert.SerializeObject(rawValue);
                    var proxy = JsonConvert.DeserializeObject<MultiSelectSaveData>(jsonStr);
                    if (proxy != null && proxy.SelectedValues != null)
                    {
                        ms.SelectedValues.Clear();
                        foreach (var val in proxy.SelectedValues) ms.SelectedValues.Add(val);
                    }
                }
                else if (setting is BindSetting bind)
                {
                    string jsonStr = JsonConvert.SerializeObject(rawValue);
                    bind.BindKeys = JsonConvert.DeserializeObject<List<KeyCode>>(jsonStr);
                }
                else if (setting is SelectSetting sel) sel.Value = Convert.ToInt32(rawValue);
                else if (setting is StringSetting str) str.Value = rawValue.ToString();
                else if (setting is BoolSetting b) b.Value = Convert.ToBoolean(rawValue);
                else if (setting is FloatSetting f) f.Value = Convert.ToSingle(rawValue);
                else if (setting is IntSetting i) i.Value = Convert.ToInt32(rawValue);
            }
            catch (Exception ex) { AutoSaveLogger.Error($"Error setting '{setting.Name}': {ex.Message}"); }
        }

        public static void InitializePrefrences()
        {
#if MELONLOADER || RELEASE_MELON
            Prefrences.MagnetarCategory = MelonPreferences.CreateCategory("Magnetar Client", "Magnetar Client");
#elif BEPINEX || RELEASE_BEPINEX
            try
            {
                string configFilePath = System.IO.Path.Combine(ProfileManager.ConfigDir, "Magnetar_Client.cfg");
                Prefrences.BepInExConfig = new ConfigFile(configFilePath, true);
            }
            catch (Exception ex)
            {
                AutoSaveLogger.Error($"Failed to initialize BepInEx ConfigFile: {ex.Message}");
            }
#endif
        }
    }
}