using UnityEngine;

using static Magnetar_Client.GameData;
using static Magnetar_Client.UI.Themes.ThemeManager;

namespace Magnetar_Client.HUDElements;

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
            value = BoardInstance.boardStatistics.plantsDeath;
        }

        displayText = $"Plant Death: {value}";
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
