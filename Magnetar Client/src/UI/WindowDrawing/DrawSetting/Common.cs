using Magnetar_Client.UI.Themes;
using UnityEngine;

namespace Magnetar_Client.UI.WindowDrawing;

/// <summary>
/// Provides methods and state tracking for drawing settings controls within GUI windows.
/// </summary>
public static partial class DrawSetting
{
    /// <summary>
    /// Check if any input field or dropdown is active.
    /// </summary>
    public static bool IsFocused =>
           FocusedControlId != -1
        || ActiveSliderId != -1
        || ActiveTextFieldId != -1
        || ActiveDropdownId != -1;

    /// <summary>
    /// Unfocuses all the focused input fields and dropdowns.
    /// </summary>
    public static void ResetInputBind()
    {
        FocusedControlId = -1;
        ActiveDropdownId = -1;
        ActiveSliderId = -1;
        ActiveTextFieldId = -1;
    }

    /// <summary>
    /// Gets or sets the control identifier of the currently focused input control.
    /// </summary>
    public static int FocusedControlId { get; set; } = -1;

    /// <summary>
    /// Gets or sets the current text input buffer for editable fields.
    /// </summary>
    public static string CurrentInputBuffer { get; set; } = "";

    /// <summary>
    /// Gets or sets the control identifier of the slider currently being interacted with.
    /// </summary>
    public static int ActiveSliderId { get; set; } = -1;

    /// <summary>
    /// Gets or sets the control identifier of the text field currently receiving input.
    /// </summary>
    public static int ActiveTextFieldId { get; set; } = -1;

    /// <summary>
    /// Gets or sets the control identifier of the currently open dropdown menu.
    /// </summary>
    public static int ActiveDropdownId { get; set; } = -1;

    /// <summary>
    /// Gets or sets the unscaled timestamp of the most recent Escape key press.
    /// </summary>
    public static float LastEscapeTime { get; set; } = -1f;

    /// <summary>
    /// Gets or sets the frame count when the Escape key was last pressed.
    /// </summary>
    public static int LastEscapeFrame { get; set; } = -1;

    /// <summary>
    /// Gets or sets the numeric setting currently being manually edited.
    /// </summary>
    public static object activeNumericSetting { get; set; }

    /// <summary>
    /// Gets or sets the control identifier of the last focused numeric input field.
    /// </summary>
    public static int lastFocusedNumericControlId { get; set; } = -1;

    /// <summary>
    /// Delegate action invoked after primary drawing to render overlays such as dropdowns and autocomplete menus.
    /// </summary>
    public static System.Action OnPostDraw;

    private static GUIStyle _placeholderStyle;
    private static GUIStyle PlaceholderStyle
    {
        get
        {
            _placeholderStyle ??= new GUIStyle
            {
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = ThemeManager.TextStyle.alignment
            };
            _placeholderStyle.fontSize = ThemeManager.TextStyle.fontSize;
            _placeholderStyle.normal.textColor = ThemeManager.TextDim;
            return _placeholderStyle;
        }
    }
}