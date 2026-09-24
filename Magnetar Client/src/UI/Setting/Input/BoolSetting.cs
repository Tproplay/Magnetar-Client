using System;
using UnityEngine;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;

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
        string translatedName = Translator.Translate(Name);

        float resetBtnW = Config.S(22f);
        float gap = Config.S(6f);
        float labelWidth = Mathf.Max(width * 0.40f, width - Config.indent * 2 - Config.SettingWidth - resetBtnW - gap);

        GUI.Label(new Rect(Config.indent, y, labelWidth, Config.elementHeight), translatedName, Magnetar_Default.SettingLabelStyle);

        Rect btnRect = new(width - Config.indent - resetBtnW - gap - Config.SettingWidth, y, Config.SettingWidth, Config.elementHeight);
        Rect resetRect = new(width - Config.indent - resetBtnW, y, resetBtnW, Config.elementHeight);

        GUI.Box(btnRect, Value ? Translator.Translate("ON") : Translator.Translate("OFF"),
            Value ? Magnetar_Default.SettingOn : Magnetar_Default.SettingOff);

        if (btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            Value = !Value;
            e.Use();
        }

        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += Config.elementHeight + Config.spacing;
    }
}