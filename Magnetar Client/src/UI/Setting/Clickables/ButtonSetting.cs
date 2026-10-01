using System;
using UnityEngine;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;

namespace Magnetar_Client.UI.Setting;

public class ButtonSetting : Setting
{
    public string ButtonText;
    public Action OnClick;
    public override bool CanReset => false;

    public ButtonSetting(string name, Action onClick, string buttonText = "Click")
    {
        Name = name;
        OnClick = onClick;
        ButtonText = buttonText;
    }

    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;

        float indent = Config.indent;
        float gap = Config.SettingsInput.Gap;

        // Label
        float labelW = width - indent * 2 - Config.SettingWidth;
        Rect labelRect = new(indent, y, labelW, Config.elementHeight);
        GUI.Label(labelRect, Translator.Translate(Name), ThemeManager.SettingLabelStyle);

        // Button
        float btnW = Config.SettingWidth;
        float btnStartX = width - indent - btnW - gap - Config.SettingsInput.ResetButtonW;
        Rect btnRect = new(btnStartX, y, Config.SettingWidth, Config.elementHeight);
        GUI.Box(btnRect, Translator.Translate(ButtonText), ThemeManager.ButtonSettingStyle);

        if (btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            if (!IsDisabled) OnClick?.Invoke();
            e.Use();
        }

        y += Config.elementHeight + Config.spacing;
    }
}