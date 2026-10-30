using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using static Magnetar_Client.Utils.Translator;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// A setting module representing an editable single-line text string with optional bracket autocomplete support.
/// </summary>
public class StringSetting : Setting
{
    private string _value;

    /// <summary>
    /// The default string value restored when <see cref="Reset"/> is executed.
    /// </summary>
    public string DefaultValue;

    /// <summary>
    /// Optional list of variable names available for bracket autocomplete (e.g. "{player}").
    /// </summary>
    public List<string> AutocompleteVars;

    /// <summary>
    /// Event invoked immediately before <see cref="Value"/> changes.
    /// </summary>
    public Action<string> OnValueChanging { get; set; }

    /// <summary>
    /// Event invoked after <see cref="Value"/> changes.
    /// </summary>
    public Action<string> OnValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the text string value.
    /// </summary>
    public string Value
    {
        get => _value;
        set
        {
            if (IsDisabled) return;
            if (_value != value)
            {
                OnValueChanging?.Invoke(value);
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="StringSetting"/> with a display name, initial value, and optional autocomplete variables.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="defaultValue">The initial and default string value.</param>
    /// <param name="autocompleteVars">Optional list of variable names for bracket autocomplete.</param>
    public StringSetting(string name, string defaultValue, List<string> autocompleteVars = null)
    {
        Name = name;
        _value = defaultValue;
        DefaultValue = defaultValue;
        AutocompleteVars = autocompleteVars;
    }

    /// <summary>
    /// Restores the string value back to <see cref="DefaultValue"/>.
    /// </summary>
    public override void Reset()
    {
        Value = DefaultValue;
    }

    /// <summary>
    /// Renders the label, text field, and reset button in IMGUI layout.
    /// </summary>
    /// <param name="y">Reference to the vertical layout offset.</param>
    /// <param name="width">The total available layout width.</param>
    public override void Draw(ref float y, float width)
    {
        float elemH = ClientConfig.elementHeight;
        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float resetBtnW = SettingValues.ResetButtonW;
        float controlW = Mathf.Min(ClientConfig.SettingWidth * 1.25f, width * 0.52f);

        // Label on the left occupies remaining width
        float labelW = width - (indent * 2f) - controlW - gap - resetBtnW;
        Rect labelRect = new(indent, y, labelW, elemH);

        // Right-to-left layout: [Control] [gap] [Reset]
        float resetStartX = width - indent - resetBtnW;
        float inputStartX = resetStartX - gap - controlW;

        Rect inputRect = new(inputStartX, y, controlW, elemH);
        Rect resetRect = new(resetStartX, y, resetBtnW, elemH);

        GUI.Label(labelRect, DisplayName, ThemeManager.SettingLabelStyle);
        Value = DrawSetting.DrawManualTextField(inputRect, Value, "", AutocompleteVars);

        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += elemH + ClientConfig.spacing;
    }
}