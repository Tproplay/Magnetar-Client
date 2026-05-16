using Magnetar_Client.Core;
using Magnetar_Client.Modules;
using MelonLoader.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils
{
    
    public static class SaveLoad
    {
        #region Data Definition
        public class MagnetarSaveData
        {
            // Config
            public bool ShowGui = false;
            public bool DimBg = true;

            // HUDManager
            public bool HudEnabled = true;
            public bool ShowBackground = false;
            public List<int> SelectedHudElements = new List<int>();
            public Dictionary<string, SimpleRect> HudPositions = new Dictionary<string, SimpleRect>();
            public Dictionary<ModuleCategory, SimpleRect> CategoryPositions = new Dictionary<ModuleCategory, SimpleRect>();
            public Dictionary<string, ModuleSaveData> Modules = new Dictionary<string, ModuleSaveData>();
        }

        // Format of a Module Save Data
        public class ModuleSaveData
        {
            public bool Active;
            public bool HoldMode;
            public List<KeyCode> KeyBinds = new List<KeyCode>();

            public Dictionary<string, object> Settings = new Dictionary<string, object>();
        }

        // Simple Rect Store
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

        // --- CORE LOGIC ---
        private static string Path => System.IO.Path.Combine(MelonEnvironment.UserDataDirectory, "Magnetar_Config.json");

        public static void Save()
        {
            MagnetarSaveData data = new MagnetarSaveData
            {
                ShowGui = Config.showgui,
                DimBg = Config.dimBg,
                HudEnabled = HUDManager.Enabled,
                ShowBackground = HUDManager.showBackground,
                SelectedHudElements = new List<int>(HUDRenderer.HudToggles.SelectedValues),
                HudPositions = new Dictionary<string, SimpleRect>(),
                CategoryPositions = new Dictionary<ModuleCategory, SimpleRect>(),
                Modules = new Dictionary<string, ModuleSaveData>()
            };

            // 1. Capture HUDManager Elements
            foreach (var element in HUDRenderer.Elements)
            {
                data.HudPositions[element.Name] = element.Bounds;
            }

            // 2. Capture Category Windows
            foreach (var kvp in ModuleManager.windowPositions)
                data.CategoryPositions[kvp.Key] = kvp.Value;

            // 3. Capture Modules & Settings
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
                        modData.Settings[ms.Name] = new MultiSelectSaveData
                        {
                            SelectedValues = new List<int>(ms.SelectedValues),
                        };
                    }
                }

                data.Modules[mod.Name] = modData;
            }

            // 4. Write to Disk
            File.WriteAllText(Path, JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        public static void Load()
        {

            if (!File.Exists(Path))
            {
#if DEBUG
                AutoSaveLogger.Warning("Save file not found.");
#endif
                return;
            }

            try
            {
                string json = File.ReadAllText(Path);

                MagnetarSaveData data = JsonConvert.DeserializeObject<MagnetarSaveData>(json);
                if (data == null)
                {
                    return;
                }
                
                Config.showgui = data.ShowGui;
                Config.dimBg = data.DimBg;

                foreach (var entry in data.CategoryPositions)
                {
                    ModuleManager.windowPositions[entry.Key] = new Rect(entry.Value.x, entry.Value.y,
                        entry.Value.w, entry.Value.h);
                }

                HUDManager.Enabled = data.HudEnabled;
                HUDManager.showBackground = data.ShowBackground;

                // HUDManager Checks
                if (data.HudPositions != null && HUDRenderer.Elements != null)
                {
#if DEBUG
                    AutoSaveLogger.Msg($"Restoring {HUDRenderer.Elements.Count} HUDManager elements...");
#endif
                    HUDRenderer.HudToggles.SelectedValues = new HashSet<int>(data.SelectedHudElements);

                    foreach (var element in HUDRenderer.Elements)
                    {
                        if (data.HudPositions.TryGetValue(element.Name, out SimpleRect savedPos))
                            element.Bounds = savedPos;
                    }
                }

                // Module Checks
                if (data.Modules != null && ModuleManager.Modules != null)
                {
#if DEBUG
                    AutoSaveLogger.Msg($"Restoring {ModuleManager.Modules.Count} Modules...");
#endif
                    foreach (var mod in ModuleManager.Modules)
                    {
                        if (string.IsNullOrEmpty(mod.Name)) continue;

                        if (data.Modules.TryGetValue(mod.Name, out ModuleSaveData modData))
                        {
                            // Keybind
                            if (modData.KeyBinds != null && mod.KeyBind != null)
                                mod.KeyBind.BindKeys = modData.KeyBinds;

                            // Hold Mode
                            mod.HoldMode = modData.HoldMode;

                            if (modData.Settings != null && mod.Settings != null)
                            {
                                foreach (var setting in mod.Settings)
                                {
                                    if (string.IsNullOrEmpty(setting.Name)) continue;
                                    if (modData.Settings.TryGetValue(setting.Name, out object rawValue))
                                    {
                                        RestoreSettingValue(setting, rawValue);
                                    }
                                }
                            }

                            // Active
                            if (mod.Active != modData.Active)
                            {
                                mod.Toggle();
                            }
                        }
                    }
                }
#if DEBUG
                AutoSaveLogger.Msg("Load Complete!");
#endif
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error($"{e.Message}\n{e.StackTrace}");
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

                    if (proxy != null)
                    {
                        if (proxy.SelectedValues != null)
                        {
                            ms.SelectedValues.Clear();
                            foreach (var val in proxy.SelectedValues) ms.SelectedValues.Add(val);
                        }
                        
                    }
                    else
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error($"CRASH ON SETTING '{setting.Name}': {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}