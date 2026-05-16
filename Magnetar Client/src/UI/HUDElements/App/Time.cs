using System;
using System.Diagnostics;
using static Magnetar_Client.UI.Themes.Magnetar_Default;
using UnityEngine;

namespace Magnetar_Client.UI.HUDElements
{
    public class UpTime : HudElement
    {
        public UpTime() : base("Up Time",HudElement.NewRect(100))
        { }

        protected override void DrawContent(float width, float height)
        {
            Process currentProcess = Process.GetCurrentProcess();

            TimeSpan upTime = DateTime.Now - currentProcess.StartTime;

            string displayText = $"<color=white>{upTime.ToString(@"hh\:mm\:ss")}</color>";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }
}
