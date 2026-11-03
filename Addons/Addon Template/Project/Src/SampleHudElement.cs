using Magnetar_Client.HUDElements;
using Magnetar_Client.UI.Themes;
using UnityEngine;

namespace AddonTemplate;

/// <summary>
/// A sample on-screen HUD element.
/// Magnetar Client automatically discovers any class extending HudElement
/// and registers it into the HUD Manager configuration menu.
/// </summary>
public class SampleHudElement : HudElement
{
    private string _displayText = "Sample HUD: <color=lime>Active</color>";

    public SampleHudElement() : base("Sample HUD Element", HudElement.NewRect(180, 24))
    {
        UpdateInterval = 1.0f;
    }

    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), _displayText, ThemeManager.HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        int frameCount = Time.frameCount;
        _displayText = $"Sample HUD: <color=lime>{Magnetar_Client.Utils.Maths.FormatInternational(frameCount)}</color>";

        // Dynamically adjust window width to fit the text with 10px padding
        AdjustWidthToText(_displayText, ThemeManager.HUDElementStyle, 10f);
    }

    public override void OnEnable()
    {
        AdjustWidthToText(_displayText, ThemeManager.HUDElementStyle, 10f);
    }
}
