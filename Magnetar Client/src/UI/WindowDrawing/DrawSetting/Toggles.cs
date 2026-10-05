using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.Setting;
using Magnetar_Client.Utils;
using UnityEngine;

namespace Magnetar_Client.UI.WindowDrawing;

public static partial class DrawSetting
{
    public static void HandleBoolSetting(BoolSetting boolSet, ref float y, float width)
    {
        Event e = Event.current;
        string translatedName = Translator.Translate(boolSet.Name);
        float labelWidth = Mathf.Max(width * 0.45f, width - Config.indent * 2 - Config.SettingWidth);

        GUI.Label(new Rect(Config.indent, y, labelWidth, Config.elementHeight),
            translatedName, ThemeManager.SettingLabelStyle);

        Rect btnRect = new(width - Config.indent - Config.SettingWidth, y,
            Config.SettingWidth, Config.elementHeight);

        GUI.Box(btnRect, boolSet.Value ? Translator.Translate("ON") : Translator.Translate("OFF"),
            boolSet.Value ? ThemeManager.SettingOn : ThemeManager.SettingOff);

        if (btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            boolSet.Value = !boolSet.Value;
            e.Use();
        }
    }

    public static void HandleBindSetting(BindSetting bSet, ref float y, float width)
    {
        Event e = Event.current;
        bool isLeftClick = e.type == EventType.MouseDown && e.button == 0;

        string translatedName = Translator.Translate(bSet.Name);
        float labelWidth = Mathf.Max(width * 0.45f, width - Config.indent * 2 - Config.SettingWidth);

        GUI.Label(new Rect(Config.indent, y, labelWidth, Config.elementHeight),
            translatedName, ThemeManager.SettingLabelStyle);

        string bindText = bSet.IsBinding ? "[...]" : bSet.GetBindString();
        Rect bindRect = new(width - Config.indent - Config.SettingWidth, y,
            Config.SettingWidth, Config.elementHeight);
        bool bindHover = bindRect.Contains(e.mousePosition);

        GUI.Box(bindRect, bindText, bSet.IsBinding ? ThemeManager.SettingOn : ThemeManager.SettingOff);

        if (bindHover && isLeftClick)
        {
            bSet.IsBinding = !bSet.IsBinding;
            if (bSet.IsBinding)
            {
                bSet.BindKeys.Clear();
                activeTextFieldId = -1;
                focusedControlId = -1;
            }
            e.Use();
        }

        if (bSet.IsBinding && e.isKey)
        {
            KeyCode key = e.keyCode;
            if (key != KeyCode.None)
            {
                if (e.type == EventType.KeyDown)
                {
                    if (key == KeyCode.Escape || key == KeyCode.RightShift)
                    {
                        bSet.BindKeys.Clear();
                        bSet.IsBinding = false;
                    }
                    else if (!bSet.BindKeys.Contains(key))
                    {
                        bSet.BindKeys.Add(key);
                    }
                    e.Use();
                }
                else if (e.type == EventType.KeyUp)
                {
                    if (bSet.BindKeys.Count > 0)
                    {
                        bSet.IsBinding = false;
                    }
                    e.Use();
                }
            }
        }
    }

    public static void HandleButtonSetting(ButtonSetting btnSet, ref float y, float width)
    {
        Event e = Event.current;
        string translatedName = Translator.Translate(btnSet.Name);
        GUI.Label(new Rect(Config.indent, y, width - Config.indent * 2 - Config.SettingWidth, Config.elementHeight), translatedName, ThemeManager.SettingLabelStyle);

        Rect btnRect = new(width - Config.indent - Config.SettingWidth, y, Config.SettingWidth, Config.elementHeight);
        bool isHovered = btnRect.Contains(e.mousePosition);

        GUI.Box(btnRect, Translator.Translate(btnSet.ButtonText), ThemeManager.SettingOff);

        if (isHovered && e.type == EventType.MouseDown && e.button == 0)
        {
            if (!btnSet.IsDisabled)
            {
                btnSet.OnClick?.Invoke();
            }
            e.Use();
        }
    }

    public static void HandleLabelSetting(LabelSetting lblSet, ref float y, float width)
    {
        if (lblSet == null || string.IsNullOrEmpty(lblSet.Name)) return;

        string displayText = Translator.Translate(lblSet.Name);
        float labelWidth = width - (Config.indent * 2f);
        float calculatedHeight = ThemeManager.SettingsDescriptionStyle.CalcHeight(
            new GUIContent(displayText),
            labelWidth
        );

        Rect labelRect = new(Config.indent, y, labelWidth, calculatedHeight);
        GUI.Label(labelRect, displayText, ThemeManager.SettingsDescriptionStyle);

        y += calculatedHeight + Config.spacing;
    }
}