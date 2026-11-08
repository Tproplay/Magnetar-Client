using UnityEngine;

namespace Magnetar_Client.HUDElements;

public class GameVersion : HudElement
{
    public GameVersion() : base("Game Name and Version", HudElement.NewRect(90))
    {
        RegisterRegexTranslation(@"^Plants Vs\. Zombies Fusion v(.*)$", "Plants Vs. Zombies Fusion v{1}");
    }

    private static string displayText;

    protected override void DrawContent(float width, float height)
    {
        Draw(displayText, width, height);
    }

    public override void OnEnable()
    {
        UpdateDisplay();
    }

    public override void OnLanguageChanged()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        string format = GetFormat(@"^Plants Vs\. Zombies Fusion v(.*)$", "Plants Vs. Zombies Fusion v{0}");
        displayText = string.Format(format, Application.version);
        AdjustWidthToText(displayText, 10f);
    }
}
