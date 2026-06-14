using static Magnetar_Client.UI.Themes.Magnetar_Default;
using UnityEngine;
using Il2Cpp;
using System;
using static Magnetar_Client.Game.AppData;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements
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

    public class WaveTimer : HudElement
    {
        public WaveTimer() : base("Next wave arrival time", HudElement.NewRect(180))
        { }

        string displayText;
        protected override void DrawContent(float width, float height)
        {
            
            if (!BoardInstanceIsNull && GameAPP.theGameStatus == GameStatus.InGame 
                && board.timeUntilNextWave>0)
                displayText = $"Next Wave arrival: {FormatTime((int)board.timeUntilNextWave)}";
            else
                displayText = "Next Wave arrival: 0s";
            AdjustWidthToText(displayText, HUDElementStyle, 10);
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }
}
