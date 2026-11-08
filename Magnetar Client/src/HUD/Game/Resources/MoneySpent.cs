using UnityEngine;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class MoneySpent : HudElement
{
    public MoneySpent() : base("Total Money Spent", HudElement.NewRect(100))
    {
        UpdateInterval = 0.5f;
        RegisterTranslation("Money Spent: NA", "Money Spent: NA");
        RegisterRegexTranslation(@"^Money Spent: (.*)$", "Money Spent: {1}");
    }

    private int value;
    private string _naText = "Money Spent: NA";
    private string _format = "Money Spent: {0}";
    private string displayText = "Money Spent: NA";

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
        _naText = Translate("Money Spent: NA");
        _format = GetFormat(@"^Money Spent: (.*)$", "Money Spent: {0}");
    }

    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            value = GameData.BoardInstance.boardStatistics.moneyConsumed;
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
