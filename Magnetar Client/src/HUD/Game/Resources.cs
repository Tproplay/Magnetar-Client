using static Magnetar_Client.UI.Themes.Magnetar_Default;
using static Magnetar_Client.Game.GameData;
using static Magnetar_Client.Utils.Maths;

using UnityEngine;
using Magnetar_Client.Game;
namespace Magnetar_Client.HUDElements
{
    public class SunObtained : HudElement
    {
        public SunObtained() : base("Total Sun Obtained", HudElement.NewRect(100))
        { }

        int value;
        string displayText;

        protected override void DrawContent(float width, float height)
        {

            if (!AppData.BoardInstanceIsNull)
            {
                value = AppData.board.boardStatistics.sunProduced;
            }

            displayText = $"Sun Obtained: {FormatInternational(value)}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class SunSpent : HudElement
    {
        public SunSpent() : base("Total Sun Spent", HudElement.NewRect(100))
        { }

        int value;
        string displayText;

        protected override void DrawContent(float width, float height)
        {

            if (!AppData.BoardInstanceIsNull)
            {
                value = AppData.board.boardStatistics.sunConsumed;
            }
            displayText = $"Sun Spent: {FormatInternational(value)}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class MoneyObtained : HudElement
    {
        public MoneyObtained() : base("Total Money Obtained", HudElement.NewRect(100))
        { }

        int value;
        string displayText;
        protected override void DrawContent(float width, float height)
        {

            if (!AppData.BoardInstanceIsNull)
            {
                value = AppData.board.boardStatistics.moneyEarned;
            }

            displayText = $"Money Obtained: {FormatInternational(value)}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }

    public class MoneySpent : HudElement
    {
        public MoneySpent() : base("Total Money Spent", HudElement.NewRect(100))
        { }

        int value;
        string displayText;
        protected override void DrawContent(float width, float height)
        {

            if (!AppData.BoardInstanceIsNull)
            {
                value = AppData.board.boardStatistics.moneyConsumed;
            }

            displayText = $"Money Spent: {FormatInternational(value)}";

            AdjustWidthToText(displayText, HUDElementStyle, 10f);

            GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
        }
    }
}