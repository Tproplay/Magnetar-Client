using Magnetar_Client.Core;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using static Magnetar_Client.Utils.Translator;
using static Magnetar_Client.Utils.Magnetar_Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// Abstract base class defining common state, rendering, and lifecycle behavior for all multi-select settings.
/// </summary>
public abstract class BaseMultiSelectSetting : Setting
{
    /// <summary>
    /// The maximum number of items that can be selected simultaneously. Set to -1 for unlimited selection.
    /// </summary>
    public int MaxSelection = -1;

    /// <summary>
    /// Whether clicking an option automatically closes the selection window when <see cref="MaxSelection"/> is 1.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool CloseOnSingleSelect { get; set; } = false;

    /// <summary>
    /// Option internal names explicitly forbidden from being selected or rendered in selection views.
    /// </summary>
    public HashSet<string> NameBlacklist = new();

    /// <summary>
    /// Action invoked when the selection view is opened.
    /// </summary>
    public Action OnWindowOpen { get; set; }

    /// <summary>
    /// Whether the options inside the selection window should be sorted alphabetically by display name.
    /// </summary>
    public bool DisplayAlphabetically;

    /// <summary>
    /// Optional fixed width for the selected option / count text display area.
    /// If greater than 0, the text will be horizontally centered inside this width.
    /// </summary>
    public float Width { get; set; } = 0f;

    /// <summary>
    /// Whether to display the selected count or option text next to the select button.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool ShowCountText { get; set; } = true;

    /// <summary>
    /// Gets the number of currently selected options.
    /// </summary>
    public abstract int SelectedCount { get; }

    /// <summary>
    /// Gets the total number of configured options.
    /// </summary>
    public abstract int OptionsCount { get; }

    /// <summary>
    /// Resolves the display label of the first selected option, or empty string if none are selected.
    /// </summary>
    public abstract string GetPrimarySelectedDisplayName();

    /// <summary>
    /// Resolves the display label for the given option key, falling back to internal name or string representation.
    /// </summary>
    public abstract string ResolveDisplayName(object key, string fallback = null);

    /// <summary>
    /// Checks whether an option is blacklisted either by key or internal name.
    /// </summary>
    public abstract bool IsBlacklisted(object key, string internalName);

    /// <summary>
    /// Yields all renderable options as tuples of (key, display label, internal name) filtering out blacklisted entries.
    /// </summary>
    public abstract IEnumerable<(object Key, string DisplayName, string InternalName)> GetRenderableOptions();

    /// <summary>
    /// Checks whether the option corresponding to the specified key is currently selected.
    /// </summary>
    public abstract bool IsSelectedRaw(object key);

    /// <summary>
    /// Toggles the selection status of the specified option key.
    /// </summary>
    public abstract void ToggleRaw(object key);

    /// <summary>
    /// Selects the option corresponding to the specified key.
    /// </summary>
    public abstract void SelectRaw(object key);

    /// <summary>
    /// Deselects the option corresponding to the specified key.
    /// </summary>
    public abstract void DeselectRaw(object key);

    /// <summary>
    /// Clears all currently selected options.
    /// </summary>
    public abstract void ClearSelections();

    /// <summary>
    /// Selects all available valid options, optionally caching the state as default.
    /// </summary>
    public abstract void SelectAll(bool setDefault = true);

    /// <summary>
    /// Deselects all options.
    /// </summary>
    public abstract void DeselectAll();

    /// <summary>
    /// Captures the current selections as the default state for future resets.
    /// </summary>
    public abstract void SetCurrentAsDefault();

    /// <summary>
    /// Exports the selection state into a persistent DTO for serialization.
    /// </summary>
    public abstract object ExportSaveData();

    /// <summary>
    /// Restores the selection state from a deserialized JSON DTO or raw value.
    /// </summary>
    public abstract void ImportSaveData(object rawValue);

    private static GUIStyle _countTextStyle;

    /// <summary>
    /// Gets a centered count text style with font size reduced to 70% of the setting label size.
    /// </summary>
    public static GUIStyle MultiSelectCountTextStyle
    {
        get
        {
            if (ThemeManager.MultiSelectCountTextStyle != null)
                return ThemeManager.MultiSelectCountTextStyle;

            if (_countTextStyle == null)
            {
                _countTextStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false,
                    clipping = TextClipping.Clip
                };
            }
            if (ThemeManager.SettingLabelStyle != null)
            {
                _countTextStyle.fontSize = Mathf.Max(1, Mathf.RoundToInt(ThemeManager.SettingLabelStyle.fontSize * 0.7f));
                _countTextStyle.normal.textColor = ThemeManager.SettingLabelStyle.normal.textColor;
            }
            return _countTextStyle;
        }
    }

    /// <summary>
    /// Indicates whether this multi-select window/dialog is currently active and focused.
    /// </summary>
    public override bool IsFocused =>
        ReferenceEquals(DrawSetting.activeMultiSelect, this)
        || ReferenceEquals(Magnetar_Client.Core.ModuleManager_.MultiSelectWindowDrawer.ActiveMultiSelect, this)
        || base.IsFocused;

    /// <summary>
    /// Renders the setting row in the window, showing the label, current count (if enabled), select button, and reset button.
    /// </summary>
    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;
        float elemH = ClientConfig.elementHeight;
        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float resetBtnW = SettingValues.ResetButtonW;
        float selectBtnW = ClientConfig.selectButtonWidth;

        bool prevEnabled = GUI.enabled;
        Color origColor = GUI.contentColor;
        if (IsDisabled)
        {
            if (ReferenceEquals(DrawSetting.activeMultiSelect, this))
            {
                DrawSetting.activeMultiSelect = null;
            }
            GUI.enabled = false;
            GUI.contentColor = ThemeManager.TextDim;
        }
        try
        {
        string countText = null;
        float textW = 0f;

        if (ShowCountText)
        {
            if (MaxSelection == 1 && SelectedCount == 1)
            {
                countText = '(' + GetPrimarySelectedDisplayName() + ')';
            }
            else
            {
                countText = '(' + Translate($"{SelectedCount} selected") + ')';
            }

            textW = (Width > 0f) ? GUIManager.S(Width) : MultiSelectCountTextStyle.CalcSize(new GUIContent(countText)).x;
        }

        // Right-to-left layout: [Select Button] [gap] [Count Text (optional)] [gap] [Reset Button]
        float resetStartX = width - indent - resetBtnW;
        float countStartX = ShowCountText ? (resetStartX - gap - textW) : resetStartX;
        float btnStartX = countStartX - gap - selectBtnW;

        // Label fills the remaining space on the left
        float labelW = Mathf.Max(0f, btnStartX - indent - gap);
        Rect labelRect = new(indent, y, labelW, elemH);
        GUI.Label(labelRect, DisplayName, ThemeManager.SettingLabelStyle);

        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);
        Rect btnRect = new(btnStartX, y, selectBtnW, elemH);

        if (btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            if (!IsDisabled)
            {
                OnWindowOpen?.Invoke();
                DrawSetting.activeMultiSelect = this;
                DrawSetting.multiSelectSearchQuery = "";
                DrawSetting.manualScrollY = 0f;
                DrawSetting.targetScrollY = 0f;
            }
            e.Use();
        }

        GUI.Box(btnRect, Translate("Select"), ThemeManager.SettingOff);

        if (ShowCountText && !string.IsNullOrEmpty(countText))
        {
            Rect countRect = new(countStartX, y, textW, elemH);
            GUI.contentColor = ThemeManager.TextDim;
            GUI.Label(countRect, countText, MultiSelectCountTextStyle);
        }

        if (!IsDisabled && DrawResetButton(resetRect))
        {
            Reset();
        }
        }
        finally
        {
            GUI.contentColor = origColor;
            GUI.enabled = prevEnabled;
        }

        y += elemH + ClientConfig.spacing;
    }
}

/// <summary>
/// A generic multi-select setting supporting arbitrary option key types <typeparamref name="T"/>
/// (e.g. <see cref="string"/>, <see cref="int"/>, or custom identifier types).
/// </summary>
public class MultiSelectSetting<T> : BaseMultiSelectSetting
{
    /// <summary>
    /// The map of all available options, where the key is the option identifier and the value is its internal name.
    /// </summary>
    public Dictionary<T, string> Options { get; set; } = new();

    /// <summary>
    /// The collection of currently selected option keys.
    /// </summary>
    public HashSet<T> SelectedValues = new();

    /// <summary>
    /// The fallback set of option keys restored when <see cref="Reset"/> is executed.
    /// </summary>
    public HashSet<T> DefaultSelectedValues = new();

    /// <summary>
    /// Option keys explicitly forbidden from being selected or rendered in selection views.
    /// </summary>
    public HashSet<T> Blacklist = new();

    /// <summary>
    /// Callback triggered when an option's selection state changes.
    /// First parameter is the option key; second parameter is <c>true</c> if selected, <c>false</c> if deselected.
    /// </summary>
    public Action<T, bool> OnSelectionChanged { get; set; }

    /// <summary>
    /// Optional dictionary providing localized or custom formatted display names overriding default names.
    /// </summary>
    public Dictionary<T, string> CustomNames;

    public override int SelectedCount => SelectedValues.Count;
    public override int OptionsCount => Options.Count;

    public MultiSelectSetting(string name)
    {
        Name = name;
    }

    public MultiSelectSetting(string name, Dictionary<T, string> options)
    {
        Name = name;
        if (options != null)
        {
            foreach (var kvp in options)
            {
                Options[kvp.Key] = kvp.Value;
            }
        }
    }

    public MultiSelectSetting(string name, IEnumerable<T> items, Func<T, string> nameSelector = null)
    {
        Name = name;
        if (items != null)
        {
            foreach (var item in items)
            {
                if (item != null)
                {
                    Options[item] = nameSelector != null ? nameSelector(item) : item.ToString();
                }
            }
        }
    }

    public MultiSelectSetting(string name, T[] items) : this(name, (IEnumerable<T>)items)
    {
    }

    public string GetDisplayName(T id, string fallbackName = null)
    {
        if (CustomNames != null && CustomNames.TryGetValue(id, out string customName))
        {
            return customName;
        }

        if (Options.TryGetValue(id, out string baseName))
        {
            return baseName;
        }

        return fallbackName ?? id?.ToString() ?? "";
    }

    public override string ResolveDisplayName(object key, string fallback = null)
    {
        if (TryConvertKey(key, out T typedKey))
        {
            return GetDisplayName(typedKey, fallback);
        }
        return fallback ?? key?.ToString() ?? "";
    }

    public override string GetPrimarySelectedDisplayName()
    {
        if (SelectedValues.Count == 0) return string.Empty;
        var first = System.Linq.Enumerable.First(SelectedValues);
        return GetDisplayName(first);
    }

    public void AddOption(T id, string displayName) => Options[id] = displayName;

    public void RemoveOption(T id)
    {
        if (IsDisabled) return;
        if (Options.Remove(id) && SelectedValues.Remove(id))
            OnSelectionChanged?.Invoke(id, false);
    }

    public void Toggle(T id)
    {
        if (IsDisabled) return;
        if (IsSelected(id)) Deselect(id);
        else Select(id);
    }

    public void Select(T id)
    {
        if (IsDisabled || SelectedValues.Contains(id) || (Blacklist != null && Blacklist.Contains(id))) return;
        if (MaxSelection == -1 || SelectedValues.Count < MaxSelection)
        {
            SelectedValues.Add(id);
            OnSelectionChanged?.Invoke(id, true);
        }
    }

    public void Deselect(T id)
    {
        if (IsDisabled) return;
        if (SelectedValues.Remove(id))
            OnSelectionChanged?.Invoke(id, false);
    }

    public bool IsSelected(T id) => SelectedValues.Contains(id);

    public void SetDirect(T id)
    {
        SelectedValues.Clear();
        SelectedValues.Add(id);
    }

    public void SetDirect(IEnumerable<T> ids)
    {
        SelectedValues.Clear();
        if (ids != null)
        {
            foreach (var id in ids)
                SelectedValues.Add(id);
        }
    }

    public override void Reset()
    {
        if (IsDisabled) return;
        var old = new HashSet<T>(SelectedValues);
        SelectedValues.Clear();
        foreach (var v in DefaultSelectedValues) SelectedValues.Add(v);
        foreach (var v in old)
        {
            if (!SelectedValues.Contains(v))
                OnSelectionChanged?.Invoke(v, false);
        }
        foreach (var v in SelectedValues)
        {
            if (!old.Contains(v))
                OnSelectionChanged?.Invoke(v, true);
        }
    }

    public override void SelectAll(bool setDefault = true)
    {
        SelectedValues.Clear();
        foreach (var key in Options.Keys)
        {
            if (Blacklist != null && Blacklist.Contains(key)) continue;
            if (NameBlacklist != null && NameBlacklist.Contains(Options[key])) continue;

            if (MaxSelection == -1 || SelectedValues.Count < MaxSelection)
            {
                SelectedValues.Add(key);
            }
        }

        if (setDefault)
        {
            SetCurrentAsDefault();
        }
    }

    public override void DeselectAll()
    {
        if (IsDisabled) return;
        var old = new List<T>(SelectedValues);
        SelectedValues.Clear();
        for (int i = 0; i < old.Count; i++)
        {
            OnSelectionChanged?.Invoke(old[i], false);
        }
    }

    public override void SetCurrentAsDefault()
    {
        DefaultSelectedValues = new HashSet<T>(SelectedValues);
    }

    public override bool IsSelectedRaw(object key)
    {
        if (TryConvertKey(key, out T typedKey)) return IsSelected(typedKey);
        return false;
    }

    public override void ToggleRaw(object key)
    {
        if (TryConvertKey(key, out T typedKey)) Toggle(typedKey);
    }

    public override void SelectRaw(object key)
    {
        if (TryConvertKey(key, out T typedKey)) Select(typedKey);
    }

    public override void DeselectRaw(object key)
    {
        if (TryConvertKey(key, out T typedKey)) Deselect(typedKey);
    }

    public override void ClearSelections()
    {
        SelectedValues.Clear();
    }

    public override bool IsBlacklisted(object key, string internalName)
    {
        if (TryConvertKey(key, out T typedKey) && Blacklist != null && Blacklist.Contains(typedKey))
            return true;
        if (!string.IsNullOrEmpty(internalName) && NameBlacklist != null && NameBlacklist.Contains(internalName))
            return true;
        return false;
    }

    public override IEnumerable<(object Key, string DisplayName, string InternalName)> GetRenderableOptions()
    {
        foreach (var kvp in Options)
        {
            if (Blacklist != null && Blacklist.Contains(kvp.Key)) continue;
            if (NameBlacklist != null && NameBlacklist.Contains(kvp.Value)) continue;
            string displayName = GetDisplayName(kvp.Key, kvp.Value);
            yield return (kvp.Key, displayName, kvp.Value);
        }
    }

    public override object ExportSaveData()
    {
        var validOptions = Options.Keys
            .Where(k => (Blacklist == null || !Blacklist.Contains(k)) &&
                        (NameBlacklist == null || !NameBlacklist.Contains(Options[k])))
            .ToList();

        int totalCount = validOptions.Count;
        int selectedCount = SelectedValues.Count;

        if (totalCount > 0 && selectedCount >= (totalCount / 2))
        {
            var unselected = validOptions.Where(k => !SelectedValues.Contains(k)).Select(k => (object)k).ToList();
            return new SaveLoadData.MultiSelectSaveData
            {
                Mode = "Deselected",
                Values = unselected
            };
        }
        else
        {
            return new SaveLoadData.MultiSelectSaveData
            {
                Mode = "Selected",
                Values = SelectedValues.Select(k => (object)k).ToList()
            };
        }
    }

    public override void ImportSaveData(object rawValue)
    {
        try
        {
            string jsonStr = JsonConvert.SerializeObject(rawValue);
            var proxy = JsonConvert.DeserializeObject<SaveLoadData.MultiSelectSaveData>(jsonStr);

            if (proxy != null)
            {
                SelectedValues.Clear();

                var validOptions = Options.Keys
                    .Where(k => (Blacklist == null || !Blacklist.Contains(k)) &&
                                (NameBlacklist == null || !NameBlacklist.Contains(Options[k])))
                    .ToList();

                var convertedValues = new HashSet<T>();
                if (proxy.Values != null)
                {
                    foreach (var obj in proxy.Values)
                    {
                        if (TryConvertKey(obj, out T converted))
                        {
                            convertedValues.Add(converted);
                        }
                    }
                }

                if (string.Equals(proxy.Mode, "Deselected", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var opt in validOptions)
                    {
                        if (!convertedValues.Contains(opt))
                        {
                            SelectedValues.Add(opt);
                        }
                    }
                }
                else
                {
                    foreach (var val in convertedValues)
                    {
                        if (Blacklist == null || !Blacklist.Contains(val))
                        {
                            SelectedValues.Add(val);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AutoSaveLogger.Error($"Error importing multi-select setting '{Name}': {ex.Message}");
        }
    }

    protected static bool TryConvertKey(object key, out T result)
    {
        if (key is T direct)
        {
            result = direct;
            return true;
        }
        if (key == null)
        {
            result = default;
            return false;
        }
        try
        {
            if (typeof(T) == typeof(int))
            {
                result = (T)(object)Convert.ToInt32(key);
                return true;
            }
            if (typeof(T) == typeof(string))
            {
                result = (T)(object)key.ToString();
                return true;
            }
            result = (T)Convert.ChangeType(key, typeof(T));
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
/// A specialized <see cref="MultiSelectSetting{T}"/> keyed by <see cref="string"/>, designed for dynamic collections
/// (e.g. file paths, external audio lists, mod names) where integer indices shift when options are modified externally.
/// </summary>
public class StringMultiSelectSetting : MultiSelectSetting<string>
{
    public StringMultiSelectSetting(string name) : base(name)
    {
    }

    public StringMultiSelectSetting(string name, Dictionary<string, string> options) : base(name, options)
    {
    }

    public StringMultiSelectSetting(string name, IEnumerable<string> options) : base(name, options)
    {
    }

    public StringMultiSelectSetting(string name, string[] options) : base(name, options)
    {
    }
}

/// <summary>
/// An integer-keyed multi-select setting primarily used for Enums and index-based collections.
/// </summary>
public class MultiSelectSetting : MultiSelectSetting<int>
{
    /// <summary>
    /// The underlying Enum type if this setting was constructed from an Enum.
    /// </summary>
    public Type EnumType { get; private set; }

    public MultiSelectSetting(string name) : base(name)
    {
    }

    public MultiSelectSetting(string name, Type enumType) : base(name)
    {
        EnumType = enumType;
        if (enumType != null && enumType.IsEnum)
        {
            foreach (var val in Enum.GetValues(enumType))
                Options[Convert.ToInt32(val)] = val.ToString();
        }
    }

    public MultiSelectSetting(string name, Dictionary<int, string> options) : base(name, options)
    {
    }

    public MultiSelectSetting(string name, List<string> options) : base(name)
    {
        if (options != null)
        {
            for (int i = 0; i < options.Count; i++)
            {
                Options[i] = options[i];
            }
        }
    }

    public MultiSelectSetting(string name, string[] options) : base(name)
    {
        if (options != null)
        {
            for (int i = 0; i < options.Length; i++)
            {
                Options[i] = options[i];
            }
        }
    }
}
