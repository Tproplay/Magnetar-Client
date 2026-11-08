using UnityEngine;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class FallSunCD : HudElement
{
    public FallSunCD() : base("Auto drop/fall sun timer", HudElement.NewRect(180))
    {
        UpdateInterval = 0.5f;
        RegisterTranslation("Auto Sun: NA", "Auto Sun: NA");
        RegisterTranslation("Auto Sun: 0s", "Auto Sun: 0s");
        RegisterRegexTranslation(@"^Auto Sun: (.*)$", "Auto Sun: {1}");
    }

    private string _zeroText = "Auto Sun: 0s";
    private string _format = "Auto Sun: {0}";
    private string displayText = "Auto Sun: 0s";

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
        _zeroText = Translate("Auto Sun: 0s");
        _format = GetFormat(@"^Auto Sun: (.*)$", "Auto Sun: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull)
            displayText = string.Format(_format, FormatTime((int)GameData.BoardInstance.theFallingSunCountDown));
        else
            displayText = _zeroText;

        AdjustWidthToText(displayText, 10f);
    }
}
