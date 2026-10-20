using static Magnetar_Client.UI.Themes.ThemeManager;
using UnityEngine;

namespace Magnetar_Client.HUDElements;

public class NumberOfZombiesSpawned : HudElement
{
    public NumberOfZombiesSpawned() : base("Zombies Spawned", HudElement.NewRect(100))
    { UpdateInterval = 0.2f; }

    string displayText = "Zombies Spawned: Na";

    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        if (GameData.BoardInstanceIsNull) return;

        displayText = $"Zombies Spawned: {GameData.BoardInstance.boardStatistics.zombiesKilled + GameData.ZombieList.Count}";

        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
