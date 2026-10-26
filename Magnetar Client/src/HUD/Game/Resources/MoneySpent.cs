using UnityEngine;

using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class MoneySpent : HudElement
{
    public MoneySpent() : base("Total Money Spent", HudElement.NewRect(100))
    {
        UpdateInterval = 0.5f;
        RegisterTranslation("Money Spent: Na", "Money Spent: Na");
        RegisterRegexTranslation(@"^Money Spent: (.*)$", "Money Spent: {1}");
    }

    int value;
    string displayText = "Money Spent: Na";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            value = GameData.BoardInstance.boardStatistics.moneyConsumed;
        }

        displayText = Translate($"Money Spent: {FormatInternational(value)}");

        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
