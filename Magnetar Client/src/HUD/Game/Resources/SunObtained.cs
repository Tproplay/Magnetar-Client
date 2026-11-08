using UnityEngine;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class SunObtained : HudElement
{
    public SunObtained() : base("Total Sun Obtained", HudElement.NewRect(100))
    {
        UpdateInterval = 0.5f;
        RegisterTranslation("Sun Obtained: NA", "Sun Obtained: NA");
        RegisterRegexTranslation(@"^Sun Obtained: (.*)$", "Sun Obtained: {1}");
    }

    private int value;
    private string _naText = "Sun Obtained: NA";
    private string _format = "Sun Obtained: {0}";
    private string displayText = "Sun Obtained: NA";

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
        _naText = Translate("Sun Obtained: NA");
        _format = GetFormat(@"^Sun Obtained: (.*)$", "Sun Obtained: {0}");
    }

    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            value = GameData.BoardInstance.boardStatistics.sunProduced;
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
