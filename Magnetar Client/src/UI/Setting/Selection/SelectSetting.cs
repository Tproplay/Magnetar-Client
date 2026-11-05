using Magnetar_Client.Core;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using static Magnetar_Client.Utils.Translator;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// Abstract base class defining common state, rendering, and lifecycle behavior for all single-choice select dropdown settings.
/// </summary>
public abstract class BaseSelectSetting : Setting
{
    /// <summary>
    /// Gets or sets the untyped selected option key.
    /// </summary>
    public abstract object ValueRaw { get; set; }

    /// <summary>
    /// Gets the display name of the currently selected option.
    /// </summary>
    public abstract string CurrentValueDisplayName { get; }

    /// <summary>
    /// Gets the total number of options available in this setting.
    /// </summary>
    public abstract int OptionsCount { get; }

    /// <summary>
    /// Checks whether the option corresponding to the specified key is currently selected.
    /// </summary>
    public abstract bool IsSelectedRaw(object key);

    /// <summary>
    /// Sets the selected option to the key represented by the given raw object.
    /// </summary>
    public abstract void SetValueRaw(object key);

    /// <summary>
    /// Sets the value directly without invoking change events or checking <see cref="Setting.IsDisabled"/>.
    /// </summary>
    public abstract void SetDirectRaw(object val);

    /// <summary>
    /// Updates the default value to match the current selection.
    /// </summary>
    public abstract void SetCurrentAsDefault();

    /// <summary>
    /// Exports the current value in a format suitable for JSON serialization.
    /// </summary>
    public abstract object ExportSaveData();

    /// <summary>
    /// Restores the selected value from a deserialized JSON object.
    /// </summary>
    public abstract void ImportSaveData(object raw);

    /// <summary>
    /// Returns the sequence of renderable key-display pairs for the dropdown options list.
    /// </summary>
    public abstract IEnumerable<(object Key, string DisplayName)> GetRenderableOptions();

    /// <summary>
    /// Indicates whether this dropdown menu is currently open and focused.
    /// </summary>
    public override bool IsFocused =>
        (DrawSetting.ActiveDropdownId != -1 && DrawSetting.ActiveDropdownId == GetHashCode())
        || base.IsFocused;

    /// <summary>
    /// Renders the select dropdown setting control within the GUI window.
    /// </summary>
    /// <param name="y">The vertical drawing coordinate reference.</param>
    /// <param name="width">The total available width for the control.</param>
    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;
        int controlId = GetHashCode();

        bool prevGUIEnabled = GUI.enabled;
        if (IsDisabled)
        {
            if (DrawSetting.ActiveDropdownId == controlId)
            {
                DrawSetting.ResetDropdownState();
            }
            GUI.enabled = false;
        }
        float elemH = ClientConfig.elementHeight;
        try
        {
        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float resetBtnW = SettingValues.ResetButtonW;
        float btnW = ClientConfig.SettingWidth;

        // Label on the left: fills remaining horizontal space
        float labelW = width - (indent * 2f) - btnW - gap - resetBtnW;
        Rect labelRect = new(indent, y, labelW, elemH);
        GUI.Label(labelRect, DisplayName, ThemeManager.SettingLabelStyle);

        // Right-to-left layout: [Dropdown Button] [gap] [Reset Button]
        float resetStartX = width - indent - resetBtnW;
        float btnStartX = resetStartX - gap - btnW;

        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);
        Rect btnRect = new(btnStartX, y, btnW, elemH);

        string currentValName = CurrentValueDisplayName;

        if (!IsDisabled && btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            if (DrawSetting.ActiveDropdownId == controlId)
            {
                DrawSetting.ResetDropdownState();
            }
            else
            {
                DrawSetting.ActiveDropdownId = controlId;
                DrawSetting.dropdownScrollY = 0f;
                DrawSetting.dropdownScrollVelocity = 0f;
                DrawSetting.dropdownIsSwiping = false;
                DrawSetting.dropdownCandidateIndex = -1;
                DrawSetting.FocusedControlId = -1;
            }
            e.Use();
        }

        string arrow = (DrawSetting.ActiveDropdownId == controlId) ? " ▲" : " ▼";
        GUI.Box(btnRect, currentValName + arrow, ThemeManager.SettingOff);

        if (DrawSetting.ActiveDropdownId == controlId)
        {
            float rowHeight = SettingValues.DropdownRowHeight;
            int maxVisibleRows = SettingValues.DropdownMaxVisibleRows;
            int itemCount = OptionsCount;
            float dropHeight = Mathf.Min(itemCount * rowHeight, maxVisibleRows * rowHeight);
            float maxScroll = Mathf.Max(0f, (itemCount * rowHeight) - dropHeight);

            Rect dropRect = new(btnRect.x, btnRect.y + btnRect.height, btnRect.width, dropHeight);
            DrawSetting.ActiveDropdownWindowRect = new Rect(dropRect.x, dropRect.y + DrawSetting.CurrentWindowGroupOffsetY, dropRect.width, dropRect.height);

            // Momentum / Inertia decay
            if (!DrawSetting.dropdownIsSwiping && Mathf.Abs(DrawSetting.dropdownScrollVelocity) > 0.5f)
            {
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                DrawSetting.dropdownScrollY = Mathf.Clamp(DrawSetting.dropdownScrollY + (DrawSetting.dropdownScrollVelocity * dt), 0f, maxScroll);
                DrawSetting.dropdownScrollVelocity = Mathf.Lerp(DrawSetting.dropdownScrollVelocity, 0f, 1f - Mathf.Exp(-4.5f * dt));

                if (DrawSetting.dropdownScrollY <= 0f || DrawSetting.dropdownScrollY >= maxScroll || Mathf.Abs(DrawSetting.dropdownScrollVelocity) < 1f)
                {
                    DrawSetting.dropdownScrollVelocity = 0f;
                }
            }

            // Mouse ScrollWheel
            if (dropRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                DrawSetting.dropdownScrollVelocity = 0f;
                DrawSetting.dropdownScrollY = Mathf.Clamp(DrawSetting.dropdownScrollY + e.delta.y * SettingValues.DropdownScrollSensitivity, 0f, maxScroll);
                e.Use();
            }

            // MouseDown / TouchDown inside dropdown
            if (dropRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                DrawSetting.dropdownTouchStart = e.mousePosition;
                DrawSetting.dropdownTouchStartScroll = DrawSetting.dropdownScrollY;
                DrawSetting.dropdownIsSwiping = false;
                DrawSetting.dropdownScrollVelocity = 0f;
                DrawSetting.dropdownLastTouchY = e.mousePosition.y;
                DrawSetting.dropdownLastTouchTime = Time.realtimeSinceStartup;

                float localY = e.mousePosition.y - dropRect.y + DrawSetting.dropdownScrollY;
                DrawSetting.dropdownCandidateIndex = (int)(localY / rowHeight);
                e.Use();
            }

            // Drag / Swipe movement
            if (DrawSetting.dropdownCandidateIndex != -1 || DrawSetting.dropdownIsSwiping)
            {
                if ((e.type == EventType.MouseDrag || Input.GetMouseButton(0)) && !DrawSetting.dropdownIsSwiping)
                {
                    if (Vector2.Distance(e.mousePosition, DrawSetting.dropdownTouchStart) > GUIManager.S(6f))
                    {
                        DrawSetting.dropdownIsSwiping = true;
                        DrawSetting.dropdownCandidateIndex = -1;
                    }
                }

                if (DrawSetting.dropdownIsSwiping && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove || Input.GetMouseButton(0)))
                {
                    float deltaY = DrawSetting.dropdownTouchStart.y - e.mousePosition.y;
                    DrawSetting.dropdownScrollY = Mathf.Clamp(DrawSetting.dropdownTouchStartScroll + deltaY, 0f, maxScroll);

                    float dt = Time.realtimeSinceStartup - DrawSetting.dropdownLastTouchTime;
                    if (dt > 0.001f)
                    {
                        float rawVel = (DrawSetting.dropdownLastTouchY - e.mousePosition.y) / dt;
                        DrawSetting.dropdownScrollVelocity = Mathf.Lerp(DrawSetting.dropdownScrollVelocity, rawVel, 0.4f);
                        DrawSetting.dropdownLastTouchY = e.mousePosition.y;
                        DrawSetting.dropdownLastTouchTime = Time.realtimeSinceStartup;
                    }

                    if (e.type == EventType.MouseDrag) e.Use();
                }
            }

            // MouseUp / TouchUp
            if (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)
            {
                if (!DrawSetting.dropdownIsSwiping && DrawSetting.dropdownCandidateIndex >= 0)
                {
                    if (dropRect.Contains(e.mousePosition))
                    {
                        int idx = 0;
                        foreach (var opt in GetRenderableOptions())
                        {
                            if (idx == DrawSetting.dropdownCandidateIndex)
                            {
                                SetValueRaw(opt.Key);
                                DrawSetting.ResetDropdownState();
                                break;
                            }
                            idx++;
                        }
                    }
                    DrawSetting.dropdownCandidateIndex = -1;
                    e.Use();
                }
                else if (DrawSetting.dropdownIsSwiping)
                {
                    DrawSetting.dropdownIsSwiping = false;
                    DrawSetting.dropdownCandidateIndex = -1;
                    float timeSinceSample = Time.realtimeSinceStartup - DrawSetting.dropdownLastTouchTime;
                    if (timeSinceSample > 0.08f)
                    {
                        DrawSetting.dropdownScrollVelocity = 0f;
                    }
                    e.Use();
                }
            }

            // Click outside closes dropdown
            if (e.type == EventType.MouseDown && !btnRect.Contains(e.mousePosition) && !dropRect.Contains(e.mousePosition))
            {
                DrawSetting.ResetDropdownState();
            }

            float scrollY = DrawSetting.dropdownScrollY;
            DrawSetting.OnPostDraw += () =>
            {
                GUI.Box(dropRect, "", ThemeManager.SettingOff);
                GUI.BeginGroup(dropRect);
                int i = 0;
                foreach (var opt in GetRenderableOptions())
                {
                    float drawY = (i * rowHeight) - scrollY;
                    if (drawY + rowHeight > 0 && drawY < dropHeight)
                    {
                        Rect row = new(0, drawY, dropRect.width, rowHeight);
                        bool isSelected = IsSelectedRaw(opt.Key);
                        GUI.Box(row, opt.DisplayName, isSelected ? ThemeManager.SettingOn : ThemeManager.SettingOff);
                    }
                    i++;
                }
                GUI.EndGroup();

                if (maxScroll > 0)
                {
                    float scrollbarW = GUIManager.S(3f);
                    float scrollPct = scrollY / maxScroll;
                    float handleHeight = Mathf.Max(GUIManager.S(12f), dropHeight * (dropHeight / (itemCount * rowHeight)));
                    float handleY = dropRect.y + (scrollPct * (dropHeight - handleHeight));
                    Rect scrollbarRect = new(dropRect.x + dropRect.width - scrollbarW - GUIManager.S(1f), handleY, scrollbarW, handleHeight);
                    GUI.Box(scrollbarRect, "", ThemeManager.SettingOn);
                }
            };
        }

        if (!IsDisabled && DrawResetButton(resetRect))
        {
            Reset();
        }
        }
        finally
        {
            GUI.enabled = prevGUIEnabled;
        }

        y += elemH + ClientConfig.spacing;
    }
}

/// <summary>
/// Generic dropdown selection setting supporting strongly-typed keys.
/// </summary>
/// <typeparam name="T">The type of the option key.</typeparam>
public class SelectSetting<T> : BaseSelectSetting
{
    private T _value;

    /// <summary>
    /// The default selected option value restored when <see cref="Reset"/> is executed.
    /// </summary>
    public T DefaultValue;

    /// <summary>
    /// Dictionary of available options mapping typed keys to their default display names.
    /// </summary>
    public Dictionary<T, string> Options { get; set; } = new();

    /// <summary>
    /// Optional dictionary mapping typed keys to custom display name overrides.
    /// </summary>
    public Dictionary<T, string> CustomNames { get; set; } = new();

    /// <summary>
    /// Event invoked when the selected option value changes.
    /// </summary>
    public Action<T> OnSelectionChanged { get; set; }

    /// <summary>
    /// Gets or sets the currently selected option value.
    /// </summary>
    public T Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                OnSelectionChanged?.Invoke(_value);
            }
        }
    }

    public override object ValueRaw
    {
        get => Value;
        set => SetValueRaw(value);
    }

    public override int OptionsCount => Options?.Count ?? 0;

    public override string CurrentValueDisplayName
    {
        get
        {
            if (Options != null && Value != null && Options.ContainsKey(Value))
            {
                return (CustomNames != null && CustomNames.ContainsKey(Value)) ? CustomNames[Value] : Options[Value];
            }
            return Value?.ToString() ?? "Unknown";
        }
    }

    public SelectSetting(string name, T defaultValue = default)
    {
        Name = name;
        _value = defaultValue;
        DefaultValue = defaultValue;
    }

    public SelectSetting(string name, Dictionary<T, string> options, T defaultValue = default) : this(name, defaultValue)
    {
        if (options != null) Options = new Dictionary<T, string>(options);
    }

    /// <summary>
    /// Adds or updates an available option with the specified ID and display name.
    /// </summary>
    /// <param name="id">The key identifying the option.</param>
    /// <param name="displayName">The display text shown for the option.</param>
    public void AddOption(T id, string displayName) => Options[id] = displayName;

    /// <summary>
    /// Restores the selected value back to <see cref="DefaultValue"/>.
    /// </summary>
    public override void Reset()
    {
        if (IsDisabled) return;
        Value = DefaultValue;
    }

    /// <summary>
    /// Sets the value directly without invoking events or checking <see cref="Setting.IsDisabled"/>.
    /// </summary>
    public void SetDirect(T val) => _value = val;

    public override void SetDirectRaw(object val)
    {
        if (TryConvertKey(val, out T typedVal))
            _value = typedVal;
    }

    public override void SetCurrentAsDefault() => DefaultValue = Value;

    public override bool IsSelectedRaw(object key)
    {
        if (TryConvertKey(key, out T typedKey))
            return EqualityComparer<T>.Default.Equals(Value, typedKey);
        return false;
    }

    public override void SetValueRaw(object key)
    {
        if (TryConvertKey(key, out T typedKey))
            Value = typedKey;
    }

    public override IEnumerable<(object Key, string DisplayName)> GetRenderableOptions()
    {
        if (Options == null) yield break;
        foreach (var kvp in Options)
        {
            string disp = (CustomNames != null && CustomNames.ContainsKey(kvp.Key)) ? CustomNames[kvp.Key] : kvp.Value;
            yield return (kvp.Key, disp);
        }
    }

    public override object ExportSaveData() => Value;

    public override void ImportSaveData(object raw)
    {
        if (raw == null) return;
        if (TryConvertKey(raw, out T typedVal))
        {
            Value = typedVal;
        }
    }

    protected bool TryConvertKey(object rawKey, out T result)
    {
        if (rawKey is T exact)
        {
            result = exact;
            return true;
        }

        if (rawKey == null)
        {
            result = default;
            return false;
        }

        try
        {
            if (typeof(T).IsEnum)
            {
                if (rawKey is string strEnum)
                {
                    result = (T)Enum.Parse(typeof(T), strEnum, true);
                    return true;
                }
                result = (T)Enum.ToObject(typeof(T), rawKey);
                return true;
            }

            result = (T)Convert.ChangeType(rawKey, typeof(T));
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }
}

/// <summary>
/// A specialized <see cref="SelectSetting{T}"/> keyed by <see cref="string"/>, designed for dynamic string collections and external lists.
/// </summary>
public class StringSelectSetting : SelectSetting<string>
{
    public StringSelectSetting(string name, string defaultValue = "") : base(name, defaultValue)
    {
    }

    public StringSelectSetting(string name, Dictionary<string, string> options, string defaultValue = null)
        : base(name, options, defaultValue ?? (options != null && options.Count > 0 ? options.Keys.First() : ""))
    {
    }

    public StringSelectSetting(string name, IEnumerable<string> options, string defaultValue = null)
        : base(name, defaultValue ?? "")
    {
        if (options != null)
        {
            foreach (var opt in options)
            {
                if (opt != null)
                {
                    Options[opt] = opt;
                    if (string.IsNullOrEmpty(Value)) Value = opt;
                    if (string.IsNullOrEmpty(DefaultValue)) DefaultValue = opt;
                }
            }
        }
        if (defaultValue != null)
        {
            Value = defaultValue;
            DefaultValue = defaultValue;
        }
    }

    public StringSelectSetting(string name, string[] options, string defaultValue = null)
        : this(name, (IEnumerable<string>)options, defaultValue)
    {
    }

    /// <summary>
    /// Adds or updates an available option with identical key and display text.
    /// </summary>
    public void AddOption(string option) => AddOption(option, option);

    /// <summary>
    /// Clears and repopulates all available options from a string collection.
    /// </summary>
    public void SetOptions(IEnumerable<string> options)
    {
        Options.Clear();
        if (options != null)
        {
            foreach (var opt in options)
            {
                if (opt != null) Options[opt] = opt;
            }
        }
    }
}

/// <summary>
/// A setting representing a single-choice dropdown selection from a collection of options or an enum (keyed by int).
/// </summary>
public class SelectSetting : SelectSetting<int>
{
    /// <summary>
    /// The enum type used to populate options, or <see langword="null"/> if not created from an enum.
    /// </summary>
    public System.Type EnumType { get; private set; }

    /// <summary>
    /// Initializes a new instance of <see cref="SelectSetting"/> with a display name and default value.
    /// </summary>
    public SelectSetting(string name, int defaultValue) : base(name, defaultValue)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SelectSetting"/> with a display name, dictionary of options, and default value.
    /// </summary>
    public SelectSetting(string name, Dictionary<int, string> options, int defaultValue = 0)
        : base(name, options, defaultValue)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SelectSetting"/> populated from the values of an enum type.
    /// </summary>
    public SelectSetting(string name, System.Type enumType, int defaultValue) : base(name, defaultValue)
    {
        EnumType = enumType;

        if (enumType != null && enumType.IsEnum)
        {
            foreach (var val in Enum.GetValues(enumType))
                Options[Convert.ToInt32(val)] = val.ToString();
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SelectSetting"/> populated with sequentially-indexed strings.
    /// </summary>
    public SelectSetting(string name, List<string> options, int defaultValue = 0) : base(name, defaultValue)
    {
        if (options != null)
        {
            for (int i = 0; i < options.Count; i++)
            {
                Options[i] = options[i];
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SelectSetting"/> populated with sequentially-indexed strings.
    /// </summary>
    public SelectSetting(string name, string[] options, int defaultValue = 0)
        : this(name, options != null ? new List<string>(options) : null, defaultValue)
    {
    }
}
