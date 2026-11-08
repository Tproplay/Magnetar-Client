using UnityEngine;

namespace Magnetar_Client.HUDElements;

public class CurrentWave : HudElement
{
    public CurrentWave() : base("Current Wave", HudElement.NewRect(235))
    {
        UpdateInterval = 1f;
        RegisterTranslation("Wave: NA", "Wave: NA");
        RegisterRegexTranslation(@"^Wave: (\d+)/(\d+)$", "Wave: {1}/{2}");
    }

    private string _naText = "Wave: NA";
    private string _format = "Wave: {0}/{1}";
    private string displayText = "Wave: NA";

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
        _naText = Translate("Wave: NA");
        _format = GetFormat(@"^Wave: (\d+)/(\d+)$", "Wave: {0}/{1}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            displayText = string.Format(_format, GameData.BoardInstance.theWave, GameData.BoardInstance.theMaxWave);
        }
        else
        {
            displayText = _naText;
        }

        AdjustWidthToText(displayText, 10f);
    }
}
