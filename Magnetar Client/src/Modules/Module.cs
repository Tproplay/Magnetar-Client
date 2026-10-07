using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Magnetar_Client.Core;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;
using Magnetar_Client.Core.ModuleManager_;


#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class ModuleCategory : IEquatable<ModuleCategory>
{
    private static int _nextId = 0;
    private static readonly Dictionary<string, ModuleCategory> _registeredCategories = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<ModuleCategory> _allCategories = new();

    public static IReadOnlyList<ModuleCategory> AllCategories => _allCategories;

    // --- Built-in Categories registered upfront in your designated order ---
    public static readonly ModuleCategory Level = RegisterInternal("Level", 10);
    public static readonly ModuleCategory Tools = RegisterInternal("Tools", 20);
    public static readonly ModuleCategory Plant = RegisterInternal("Plant", 30);
    public static readonly ModuleCategory Zombie = RegisterInternal("Zombie", 40);
    public static readonly ModuleCategory Misc = RegisterInternal("Misc", 50);
    public static readonly ModuleCategory Visual = RegisterInternal("Visual", 60);

    // Instance variables
    public int Id { get; }
    public string Name { get; }
    public bool IsCustom { get; }
    public int DisplayOrder { get; set; }

    private ModuleCategory(string name, int id, bool isCustom, int displayOrder = 100)
    {
        Name = name;
        Id = id;
        IsCustom = isCustom;
        DisplayOrder = displayOrder;
    }

    private static ModuleCategory RegisterInternal(string name, int displayOrder)
    {
        if (_registeredCategories.TryGetValue(name, out var existing))
            return existing;

        var cat = new ModuleCategory(name, _nextId++, false, displayOrder);
        _registeredCategories[name] = cat;
        _allCategories.Add(cat);
        return cat;
    }

    /// <summary>
    /// Explicitly initializes and registers the built-in categories so they exist
    /// and occupy layout slots before any module accesses or assigns them.
    /// </summary>
    public static void Init()
    {
        // Forces static constructor execution to ensure built-ins are populated
        _ = Level;
        _ = Tools;
        _ = Plant;
        _ = Zombie;
        _ = Misc;
        _ = Visual;
    }

    /// <summary>
    /// Registers a new category or returns an existing one if already registered.
    /// </summary>
    public static ModuleCategory Register(string name, int displayOrder = 100)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be null or empty.", nameof(name));

        string cleanName = name.Trim();
        if (_registeredCategories.TryGetValue(cleanName, out var existing))
            return existing;

        var cat = new ModuleCategory(cleanName, _nextId++, true, displayOrder);
        _registeredCategories[cleanName] = cat;
        _allCategories.Add(cat);

        CategoryWindowDrawer.EnsureCategoryInitialized(cat);
        return cat;
    }

    public static ModuleCategory Get(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        _registeredCategories.TryGetValue(name.Trim(), out var cat);
        return cat;
    }

    public static ModuleCategory GetOrCreate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Misc;
        return Get(name) ?? Register(name);
    }

    public static bool TryGet(string name, out ModuleCategory category)
    {
        category = Get(name);
        return category != null;
    }

    // --- Query Methods ---
    public List<Modules.Module> GetAllModules()
    {
        return ModuleManager.Modules.Where(m => m.Category == this).ToList();
    }

    public Modules.Module Find(string moduleName)
    {
        if (string.IsNullOrEmpty(moduleName)) return null;
        return ModuleManager.Modules.FirstOrDefault(m =>
            m.Category == this && string.Equals(m.Name, moduleName, StringComparison.OrdinalIgnoreCase));
    }

    public Modules.Module Find(Predicate<Modules.Module> match)
    {
        if (match == null) return null;
        return ModuleManager.Modules.FirstOrDefault(m => m.Category == this && match(m));
    }

    public List<Modules.Module> FindAll(Predicate<Modules.Module> match)
    {
        if (match == null) return new List<Modules.Module>();
        return ModuleManager.Modules.Where(m => m.Category == this && match(m)).ToList();
    }

    // --- Conversion & Equality ---
    public static implicit operator ModuleCategory(string name) => GetOrCreate(name);
    public static implicit operator string(ModuleCategory cat) => cat?.Name;

    public override string ToString() => Name;

    public override bool Equals(object obj) => obj is ModuleCategory other && Equals(other);

    public bool Equals(ModuleCategory other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

    public static bool operator ==(ModuleCategory left, ModuleCategory right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ModuleCategory left, ModuleCategory right) => !(left == right);
}

public abstract class Module
{
    public abstract string Name { get; set; }
    public abstract string SearchHints { get; set; }
    public virtual string Author { get; set; } = "";
    public abstract string Description { get; set; }

    /// <summary>
    /// Category object. Can be assigned via `ModuleCategory.Plant` or as a string `"MyCategory"`.
    /// </summary>
    public abstract ModuleCategory Category { get; set; }

    public virtual bool enableInVanillaMode { get; set; } = false;

    public BindSetting KeyBind = new("Keybind");
    public string GetBindString() => KeyBind.GetBindString();
    public List<KeyCode> BindKeys => KeyBind.BindKeys;
    public virtual bool HoldMode { get; set; } = false;
    public virtual bool Active { get; set; } = false;
    public virtual bool ShowSettings { get; set; } = false;

    public List<Setting> Settings = new();

    public void Toggle()
    {
        Active = !Active;
        if (Active) OnEnable();
        else OnDisable();
    }

    public virtual void OnEnable() { }
    public virtual void OnDisable() { }
    public virtual void OnUpdate() { if (Active) OnUpdateActive(); }
    public virtual void OnUpdateActive() { }
    public virtual void OnGUI() { }

    public void AddSettings(params Setting[] settings) => Settings.AddRange(settings);
    public virtual bool Initialized { get; set; } = false;
    public virtual void OnLanguageChanged() { }

    public static Dictionary<int, string> TranslatedNames(Type enumType)
    {
        if (enumType == null || !enumType.IsEnum) return new Dictionary<int, string>();

        Dictionary<int, string> names = new();
#if ANDROID
        foreach (var val in Enum.GetValues(enumType))
        {
            int key = Convert.ToInt32(val);
            string name = Enum.GetName(enumType, val) ?? val.ToString();
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

    public void CreateCategory(string name, bool defaultExpanded = true) => Settings.Add(new CategorySetting(name, defaultExpanded));
    public void EndCategory() => Settings.Add(new EndCategorySetting());

    public static bool GetKeyComboDown(List<KeyCode> keyCodes)
    {
        if (keyCodes == null || keyCodes.Count == 0) return false;

        KeyCode triggerKey = keyCodes[keyCodes.Count - 1];
        if (!Input.GetKeyDown(triggerKey)) return false;

        for (int i = 0; i < keyCodes.Count - 1; i++)
        {
            if (!Input.GetKey(keyCodes[i])) return false;
        }
        return true;
    }

    public static bool GetKeyCombo(List<KeyCode> keyCodes)
    {
        if (keyCodes == null || keyCodes.Count == 0) return false;

        foreach(KeyCode keyCode in keyCodes)
        {
            if (!Input.GetKey(keyCode)) return false;
        }
        return true;
    }

    public struct Banned
    {
        public static readonly HashSet<int> PlantTypeBanned = new()
        {
            (int)PlantType.Nothing, (int)PlantType.MagnetInterface,
            (int)PlantType.MagnetBox, (int)PlantType.Pit,
            (int)PlantType.Refrash, (int)PlantType.Extract_single,
            (int)PlantType.Extract_ten,
            261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275,
            3000,
        };
        public static readonly HashSet<int> ZombieTypeBanned = new()
        {
            (int)ZombieType.Nothing,
        };
    }

    public virtual bool defaultHoldMode { get; } = false;
    public virtual bool defaultActive { get; } = false;

    public virtual void ResetBuiltIns()
    {
        KeyBind?.Reset();
        HoldMode = defaultHoldMode;
        if (Active != defaultActive) Toggle();
    }
    public virtual bool SaveData { get; } = true;
}