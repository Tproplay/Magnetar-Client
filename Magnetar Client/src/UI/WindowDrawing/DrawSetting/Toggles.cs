using Magnetar_Client.UI.Setting;
using Magnetar_Client.UI.Themes;
using static Magnetar_Client.Utils.Translator;
using UnityEngine;

namespace Magnetar_Client.UI.WindowDrawing;

public static partial class DrawSetting
{
    /// <summary>
    /// Renders a boolean toggle setting control and updates its state upon user interaction.
    /// </summary>
    /// <param name="boolSet">The <see cref="BoolSetting"/> to draw and handle.</param>
    /// <param name="y">The vertical drawing coordinate reference.</param>
    /// <param name="width">The total available width for the control.</param>
    public static void HandleBoolSetting(BoolSetting boolSet, ref float y, float width)
    {
        Event e = Event.current;
        string translatedName = Translate(boolSet.Name);
        float labelWidth = Mathf.Max(width * 0.45f, width - ClientConfig.indent * 2 - ClientConfig.SettingWidth);

        bool prevEnabled = GUI.enabled;
        if (boolSet.IsDisabled) GUI.enabled = false;

        GUI.Label(new Rect(ClientConfig.indent, y, labelWidth, ClientConfig.elementHeight),
            translatedName, ThemeManager.SettingLabelStyle);

        Rect btnRect = new(width - ClientConfig.indent - ClientConfig.SettingWidth, y,
            ClientConfig.SettingWidth, ClientConfig.elementHeight);

        GUI.Box(btnRect, boolSet.Value ? Translate("ON") : Translate("OFF"),
            boolSet.Value ? ThemeManager.SettingOn : ThemeManager.SettingOff);

        if (!boolSet.IsDisabled && btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            boolSet.Value = !boolSet.Value;
            e.Use();
        }

        GUI.enabled = prevEnabled;
    }

    /// <summary>
    /// Renders a keybind setting control and captures keystrokes when in binding mode.
    /// </summary>
    /// <param name="bSet">The <see cref="BindSetting"/> to draw and handle.</param>
    /// <param name="y">The vertical drawing coordinate reference.</param>
    /// <param name="width">The total available width for the control.</param>
    public static void HandleBindSetting(BindSetting bSet, ref float y, float width)
    {
        Event e = Event.current;
        bool isLeftClick = e.type == EventType.MouseDown && e.button == 0;

        string translatedName = Translate(bSet.Name);
        float labelWidth = Mathf.Max(width * 0.45f, width - ClientConfig.indent * 2 - ClientConfig.SettingWidth);

        GUI.Label(new Rect(ClientConfig.indent, y, labelWidth, ClientConfig.elementHeight),
            translatedName, ThemeManager.SettingLabelStyle);

        string bindText = bSet.IsBinding ? "[...]" : bSet.GetBindString();
        Rect bindRect = new(width - ClientConfig.indent - ClientConfig.SettingWidth, y,
            ClientConfig.SettingWidth, ClientConfig.elementHeight);
        bool bindHover = bindRect.Contains(e.mousePosition);

        GUI.Box(bindRect, bindText, bSet.IsBinding ? ThemeManager.SettingOn : ThemeManager.SettingOff);

        if (bindHover && isLeftClick)
        {
            bSet.IsBinding = !bSet.IsBinding;
            if (bSet.IsBinding)
            {
                bSet.BindKeys.Clear();
                ActiveTextFieldId = -1;
                FocusedControlId = -1;
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
                    if (key == KeyCode.Escape || key == ClientConfig.MenuOpenKey)
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

    /// <summary>
    /// Renders a clickable button setting control and triggers its associated action on click.
    /// </summary>
    /// <param name="btnSet">The <see cref="ButtonSetting"/> to draw and handle.</param>
    /// <param name="y">The vertical drawing coordinate reference.</param>
    /// <param name="width">The total available width for the control.</param>
    public static void HandleButtonSetting(ButtonSetting btnSet, ref float y, float width)
    {
        Event e = Event.current;
        string translatedName = Translate(btnSet.Name);
        GUI.Label(new Rect(ClientConfig.indent, y, width - ClientConfig.indent * 2 - ClientConfig.SettingWidth, ClientConfig.elementHeight), translatedName, ThemeManager.SettingLabelStyle);

        Rect btnRect = new(width - ClientConfig.indent - ClientConfig.SettingWidth, y, ClientConfig.SettingWidth, ClientConfig.elementHeight);
        bool isHovered = btnRect.Contains(e.mousePosition);

        GUI.Box(btnRect, Translate(btnSet.ButtonText), ThemeManager.SettingOff);

        if (isHovered && e.type == EventType.MouseDown && e.button == 0)
        {
            if (!btnSet.IsDisabled)
            {
                btnSet.OnClick?.Invoke();
            }
            e.Use();
        }
    }

    /// <summary>
    /// Renders an informational label or description text within the settings window.
    /// </summary>
    /// <param name="lblSet">The <see cref="LabelSetting"/> containing text to display.</param>
    /// <param name="y">The vertical drawing coordinate reference.</param>
    /// <param name="width">The total available width for the control.</param>
    public static void HandleLabelSetting(LabelSetting lblSet, ref float y, float width)
    {
        if (lblSet == null) return;

        string rawText = lblSet.DisplayText;
        if (string.IsNullOrEmpty(rawText)) return;

        string textToDisplay = lblSet.AutoTranslate ? Translate(rawText) : rawText;
        float labelWidth = width - (ClientConfig.indent * 2f);
        float calculatedHeight = ThemeManager.SettingsDescriptionStyle.CalcHeight(
            new GUIContent(textToDisplay),
            labelWidth
        );

        Rect labelRect = new(ClientConfig.indent, y, labelWidth, calculatedHeight);
        GUI.Label(labelRect, textToDisplay, ThemeManager.SettingsDescriptionStyle);

        y += calculatedHeight + ClientConfig.spacing;
    }
}