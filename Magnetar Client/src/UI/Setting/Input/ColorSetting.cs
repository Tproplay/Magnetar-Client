using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using System;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// A setting module representing an editable color with a hex code input field and an interactive color preview swatch.
/// </summary>
public class ColorSetting : Setting
{
    private Color _value;
    private string _textBuffer;

    /// <summary>
    /// The default color restored when <see cref="Reset"/> is executed.
    /// </summary>
    public Color DefaultValue;

    /// <summary>
    /// Indicates whether alpha transparency is supported in the hex string format.
    /// When <see langword="true"/>, formats as #RRGGBBAA; when <see langword="false"/>, formats as #RRGGBB.
    /// </summary>
    public bool SupportAlpha { get; set; } = true;

    /// <summary>
    /// Event invoked immediately before <see cref="Value"/> changes.
    /// </summary>
    public Action<Color> OnValueChanging { get; set; }

    /// <summary>
    /// Event invoked after <see cref="Value"/> changes.
    /// </summary>
    public Action<Color> OnValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the validated <see cref="UnityEngine.Color"/> value.
    /// </summary>
    public Color Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            if (_value != value)
            {
                OnValueChanging?.Invoke(value);
                _value = value;
                _textBuffer = ColorToHex(_value, SupportAlpha);
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the color as an HTML hex code string (e.g. "#FF5500" or "#FF5500FF").
    /// </summary>
    public string HexValue
    {
        get => _textBuffer ?? ColorToHex(_value, SupportAlpha);
        set
        {
            _textBuffer = value;
            if (TryParseColor(value, out Color parsed))
            {
                Value = parsed;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ColorSetting"/> with a display name and initial color.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="defaultColor">The initial and default <see cref="Color"/> value.</param>
    /// <param name="supportAlpha">Whether the hex code supports alpha transparency.</param>
    public ColorSetting(string name, Color defaultColor, bool supportAlpha = true)
    {
        Name = name;
        SupportAlpha = supportAlpha;
        _value = defaultColor;
        DefaultValue = defaultColor;
        _textBuffer = ColorToHex(defaultColor, supportAlpha);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ColorSetting"/> with a display name and initial hex code.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="defaultHex">The initial and default hex string (e.g. "#FFFFFF").</param>
    /// <param name="supportAlpha">Whether the hex code supports alpha transparency.</param>
    public ColorSetting(string name, string defaultHex, bool supportAlpha = true)
    {
        Name = name;
        SupportAlpha = supportAlpha;
        if (TryParseColor(defaultHex, out Color parsed))
        {
            _value = parsed;
            DefaultValue = parsed;
        }
        else
        {
            _value = Color.white;
            DefaultValue = Color.white;
        }
        _textBuffer = ColorToHex(_value, supportAlpha);
    }

    /// <summary>
    /// Restores the color back to <see cref="DefaultValue"/>.
    /// </summary>
    public override void Reset()
    {
        Value = DefaultValue;
        _textBuffer = ColorToHex(DefaultValue, SupportAlpha);
    }

    /// <summary>
    /// Renders the label, color swatch preview box, hex input field, and reset button in IMGUI layout.
    /// </summary>
    /// <param name="y">Reference to the vertical layout offset.</param>
    /// <param name="width">The total available layout width.</param>
    public override void Draw(ref float y, float width)
    {
        float elemH = ClientConfig.elementHeight;
        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float resetBtnW = SettingValues.ResetButtonW;

        // The color preview swatch is a square box on the left side of the input field
        float swatchW = elemH;
        float controlW = Mathf.Min(ClientConfig.SettingWidth * 1.25f, width * 0.52f);
        float inputW = controlW - swatchW - gap;

        // Label occupies the remaining space on the left
        float labelW = width - (indent * 2f) - controlW - gap - resetBtnW;
        Rect labelRect = new(indent, y, labelW, elemH);

        // Right-to-left layout: [Swatch] [gap] [Input] [gap] [Reset]
        float resetStartX = width - indent - resetBtnW;
        float inputStartX = resetStartX - gap - inputW;
        float swatchStartX = inputStartX - gap - swatchW;

        Rect swatchRect = new(swatchStartX, y, swatchW, elemH);
        Rect inputRect = new(inputStartX, y, inputW, elemH);
        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);

        bool prevEnabled = GUI.enabled;
        if (IsDisabled) GUI.enabled = false;

        // 1. Draw Label
        GUI.Label(labelRect, DisplayName, ThemeManager.SettingLabelStyle);

        // 2. Draw Color Swatch Box (left of text field)
        GUI.Box(swatchRect, "", ThemeManager.SettingOff);
        Rect innerSwatch = new(swatchRect.x + 2f, swatchRect.y + 2f, swatchRect.width - 4f, swatchRect.height - 4f);
        GUI.DrawTexture(innerSwatch, ThemeData.GetTex(_value));

        // 3. Draw Hex Text Field
        if (DrawSetting.ActiveTextFieldId != inputRect.GetHashCode())
        {
            _textBuffer = ColorToHex(_value, SupportAlpha);
        }

        string newText = DrawSetting.DrawManualTextField(inputRect, _textBuffer ?? "", "");
        if (newText != _textBuffer)
        {
            _textBuffer = FilterHexInput(newText);
            if (TryParseColor(_textBuffer, out Color parsed))
            {
                Value = parsed;
            }
        }

        // 4. Draw Reset Button
        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        GUI.enabled = prevEnabled;

        y += elemH + ClientConfig.spacing;
    }

    /// <summary>
    /// Parses an HTML hex color string with or without a leading '#' character.
    /// </summary>
    public static bool TryParseColor(string hex, out Color color)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            color = Color.white;
            return false;
        }

        hex = hex.Trim();
        if (!hex.StartsWith("#"))
        {
            hex = "#" + hex;
        }

        return ColorUtility.TryParseHtmlString(hex, out color);
    }

    /// <summary>
    /// Converts a <see cref="UnityEngine.Color"/> to an HTML hex color string.
    /// </summary>
    public static string ColorToHex(Color color, bool includeAlpha = true)
    {
        return ThemeData.ColorToHex(color, includeAlpha);
    }

    /// <summary>
    /// Filters raw input text to only allow '#' and valid hex characters (0-9, A-F) up to 9 characters.
    /// </summary>
    public static string FilterHexInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var sb = new System.Text.StringBuilder(9);
        for (int i = 0; i < input.Length && sb.Length < 9; i++)
        {
            char c = input[i];
            if (i == 0 && c == '#')
            {
                sb.Append('#');
            }
            else if (Uri.IsHexDigit(c))
            {
                sb.Append(char.ToUpperInvariant(c));
            }
        }
        return sb.ToString();
    }
}
