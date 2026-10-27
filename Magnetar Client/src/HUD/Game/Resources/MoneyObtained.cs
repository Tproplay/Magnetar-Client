using UnityEngine;

using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class MoneyObtained : HudElement
{
    public MoneyObtained() : base("Total Money Obtained", HudElement.NewRect(100))
    {
        UpdateInterval = 0.5f;
        RegisterTranslation("Money Obtained: NA", "Money Obtained: NA");
        RegisterRegexTranslation(@"^Money Obtained: (.*)$", "Money Obtained: {1}");
    }

    int value;
    string displayText = "Money Obtained: NA";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            value = GameData.BoardInstance.boardStatistics.moneyEarned;
        }

        displayText = Translate($"Money Obtained: {FormatInternational(value)}");

        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
