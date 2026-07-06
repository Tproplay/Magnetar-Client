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
        { UpdateInterval = 1f; }

        string displayText = "Total Plants: Na";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            displayText = $"Total Plants: {plantList.Count}";
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    public class NumberOfPlantsSpawned : HudElement
    {
        public NumberOfPlantsSpawned() : base("Plants Placed", HudElement.NewRect(100))
        { UpdateInterval = 1f; }

        int value;
        string displayText = "Plants Placed: Na";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            if (!BoardInstanceIsNull)
            {
                value = board.boardStatistics.plantsPlanted;
            }

            displayText = $"Plants Placed: {value}";
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }

    public class NumberOfPlantsKilled : HudElement
    {
        public NumberOfPlantsKilled() : base("Plant Deaths", HudElement.NewRect(100))
        { UpdateInterval = 1f; }

        int value;
        string displayText = "Plant Death: Na";
        protected override void DrawContent(float width, float height)
        {
            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }

        public override void OnUpdateActive()
        {
            if (!BoardInstanceIsNull)
            {
                value = board.boardStatistics.plantsDeath;
            }

            displayText = $"Plant Death: {value}";
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
        public override void OnEnable()
        {
            AdjustWidthToText(displayText, HUDElementStyle, 10f);
        }
    }
}
