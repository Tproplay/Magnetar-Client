using UnityEngine;
using static Magnetar_Client.GameData;

namespace Magnetar_Client.HUDElements;

public class NumberOfZombiesKilled : HudElement
{
    public NumberOfZombiesKilled() : base("Zombies Killed", HudElement.NewRect(220))
    {
        UpdateInterval = 0.2f;
        RegisterTranslation("Zombies Killed: NA", "Zombies Killed: NA");
        RegisterRegexTranslation(@"^Zombies Killed: (.*)$", "Zombies Killed: {1}");
    }

    private string _naText = "Zombies Killed: NA";
    private string _format = "Zombies Killed: {0}";
    private string displayText = "Zombies Killed: NA";

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
        _naText = Translate("Zombies Killed: NA");
        _format = GetFormat(@"^Zombies Killed: (.*)$", "Zombies Killed: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!BoardInstanceIsNull)
            displayText = string.Format(_format, GameData.BoardInstance.boardStatistics.zombiesKilled);
        else
            displayText = _naText;

        AdjustWidthToText(displayText, 10f);
    }
}
