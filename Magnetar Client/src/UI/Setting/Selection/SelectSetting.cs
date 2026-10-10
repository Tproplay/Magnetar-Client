using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using static Magnetar_Client.Utils.Translator;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

public class SelectSetting : Setting
{
    private int _value;
    public int DefaultValue;
    public Dictionary<int, string> Options { get; set; } = new();
    public System.Type EnumType { get; private set; }
    public Dictionary<int, string> CustomNames { get; set; } = new();

    public Action<int> OnSelectionChanged { get; set; }

    public int Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            if (_value != value)
            {
                _value = value;
                OnSelectionChanged?.Invoke(_value);
            }
        }
    }

    public SelectSetting(string name, int defaultValue)
    {
        Name = name;
        _value = defaultValue;
        DefaultValue = defaultValue;
    }

    public SelectSetting(string name, System.Type enumType, int defaultValue)
    {
        Name = name;
        _value = defaultValue;
        DefaultValue = defaultValue;
        EnumType = enumType;

        if (enumType != null && enumType.IsEnum)
        {
            foreach (var val in Enum.GetValues(enumType))
                Options[Convert.ToInt32(val)] = val.ToString();
        }
    }

    public void AddOption(int id, string displayName) => Options[id] = displayName;

    public override void Reset() => Value = DefaultValue;

    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;
        int controlId = GetHashCode();

        float elemH = Config.elementHeight;
        float indent = Config.indent;
        float gap = Config.SettingsInput.Gap;
        float resetBtnW = Config.SettingsInput.ResetButtonW;
        float btnW = Config.SettingWidth;

        // Label on the left: fills remaining horizontal space
        float labelW = width - (indent * 2f) - btnW - gap - resetBtnW;
        Rect labelRect = new(indent, y, labelW, elemH);
        GUI.Label(labelRect, Translate(Name), ThemeManager.SettingLabelStyle);

        // Right-to-left layout: [Dropdown Button] [gap] [Reset Button]
        float resetStartX = width - indent - resetBtnW;
        float btnStartX = resetStartX - gap - btnW;

        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);
        Rect btnRect = new(btnStartX, y, btnW, elemH);

        string currentValName = "Unknown";
        if (Options.ContainsKey(Value))
        {
            currentValName = (CustomNames != null && CustomNames.ContainsKey(Value)) ? CustomNames[Value] : Options[Value];
        }

        if (btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            DrawSetting.ActiveDropdownId = (DrawSetting.ActiveDropdownId == controlId) ? -1 : controlId;
            DrawSetting.dropdownScrollY = 0f;
            DrawSetting.FocusedControlId = -1;
            e.Use();
        }

        string arrow = (DrawSetting.ActiveDropdownId == controlId) ? " ▲" : " ▼";
        GUI.Box(btnRect, currentValName + arrow, ThemeManager.SettingOff);

        if (DrawSetting.ActiveDropdownId == controlId)
        {
            float rowHeight = Config.SettingsInput.DropdownRowHeight;
            int maxVisibleRows = Config.SettingsInput.DropdownMaxVisibleRows;
            int itemCount = Options.Count;
            float dropHeight = Mathf.Min(itemCount * rowHeight, maxVisibleRows * rowHeight);

            Rect dropRect = new(btnRect.x, btnRect.y + btnRect.height, btnRect.width, dropHeight);

            if (dropRect.Contains(e.mousePosition) && e.type == EventType.ScrollWheel)
            {
                DrawSetting.dropdownScrollY = Mathf.Clamp(DrawSetting.dropdownScrollY + e.delta.y * Config.SettingsInput.DropdownScrollSensitivity,
                    0, Mathf.Max(0, (itemCount * rowHeight) - dropHeight));
                e.Use();
            }

            if (dropRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
            {
                float localY = e.mousePosition.y - dropRect.y + DrawSetting.dropdownScrollY;
                int clickedIndex = (int)(localY / rowHeight);
                int idx = 0;
                foreach (var kvp in Options)
                {
                    if (idx == clickedIndex)
                    {
                        Value = kvp.Key;
                        DrawSetting.ActiveDropdownId = -1;
                        e.Use();
                        break;
                    }
                    idx++;
                }
            }

            if (e.type == EventType.MouseDown && !btnRect.Contains(e.mousePosition) && !dropRect.Contains(e.mousePosition))
            {
                DrawSetting.ActiveDropdownId = -1;
            }

            float scrollY = DrawSetting.dropdownScrollY;
            DrawSetting.OnPostDraw += () =>
            {
                GUI.Box(dropRect, "", ThemeManager.SettingOff);
                GUI.BeginGroup(dropRect);
                int i = 0;
                foreach (var kvp in Options)
                {
                    float drawY = (i * rowHeight) - scrollY;
                    if (drawY + rowHeight > 0 && drawY < dropHeight)
                    {
                        Rect row = new(0, drawY, dropRect.width, rowHeight);
                        string disp = (CustomNames != null && CustomNames.ContainsKey(kvp.Key)) ? CustomNames[kvp.Key] : kvp.Value;
                        GUI.Box(row, disp, (Value == kvp.Key) ? ThemeManager.SettingOn : ThemeManager.SettingOff);
                    }
                    i++;
                }
                GUI.EndGroup();
            };
        }

        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + Config.spacing;
    }
}