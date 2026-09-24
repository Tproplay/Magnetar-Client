using Magnetar_Client.Utils;
using Magnetar_Client.UI.Setting;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public enum ModuleCategory
{
    Level,
    Tools,
    Plant,
    Zombie,
    Misc,
    Visual,
    Addon
}


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
    public virtual bool Initialized { get; set; } = false;

    /// <summary>
    /// Runs When a the mod's language is changed
    /// </summary>
    public virtual void OnLanguageChanged() { }

    public static Dictionary<int, string> TranslatedNames(System.Type enumType)
    {
        if (enumType == null || !enumType.IsEnum) return new Dictionary<int, string>();

        Dictionary<int, string> names = new();

#if ANDROID
        foreach (var val in System.Enum.GetValues(enumType))
        {
            int key = System.Convert.ToInt32(val);
            string name = System.Enum.GetName(enumType, val) ?? val.ToString();
            names[key] = $"{name} ({key})";
        }
#else
        names = Translator.TranslateEnum(enumType);

        foreach (var kvp in names.ToList())
        {
            names[kvp.Key] = $"{kvp.Value} ({kvp.Key})";
}
#endif

        return names;
    }

    public virtual float SettingsWidth { get; set; } = Config.ModuleManager.SettingsWidth;

    // Add these category helper methods anywhere inside the ModuleManager class
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

    public virtual bool defaultHoldMode { get; set; } = false;
    public virtual bool defaultActive { get; set; } = false;

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

}
