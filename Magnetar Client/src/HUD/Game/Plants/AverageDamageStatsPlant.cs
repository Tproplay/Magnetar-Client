using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class AverageDamageStatsPlant : HudElement
{
    public AverageDamageStatsPlant() : base("Average Damage by Plants", HudElement.NewRect(100))
    {
        UpdateInterval = 0.1f;
        RegisterTranslation("Plant DPS: 0", "Plant DPS: 0");
        RegisterRegexTranslation(@"^Plant DPS: (.*)$", "Plant DPS: {1}");
    }

    private List<long> damageHistory = new();
    private long Dps;
    private string _format = "Plant DPS: {0}";
    private string displayText = "Plant DPS: 0";

    protected override void DrawContent(float width, float height)
    {
        Draw(displayText, width, height);
    }

    public override void OnEnable()
    {
        UpdateTranslations();
        UpdateDisplay();
    }

    public override void OnLanguageChanged()
    {
        UpdateTranslations();
        UpdateDisplay();
    }

    private void UpdateTranslations()
    {
        _format = GetFormat(@"^Plant DPS: (.*)$", "Plant DPS: {0}");
    }

    public override void OnUpdateActive()
    {
        if (GameData.BoardInstanceIsNull) return;

        damageHistory.Add((long)GameData.BoardInstance.boardStatistics.totalZombieDamage);

        // Keep only the last 10 snapshots (covering 5 second of gameplay)
        if (damageHistory.Count > 10)
        {
            damageHistory.RemoveAt(0);
            Dps = (damageHistory[9] - damageHistory[0]) / 5;
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        displayText = string.Format(_format, FormatInternational(Dps));
        AdjustWidthToText(displayText, 10f);
    }
}
