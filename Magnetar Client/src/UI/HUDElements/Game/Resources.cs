using static Magnetar_Client.UI.Themes.Magnetar_Default;
using static Magnetar_Client.Game.GameData;
using static Magnetar_Client.Utils.Maths;

using UnityEngine;
namespace Magnetar_Client.UI.HUDElements
{
    public class SunObtained : HudElement
    {
        public SunObtained() : base("Total Sun Obtained", HudElement.NewRect(100))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Sun Obtained: {FormatInternational(TotalAmountOfSunObtained)}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class SunSpent : HudElement
    {
        public SunSpent() : base("Total Sun Spent", HudElement.NewRect(100))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Sun Spent: {FormatInternational(TotalAmountOfSunSpent)}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class MoneyObtained : HudElement
    {
        public MoneyObtained() : base("Total Money Obtained", HudElement.NewRect(100))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Money Obtained: {FormatInternational(TotalAmountOfMoneyObtained)}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class MoneySpent : HudElement
    {
        public MoneySpent() : base("Total Money Spent", HudElement.NewRect(100))
        { }


        protected override void DrawContent(float width, float height)
        {

            string displayText = $"Money Spent: {FormatInternational(TotalAmountOfMoneySpent)}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }
}