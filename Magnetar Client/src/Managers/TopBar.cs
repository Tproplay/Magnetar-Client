using Magnetar_Client.UI.Themes;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Utils.Translator;
using Magnetar_Client.Core.Lifecycle;

namespace Magnetar_Client.Core;

public class TabType : IEquatable<TabType>
{
    private static int _nextId = 0;
    private static readonly Dictionary<string, TabType> _registeredTabs = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<TabType> _allTabs = new();

    public static IReadOnlyList<TabType> AllTabs => _allTabs;

    public int Id { get; }
    public string Name { get; }
    public bool IsCustom { get; }
    public Action OnSelected { get; set; }
    public Action OnGUI { get; set; }

    // Predefined built-in tabs (mirrors original enum values)
    public static readonly TabType MODULES = RegisterInternal("MODULES");
    public static readonly TabType HUD = RegisterInternal("HUD");
    public static readonly TabType GUI = RegisterInternal("GUI");
    public static readonly TabType NEF = RegisterInternal("NEF");
    public static readonly TabType PROFILE = RegisterInternal("PROFILE");

    private TabType(string name, int id, bool isCustom, Action onSelected = null, Action onGUI = null)
    {
        Name = name;
        Id = id;
        IsCustom = isCustom;
        OnSelected = onSelected;
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
    public static TabType Register(string name, Action onSelected = null, Action onGUI = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tab name cannot be null or empty.", nameof(name));

        string cleanName = name.Trim();
        if (_registeredTabs.TryGetValue(cleanName, out var existing))
        {
            if (onSelected != null) existing.OnSelected = onSelected;
            if (onGUI != null) existing.OnGUI = onGUI;
            return existing;
        }

        var tab = new TabType(cleanName, _nextId++, true, onSelected, onGUI);
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
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

    public static bool operator ==(TabType left, TabType right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null)) return false;
        return left.Equals(right);
    }

    public static bool operator !=(TabType left, TabType right) => !(left == right);
}

public static class TopBar
{
    private static readonly List<TabType> Order = new();
    private static readonly Dictionary<TabType, float> BaseWidth = new();
    private static float[] baseOffsets = Array.Empty<float>();

    private const float BaseHeight = 30f;

    public static void Init()
    {
        RebuildOffsets();
        ServiceRegistry.Register(new TopBarService());
    }

    /// <summary>
    /// Registers a custom tab directly from TopBar.
    /// </summary>
    public static TabType RegisterTab(string name, Action onSelected = null, Action onGUI = null)
    {
        return TabType.Register(name, onSelected, onGUI);
    }

    /// <summary>
    /// Recalculates base tab widths and cumulative offsets whenever tabs are registered.
    /// </summary>
    public static void RebuildOffsets()
    {
        Order.Clear();
        BaseWidth.Clear();

        foreach (TabType tab in TabType.AllTabs)
        {
            string translated = Translate(tab.Name);
            float btnWidth = Mathf.Max(translated.Length * 13f, 50f);

            Order.Add(tab);
            BaseWidth[tab] = btnWidth;
        }

        baseOffsets = new float[Order.Count + 1];
        float running = 0f;
        for (int i = 0; i < Order.Count; i++)
        {
            baseOffsets[i] = running;
            running += BaseWidth[Order[i]];
        }
        baseOffsets[Order.Count] = running;
    }

    public static void Render()
    {
        int count = Order.Count;
        if (count == 0) return;

        float[] scaledOffsets = new float[count + 1];
        for (int i = 0; i <= count; i++)
        {
            scaledOffsets[i] = Mathf.Round(Config.S(baseOffsets[i]));
        }

        float scaledTotalWidth = scaledOffsets[count];
        float scaledHeight = Mathf.Round(Config.S(BaseHeight));
        float startX = Mathf.Round((Config.NativeWidth / 2f) - (scaledTotalWidth / 2f));

        Rect barArea = new(startX, 0, scaledTotalWidth, scaledHeight);

        if (barArea.Contains(Event.current.mousePosition))
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                Input.ResetInputAxes();
            }
        }

        Rect barGroupRect = new(startX, 0, scaledTotalWidth, scaledHeight);
        GUI.BeginGroup(barGroupRect);

        Event e = Event.current;

        for (int i = 0; i < count; i++)
        {
            TabType tab = Order[i];
            float btnX = scaledOffsets[i];
            float btnW = (scaledOffsets[i + 1] - scaledOffsets[i]) + (i < count - 1 ? 1f : 0f);

            Rect btnRect = new(btnX, 0, btnW, scaledHeight);
            string displayName = Translate(tab.Name);

            bool isSelected = (Config.CurrentTab == tab);
            GUIStyle btnStyle = isSelected
                ? ThemeManager.TopBarActiveStyle
                : ThemeManager.TopBarStyle;

            // Render button visuals
            GUI.Box(btnRect, displayName, btnStyle);

            if (e.type == EventType.MouseDown && e.button == 0 && btnRect.Contains(e.mousePosition))
            {
                Config.CurrentTab = tab;
                tab.OnSelected?.Invoke();
                e.Use();
            }
        }

        GUI.EndGroup();
    }

    private class TopBarService : IInitializable, IWarmUp, IMenuRenderable, ILanguageAware
    {
        public string Name => "TopBar";
        public int Priority => ServicePriority.Highest;

        public void Initialize() => TopBar.RebuildOffsets();
        public void OnWarmUp() => TopBar.RebuildOffsets();

        public void OnMenuGUI()
        {
            TopBar.Render();

            // Invoke custom tab render delegates if registered
            if (Config.CurrentTab != null && Config.CurrentTab.IsCustom)
            {
                Config.CurrentTab.OnGUI?.Invoke();
            }
        }

        public void OnLanguageChanged()
        {
            TopBar.RebuildOffsets();
        }
    }
}