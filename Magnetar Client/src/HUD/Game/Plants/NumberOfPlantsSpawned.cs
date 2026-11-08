using UnityEngine;
using static Magnetar_Client.GameData;

namespace Magnetar_Client.HUDElements;

public class NumberOfPlantsSpawned : HudElement
{
    public NumberOfPlantsSpawned() : base("Plants Placed", HudElement.NewRect(100))
    {
        UpdateInterval = 1f;
        RegisterTranslation("Plants Placed: NA", "Plants Placed: NA");
        RegisterRegexTranslation(@"^Plants Placed: (.*)$", "Plants Placed: {1}");
    }

    private int value;
    private string _naText = "Plants Placed: NA";
    private string _format = "Plants Placed: {0}";
    private string displayText = "Plants Placed: NA";

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
        _naText = Translate("Plants Placed: NA");
        _format = GetFormat(@"^Plants Placed: (.*)$", "Plants Placed: {0}");
    }

    public override void OnUpdateActive()
    {
        if (!BoardInstanceIsNull)
        {
            value = BoardInstance.boardStatistics.plantsPlanted;
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
