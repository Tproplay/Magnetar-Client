using UnityEngine;

namespace Magnetar_Client.HUDElements;

#if !ANDROID
public class CurrentTime : HudElement
{
    public CurrentTime() : base("Current Time", HudElement.NewRect(80))
    {
        RegisterRegexTranslation(@"^<color=white>(.*)</color>$", "<color=white>{1}</color>");
    }

    private string _format = "<color=white>{0}</color>";
    private string displayText;

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
        _format = GetFormat(@"^<color=white>(.*)</color>$", "<color=white>{0}</color>");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        displayText = string.Format(_format, SystemClock.now.ToString("HH:mm:ss"));
        AdjustWidthToText(displayText, 10f);
    }
}
#endif
