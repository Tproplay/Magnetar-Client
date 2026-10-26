using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.GameData;
using UnityEngine;
using System.Linq;

namespace Magnetar_Client.HUDElements;

public class NumberOfZombies : HudElement
{
    public NumberOfZombies() : base("Zombies On Lawn", HudElement.NewRect(100))
    {
        UpdateInterval = 0.2f;
        RegisterTranslation("Zombies On Lawn: Na", "Zombies On Lawn: Na");
        RegisterRegexTranslation(@"^Zombies On Lawn: (.*)$", "Zombies On Lawn: {1}");
    }
    string displayText = "Zombies On Lawn: Na";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        if (BoardInstanceIsNull) return;

        displayText = Translate($"Zombies On Lawn: {ZombieList.Count(z => !z.isMindControlled)}");

        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
