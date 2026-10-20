using UnityEngine;
using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class DamageStatsPlant : HudElement
{
    public DamageStatsPlant() : base("Total Damage by Plants", HudElement.NewRect(100))
    { UpdateInterval = 0.5f; }

    float value;
    string displayText = "Plant Damage: Na";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            value = GameData.BoardInstance.boardStatistics.totalZombieDamage;
        }

        displayText = $"Plant Damage: {FormatInternational((long)value)}";

        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
