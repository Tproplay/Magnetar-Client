using static Magnetar_Client.UI.Themes.Magnetar_Default;
using static Magnetar_Client.Game.GameData;
using UnityEngine;
namespace Magnetar_Client.UI.HUDElements
{
    public class NumberOfZombies : HudElement
    {
        public NumberOfZombies() : base("Total Zombies", HudElement.NewRect(150))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Total Zombies: {zombieList.Count}";

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }
}
