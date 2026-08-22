using UnityEngine;
using static Magnetar_Client.UI.Themes.Magnetar_Default;

namespace Magnetar_Client.HUDElements
{
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


    public class VanillaMode : HudElement
    {
        public static VanillaMode instance {  get; private set; }
        public VanillaMode() : base("Using Vanilla Mode", HudElement.NewRect(90))
        { }
        private string displayText = "Vanilla Mode: <color=yellow>Na</color>";

        public bool VanillaModeEnabled;

        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnEnable()
        {
            instance = this;
            UpdateText();
        }

        public void UpdateText()
        {
            string color, text;

            if (VanillaModeEnabled) 
            { 
                color = "green";
                text = "Active";
            }
            else 
            { 
                color = "red";
                text = "Inactive";
            }

            displayText = $"Vanilla Mode: <color={color}>{text}</color>";
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }
}