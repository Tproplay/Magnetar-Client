using System.Reflection;
using UnityEngine;
using static Magnetar_Client.UI.Themes.Magnetar_Default;

namespace Magnetar_Client.HUDElements
{

    public class GameVersion : HudElement
    {
        public GameVersion() : base("Game Name and Version", HudElement.NewRect(90))
        { }

        private static readonly string displayText = $"Plants Vs. Zombies Fusion v{Application.version}";

        protected override void DrawContent(float width, float height)
        {

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

    }

    public class MagnetarVersion : HudElement
    {
        private static readonly string DisplayText = $"Magnetar Client v{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}";

        public MagnetarVersion() : base("Magnetar Name and Version", HudElement.NewRect(90))
        { }

        protected override void DrawContent(float width, float height)
        {
            AdjustWidthToText(DisplayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), DisplayText, HUDElementStyle);
        }

    }

}
