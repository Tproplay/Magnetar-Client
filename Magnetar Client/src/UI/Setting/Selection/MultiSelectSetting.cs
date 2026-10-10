using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using static Magnetar_Client.Utils.Translator;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

public class MultiSelectSetting : Setting
{
    public int MaxSelection = -1;
    public Dictionary<int, string> Options { get; set; } = new();
    public HashSet<int> SelectedValues = new();
    public HashSet<int> DefaultSelectedValues = new();
    public HashSet<int> Blacklist = new();
    public HashSet<string> NameBlacklist = new();
    public System.Type EnumType { get; private set; }

    public Action<int, bool> OnSelectionChanged { get; set; }
    public Action OnWindowOpen { get; set; }

    public Dictionary<int, string> CustomNames;
    public bool DisplayAlphabetically;

    public MultiSelectSetting(string name)
    {
        Name = name;
    }

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

    public void AddOption(int id, string displayName) => Options[id] = displayName;

    public void RemoveOption(int id)
    {
        if (IsDisabled) return;
        if (Options.Remove(id) && SelectedValues.Remove(id))
            OnSelectionChanged?.Invoke(id, false);
    }

    public void Toggle(int id)
    {
        if (IsDisabled) return;
        if (IsSelected(id)) Deselect(id);
        else Select(id);
    }

    public void Select(int id)
    {
        if (IsDisabled || SelectedValues.Contains(id) || Blacklist.Contains(id)) return;
        if (MaxSelection == -1 || SelectedValues.Count < MaxSelection)
        {
            SelectedValues.Add(id);
            OnSelectionChanged?.Invoke(id, true);
        }
    }

    public void Deselect(int id)
    {
        if (IsDisabled) return;
        if (SelectedValues.Remove(id))
            OnSelectionChanged?.Invoke(id, false);
    }

    public bool IsSelected(int id) => SelectedValues.Contains(id);

    public override void Reset()
    {
        SelectedValues.Clear();
        foreach (var v in DefaultSelectedValues) SelectedValues.Add(v);
    }

    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;
        float elemH = Config.elementHeight;
        float indent = Config.indent;
        float gap = Config.SettingsInput.Gap;
        float resetBtnW = Config.SettingsInput.ResetButtonW;
        float selectBtnW = Config.selectButtonWidth;

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

        y += elemH + Config.spacing;
    }

    /// <summary>
    /// Selects all valid options (excluding blacklists) and optionally caches them as the default state.
    /// </summary>
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
    /// Saves the current selection state into DefaultSelectedValues for reset operations.
    /// </summary>
    public void SetCurrentAsDefault()
    {
        DefaultSelectedValues = new HashSet<int>(SelectedValues);
    }
}