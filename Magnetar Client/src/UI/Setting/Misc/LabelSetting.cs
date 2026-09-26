using UnityEngine;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;

namespace Magnetar_Client.UI.Setting;

public class LabelSetting : Setting
{
    public override bool CanReset => false;

    public LabelSetting(string text)
    {
        Name = text ?? string.Empty;
    }

    public override void Draw(ref float y, float width)
    {
        if (string.IsNullOrEmpty(Name)) return;

        string displayText = Translator.Translate(Name);

        float labelWidth = width - (Config.indent * 2f);
        float calculatedHeight = Magnetar_Default.SettingsDescriptionStyle.CalcHeight(
            new GUIContent(displayText),
            labelWidth
        );

        Rect labelRect = new(Config.indent, y, labelWidth, calculatedHeight);
        GUI.Label(labelRect, displayText, Magnetar_Default.SettingsDescriptionStyle);

        y += calculatedHeight + Config.spacing;
    }
}