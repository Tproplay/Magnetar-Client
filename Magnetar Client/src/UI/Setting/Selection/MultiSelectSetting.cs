using System;
using System.Collections.Generic;
using UnityEngine;
using Magnetar_Client.Core;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;

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

    private Dictionary<int, string> _customNames;
    public Dictionary<int, string> CustomNames
    {
        get => _customNames;
        set
        {
            _customNames = value;
            if (_customNames != null)
            {
                foreach (var kvp in _customNames) Options[kvp.Key] = kvp.Value;
            }
        }
    }

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

    public string GetDisplayName(int id, string fallbackName) => Options.ContainsKey(id) ? Options[id] : fallbackName;
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
        float resetBtnW = Config.S(22f);
        float gap = Config.S(6f);

        GUI.Label(new Rect(Config.indent, y, width * 0.38f, Config.elementHeight), Translator.Translate(Name), Magnetar_Default.SettingLabelStyle);

        string countText = '(' + Translator.Translate($"{SelectedValues.Count} selected") + ')';
        float countTextWidth = Magnetar_Default.SettingLabelStyle.CalcSize(new GUIContent(countText)).x;

        Rect resetRect = new(width - Config.indent - resetBtnW, y, resetBtnW, Config.elementHeight);
        Rect countRect = new(resetRect.x - gap - countTextWidth, y, countTextWidth, Config.elementHeight);
        Rect btnRect = new(countRect.x - gap - Config.selectButtonWidth, y, Config.selectButtonWidth, Config.elementHeight);

        if (btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            ModuleManager.showModules = false;
            ModuleManager.showSettings = false;
            ModuleManager.showSelectionGui = true;

            MultiSelectWindowDrawer.ActiveMultiSelect = this;
            DrawSetting.multiSelectSearchQuery = "";
            DrawSetting.manualScrollY = 0f;
            e.Use();
        }

        GUI.Box(btnRect, Translator.Translate("Select"), Magnetar_Default.SettingOff);

        Color orig = GUI.contentColor;
        GUI.contentColor = Magnetar_Default.TextDim;
        GUI.Label(countRect, countText, Magnetar_Default.SettingLabelStyle);
        GUI.contentColor = orig;

        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += Config.elementHeight + Config.spacing;
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