using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Magnetar_Client.Core;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;




#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public abstract class Module
{
    /// <summary>
    /// Name to be displayed
    /// </summary>
    public abstract string Name { get; set; }
    /// <summary>
    /// Search hints to be used when searching for the module
    /// </summary>
    public abstract string SearchHints { get; set; }
    /// <summary>
    /// Optional name for mod author. Supports rich text.
    /// </summary>
    public virtual string Author { get; set; } = "";
    /// <summary>
    /// Description for the module. Supports rich text.
    /// </summary>
    public abstract string Description { get; set; }
    /// <summary>
    /// The category to put the module in.
    /// </summary>
    public abstract ModuleCategory Category { get; set; }

    public virtual bool enableInVanillaMode { get; set; } = false;

    // These will be in Every ModuleManager.
    // Edit if you want a different default keybind or want it to be enabled by default.

    public BindSetting KeyBind = new("Keybind");

    public string GetBindString() => KeyBind.GetBindString();

    public List<KeyCode> BindKeys => KeyBind.BindKeys;
    public virtual bool HoldMode { get; set; } = false;
    public virtual bool Active { get; set; } = false;
    /// <summary>
    /// Used to determine whether the setting window of the module is opened.
    /// </summary>
    public virtual bool ShowSettings { get; set; } = false;


    /// <summary>
    /// Used to store all the settings for the module.
    /// </summary>
    public List<Setting> Settings = new();

    public void Toggle()
    {
        Active = !Active;
        if (Active) OnEnable();
        else OnDisable();
    }

    /// <summary>
    /// Runs once when the module is enabled. Runs Before OnUpdateActive.
    /// </summary>
    public virtual void OnEnable() { }
    /// <summary>
    /// Runs once when the module is disabled. Runs After OnUpdateActive.
    /// </summary>
    public virtual void OnDisable() { }


    /// <summary>
    /// Runs every frame regardless of whether the module is active or not.
    /// </summary>
    public virtual void OnUpdate() { if (Active) OnUpdateActive(); }

    /// <summary>
    /// Runs every frame only when the module is active. Will not run if OnUpdate is overridden without calling base.OnUpdate().
    /// </summary>
    public virtual void OnUpdateActive() { }

    /// <summary>
    /// Runs every frame on UnityEngine.OnGUI
    /// </summary>
    public virtual void OnGUI() { }

    /// <summary>
    /// Static method to add settings to the module. Call this in the constructor of your module with all the settings you want to add.
    /// </summary>
    public void AddSettings(params Setting[] settings)
    {
        Settings.AddRange(settings);
    }

    /// <summary>
    /// Runs When a the mod's language is changed
    /// </summary>
    public virtual void OnLanguageChanged() { }

    /// <summary>
    /// Translates a enum
    /// </summary>
    public static Dictionary<int, string> TranslateEnum(Type enumType, bool enumKey = true)
    {
        if (enumType == null || !enumType.IsEnum) return new Dictionary<int, string>();

        var names = ModuleManager.TranslateEnum(enumType);
        if (enumKey)
        {
            foreach (var kvp in names.ToList())
            {
                names[kvp.Key] = $"{kvp.Value} ({kvp.Key})";
            }
        }
        
        return names;
    }
    /// <summary>
    /// Translates a enum
    /// </summary>
    public static Dictionary<int, string> TranslateEnum<T>() => TranslateEnum(typeof(T));
    /// <summary>
    /// Translates a enum
    /// </summary>
    public static Dictionary<int, string> TranslateEnum<T>(bool enumKey = true) => TranslateEnum(typeof(T), enumKey);

    public virtual float SettingsWidth { get; set; } = Config.ModuleManager.SettingsWidth;

    /// <summary>
    /// Creates a new Category. Use EndCategory() to define the end.
    /// </summary>
    public void CreateCategory(string name, bool defaultExpanded = true)
    {
        Settings.Add(new CategorySetting(name, defaultExpanded));
    }

    public void EndCategory()
    {
        Settings.Add(new EndCategorySetting());
    }

    public static bool GetKeyComboDown(List<KeyCode> keyCodes)
    {
        if (keyCodes == null || keyCodes.Count == 0) return false;

        KeyCode triggerKey = keyCodes[keyCodes.Count - 1];
        if (!Input.GetKeyDown(triggerKey)) return false;

        for (int i = 0; i < keyCodes.Count - 1; i++)
        {
            if (!Input.GetKey(keyCodes[i]))
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Contains default Banned HashSets for Plants and Zombies
    /// </summary>
    public struct Banned
    {
        public static HashSet<int> PlantTypeBanned = new()
        {
            // Not a plant
            (int)PlantType.Nothing, (int)PlantType.MagnetInterface,
            (int)PlantType.MagnetBox, (int)PlantType.Pit,
            (int)PlantType.Refrash, (int)PlantType.Extract_single,
            (int)PlantType.Extract_ten,

            // EnumValueAsmResolver_002EDotNet_002ESerialized_002ESerializedConstant
            261,262,263,264,265,266,267,268,269,270,271,272,273,274,275,

            // Unreleased
            3000,
        };
        public static HashSet<int> ZombieTypeBanned = new()
        {
            // Not a zombie
            (int)ZombieType.Nothing,
        };
    }

    public virtual bool defaultHoldMode { get; } = false;
    public virtual bool defaultActive { get; } = false;

    public virtual void ResetBuiltIns()
    {
        if (KeyBind != null)
        {
            KeyBind.Reset();
        }

        HoldMode = defaultHoldMode;

        if (Active != defaultActive)
        {
            Toggle();
        }
    }
    public virtual bool SaveData { get; } = true;
    public virtual TranslationDomain Domain => ModuleManager.Domain;
    public virtual string TranslationSource => $"Modules/{Name}.json";
    #region Custom Saved Profile Data

    /// <summary>
    /// Arbitrary key-value store for module state (floats, ints, bools, strings, objects).
    /// Serialized into the profile save file by ModuleManager and restored on profile load.
    /// </summary>
    public Dictionary<string, object> SavedData { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public void SetSaved<T>(string key, T value)
    {
        SavedData[key] = value;
    }

    public T GetSaved<T>(string key, T defaultValue = default)
    {
        if (SavedData != null && SavedData.TryGetValue(key, out var raw))
        {
            if (raw is T typed) return typed;
            if (raw == null) return defaultValue;

            try
            {
                if (raw is JToken token)
                    return token.ToObject<T>();

                Type targetType = typeof(T);
                if (targetType.IsEnum)
                    return (T)Enum.ToObject(targetType, raw);

                return (T)Convert.ChangeType(raw, targetType);
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    #endregion

    #region Custom Module Translations & Regex

    /// <summary>
    /// Key-value pairs for module-specific phrases. Dumped into the module's 
    /// translation JSON and overwritten with translated values when a language loads.
    /// </summary>
    public Dictionary<string, string> SavedTranslationData { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Custom regex replacement rules (RegexPattern -> ReplacementTemplate).
    /// </summary>
    public Dictionary<string, string> SavedRegexTranslations { get; set; } = new();

    public void RegisterTranslation(string key, string defaultValue)
    {
        if (!SavedTranslationData.ContainsKey(key))
            SavedTranslationData[key] = defaultValue;
    }

    public void RegisterTranslations(Dictionary<string, string> translations)
    {
        if (translations == null) return;
        foreach (var kvp in translations)
        {
            if (!SavedTranslationData.ContainsKey(kvp.Key))
                SavedTranslationData[kvp.Key] = kvp.Value;
        }
    }

    public void RegisterRegexTranslation(string pattern, string replacement)
    {
        SavedRegexTranslations[pattern] = replacement;
    }

    /// <summary>
    /// Translates an input string checking module-specific translations and regexes first,
    /// then falling back to the module's translation domain file scope.
    /// </summary>
    public string Translate(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // 1. Exact match from SavedTranslationData
        if (SavedTranslationData != null && SavedTranslationData.TryGetValue(input, out string exact) && !string.IsNullOrEmpty(exact))
        {
            return exact;
        }

        // 2. Module-specific regex rules
        if (SavedRegexTranslations != null && SavedRegexTranslations.Count > 0)
        {
            foreach (var kvp in SavedRegexTranslations)
            {
                try
                {
                    Match match = Regex.Match(input, kvp.Key);
                    if (match.Success)
                    {
                        string template = kvp.Value;
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            if (match.Groups[i].Success)
                            {
                                string val = match.Groups[i].Value;
                                string sub = (SavedTranslationData != null && SavedTranslationData.TryGetValue(val, out var transSub))
                                    ? transSub
                                    : val;

                                template = template.Replace($"{{{i}}}", sub);
                            }
                        }
                        return template;
                    }
                }
                catch { }
            }
        }

        // 3. Fallback to ModuleManager.Domain scoped to this module's file
        if (Domain != null)
        {
            return Domain.Translate(input, TranslationSource);
        }

        return input;
    }

    public virtual void OnDomainLanguageChanged()
    {
        ModuleManager.SetDomain(this);
    }

    #endregion
}