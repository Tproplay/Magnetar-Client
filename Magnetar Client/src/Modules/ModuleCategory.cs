using System;
using System.Collections.Generic;
using System.Linq;
using Magnetar_Client.Core;
using Magnetar_Client.Core.ModuleManager_;
using Magnetar_Client.Api;



#if MELONLOADER || RELEASE_MELON
#endif

namespace Magnetar_Client.Modules;

public class ModuleCategory : IEquatable<ModuleCategory>
{
    private static int _nextId;
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
        _ = Level;
        _ = Tools;
        _ = Plant;
        _ = Zombie;
        _ = Misc;
        _ = Visual;

        Actions.OnModuleCategoyInitialized?.Invoke();
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
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

    public static bool operator ==(ModuleCategory left, ModuleCategory right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ModuleCategory left, ModuleCategory right) => !(left == right);
}
