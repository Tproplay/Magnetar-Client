using Magnetar_Client.UI.Themes;
using UnityEngine;

namespace Magnetar_Client.UI.WindowDrawing;

public static partial class DrawSetting
{
    /// <summary>
    /// Check if any input field or dropdown is active
    /// </summary>
    public static bool IsFocused =>
           FocusedControlId != -1
        || ActiveSliderId != -1
        || ActiveTextFieldId != -1
        || ActiveDropdownId != -1;

    /// <summary>
    /// Unfocuses all the focused input fields and dropdowns
    /// </summary>
    public static void ResetInputBind()
    {
        FocusedControlId = -1;
        ActiveDropdownId = -1;
        ActiveSliderId = -1;
        ActiveTextFieldId = -1;
    }

#pragma warning disable CS1591
    public static int FocusedControlId { get; set; } = -1;
    public static string CurrentInputBuffer { get; set; } = "";
    public static int ActiveSliderId { get; set; } = -1;
    public static int ActiveTextFieldId { get; set; } = -1;
    public static int ActiveDropdownId { get; set; } = -1;

    public static float LastEscapeTime { get; set; } = -1f;
    public static int LastEscapeFrame { get; set; } = -1;

    public static object activeNumericSetting { get; set; }
    public static int lastFocusedNumericControlId { get; set; } = -1;
    public static System.Action OnPostDraw;
#pragma warning restore CS1591

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