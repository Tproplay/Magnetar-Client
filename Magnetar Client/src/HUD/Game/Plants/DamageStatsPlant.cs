using UnityEngine;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class DamageStatsPlant : HudElement
{
    public DamageStatsPlant() : base("Total Damage by Plants", HudElement.NewRect(100))
    {
        UpdateInterval = 0.5f;
        RegisterTranslation("Plant Damage: NA", "Plant Damage: NA");
        RegisterRegexTranslation(@"^Plant Damage: (.*)$", "Plant Damage: {1}");
    }

    private float value;
    private string _naText = "Plant Damage: NA";
    private string _format = "Plant Damage: {0}";
    private string displayText = "Plant Damage: NA";

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
        _naText = Translate("Plant Damage: NA");
        _format = GetFormat(@"^Plant Damage: (.*)$", "Plant Damage: {0}");
    }

    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            value = GameData.BoardInstance.boardStatistics.totalZombieDamage;
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull)
            displayText = string.Format(_format, FormatInternational((long)value));
        else
            displayText = _naText;

        AdjustWidthToText(displayText, 10f);
    }
}
