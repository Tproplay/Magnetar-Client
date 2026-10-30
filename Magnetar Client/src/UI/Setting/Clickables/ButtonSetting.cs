using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;
using System;
using System.Security.AccessControl;
using System.Xml.Linq;
using UnityEngine;
using static Magnetar_Client.Utils.Translator;

namespace Magnetar_Client.UI.Setting;

/// <summary>
/// An actionable setting that displays a clickable button to trigger custom actions or callbacks.
/// </summary>
public class ButtonSetting : Setting
{
    /// <summary>
    /// The text displayed inside the clickable button box.
    /// </summary>
    public string ButtonText;

    /// <summary>
    /// Optional custom button text display name override. If set, this name is 
    /// displayed directly in the UI instead of translating <see cref="ButtonText"/>.
    /// </summary>
    public string CustomButtonText { get; set; }

    /// <summary>
    /// Gets the resolved display text for rendering this setting's label.
    /// Returns <see cref="CustomButtonText"/> if specified; otherwise translates <see cref="ButtonText"/>.
    /// </summary>
    public virtual string DisplayButtonText => !string.IsNullOrEmpty(CustomButtonText)
        ? CustomButtonText
        : Translator.Translate(ButtonText);

    /// <summary>
    /// The action invoked when the button is clicked.
    /// </summary>
    public Action OnClick;

    /// <summary>
    /// Indicates that action buttons do not maintain restorable value state.
    /// </summary>
    public override bool CanReset => false;

    /// <summary>
    /// Initializes a new instance of <see cref="ButtonSetting"/>.
    /// </summary>
    /// <param name="name">The label name displayed on the left side of the setting row.</param>
    /// <param name="onClick">The callback delegate triggered on click.</param>
    /// <param name="buttonText">The text label shown on the button surface.</param>
    public ButtonSetting(string name, Action onClick, string buttonText = "Click")
    {
        Name = name;
        OnClick = onClick;
        ButtonText = buttonText;
    }

    /// <summary>
    /// Renders the setting label and button, processing click events in the IMGUI layout.
    /// </summary>
    /// <param name="y">Reference to the vertical layout offset.</param>
    /// <param name="width">The total available layout width.</param>
    public override void Draw(ref float y, float width)
    {
        Event e = Event.current;

        float indent = ClientConfig.indent;
        float gap = SettingValues.Gap;

        string localizedName = DisplayName;
        string localizedButtonText = DisplayButtonText;

        // Label
        float labelW = width - indent * 2 - ClientConfig.SettingWidth;
        Rect labelRect = new(indent, y, labelW, ClientConfig.elementHeight);
        GUI.Label(labelRect, localizedName, ThemeManager.SettingLabelStyle);

        // Button
        float btnW = ClientConfig.SettingWidth;
        float btnStartX = width - indent - btnW - gap - SettingValues.ResetButtonW;
        Rect btnRect = new(btnStartX, y, ClientConfig.SettingWidth, ClientConfig.elementHeight);
        GUI.Box(btnRect, localizedButtonText, ThemeManager.ButtonSettingStyle);

        if (btnRect.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
        {
            if (!IsDisabled) OnClick?.Invoke();
            e.Use();
        }

        y += ClientConfig.elementHeight + ClientConfig.spacing;
    }
}