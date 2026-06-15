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

    public class ActiveStarTimer : HudElement
    {
        public ActiveStarTimer() : base("Active Star drop Cooldown", HudElement.NewRect(180))
        { }

        string displayText;
        protected override void DrawContent(float width, float height)
        {

            if (!BoardInstanceIsNull && (GameAPP.theGameStatus == GameStatus.InGame 
                || GameAPP.theGameStatus == GameStatus.Pause)
                && board.bigStarActiveCountDown > 0)
                displayText = $"Active Star drop: {FormatTime((int)board.bigStarActiveCountDown)}";
            else
                displayText = "Active Star drop: NA";
            AdjustWidthToText(displayText, HUDElementStyle, 10);
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class PassiveStarTimer : HudElement
    {
        public PassiveStarTimer() : base("Passive Star drop Cooldown", HudElement.NewRect(180))
        { }

        string displayText;
        protected override void DrawContent(float width, float height)
        {

            if (!BoardInstanceIsNull && (GameAPP.theGameStatus == GameStatus.InGame
                || GameAPP.theGameStatus == GameStatus.Pause)
                && board.bigStarPassiveCountDown > 0)
                displayText = $"Passive Star drop: {FormatTime((int)board.bigStarPassiveCountDown)}";
            else
                displayText = "Passive Star drop: NA";
            AdjustWidthToText(displayText, HUDElementStyle, 10);
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class TimeScale : HudElement
    {
        public TimeScale() : base("Game Speed", HudElement.NewRect(180))
        { }
        string format = "0.##";
        protected override void DrawContent(float width, float height)
        {
            string displayText = $"TimeScale: {(UnityEngine.Time.timeScale).ToString(format)}";
            AdjustWidthToText(displayText, HUDElementStyle, 10);
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }
}
