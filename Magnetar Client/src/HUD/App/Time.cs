using System;
using System.Diagnostics;
using UnityEngine;

namespace Magnetar_Client.HUDElements;

#if !ANDROID
public class UpTime : HudElement
{
    public UpTime() : base("Up Time", HudElement.NewRect(100))
    { }

    protected override void DrawContent(float width, float height)
    {
        Process currentProcess = Process.GetCurrentProcess();

        TimeSpan upTime = DateTime.Now - currentProcess.StartTime;

        string displayText = $"<color=white>{upTime.ToString(@"hh\:mm\:ss")}</color>";

        AdjustWidthToText(displayText, 10f);

        Draw(displayText, width, height);
    }
}
#endif
