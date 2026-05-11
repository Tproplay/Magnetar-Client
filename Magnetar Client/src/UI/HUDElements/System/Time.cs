using static Magnetar_Client.UI.Themes.Magnetar_Default;
using UnityEngine;

namespace Magnetar_Client.UI.HUDElements
{
    public class CurrentTime : HudElement
    {
        public CurrentTime() : base("Current Time", HudElement.NewRect(100))
        { }

        protected override void DrawContent(float width, float height)
        { 

            string displayText = $"<color=white>{SystemClock.now.ToString("HH:mm:ss")}</color>";

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }
}
