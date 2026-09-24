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
        string translatedName = Translator.Translate(Name);
        GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, Config.elementHeight), translatedName, Magnetar_Default.SettingLabelStyle);

        Rect btnRect = new(width - Config.indent - Config.SettingWidth, y, Config.SettingWidth, Config.elementHeight);
        GUI.Box(btnRect, Translator.Translate(ButtonText), Magnetar_Default.SettingOff);

        if (btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            if (!IsDisabled) OnClick?.Invoke();
            e.Use();
        }

        y += Config.elementHeight + Config.spacing;
    }
}
