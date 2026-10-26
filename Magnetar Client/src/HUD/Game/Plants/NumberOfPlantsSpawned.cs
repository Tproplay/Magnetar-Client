using UnityEngine;

using static Magnetar_Client.GameData;
using static Magnetar_Client.UI.Themes.ThemeManager;

namespace Magnetar_Client.HUDElements;

public class NumberOfPlantsSpawned : HudElement
{
    public NumberOfPlantsSpawned() : base("Plants Placed", HudElement.NewRect(100))
    {
        UpdateInterval = 1f;
        RegisterTranslation("Plants Placed: Na", "Plants Placed: Na");
        RegisterRegexTranslation(@"^Plants Placed: (.*)$", "Plants Placed: {1}");
    }

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
            value = BoardInstance.boardStatistics.plantsPlanted;
        }

        displayText = Translate($"Plants Placed: {value}");
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
