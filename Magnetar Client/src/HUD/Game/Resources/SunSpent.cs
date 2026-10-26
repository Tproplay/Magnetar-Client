using UnityEngine;

using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class SunSpent : HudElement
{
    public SunSpent() : base("Total Sun Spent", HudElement.NewRect(100))
    {
        UpdateInterval = 0.5f;
        RegisterTranslation("Sun Spent: Na", "Sun Spent: Na");
        RegisterRegexTranslation(@"^Sun Spent: (.*)$", "Sun Spent: {1}");
    }

    int value;
    string displayText = "Sun Spent: Na";

    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            value = GameData.BoardInstance.boardStatistics.sunConsumed;
        }
        displayText = Translate($"Sun Spent: {FormatInternational(value)}");

        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
