#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif
using static Magnetar_Client.UI.Themes.Magnetar_Default;
using UnityEngine;
using System;
using static Magnetar_Client.Game.AppData;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements
{
    public class TimeInLevel : HudElement
    {
        public TimeInLevel() : base("Time In Current Level", HudElement.NewRect(180))
        { UpdateInterval = 0.1f; }

        DateTime StartTime = DateTime.Now;
        bool saved = false;
        public override void OnUpdate()
        {
            base.OnUpdate();
            if (BoardInstanceIsNull) { saved= false; return; }

            if (!saved) { StartTime = DateTime.Now; saved = true; }
        }

        string displayText = $"Playing For: {(DateTime.Now-DateTime.Now):hh\\:mm\\:ss}";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            displayText = $"Playing For: {(DateTime.Now - StartTime):hh\\:mm\\:ss}";
            AdjustWidthToText(displayText, HUDElementStyle, 10);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    public class WaveTimer : HudElement
    {
        public WaveTimer() : base("Next wave arrival time", HudElement.NewRect(180))
        { UpdateInterval = 0.1f; }

        string displayText = "Next Wave arrival: Na";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
        public override void OnUpdateActive()
        {
            if (!BoardInstanceIsNull)
                displayText = $"Next Wave arrival: {FormatTime((int)board.timeUntilNextWave >= 0 ? (int)board.timeUntilNextWave : 0)}";
            else
                displayText = "Next Wave arrival: Na";
            AdjustWidthToText(displayText, HUDElementStyle, 10);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    public class ActiveStarTimer : HudElement
    {
        public ActiveStarTimer() : base("Active Star drop Cooldown", HudElement.NewRect(180))
        { UpdateInterval = 0.1f; }

        string displayText = "Active Star drop: NA";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
        public override void OnUpdateActive()
        {
            if (!BoardInstanceIsNull)
                displayText = $"Active Star drop: {FormatTime((int)board.bigStarActiveCountDown)}";
            else
                displayText = "Active Star drop: Na";
            AdjustWidthToText(displayText, HUDElementStyle, 10);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    public class PassiveStarTimer : HudElement
    {
        public PassiveStarTimer() : base("Passive Star drop Cooldown", HudElement.NewRect(180))
        { UpdateInterval = 0.1f; }

        string displayText = "Passive Star drop: NA";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
        public override void OnUpdateActive()
        {
            if (!BoardInstanceIsNull)
                displayText = $"Passive Star drop: {FormatTime((int)board.bigStarPassiveCountDown)}";
            else
                displayText = "Passive Star drop: Na";
            AdjustWidthToText(displayText, HUDElementStyle, 10);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    public class TimeScale : HudElement
    {
        public TimeScale() : base("Game Speed", HudElement.NewRect(180))
        { UpdateInterval = 0.1f; }
        string format = "0.##";
        string displayText = "TimeScale: 0";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
        public override void OnUpdateActive()
        {
            displayText = $"TimeScale: {(UnityEngine.Time.timeScale).ToString(format)}";
            AdjustWidthToText(displayText, HUDElementStyle, 10);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }
}
