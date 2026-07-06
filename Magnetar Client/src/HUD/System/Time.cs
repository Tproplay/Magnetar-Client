using static Magnetar_Client.UI.Themes.Magnetar_Default;
using UnityEngine;

namespace Magnetar_Client.HUDElements
{
    public class CurrentTime : HudElement
    {
        public CurrentTime() : base("Current Time", HudElement.NewRect(80))
        { }
        string displayText;
        protected override void DrawContent(float width, float height)
        { 
            GUI.Label(new Rect(5, 5, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            displayText = $"<color=white>{SystemClock.now.ToString("HH:mm:ss")}</color>";

            AdjustWidthToText(displayText, HUDElementStyle, 10);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }
}
