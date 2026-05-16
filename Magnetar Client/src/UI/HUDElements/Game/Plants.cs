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

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class NumberOfPlantsSpawned : HudElement
    {
        public NumberOfPlantsSpawned() : base("Plants Placed", HudElement.NewRect(100))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Plants Placed: {TotalNumberOfPlantsSpawned}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class NumberOfPlantsKilled : HudElement
    {
        public NumberOfPlantsKilled() : base("Plant Deaths", HudElement.NewRect(100))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Plant Death: {TotalNumberOfPlantsKilled}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }
}
