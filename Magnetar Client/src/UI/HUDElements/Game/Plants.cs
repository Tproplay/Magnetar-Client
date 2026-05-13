using static Magnetar_Client.UI.Themes.Magnetar_Default;
using static Magnetar_Client.Game.GameData;
using UnityEngine;
namespace Magnetar_Client.UI.HUDElements
{
    public class NumberOfPlants : HudElement
    {
        public NumberOfPlants() : base("Total Plants", HudElement.NewRect(150))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Total Plants: {plantList.Count}";

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class NumberOfPlantsSpawned : HudElement
    {
        public NumberOfPlantsSpawned() : base("Total Plants Placed", HudElement.NewRect(215))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Total Plants Placed: {TotalNumberOfPlantsSpawned}";

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class NumberOfPlantsKilled : HudElement
    {
        public NumberOfPlantsKilled() : base("Total Plant Deaths", HudElement.NewRect(190))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Total Plant Death: {TotalNumberOfPlantsKilled}";

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }
}
