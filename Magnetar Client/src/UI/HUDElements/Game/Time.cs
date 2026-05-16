using static Magnetar_Client.UI.Themes.Magnetar_Default;
using UnityEngine;
using Il2Cpp;
using System;
using static Magnetar_Client.Game.AppData;

namespace Magnetar_Client.UI.HUDElements
{
    public class TimeInLevel : HudElement
    {
        public TimeInLevel() : base("Time In Current Level", HudElement.NewRect(180))
        { }

        DateTime StartTime = DateTime.Now;
        bool saved = false;
        public override void OnUpdate()
        {
            if (BoardInstanceIsNull) { saved= false; return; }

            if (!saved) { StartTime = DateTime.Now; saved = true; }

        }

        string displayText = $"Playing For: {(DateTime.Now-DateTime.Now):hh\\:mm\\:ss}";
        protected override void DrawContent(float width, float height)
        {
            if (board != null) 
                displayText = $"Playing For: {(DateTime.Now-StartTime):hh\\:mm\\:ss}";

            AdjustWidthToText(displayText, HUDElementStyle, 10);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }
}
