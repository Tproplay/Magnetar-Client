using System;
using System.Collections.Generic;
using UnityEngine;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;

namespace Magnetar_Client.UI.Setting;

public class StringSetting : Setting
{
    private string _value;
    public string DefaultValue;
    public List<string> AutocompleteVars;

    public Action<string> OnValueChanging { get; set; }
    public Action<string> OnValueChanged { get; set; }

    public string Value
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

    public StringSetting(string name, string defaultValue, List<string> autocompleteVars = null)
    {
        Name = name;
        _value = defaultValue;
        DefaultValue = defaultValue;
        AutocompleteVars = autocompleteVars;
    }

    public override void Reset()
    {
        Value = DefaultValue;
    }

    public override void Draw(ref float y, float width)
    {
        float elemH = Config.elementHeight;
        string translatedName = Translator.Translate(Name);

        float resetBtnW = Config.S(22f);
        float gap = Config.S(6f);
        float controlW = Mathf.Min(Config.SettingWidth * 1.25f, width * 0.52f);
        float labelW = Mathf.Max(width * 0.35f, width - (Config.indent * 2f) - controlW - resetBtnW - (gap * 2f));

        Rect labelRect = new(Config.indent, y, labelW, elemH);
        Rect inputRect = new(width - Config.indent - resetBtnW - gap - controlW, y, controlW, elemH);
        Rect resetRect = new(width - Config.indent - resetBtnW, y, resetBtnW, elemH);

        GUI.Label(labelRect, translatedName, Magnetar_Default.SettingLabelStyle);
        Value = DrawSetting.DrawManualTextField(inputRect, Value, "", AutocompleteVars);

        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + Config.spacing;
    }
}