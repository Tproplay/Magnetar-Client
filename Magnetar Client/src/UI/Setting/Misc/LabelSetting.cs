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

    private string _displayText;

    /// <summary>
    /// Gets or sets the text to be displayed in the UI.
    /// If not explicitly set or empty, falls back to <see cref="Setting.Name"/>.
    /// </summary>
    public string DisplayText
    {
        get => !string.IsNullOrEmpty(_displayText) ? _displayText : Name;
        set => _displayText = value;
    }

    /// <summary>
    /// Backwards compatibility alias for <see cref="DisplayText"/>.
    /// </summary>
    public string DispayText
    {
        get => DisplayText;
        set => DisplayText = value;
    }

    /// <summary>
    /// Whether this label should automatically be translated via the global Translator during rendering.
    /// Set to <see langword="false"/> when manually handling translations or updating display text dynamically at runtime.
    /// </summary>
    public bool AutoTranslate { get; set; } = true;

    private bool? _saveTranslation;

    /// <summary>
    /// Indicates whether this label should be saved to module translation files.
    /// Defaults to <see cref="AutoTranslate"/> unless explicitly overridden.
    /// </summary>
    public override bool SaveTranslation
    {
        get => _saveTranslation ?? AutoTranslate;
        set => _saveTranslation = value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LabelSetting"/> with the specified display text.
    /// </summary>
    /// <param name="text">The descriptive text or translation key to display.</param>
    public LabelSetting(string text)
    {
        Name = text ?? string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LabelSetting"/> with the specified display text and translation mode.
    /// </summary>
    /// <param name="text">The descriptive text or translation key to display.</param>
    /// <param name="autoTranslate">Whether the label should automatically be translated and saved to translation files.</param>
    public LabelSetting(string text, bool autoTranslate)
    {
        Name = text ?? string.Empty;
        AutoTranslate = autoTranslate;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="LabelSetting"/> with distinct name, display text, and translation mode.
    /// </summary>
    /// <param name="name">The unique setting identifier name.</param>
    /// <param name="displayText">The initial display text.</param>
    /// <param name="autoTranslate">Whether the label should automatically be translated and saved to translation files.</param>
    public LabelSetting(string name, string displayText, bool autoTranslate = true)
    {
        Name = name ?? string.Empty;
        _displayText = displayText;
        AutoTranslate = autoTranslate;
    }

    /// <summary>
    /// Calculates text height and renders the label using description styling in the IMGUI layout.
    /// </summary>
    /// <param name="y">Reference to the vertical layout offset.</param>
    /// <param name="width">The total available layout width.</param>
    public override void Draw(ref float y, float width)
    {
        string rawText = DisplayText;
        if (string.IsNullOrEmpty(rawText)) return;

        string textToDisplay = AutoTranslate ? Translate(rawText) : rawText;

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
