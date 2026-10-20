using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class AverageDamageStatsPlant : HudElement
{
    public AverageDamageStatsPlant() : base("Average Damage by Plants", HudElement.NewRect(100))
    { UpdateInterval = 0.1f; }

    private List<long> damageHistory = new();

    long Dps;
    string displayText = "Plant DPS: 0";
    public override void OnUpdateActive()
    {
        if (GameData.BoardInstanceIsNull) return;

        damageHistory.Add((long)GameData.BoardInstance.boardStatistics.totalZombieDamage);

        // Keep only the last 10 snapshots (covering 5 second of gameplay)
        if (damageHistory.Count > 10)
        {
            damageHistory.RemoveAt(0);
            Dps = (damageHistory[9] - damageHistory[0])/5;
        }

        displayText = $"Plant DPS: {FormatInternational(Dps)}";
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
        
    }

    
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }
    public override void OnEnable()
    {
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
