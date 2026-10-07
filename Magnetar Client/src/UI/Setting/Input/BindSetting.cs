using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

public class BindSetting : Setting
{
    private List<KeyCode> _bindKeys = new();
    public List<KeyCode> DefaultKeys { get; private set; } = new();
    public bool IsBinding = false;

    public Action<List<KeyCode>> OnValueChanging { get; set; }
    public Action<List<KeyCode>> OnValueChanged { get; set; }

    public List<KeyCode> BindKeys
    {
        get => _bindKeys;
        set
        {
            if (IsDisabled) return;
            if (value == null) value = new List<KeyCode>();
            if (!_bindKeys.SequenceEqual(value))
            {
                OnValueChanging?.Invoke(value);
                _bindKeys = value;
                OnValueChanged?.Invoke(_bindKeys);
            }
        }
    }

    public BindSetting(string name, List<KeyCode> defaultKeys = null)
    {
        Name = name;
        if (defaultKeys != null)
        {
            DefaultKeys = new List<KeyCode>(defaultKeys);
            _bindKeys = new List<KeyCode>(defaultKeys);
        }
    }

    public string GetBindString()
    {
        if (BindKeys == null || BindKeys.Count == 0) return "None";
        return string.Join(" + ", BindKeys.Select(k => k.ToString()).ToArray());
    }

    public override void Reset()
    {
        BindKeys = new List<KeyCode>(DefaultKeys);
        IsBinding = false;
    }

    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;
        bool isLeftClick = e.type == EventType.MouseDown && e.button == 0;

        float indent = Config.indent;
        float gap = Config.SettingsInput.Gap;
        float resetBtnW = Config.SettingsInput.ResetButtonW;
        float settingW = Config.SettingWidth;

        // Label: occupies remaining width on the left
        float labelW = width - (indent * 2f) - settingW - gap - resetBtnW;
        Rect labelRect = new(indent, y, labelW, Config.elementHeight);
        GUI.Label(labelRect, Translator.Translate(Name), ThemeManager.SettingLabelStyle);

        // Right-aligned elements: [Control Button] [gap] [Reset Button]
        float resetStartX = width - indent - resetBtnW;
        float bindStartX = resetStartX - gap - settingW;

        Rect bindRect = new(bindStartX, y, settingW, Config.elementHeight);
        Rect resetRect = new(resetStartX, y, resetBtnW, Config.elementHeight);

        string bindText = IsBinding ? "[...]" : GetBindString();
        GUI.Box(bindRect, bindText, IsBinding ? ThemeManager.SettingOn : ThemeManager.SettingOff);

        if (bindRect.Contains(e.mousePosition) && isLeftClick)
        {
            IsBinding = !IsBinding;
            if (IsBinding)
            {
                BindKeys.Clear();
                DrawSetting.activeTextFieldId = -1;
                DrawSetting.focusedControlId = -1;
            }
            e.Use();
        }

        if (IsBinding && e.isKey)
        {
            KeyCode key = e.keyCode;
            if (key != KeyCode.None)
            {
                if (e.type == EventType.KeyDown)
                {
                    if (key == KeyCode.Escape || key == KeyCode.RightShift)
                    {
                        BindKeys.Clear();
                        IsBinding = false;
                    }
                    else if (!BindKeys.Contains(key))
                    {
                        BindKeys.Add(key);
                    }
                    e.Use();
                }
                else if (e.type == EventType.KeyUp && BindKeys.Count > 0)
                {
                    IsBinding = false;
                    e.Use();
                }
            }
        }

        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += Config.elementHeight + Config.spacing;
    }
}