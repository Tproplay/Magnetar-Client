using Magnetar_Client.UI.Setting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Magnetar_Client.Utils.Magnetar_Logger;

namespace Magnetar_Client.Utils;

public static class SaveLoadData
{
    #region Entry Data Interface & Registries

    public interface ISaveEntry
    {
        string Key { get; }
        object GetValue();
        void SetValue(object rawValue);
        void ResetToDefault();
    }

    public class SaveEntry<T> : ISaveEntry
    {
        public string Key { get; }
        public Func<T> Getter { get; }
        public Action<T> Setter { get; }
        public T DefaultValue { get; }

        public SaveEntry(string key, Func<T> getter, Action<T> setter, T defaultValue = default)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Getter = getter ?? throw new ArgumentNullException(nameof(getter));
            Setter = setter ?? throw new ArgumentNullException(nameof(setter));
            DefaultValue = defaultValue;
        }

        public object GetValue() => Getter();

        public void SetValue(object rawValue)
        {
            if (rawValue == null)
            {
                Setter(DefaultValue);
                return;
            }

            try
            {
                if (rawValue is T typedVal)
                {
                    Setter(typedVal);
                    return;
                }

                if (rawValue is JToken token)
                {
                    T converted = token.ToObject<T>();
                    Setter(converted);
                    return;
                }

                T val = (T)Convert.ChangeType(rawValue, typeof(T));
                Setter(val);
            }
            catch (Exception ex)
            {
                AutoSaveLogger.Error($"[SaveRegistry] Failed to deserialize entry '{Key}': {ex.Message}");
                Setter(DefaultValue);
            }
        }

        public void ResetToDefault() => Setter(DefaultValue);
    }

    public static class SaveRegistry
    {
        private static readonly Dictionary<string, ISaveEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<ISaveEntry> Entries => _entries.Values;

        /// <summary>
        /// Registers a new save entry. Can be called anywhere (e.g. extensions, plugins, managers).
        /// </summary>
        public static SaveEntry<T> Register<T>(string key, Func<T> getter, Action<T> setter, T defaultValue = default)
        {
            var entry = new SaveEntry<T>(key, getter, setter, defaultValue);
            _entries[key] = entry;
            return entry;
        }

        public static bool Unregister(string key) => _entries.Remove(key);

        public static bool TryGetEntry(string key, out ISaveEntry entry) => _entries.TryGetValue(key, out entry);
    }

    #endregion

    #region Data Transfer Objects

    public class MagnetarSaveData
    {
        // Core structural dictionaries
        public List<int> SelectedHudElements = new();
        public Dictionary<string, SimpleRect> HudPositions = new();
        public Dictionary<string, SimpleRect> CategoryPositions = new();
        public Dictionary<string, ModuleSaveData> Modules = new();

        // Extensible storage for registered EntryData
        public Dictionary<string, object> Entries = new(StringComparer.OrdinalIgnoreCase);
    }

    public class TextureSaveData
    {
        public Dictionary<int, string> PlantTextureOverrides = new();
        public Dictionary<int, string> ZombieTextureOverrides = new();
    }

    public class ModuleSaveData
    {
        public bool Active;
        public bool HoldMode;
        public List<KeyCode> KeyBinds = new();
        public Dictionary<string, object> Settings = new();
    }

    public class SimpleRect
    {
        public float x, y, w, h;
        public static implicit operator Rect(SimpleRect s) => new(s.x, s.y, s.w, s.h);
        public static implicit operator SimpleRect(Rect r) => new() { x = r.x, y = r.y, w = r.width, h = r.height };
    }

    public class MultiSelectSaveData
    {
        public string Mode { get; set; } = "Selected";
        public List<int> Values { get; set; } = new();
    }

    #endregion

    #region Setting Serialization & Restoration

    public static object SerializeSettingValue(Setting setting)
    {
        if (setting == null) return null;

        if (setting is CategorySetting cat) return cat.IsExpanded;
        if (setting is MultiSelectSetting ms)
        {
            var validOptions = ms.Options.Keys
                .Where(k => (ms.Blacklist == null || !ms.Blacklist.Contains(k)) &&
                            (ms.NameBlacklist == null || !ms.NameBlacklist.Contains(ms.Options[k])))
                .ToList();

            int totalCount = validOptions.Count;
            int selectedCount = ms.SelectedValues.Count;

            if (totalCount > 0 && selectedCount >= (totalCount / 2))
            {
                var unselected = validOptions.Where(k => !ms.SelectedValues.Contains(k)).ToList();
                return new MultiSelectSaveData
                {
                    Mode = "Deselected",
                    Values = unselected
                };
            }
            else
            {
                return new MultiSelectSaveData
                {
                    Mode = "Selected",
                    Values = ms.SelectedValues.ToList()
                };
            }
        }
        if (setting is BindSetting bind) return bind.BindKeys;
        if (setting is SelectSetting sel) return sel.Value;
        if (setting is StringSetting str) return str.Value;
        if (setting is ListStringSetting list) return new List<string>(list.Values);
        if (setting is BoolSetting b) return b.Value;
        if (setting is FloatSetting f) return f.Value;
        if (setting is IntSetting i) return i.Value;
        if (setting is Vector2Setting v2) return new float[] { v2.Value.x, v2.Value.y };
        if (setting is Vector3Setting v3) return new float[] { v3.Value.x, v3.Value.y, v3.Value.z };
        if (setting is SectionSetting sec)
        {
            var secList = new List<Dictionary<string, object>>();
            foreach (var section in sec.Sections)
            {
                var sDict = new Dictionary<string, object>();
                foreach (var child in section.ChildSettings)
                {
                    if (child == null || string.IsNullOrEmpty(child.Name)) continue;
                    string childKey = child is CategorySetting ? child.Name + "_Category" : child.Name;

                    object childVal = SerializeSettingValue(child);
                    if (childVal != null)
                    {
                        sDict[childKey] = childVal;
                    }
                }
                secList.Add(sDict);
            }
            return secList;
        }

        return null;
    }

    public static void RestoreSettingValue(Setting setting, object rawValue)
    {
        try
        {
            if (setting is CategorySetting cat) cat.IsExpanded = Convert.ToBoolean(rawValue);
            else if (setting is MultiSelectSetting ms)
            {
                string jsonStr = JsonConvert.SerializeObject(rawValue);
                var proxy = JsonConvert.DeserializeObject<MultiSelectSaveData>(jsonStr);

                if (proxy != null)
                {
                    ms.SelectedValues.Clear();

                    var validOptions = ms.Options.Keys
                        .Where(k => (ms.Blacklist == null || !ms.Blacklist.Contains(k)) &&
                                    (ms.NameBlacklist == null || !ms.NameBlacklist.Contains(ms.Options[k])))
                        .ToList();

                    if (string.Equals(proxy.Mode, "Deselected", StringComparison.OrdinalIgnoreCase))
                    {
                        var excluded = new HashSet<int>(proxy.Values ?? new List<int>());
                        foreach (var opt in validOptions)
                        {
                            if (!excluded.Contains(opt))
                            {
                                ms.SelectedValues.Add(opt);
                            }
                        }
                    }
                    else
                    {
                        if (proxy.Values != null)
                        {
                            foreach (var val in proxy.Values)
                            {
                                if (ms.Blacklist == null || !ms.Blacklist.Contains(val))
                                {
                                    ms.SelectedValues.Add(val);
                                }
                            }
                        }
                    }
                }
            }
            else if (setting is BindSetting bind)
            {
                string jsonStr = JsonConvert.SerializeObject(rawValue);
                bind.BindKeys = JsonConvert.DeserializeObject<List<KeyCode>>(jsonStr) ?? new List<KeyCode>();
            }
            else if (setting is SelectSetting sel) sel.Value = Convert.ToInt32(rawValue);
            else if (setting is StringSetting str) str.Value = rawValue?.ToString() ?? "";
            else if (setting is ListStringSetting list)
            {
                string jsonStr = JsonConvert.SerializeObject(rawValue);
                list.Values = JsonConvert.DeserializeObject<List<string>>(jsonStr) ?? new List<string>();
            }
            else if (setting is BoolSetting b) b.Value = Convert.ToBoolean(rawValue);
            else if (setting is FloatSetting f) f.Value = Convert.ToSingle(rawValue);
            else if (setting is IntSetting i) i.Value = Convert.ToInt32(rawValue);
            else if (setting is Vector2Setting v2)
            {
                string jsonStr = JsonConvert.SerializeObject(rawValue);
                float[] arr = JsonConvert.DeserializeObject<float[]>(jsonStr);
                if (arr != null && arr.Length >= 2) v2.Value = new Vector2(arr[0], arr[1]);
            }
            else if (setting is Vector3Setting v3)
            {
                string jsonStr = JsonConvert.SerializeObject(rawValue);
                float[] arr = JsonConvert.DeserializeObject<float[]>(jsonStr);
                if (arr != null && arr.Length >= 3) v3.Value = new Vector3(arr[0], arr[1], arr[2]);
            }
            else if (setting is SectionSetting sec)
            {
                string jsonStr = JsonConvert.SerializeObject(rawValue);
                var secData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonStr);
                if (secData != null && sec.TemplateFactory != null)
                {
                    sec.Sections.Clear();
                    for (int s = 0; s < secData.Count; s++)
                    {
                        var childList = sec.TemplateFactory(s);
                        foreach (var child in childList)
                        {
                            if (child == null || string.IsNullOrEmpty(child.Name)) continue;
                            string loadKey = child is CategorySetting ? child.Name + "_Category" : child.Name;

                            if (secData[s].TryGetValue(loadKey, out var childVal) ||
                                secData[s].TryGetValue(child.Name, out childVal))
                            {
                                RestoreSettingValue(child, childVal);
                            }
                        }
                        sec.Sections.Add(new SectionInstance($"Section #{s + 1}", childList));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AutoSaveLogger.Error($"Error restoring setting '{setting.Name}': {ex.Message}");
        }
    }

    #endregion
}