using Magnetar_Client.UI.Themes;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// Abstract base class defining common state, rendering, and lifecycle behavior for all settings in Magnetar Client.
/// </summary>
public abstract class Setting
{
    /// <summary>
    /// The display name or localization key for this setting.
    /// </summary>
    public string Name;

    /// <summary>
    /// Optional override for the serialization/persistence key name.
    /// If null or empty, defaults to <see cref="Name"/> (or Name + "_Category" for <see cref="CategorySetting"/>).
    /// </summary>
    public string CustomSaveKey { get; set; }

    /// <summary>
    /// Optional collection of legacy or alternative keys to check when loading saved data.
    /// </summary>
    public System.Collections.Generic.List<string> FallbackSaveKeys { get; set; }

    /// <summary>
    /// Gets the unique persistence key used when saving and loading this setting.
    /// </summary>
    public virtual string SaveKey => !string.IsNullOrEmpty(CustomSaveKey)
        ? CustomSaveKey
        : Name;

    /// <summary>
    /// Gets or sets whether this setting is disabled, suppressing modifications and user interactions.
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Indicates whether this setting should be exported and saved to translation files.
    /// </summary>
    public virtual bool SaveTranslation { get; set; } = true;

    /// <summary>
    /// Indicates whether this setting can be reverted back to its default value.
    /// </summary>
    public virtual bool CanReset => true;

    /// <summary>
    /// The symbol or character displayed on standard reset buttons.
    /// </summary>
    public const string ResetSymbol = "R";

    /// <summary>
    /// Reverts the setting to its default configured state.
    /// </summary>
    public virtual void Reset() { }

    /// <summary>
    /// Renders the setting control into the IMGUI layout, advancing the vertical offset.
    /// </summary>
    /// <param name="y">Reference to the current vertical layout offset.</param>
    /// <param name="width">The total width available for rendering this row.</param>
    public abstract void Draw(ref float y, float width);

    /// <summary>
    /// Helper method that renders a standardized reset button and detects left-click events.
    /// </summary>
    /// <param name="rect">The screen rectangle where the reset button will be drawn.</param>
    /// <returns><see langword="true"/> if the button was clicked; otherwise, <see langword="false"/>.</returns>
    public static bool DrawResetButton(Rect rect)
    {
        Event e = Event.current;
        bool isHovered = rect.Contains(e.mousePosition);

        GUI.Box(rect, ResetSymbol, ThemeManager.ResetButtonStyle);

        if (isHovered && e.type == EventType.MouseDown && e.button == 0)
        {
            e.Use();
            return true;
        }

        return false;
    }
}