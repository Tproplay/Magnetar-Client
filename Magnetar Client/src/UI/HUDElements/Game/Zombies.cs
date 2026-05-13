using static Magnetar_Client.UI.Themes.Magnetar_Default;
using static Magnetar_Client.Game.GameData;
using UnityEngine;
namespace Magnetar_Client.UI.HUDElements
{
    public class NumberOfZombies : HudElement
    {
        public NumberOfZombies() : base("Total Zombies", HudElement.NewRect(165))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Total Zombies: {zombieList.Count}";

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class NumberOfZombiesSpawned : HudElement
    {
        public NumberOfZombiesSpawned() : base("Total Zombie Spawned", HudElement.NewRect(240))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Total Zombie Spawned: {TotalNumberOfZombiesSpawed}";

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class NumberOfZombiesKilled : HudElement
    {
        public NumberOfZombiesKilled() : base("Total Zombie Killed", HudElement.NewRect(220))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Total Zombie Killed: {TotalNumberOfZombiesKilled}";

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }
}
