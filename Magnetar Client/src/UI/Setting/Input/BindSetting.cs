using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using static Magnetar_Client.Utils.Translator;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// A setting module representing a customizable key combination or hotkey shortcut.
/// </summary>
public class BindSetting : Setting
{
    private List<KeyCode> _bindKeys = new();

    /// <summary>
    /// The default key combination restored when <see cref="Reset"/> is executed.
    /// </summary>
    public List<KeyCode> DefaultKeys { get; private set; } = new();

    /// <summary>
    /// Indicates whether this setting is actively listening for keyboard input to bind.
    /// </summary>
    public bool IsBinding;

    /// <summary>
    /// Event invoked immediately before <see cref="BindKeys"/> changes, passing the new candidate keys.
    /// </summary>
    public Action<List<KeyCode>> OnValueChanging { get; set; }

    /// <summary>
    /// Event invoked after <see cref="BindKeys"/> changes.
    /// </summary>
    public Action<List<KeyCode>> OnValueChanged { get; set; }

    /// <summary>
    /// Gets or sets the list of bound keys forming the shortcut chord.
    /// </summary>
    public List<KeyCode> BindKeys
    {
        get => _bindKeys;
        set
        {
            if (IsDisabled) return;
            value ??= new List<KeyCode>();
            if (!_bindKeys.SequenceEqual(value))
            {
                OnValueChanging?.Invoke(value);
                _bindKeys = value;
                OnValueChanged?.Invoke(_bindKeys);
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="BindSetting"/> with a name and optional default key list.
    /// </summary>
    /// <param name="name">The display name of the setting.</param>
    /// <param name="defaultKeys">The initial key combination.</param>
    public BindSetting(string name, List<KeyCode> defaultKeys = null)
    {
        Name = name;
        if (defaultKeys != null)
        {
            DefaultKeys = new List<KeyCode>(defaultKeys);
            _bindKeys = new List<KeyCode>(defaultKeys);
        }
    }

    /// <summary>
    /// Formats the current keybind combination into a user-readable string (e.g., "LeftControl + F").
    /// </summary>
    /// <returns>A formatted representation of the bound keys, or "None" if empty.</returns>
    public string GetBindString()
    {
        if (BindKeys == null || BindKeys.Count == 0) return "None";
        return string.Join(" + ", BindKeys.Select(k => k.ToString()).ToArray());
    }

    /// <summary>
    /// Restores the keybinds to <see cref="DefaultKeys"/> and cancels active listening.
    /// </summary>
    public override void Reset()
    {
        BindKeys = new List<KeyCode>(DefaultKeys);
        IsBinding = false;
    }

    /// <summary>
    /// Renders the keybind button, handles key capture listening, and draws the reset button in IMGUI layout.
    /// </summary>
    /// <param name="y">Reference to the vertical layout offset.</param>
    /// <param name="width">The total available layout width.</param>
    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;
        bool isLeftClick = e.type == EventType.MouseDown && e.button == 0;

        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;
        float resetBtnW = SettingValues.ResetButtonW;
        float settingW = ClientConfig.SettingWidth;

        // Label: occupies remaining width on the left
        float labelW = width - (indent * 2f) - settingW - gap - resetBtnW;
        Rect labelRect = new(indent, y, labelW, ClientConfig.elementHeight);
        GUI.Label(labelRect, Translate(Name), ThemeManager.SettingLabelStyle);

        // Right-aligned elements: [Control Button] [gap] [Reset Button]
        float resetStartX = width - indent - resetBtnW;
        float bindStartX = resetStartX - gap - settingW;

        Rect bindRect = new(bindStartX, y, settingW, ClientConfig.elementHeight);
        Rect resetRect = new(resetStartX, y, resetBtnW, ClientConfig.elementHeight);

        string bindText = IsBinding ? "[...]" : GetBindString();
        GUI.Box(bindRect, bindText, IsBinding ? ThemeManager.SettingOn : ThemeManager.SettingOff);

        if (bindRect.Contains(e.mousePosition) && isLeftClick)
        {
            IsBinding = !IsBinding;
            if (IsBinding)
            {
                BindKeys.Clear();
                DrawSetting.ActiveTextFieldId = -1;
                DrawSetting.FocusedControlId = -1;
            }
            e.Use();
        }

        if (IsBinding && e.isKey)
        {
            KeyCode key = e.keyCode;
            if (key != KeyCode.None)
            {
                if (e.type == EventType.KeyDown)
                {
                    if (key == KeyCode.Escape || key == ClientConfig.MenuOpenKey)
                    {
                        BindKeys.Clear();
                        IsBinding = false;
                    }
                    else if (!BindKeys.Contains(key))
                    {
                        BindKeys.Add(key);
                    }
                    e.Use();
                }
                else if (e.type == EventType.KeyUp && BindKeys.Count > 0)
                {
                    IsBinding = false;
                    e.Use();
                }
            }
        }

        if (DrawResetButton(resetRect))
        {
            Reset();
        }

        y += ClientConfig.elementHeight + ClientConfig.spacing;
    }
}