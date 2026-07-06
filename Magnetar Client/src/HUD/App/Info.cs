using System.Reflection;
using UnityEngine;
using static Magnetar_Client.UI.Themes.Magnetar_Default;

namespace Magnetar_Client.HUDElements
{

    public class GameVersion : HudElement
    {
        public GameVersion() : base("Game Name and Version", HudElement.NewRect(90))
        { }
        private static string displayText;

        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnEnable()
        {
            displayText = $"Plants Vs. Zombies Fusion v{Application.version}";
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }

    }

    public class MagnetarVersion : HudElement
    {
        private static readonly string displayText = $"Magnetar Client v{Magnetar_Info.Version}";

        public MagnetarVersion() : base("Magnetar Name and Version", HudElement.NewRect(90))
        { }

        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }

    }

}
