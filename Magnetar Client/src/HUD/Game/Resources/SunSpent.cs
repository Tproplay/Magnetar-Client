using UnityEngine;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class SunSpent : HudElement
{
    public SunSpent() : base("Total Sun Spent", HudElement.NewRect(100))
    {
        UpdateInterval = 0.5f;
        RegisterTranslation("Sun Spent: NA", "Sun Spent: NA");
        RegisterRegexTranslation(@"^Sun Spent: (.*)$", "Sun Spent: {1}");
    }

    private int value;
    private string _naText = "Sun Spent: NA";
    private string _format = "Sun Spent: {0}";
    private string displayText = "Sun Spent: NA";

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
        _naText = Translate("Sun Spent: NA");
        _format = GetFormat(@"^Sun Spent: (.*)$", "Sun Spent: {0}");
    }

    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            value = GameData.BoardInstance.boardStatistics.sunConsumed;
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
