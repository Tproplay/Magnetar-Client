using UnityEngine;
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

    private int value;
    private string _naText = "Money Obtained: NA";
    private string _format = "Money Obtained: {0}";
    private string displayText = "Money Obtained: NA";

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
        _naText = Translate("Money Obtained: NA");
        _format = GetFormat(@"^Money Obtained: (.*)$", "Money Obtained: {0}");
    }

    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            value = GameData.BoardInstance.boardStatistics.moneyEarned;
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull)
            displayText = string.Format(_format, FormatInternational(value));
        else
            displayText = _naText;

        AdjustWidthToText(displayText, 10f);
    }
}
