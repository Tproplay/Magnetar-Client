using UnityEngine;

namespace Magnetar_Client.HUDElements;

public class TimeScale : HudElement
{
    public TimeScale() : base("Game Speed", HudElement.NewRect(180))
    {
        UpdateInterval = 0.1f;
        RegisterTranslation("TimeScale: 0", "TimeScale: 0");
        RegisterRegexTranslation(@"^TimeScale: (.*)$", "TimeScale: {1}");
    }

    private const string NumberFormat = "0.##";
    private string _format = "TimeScale: {0}";
    private string displayText = "TimeScale: 1";

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
        _format = GetFormat(@"^TimeScale: (.*)$", "TimeScale: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        displayText = string.Format(_format, Time.timeScale.ToString(NumberFormat));
        AdjustWidthToText(displayText, 10f);
    }
}
