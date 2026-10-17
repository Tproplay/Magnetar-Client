using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using static Magnetar_Client.Utils.Translator;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// A setting for having multiple options selectable from a list
/// </summary>
public class MultiSelectSetting : Setting
{
    /// <summary>
    /// The maximum number of items that can be selected simultaneously. Set to -1 for unlimited selection.
    /// </summary>
    public int MaxSelection = -1;

    /// <summary>
    /// The map of all available options, where the key is the option's integer ID and the value is its internal name.
    /// </summary>
    public Dictionary<int, string> Options { get; set; } = new();

    /// <summary>
    /// The collection of currently selected option IDs.
    /// </summary>
    public HashSet<int> SelectedValues = new();

    /// <summary>
    /// The fallback set of option IDs restored when <see cref="Reset"/> is executed.
    /// </summary>
    public HashSet<int> DefaultSelectedValues = new();

    /// <summary>
    /// Option IDs explicitly forbidden from being selected or rendered in selection views.
    /// </summary>
    public HashSet<int> Blacklist = new();

    /// <summary>
    /// Option internal names explicitly forbidden from being selected or rendered in selection views.
    /// </summary>
    public HashSet<string> NameBlacklist = new();

    /// <summary>
    /// The underlying Enum type if this setting was constructed from an Enum.
    /// </summary>
    public Type EnumType { get; private set; }

    /// <summary>
    /// Callback triggered when an option's selection state changes.
    /// First parameter is the option ID; second parameter is <c>true</c> if selected, <c>false</c> if deselected.
    /// </summary>
    public Action<int, bool> OnSelectionChanged { get; set; }

    /// <summary>
    /// Action invoked when the selection view is opened.
    /// </summary>
    public Action OnWindowOpen { get; set; }

    /// <summary>
    /// Optional dictionary providing localized or custom formatted display names overriding default names.
    /// </summary>
    public Dictionary<int, string> CustomNames;

    /// <summary>
    /// Whether the options inside the selection window should be sorted alphabetically by display name.
    /// </summary>
    public bool DisplayAlphabetically;

    /// <summary>
    /// Initializes an empty <see cref="MultiSelectSetting"/> with the specified display name.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    public MultiSelectSetting(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Initializes a new <see cref="MultiSelectSetting"/> populated with the integer values and names of an enum.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="enumType">The enum type whose entries define the selectable options.</param>
    public MultiSelectSetting(string name, System.Type enumType)
    {
        Name = name;
        EnumType = enumType;

        if (enumType != null && enumType.IsEnum)
        {
            foreach (var val in Enum.GetValues(enumType))
                Options[Convert.ToInt32(val)] = val.ToString();
        }
    }

    /// <summary>
    /// Initializes a new <see cref="MultiSelectSetting"/> populated from an existing dictionary of options.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="options">Dictionary mapping integer IDs to option display labels.</param>
    public MultiSelectSetting(string name, Dictionary<int, string> options)
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

    /// <summary>
    /// Initializes a new <see cref="MultiSelectSetting"/> from a list of option strings.
    /// Each item's list index is used as its option ID.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="options">List of option labels.</param>
    public MultiSelectSetting(string name, List<string> options)
    {
        Name = name;
        if (options != null)
        {
            for (int i = 0; i < options.Count; i++)
            {
                Options[i] = options[i];
            }
        }
    }

    /// <summary>
    /// Initializes a new <see cref="MultiSelectSetting"/> from an array of option strings.
    /// Each item's array index is used as its option ID.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="options">Array of option labels.</param>
    public MultiSelectSetting(string name, string[] options)
    {
        Name = name;
        if (options != null)
        {
            for (int i = 0; i < options.Length; i++)
            {
                Options[i] = options[i];
            }
        }
    }

    /// <summary>
    /// Resolves the user-facing display name for a given option ID. Checks <see cref="CustomNames"/> first,
    /// then <see cref="Options"/>, and finally falls back to <paramref name="fallbackName"/> or the ID string.
    /// </summary>
    /// <param name="id">The option ID to look up.</param>
    /// <param name="fallbackName">Fallback text to use if no key match is found.</param>
    /// <returns>The resolved display string.</returns>
    public string GetDisplayName(int id, string fallbackName = null)
    {
        if (CustomNames != null && CustomNames.TryGetValue(id, out string customName))
        {
            return customName;
        }

        if (Options.TryGetValue(id, out string baseName))
        {
            return baseName;
        }

        return fallbackName ?? id.ToString();
    }

    /// <summary>
    /// Adds or updates an option in the available options dictionary.
    /// </summary>
    /// <param name="id">The unique identifier of the option.</param>
    /// <param name="displayName">The label shown for the option.</param>
    public void AddOption(int id, string displayName) => Options[id] = displayName;

    /// <summary>
    /// Removes an option from both available options and current selections.
    /// </summary>
    /// <param name="id">The option ID to remove.</param>
    public void RemoveOption(int id)
    {
        if (IsDisabled) return;
        if (Options.Remove(id) && SelectedValues.Remove(id))
            OnSelectionChanged?.Invoke(id, false);
    }

    /// <summary>
    /// Toggles the selection status of the given option ID.
    /// </summary>
    /// <param name="id">The option ID to toggle.</param>
    public void Toggle(int id)
    {
        if (IsDisabled) return;
        if (IsSelected(id)) Deselect(id);
        else Select(id);
    }

    /// <summary>
    /// Selects an option if not already selected, not blacklisted, and within the <see cref="MaxSelection"/> bound.
    /// </summary>
    /// <param name="id">The option ID to select.</param>
    public void Select(int id)
    {
        if (IsDisabled || SelectedValues.Contains(id) || Blacklist.Contains(id)) return;
        if (MaxSelection == -1 || SelectedValues.Count < MaxSelection)
        {
            SelectedValues.Add(id);
            OnSelectionChanged?.Invoke(id, true);
        }
    }

    /// <summary>
    /// Deselects the given option ID if it is currently selected.
    /// </summary>
    /// <param name="id">The option ID to deselect.</param>
    public void Deselect(int id)
    {
        if (IsDisabled) return;
        if (SelectedValues.Remove(id))
            OnSelectionChanged?.Invoke(id, false);
    }

    /// <summary>
    /// Checks whether an option ID is currently selected.
    /// </summary>
    /// <param name="id">The option ID to check.</param>
    /// <returns><c>true</c> if selected; otherwise, <c>false</c>.</returns>
    public bool IsSelected(int id) => SelectedValues.Contains(id);

    /// <summary>
    /// Restores the selection set to match <see cref="DefaultSelectedValues"/>.
    /// </summary>
    public override void Reset()
    {
        SelectedValues.Clear();
        foreach (var v in DefaultSelectedValues) SelectedValues.Add(v);
    }

    /// <summary>
    /// Renders the setting row in the window, showing the label, current count, select button, and reset button.
    /// </summary>
    /// <param name="y">Reference to the vertical position accumulator.</param>
    /// <param name="width">Total available width for this settings row.</param>
    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;
        float elemH = ClientConfig.elementHeight;
        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float resetBtnW = SettingValues.ResetButtonW;
        float selectBtnW = ClientConfig.selectButtonWidth;

        string countText = '(' + Translate($"{SelectedValues.Count} selected") + ')';
        float countTextW = ThemeManager.SettingLabelStyle.CalcSize(new GUIContent(countText)).x;

        // Label fills the remaining space on the left
        float labelW = width - (indent * 2f) - selectBtnW - countTextW - (gap * 2f) - resetBtnW;
        Rect labelRect = new(indent, y, labelW, elemH);
        GUI.Label(labelRect, Translate(Name), ThemeManager.SettingLabelStyle);

        // Right-to-left layout: [Select Button] [gap] [Count Text] [gap] [Reset Button]
        float resetStartX = width - indent - resetBtnW;
        float countStartX = resetStartX - gap - countTextW;
        float btnStartX = countStartX - gap - selectBtnW;

        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);
        Rect countRect = new(countStartX, y, countTextW, elemH);
        Rect btnRect = new(btnStartX, y, selectBtnW, elemH);

        if (btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            OnWindowOpen?.Invoke();
            DrawSetting.multiSelectSearchQuery = "";
            DrawSetting.manualScrollY = 0f;
            e.Use();
        }

        GUI.Box(btnRect, Translate("Select"), ThemeManager.SettingOff);

        Color orig = GUI.contentColor;
        GUI.contentColor = ThemeManager.TextDim;
        GUI.Label(countRect, countText, ThemeManager.SettingLabelStyle);
        GUI.contentColor = orig;

        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + ClientConfig.spacing;
    }

    /// <summary>
    /// Selects all valid options (excluding blacklists) and optionally caches them as the default state.
    /// </summary>
    /// <param name="setDefault">If <c>true</c>, updates <see cref="DefaultSelectedValues"/> to match the current selection.</param>
    public void SelectAll(bool setDefault = true)
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

    /// <summary>
    /// Saves the current selection state into <see cref="DefaultSelectedValues"/> for subsequent reset operations.
    /// </summary>
    public void SetCurrentAsDefault()
    {
        DefaultSelectedValues = new HashSet<int>(SelectedValues);
    }
}