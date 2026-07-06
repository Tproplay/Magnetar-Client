using Magnetar_Client.Utils;
using UnityEngine;
using static Magnetar_Client.Game.GameData;
using static Magnetar_Client.UI.Themes.Magnetar_Default;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements
{
    public class NumberOfBulletsSpawned : HudElement
    {
        public NumberOfBulletsSpawned() : base("Number of Bullets Spawned", HudElement.NewRect(100))
        { UpdateInterval = 0.5f; }

        string displayText = "Bullets Spawned: Na";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            displayText = $"Bullets Spawned: {FormatInternational(TotalNumberOfBulletsSpawned)}";
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }
}