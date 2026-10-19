using static Magnetar_Client.UI.Themes.ThemeManager;
using UnityEngine;

namespace Magnetar_Client.HUDElements;

public class NumberOfHypnotizedZombiesKilled : HudElement
{
    public NumberOfHypnotizedZombiesKilled() : base("Hypnotized Zombies Killed", HudElement.NewRect(220))
    { 
        UpdateInterval = 0.2f;
        GameData.OnZombieDie += (zombie) => Hypno_Zombies_Killed += zombie.isMindControlled ? 1 : 0;
    }
    int Hypno_Zombies_Killed;
    string displayText = "Hypno Zombies Killed: Na";

    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        if (GameData.BoardInstanceIsNull) return;

        displayText = $"Hypno Zombies Killed: {Hypno_Zombies_Killed}";

        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
