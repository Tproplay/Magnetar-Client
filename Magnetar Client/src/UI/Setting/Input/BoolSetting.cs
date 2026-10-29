using Magnetar_Client.UI.Themes;
using static Magnetar_Client.Utils.Translator;
using System;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// A setting module representing a toggleable boolean value (ON/OFF).
/// </summary>
public class BoolSetting : Setting
{
    private bool _value;

    /// <summary>
    /// The default fallback value restored when <see cref="Reset"/> is executed.
    /// </summary>
    public bool DefaultValue;

    /// <summary>
    /// Event invoked immediately before <see cref="Value"/> changes.
    /// </summary>
    public Action<bool> OnValueChanging { get; set; }

    /// <summary>
    /// Event invoked after <see cref="Value"/> changes.
    /// </summary>
    public Action<bool> OnValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the boolean state of the toggle.
    /// </summary>
    public bool Value
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
    /// Initializes a new instance of <see cref="BoolSetting"/> with a display name and initial value.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="defaultValue">The initial and default boolean value.</param>
    public BoolSetting(string name, bool defaultValue)
    {
        Name = name;
        _value = defaultValue;
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Restores the setting back to <see cref="DefaultValue"/>.
    /// </summary>
    public override void Reset() => Value = DefaultValue;

    /// <summary>
    /// Sets the value directly without invoking events or checking <see cref="Setting.IsDisabled"/>.
    /// </summary>
    public void SetDirect(bool val)
    {
        _value = val;
    }

    /// <summary>
    /// Renders the toggle button, state label, and reset button in IMGUI layout.
    /// </summary>
    /// <param name="y">Reference to the vertical layout offset.</param>
    /// <param name="width">The total available layout width.</param>
    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;

        bool prevGUIEnabled = GUI.enabled;
        if (IsDisabled) GUI.enabled = false;

        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float resetBtnW = SettingValues.ResetButtonW;
        float btnW = ClientConfig.SettingWidth;

        // Label: occupies remaining width on the left
        float labelW = width - (indent * 2f) - btnW - gap - resetBtnW;
        Rect labelRect = new(indent, y, labelW, ClientConfig.elementHeight);
        GUI.Label(labelRect, Translate(Name), ThemeManager.SettingLabelStyle);

        // Right-aligned elements: [Toggle Button] [gap] [Reset Button]
        float resetStartX = width - indent - resetBtnW;
        float btnStartX = resetStartX - gap - btnW;

        Rect btnRect = new(btnStartX, y, btnW, ClientConfig.elementHeight);
        Rect resetRect = new(resetStartX, y, resetBtnW, ClientConfig.elementHeight);

        GUI.Box(btnRect, Value ? Translate("ON") : Translate("OFF"),
            Value ? ThemeManager.SettingOn : ThemeManager.SettingOff);

        if (btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            if (!IsDisabled) Value = !Value;
            e.Use();
        }

        if (!IsDisabled && DrawResetButton(resetRect))
        {
            Reset();
        }

        GUI.enabled = prevGUIEnabled;

        y += ClientConfig.elementHeight + ClientConfig.spacing;
    }
}