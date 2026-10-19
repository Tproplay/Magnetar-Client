using Magnetar_Client.UI.Themes;
using static Magnetar_Client.Utils.Translator;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// A static informational text setting that renders descriptive labels or multi-line notes.
/// </summary>
public class LabelSetting : Setting
{
    /// <summary>
    /// Indicates that static labels do not hold variable state and cannot be reset.
    /// </summary>
    public override bool CanReset => false;

    /// <summary>
    /// Initializes a new instance of <see cref="LabelSetting"/> with the specified display text.
    /// </summary>
    /// <param name="text">The descriptive text or translation key to display.</param>
    public LabelSetting(string text)
    {
        Name = text ?? string.Empty;
    }

    /// <summary>
    /// Calculates text height and renders the label using description styling in the IMGUI layout.
    /// </summary>
    /// <param name="y">Reference to the vertical layout offset.</param>
    /// <param name="width">The total available layout width.</param>
    public override void Draw(ref float y, float width)
    {
        if (string.IsNullOrEmpty(Name)) return;

        string displayText = Translate(Name);

        float labelWidth = width - (ClientConfig.indent * 2f);
        float calculatedHeight = ThemeManager.SettingsDescriptionStyle.CalcHeight(
            new GUIContent(displayText),
            labelWidth
        );

        Rect labelRect = new(ClientConfig.indent, y, labelWidth, calculatedHeight);
        GUI.Label(labelRect, displayText, ThemeManager.SettingsDescriptionStyle);

        y += calculatedHeight + ClientConfig.spacing;
    }
}