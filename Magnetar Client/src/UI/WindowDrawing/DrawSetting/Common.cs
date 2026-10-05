using Magnetar_Client.UI.Themes;
using UnityEngine;

namespace Magnetar_Client.UI.WindowDrawing;

public static partial class DrawSetting
{
    public static int focusedControlId = -1;
    public static string currentInputBuffer = "";
    public static int activeSliderId = -1;
    public static int activeTextFieldId = -1;
    public static int activeDropdownId = -1;

    public static object activeNumericSetting = null;
    public static int lastFocusedNumericControlId = -1;
    public static System.Action OnPostDraw = null;

    private static GUIStyle _placeholderStyle;
    private static GUIStyle PlaceholderStyle
    {
        get
        {
            if (_placeholderStyle == null)
            {
                _placeholderStyle = new GUIStyle
                {
                    wordWrap = false,
                    clipping = TextClipping.Clip,
                    alignment = ThemeManager.TextStyle.alignment
                };
            }
            _placeholderStyle.fontSize = ThemeManager.TextStyle.fontSize;
            _placeholderStyle.normal.textColor = ThemeManager.TextDim;
            return _placeholderStyle;
        }
    }
}