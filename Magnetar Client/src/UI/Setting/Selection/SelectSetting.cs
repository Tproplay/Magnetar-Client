using System;
using System.Collections.Generic;
using UnityEngine;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;

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

        string translatedName = Translator.Translate(Name);
        float resetBtnW = Config.S(22f);
        float gap = Config.S(6f);

        GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth - resetBtnW - gap, Config.elementHeight),
            translatedName, Magnetar_Default.SettingLabelStyle);

        string currentValName = "Unknown";
        if (Options.ContainsKey(Value))
        {
            currentValName = (CustomNames != null && CustomNames.ContainsKey(Value)) ? CustomNames[Value] : Options[Value];
        }

        Rect btnRect = new(width - Config.indent - resetBtnW - gap - Config.SettingWidth, y, Config.SettingWidth, Config.elementHeight);
        Rect resetRect = new(width - Config.indent - resetBtnW, y, resetBtnW, Config.elementHeight);

        if (btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            DrawSetting.activeDropdownId = (DrawSetting.activeDropdownId == controlId) ? -1 : controlId;
            DrawSetting.dropdownScrollY = 0f;
            DrawSetting.focusedControlId = -1;
            e.Use();
        }

        string arrow = (DrawSetting.activeDropdownId == controlId) ? " ▲" : " ▼";
        GUI.Box(btnRect, currentValName + arrow, Magnetar_Default.SettingOff);

        if (DrawSetting.activeDropdownId == controlId)
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
                        DrawSetting.activeDropdownId = -1;
                        e.Use();
                        break;
                    }
                    idx++;
                }
            }

            if (e.type == EventType.MouseDown && !btnRect.Contains(e.mousePosition) && !dropRect.Contains(e.mousePosition))
            {
                DrawSetting.activeDropdownId = -1;
            }

            float scrollY = DrawSetting.dropdownScrollY;
            DrawSetting.OnPostDraw += () =>
            {
                GUI.Box(dropRect, "", Magnetar_Default.SettingOff);
                GUI.BeginGroup(dropRect);
                int i = 0;
                foreach (var kvp in Options)
                {
                    float drawY = (i * rowHeight) - scrollY;
                    if (drawY + rowHeight > 0 && drawY < dropHeight)
                    {
                        Rect row = new(0, drawY, dropRect.width, rowHeight);
                        string disp = (CustomNames != null && CustomNames.ContainsKey(kvp.Key)) ? CustomNames[kvp.Key] : kvp.Value;
                        GUI.Box(row, disp, (Value == kvp.Key) ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);
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

        y += Config.elementHeight + Config.spacing;
    }
}