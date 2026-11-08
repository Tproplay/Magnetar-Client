using UnityEngine;

namespace Magnetar_Client.HUDElements;

#if !ANDROID
public class CurrentTime : HudElement
{
    public CurrentTime() : base("Current Time", HudElement.NewRect(80))
    { }

    protected override void DrawContent(float width, float height)
    {
        string displayText = SystemClock.now.ToString("HH:mm:ss");
        AdjustWidthToText(displayText, 10f);
        Draw(displayText, width, height);
    }
}
#endif
