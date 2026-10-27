using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.GameData;
using UnityEngine;
using System.Linq;

namespace Magnetar_Client.HUDElements;

public class NumberOfHypnotizedZombies : HudElement
{
    public NumberOfHypnotizedZombies() : base("Hypnotized Zombies On Lawn", HudElement.NewRect(100))
    {
        UpdateInterval = 0.2f;
        RegisterTranslation("Hypno Zombies On Lawn: NA", "Hypno Zombies On Lawn: NA");
        RegisterRegexTranslation(@"^Hypno Zombies On Lawn: (.*)$", "Hypno Zombies On Lawn: {1}");
    }

    string displayText = "Hypno Zombies On Lawn: NA";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        if (BoardInstanceIsNull) return;

        displayText = Translate($"Hypno Zombies On Lawn: {ZombieList.Count(z => z.isMindControlled)}");

        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
