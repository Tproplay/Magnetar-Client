using UnityEngine;

namespace Magnetar_Client.HUDElements;

public class FreezeTimer : HudElement
{
    public FreezeTimer() : base("Freeze Timer", HudElement.NewRect(130))
    {
        UpdateInterval = 0.1f;
        RegisterTranslation("Freeze: NA", "Freeze: NA");
        RegisterTranslation("Freeze: 0.0s", "Freeze: 0.0s");
        RegisterRegexTranslation(@"^Freeze: (.*)s$", "Freeze: {1}s");
    }

    private string _naText = "Freeze: NA";
    private string _zeroText = "Freeze: 0.0s";
    private string _format = "Freeze: {0}s";
    private string displayText = "Freeze: NA";

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
        _naText = Translate("Freeze: NA");
        _zeroText = Translate("Freeze: 0.0s");
        _format = GetFormat(@"^Freeze: (.*)s$", "Freeze: {0}s");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            float freezeTime = GameData.BoardInstance.iceDoomFreezeTime;
            if (freezeTime > 0.05f)
            {
                displayText = string.Format(_format, freezeTime.ToString("0.0"));
            }
            else
            {
                displayText = _zeroText;
            }
        }
        else
        {
            displayText = _naText;
        }

        AdjustWidthToText(displayText, 10f);
    }
}
