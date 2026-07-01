using Newtonsoft.Json.Linq;
using UnityEngine;
using static Magnetar_Client.Game.AppData;
using static Magnetar_Client.Game.GameData;
using static Magnetar_Client.UI.Themes.Magnetar_Default;
namespace Magnetar_Client.HUDElements
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

        int value;
        protected override void DrawContent(float width, float height)
        {
            if (!BoardInstanceIsNull)
            {
                value = board.boardStatistics.plantsPlanted;
            }
            string displayText = $"Plants Placed: {value}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class NumberOfPlantsKilled : HudElement
    {
        public NumberOfPlantsKilled() : base("Plant Deaths", HudElement.NewRect(100))
        { }

        int value;
        protected override void DrawContent(float width, float height)
        {
            if (!BoardInstanceIsNull)
            {
                value = board.boardStatistics.plantsDeath;
            }
            string displayText = $"Plant Death: {value}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }
}
