using UnityEngine;
using static Magnetar_Client.UI.Themes.ThemeManager;

namespace Magnetar_Client.HUDElements;

#if !ANDROID
public class CurrentTime : HudElement
{
    public CurrentTime() : base("Current Time", HudElement.NewRect(80))
    {
        RegisterRegexTranslation(@"^<color=white>(.*)</color>$", "<color=white>{1}</color>");
    }
    string displayText;
    protected override void DrawContent(float width, float height)
    { 
        GUI.Label(new Rect(5, 5, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        displayText = Translate($"<color=white>{SystemClock.now.ToString("HH:mm:ss")}</color>");

        AdjustWidthToText(displayText, HUDElementStyle, 10);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
#endif