using UnityEngine;

using static Magnetar_Client.UI.Themes.ThemeManager;

namespace Magnetar_Client.HUDElements;

public class TimeScale : HudElement
{
    public TimeScale() : base("Game Speed", HudElement.NewRect(180))
    {
        UpdateInterval = 0.1f;
        RegisterTranslation("TimeScale: 0", "TimeScale: 0");
        RegisterRegexTranslation(@"^TimeScale: (.*)$", "TimeScale: {1}");
    }
    string format = "0.##";
    string displayText = "TimeScale: 0";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }
    public override void OnUpdateActive()
    {
        displayText = Translate($"TimeScale: {(UnityEngine.Time.timeScale).ToString(format)}");
        AdjustWidthToText(displayText, HUDElementStyle, 10);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
