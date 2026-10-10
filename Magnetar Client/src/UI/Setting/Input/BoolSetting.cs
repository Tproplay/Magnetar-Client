using Magnetar_Client.UI.Themes;
using static Magnetar_Client.Utils.Translator;
using System;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

public class BoolSetting : Setting
{
    private bool _value;
    public bool DefaultValue;

    public Action<bool> OnValueChanging { get; set; }
    public Action<bool> OnValueChanged { get; set; }

    public bool Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            if (_value != value)
            {
                OnValueChanging?.Invoke(value);
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    public BoolSetting(string name, bool defaultValue)
    {
        Name = name;
        _value = defaultValue;
        DefaultValue = defaultValue;
    }

    public override void Reset() => Value = DefaultValue;

    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;

        float indent = Config.indent;
        float gap = Config.SettingsInput.Gap;
        float resetBtnW = Config.SettingsInput.ResetButtonW;
        float btnW = Config.SettingWidth;

        // Label: occupies remaining width on the left
        float labelW = width - (indent * 2f) - btnW - gap - resetBtnW;
        Rect labelRect = new(indent, y, labelW, Config.elementHeight);
        GUI.Label(labelRect, Translate(Name), ThemeManager.SettingLabelStyle);

        // Right-aligned elements: [Toggle Button] [gap] [Reset Button]
        float resetStartX = width - indent - resetBtnW;
        float btnStartX = resetStartX - gap - btnW;

        Rect btnRect = new(btnStartX, y, btnW, Config.elementHeight);
        Rect resetRect = new(resetStartX, y, resetBtnW, Config.elementHeight);

        GUI.Box(btnRect, Value ? Translate("ON") : Translate("OFF"),
            Value ? ThemeManager.SettingOn : ThemeManager.SettingOff);

        if (btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            if (!IsDisabled) Value = !Value;
            e.Use();
        }

        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += Config.elementHeight + Config.spacing;
    }
}