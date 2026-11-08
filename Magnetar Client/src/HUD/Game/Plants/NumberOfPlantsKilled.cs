using UnityEngine;
using static Magnetar_Client.GameData;

namespace Magnetar_Client.HUDElements;

public class NumberOfPlantsKilled : HudElement
{
    public NumberOfPlantsKilled() : base("Plant Deaths", HudElement.NewRect(100))
    {
        UpdateInterval = 1f;
        RegisterTranslation("Plant Death: NA", "Plant Death: NA");
        RegisterRegexTranslation(@"^Plant Death: (.*)$", "Plant Death: {1}");
    }

    private int value;
    private string _naText = "Plant Death: NA";
    private string _format = "Plant Death: {0}";
    private string displayText = "Plant Death: NA";

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
        _naText = Translate("Plant Death: NA");
        _format = GetFormat(@"^Plant Death: (.*)$", "Plant Death: {0}");
    }

    public override void OnUpdateActive()
    {
        if (!BoardInstanceIsNull)
        {
            value = BoardInstance.boardStatistics.plantsDeath;
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!BoardInstanceIsNull)
            displayText = string.Format(_format, value);
        else
            displayText = _naText;

        AdjustWidthToText(displayText, 10f);
    }
}
