using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.GameData;
using UnityEngine;

namespace Magnetar_Client.HUDElements;

public class NumberOfZombiesKilled : HudElement
{
    public NumberOfZombiesKilled() : base("Zombies Killed", HudElement.NewRect(220))
    { UpdateInterval = 0.2f; }

    string displayText = "Zombies Killed: Na";

    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        if (BoardInstanceIsNull) return;

        displayText = $"Zombies Killed: {GameData.BoardInstance.boardStatistics.zombiesKilled}";

        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
