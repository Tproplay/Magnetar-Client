using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.GameData;
using UnityEngine;

namespace Magnetar_Client.HUDElements;

public class NumberOfZombiesKilled : HudElement
{
    public NumberOfZombiesKilled() : base("Zombies Killed", HudElement.NewRect(220))
    {
        UpdateInterval = 0.2f;
        RegisterTranslation("Zombies Killed: NA", "Zombies Killed: NA");
        RegisterRegexTranslation(@"^Zombies Killed: (.*)$", "Zombies Killed: {1}");
    }

    string displayText = "Zombies Killed: NA";

    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        if (BoardInstanceIsNull) return;

        displayText = Translate($"Zombies Killed: {GameData.BoardInstance.boardStatistics.zombiesKilled}");

        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
