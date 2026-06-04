using Magnetar_Client.Core;
using Magnetar_Client.Modules;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils
{
    public static class SaveLoad
    {
        #region Data Definition
        public class MagnetarSaveData
        {
            public bool ShowGui = false;
            public bool DimBg = true;
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

        private static string Path => System.IO.Path.Combine(MelonEnvironment.UserDataDirectory, "Magnetar_Config.json");
        private static string TexturePath => System.IO.Path.Combine(MelonEnvironment.ModsDirectory, "Magnetar Data", "TextureData.json");


        static float LastSaved;

        public static void Save(bool force = false)
        {

            // Ensure that it only save after some time to reduce lag
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

            // ==========================================
            // 1. SAVE MAIN MAGNETAR CONFIG
            // ==========================================
            MagnetarSaveData data = new MagnetarSaveData
            {
                ShowGui = Config.showgui,
                DimBg = Config.dimBg,
                HudEnabled = HUDManager.Enabled,
                ShowBackground = HUDManager.showBackground,
                SelectedHudElements = new List<int>(HUDRenderer.HudToggles.SelectedValues),
                HudPositions = new Dictionary<string, SimpleRect>(),
                CategoryPositions = new Dictionary<string, SimpleRect>(),
                Modules = new Dictionary<string, ModuleSaveData>(),
                Language = GUIManager.LanguageSetting.SelectedValues.First(),
            };

            foreach (var element in HUDRenderer.Elements)
            {
                data.HudPositions[element.Name] = element.Bounds;
            }

            foreach (var kvp in ModuleManager.windowPositions)
                data.CategoryPositions[kvp.Key.ToString()] = kvp.Value;

            foreach (var mod in ModuleManager.Modules)
            {
                ModuleSaveData modData = new ModuleSaveData
                {
                    Active = mod.Active,
                    HoldMode = mod.HoldMode,
                    KeyBinds = new List<KeyCode>(mod.BindKeys)
                };

                foreach (var setting in mod.Settings)
                {
                    if (setting is IntSetting i) modData.Settings[i.Name] = i.Value;
                    else if (setting is FloatSetting f) modData.Settings[f.Name] = f.Value;
                    else if (setting is BoolSetting b) modData.Settings[b.Name] = b.Value;
                    else if (setting is BindSetting bind) modData.Settings[bind.Name] = bind.BindKeys;
                    else if (setting is MultiSelectSetting ms)
                    {
                        modData.Settings[ms.Name] = new MultiSelectSaveData { SelectedValues = new List<int>(ms.SelectedValues) };
                    }
                }
                data.Modules[mod.Name] = modData;
            }

            File.WriteAllText(Path, JsonConvert.SerializeObject(data, Formatting.Indented));

            // ==========================================
            // 2. SAVE TEXTURE LOADER DATA
            // ==========================================
            try
            {
                // Pull active changes written manually to the disk while the game was running
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
                    catch (Exception e) { TranslatorLogger.Error("Failed to read Texture Data: " + e);  }
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

            if (!force)
                AutoSaveLogger.Msg("Saved the current current Config Data");
        }

        public static void Load()
        {
            if (File.Exists(Path))
            {
                try
                {
                    string json = File.ReadAllText(Path);
                    MagnetarSaveData data = JsonConvert.DeserializeObject<MagnetarSaveData>(json);
                    if (data != null)
                    {
                        Config.showgui = data.ShowGui;
                        Config.dimBg = data.DimBg;
                        GUIManager.LanguageSetting.Deselect(0);
                        GUIManager.LanguageSetting.Select(data.Language);

                        foreach (var entry in data.CategoryPositions)
                        {
                            if (Enum.TryParse(entry.Key, out ModuleCategory category))
                                ModuleManager.windowPositions[category] = new Rect(entry.Value.x, entry.Value.y, entry.Value.w, entry.Value.h);
                        }

                        HUDManager.Enabled = data.HudEnabled;
                        HUDManager.showBackground = data.ShowBackground;

                        if (data.HudPositions != null && HUDRenderer.Elements != null)
                        {
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
                                            if (modData.Settings.TryGetValue(setting.Name, out object rawValue)) RestoreSettingValue(setting, rawValue);
                                        }
                                    }
                                    if (mod.Active != modData.Active) mod.Toggle();
                                }
                            }
                        }

                        MelonLogger.Msg("Loaded Magnetar Profile 'Magnetar_Config'");
                    }
                }
                catch (Exception e) { MelonLoader.MelonLogger.Error($"Main SaveLoad Error: {e.Message}"); }
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
                catch (Exception e) { MelonLoader.MelonLogger.Error($"Texture Load Error: {e.Message}"); }
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
                if (setting is IntSetting i) i.Value = Convert.ToInt32(rawValue);
                else if (setting is FloatSetting f) f.Value = Convert.ToSingle(rawValue);
                else if (setting is BoolSetting b) b.Value = Convert.ToBoolean(rawValue);
                else if (setting is BindSetting bind)
                {
                    string jsonStr = JsonConvert.SerializeObject(rawValue);
                    bind.BindKeys = JsonConvert.DeserializeObject<List<KeyCode>>(jsonStr);
                }
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
            }
            catch (Exception ex) { MelonLoader.MelonLogger.Error($"Error setting '{setting.Name}': {ex.Message}"); }
        }
    }
}