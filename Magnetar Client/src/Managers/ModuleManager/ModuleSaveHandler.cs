using Magnetar_Client.Api;
using Magnetar_Client.Modules;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.Core.ModuleManager_;

public class ModuleSaveHandler : ISaveHandler
{
    public string SectionKey => "Modules";

    public object ExportData()
    {
        var export = new ModuleManagerSaveData();

        // 1. Export Category window positions
        if (ModuleManager.windowPositions != null)
        {
            foreach (var kvp in ModuleManager.windowPositions)
                export.CategoryPositions[kvp.Key.ToString()] = kvp.Value;
        }

        // 2. Export modules state, settings, and arbitrary SavedData
        if (ModuleManager.Modules != null)
        {
            foreach (var mod in ModuleManager.Modules)
            {
                if (mod == null || !mod.SaveData) continue;

                var modData = new ModuleSaveData
                {
                    Active = mod.Active,
                    HoldMode = mod.HoldMode,
                    KeyBinds = mod.BindKeys != null ? new List<KeyCode>(mod.BindKeys) : new List<KeyCode>(),
                    SavedData = mod.SavedData != null ? new Dictionary<string, object>(mod.SavedData) : new()
                };

                if (mod.Settings != null)
                {
                    foreach (var setting in mod.Settings)
                    {
                        if (setting == null || string.IsNullOrEmpty(setting.Name)) continue;
                        string saveKey = setting is CategorySetting ? setting.Name + "_Category" : setting.Name;
                        object serialized = SaveLoadData.SerializeSettingValue(setting);
                        if (serialized != null) modData.Settings[saveKey] = serialized;
                    }
                }

                export.Modules[mod.Name] = modData;
            }
        }

        return export;
    }

    public void ImportData(JToken token)
    {
        if (token == null) return;

        // Restore category window layout
        JToken catPosToken = token["CategoryPositions"];
        if (catPosToken != null && ModuleManager.windowPositions != null)
        {
            var catDict = catPosToken.ToObject<Dictionary<string, SaveLoadData.SimpleRect>>();
            if (catDict != null)
            {
                foreach (var entry in catDict)
                {
                    if (ModuleCategory.TryGet(entry.Key, out ModuleCategory category))
                    {
                        ModuleManager.windowPositions[category] = new Rect(entry.Value.x, entry.Value.y, entry.Value.w, entry.Value.h);
                    }
                }
            }
        }

        // Restore individual module states
        JToken modToken = token["Modules"] ?? token;
        if (modToken != null && ModuleManager.Modules != null)
        {
            var modDict = modToken.ToObject<Dictionary<string, ModuleSaveData>>();
            if (modDict != null)
            {
                foreach (var mod in ModuleManager.Modules)
                {
                    if (mod == null || !mod.SaveData || string.IsNullOrEmpty(mod.Name)) continue;
                    if (modDict.TryGetValue(mod.Name, out var data))
                    {
                        RestoreModuleSettings(mod, data);
                    }
                }
            }
        }
    }

    public void ResetToDefault()
    {
        ModuleManager.ResetToDefault();
    }

    public static void RestoreModuleSettings(Modules.Module mod, ModuleSaveData modData)
    {
        if (mod == null || !mod.SaveData || modData == null) return;

        // Restore arbitrary custom module SavedData
        if (modData.SavedData != null)
        {
            mod.SavedData = new Dictionary<string, object>(modData.SavedData, System.StringComparer.OrdinalIgnoreCase);
        }

        if (modData.KeyBinds != null && mod.KeyBind != null)
        {
            mod.KeyBind.BindKeys = new List<KeyCode>(modData.KeyBinds);
        }
        mod.HoldMode = modData.HoldMode;

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

        if (mod.Active != modData.Active)
        {
            mod.Toggle();
        }
    }
}

public class ModuleManagerSaveData
{
    public Dictionary<string, SaveLoadData.SimpleRect> CategoryPositions = new();
    public Dictionary<string, ModuleSaveData> Modules = new();
}

public class ModuleSaveData
{
    public bool Active;
    public bool HoldMode;
    public List<KeyCode> KeyBinds = new();
    public Dictionary<string, object> Settings = new();
    public Dictionary<string, object> SavedData = new();
}