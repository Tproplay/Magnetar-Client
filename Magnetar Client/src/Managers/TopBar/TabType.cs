using System;
using System.Collections.Generic;

namespace Magnetar_Client.Core;

public class TabType : IEquatable<TabType>
{
    public const string AnimationGroup = "Tabs";
    private static int _nextId = 0;
    private static readonly Dictionary<string, TabType> _registeredTabs = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<TabType> _allTabs = new();

    public static IReadOnlyList<TabType> AllTabs => _allTabs;

    public int Id { get; }
    public string Name { get; }
    public bool IsCustom { get; }
    public Action OnSelected { get; set; }
    public Action OnDeselected { get; set; }
    public Action OnGUI { get; set; }

    // Predefined built-in tabs (mirrors original enum values)
    public static readonly TabType MODULES = RegisterInternal("MODULES");
    public static readonly TabType HUD = RegisterInternal("HUD");
    public static readonly TabType GUI = RegisterInternal("GUI");
    public static readonly TabType NEF = RegisterInternal("NEF");
    public static readonly TabType PROFILE = RegisterInternal("PROFILE");

    private TabType(string name, int id, bool isCustom, Action onSelected = null, Action onDeselected = null, Action onGUI = null)
    {
        Name = name;
        Id = id;
        IsCustom = isCustom;
        OnSelected = onSelected;
        OnDeselected = onDeselected;
        OnGUI = onGUI;
    }

    private static TabType RegisterInternal(string name)
    {
        if (_registeredTabs.TryGetValue(name, out var existing)) return existing;
        var tab = new TabType(name, _nextId++, false);
        _registeredTabs[name] = tab;
        _allTabs.Add(tab);
        return tab;
    }

    /// <summary>
    /// Registers a new custom tab. Automatically updates TopBar geometry.
    /// </summary>
    public static TabType Register(string name, Action onSelected = null, Action onDeselected = null, Action onGUI = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tab name cannot be null or empty.", nameof(name));

        string cleanName = name.Trim();
        if (_registeredTabs.TryGetValue(cleanName, out var existing))
        {
            if (onSelected != null) existing.OnSelected = onSelected;
            if (onDeselected != null) existing.OnDeselected = onDeselected;
            if (onGUI != null) existing.OnGUI = onGUI;
            return existing;
        }

        var tab = new TabType(cleanName, _nextId++, true, onSelected, onDeselected, onGUI);
        _registeredTabs[cleanName] = tab;
        _allTabs.Add(tab);

        // Dynamically recalculate TopBar widths
        TopBar.RebuildOffsets();
        return tab;
    }

    public static TabType Get(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        _registeredTabs.TryGetValue(name.Trim(), out var tab);
        return tab;
    }

    public static TabType GetOrCreate(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return Get(name) ?? Register(name);
    }

    public static bool TryParse(string name, out TabType tab)
    {
        tab = Get(name);
        return tab != null;
    }

    public static implicit operator TabType(string name) => GetOrCreate(name);
    public static implicit operator string(TabType tab) => tab?.Name;

    public override string ToString() => Name;

    public override bool Equals(object obj) => obj is TabType other && Equals(other);

    public bool Equals(TabType other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

    public static bool operator ==(TabType left, TabType right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(TabType left, TabType right) => !(left == right);
}
