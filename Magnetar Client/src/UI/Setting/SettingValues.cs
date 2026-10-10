using Magnetar_Client.Core;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

public static class SettingValues
{

    // Fraction of the slider's own value range moved per scroll tick -
    // not a pixel size, so it does not scale with GUIScale.
    public static float SliderScrollStep = 0.04f;

    // Row count, not a size - does not scale.
    public static int DropdownMaxVisibleRows = 6;

    // Text Fields & Autocomplete
    // History depth, not a size - does not scale.
    public static int TextFieldUndoLimit = 200;
    private static float _baseResetButtonW = 22;
    private static float _baseGap = 6;

    // Numeric Sliders
    private static float _baseNumericInputWidth = 75f;

    private static float _baseSliderHeight = 7f;

    private static int _baseSliderThumbFontSize = 40;

    // Multi-Select Window
    private static float _baseMultiSelectRowHeight = 22f;

    private static float _baseMultiSelectHeaderHeight = 65f;

    // Dropdowns (Select Setting)
    private static float _baseDropdownRowHeight = 22f;

    private static float _baseDropdownScrollSensitivity = 15f;

    private static float _baseAutocompleteRowHeight = 22f;

    private static float _baseAutocompleteMaxHeight = 150f;

    private static float _baseAutocompleteScrollSensitivity = 15f;
    public static float ResetButtonW
    {
        get => GUIManager.S(_baseResetButtonW);
        set => _baseResetButtonW = value;
    }
    public static float Gap
    {
        get => GUIManager.S(_baseGap);
        set => _baseGap = value;
    }
    public static float NumericInputWidth
    {
        get => GUIManager.S(_baseNumericInputWidth);
        set => _baseNumericInputWidth = value;
    }
    public static float SliderHeight
    {
        get => GUIManager.S(_baseSliderHeight);
        set => _baseSliderHeight = value;
    }
    public static int SliderThumbFontSize
    {
        get => Mathf.Max(1, Mathf.RoundToInt(GUIManager.S(_baseSliderThumbFontSize)));
        set => _baseSliderThumbFontSize = value;
    }
    public static float MultiSelectRowHeight
    {
        get => GUIManager.S(_baseMultiSelectRowHeight);
        set => _baseMultiSelectRowHeight = value;
    }
    public static float MultiSelectHeaderHeight
    {
        get => GUIManager.S(_baseMultiSelectHeaderHeight);
        set => _baseMultiSelectHeaderHeight = value;
    }
    public static float DropdownRowHeight
    {
        get => GUIManager.S(_baseDropdownRowHeight);
        set => _baseDropdownRowHeight = value;
    }
    public static float DropdownScrollSensitivity
    {
        get => GUIManager.S(_baseDropdownScrollSensitivity);
        set => _baseDropdownScrollSensitivity = value;
    }
    public static float AutocompleteRowHeight
    {
        get => GUIManager.S(_baseAutocompleteRowHeight);
        set => _baseAutocompleteRowHeight = value;
    }
    public static float AutocompleteMaxHeight
    {
        get => GUIManager.S(_baseAutocompleteMaxHeight);
        set => _baseAutocompleteMaxHeight = value;
    }
    public static float AutocompleteScrollSensitivity
    {
        get => GUIManager.S(_baseAutocompleteScrollSensitivity);
        set => _baseAutocompleteScrollSensitivity = value;
    }
}
