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
        float indent = Config.indent;
        float gap = Config.SettingsInput.Gap;
        float resetBtnW = Config.SettingsInput.ResetButtonW;
        float controlW = Mathf.Min(Config.SettingWidth * 1.25f, width * 0.52f);

        // Label on the left occupies remaining width
        float labelW = width - (indent * 2f) - controlW - gap - resetBtnW;
        Rect labelRect = new(indent, y, labelW, elemH);

        // Right-to-left layout: [Control] [gap] [Reset]
        float resetStartX = width - indent - resetBtnW;
        float inputStartX = resetStartX - gap - controlW;

        Rect inputRect = new(inputStartX, y, controlW, elemH);
        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);

        GUI.Label(labelRect, Translator.Translate(Name), ThemeManager.SettingLabelStyle);
        Value = DrawSetting.DrawManualTextField(inputRect, Value, "", AutocompleteVars);

        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + Config.spacing;
    }
}