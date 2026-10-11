using Magnetar_Client.UI.Themes;
using static Magnetar_Client.Utils.Translator;
using System;
using UnityEngine;

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

        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;

        string localizedName = Translate(Name);
        string localizedButtonText = Translate(ButtonText);

        // Label
        float labelW = width - indent * 2 - ClientConfig.SettingWidth;
        Rect labelRect = new(indent, y, labelW, ClientConfig.elementHeight);
        GUI.Label(labelRect, localizedName, ThemeManager.SettingLabelStyle);

        // Button
        float btnW = ClientConfig.SettingWidth;
        float btnStartX = width - indent - btnW - gap - SettingValues.ResetButtonW;
        Rect btnRect = new(btnStartX, y, ClientConfig.SettingWidth, ClientConfig.elementHeight);
        GUI.Box(btnRect, localizedButtonText, ThemeManager.ButtonSettingStyle);

        if (btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            if (!IsDisabled) OnClick?.Invoke();
            e.Use();
        }

        y += ClientConfig.elementHeight + ClientConfig.spacing;
    }
}