using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.GameData;
using static Magnetar_Client.Utils.Maths;
using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.HUDElements;

public class TotalHypnotizedZombieHealth : HudElement
{
    public TotalHypnotizedZombieHealth() : base("Hypnotized Zombies Health", HudElement.NewRect(235))
    {
        UpdateInterval = 0.2f;
        RegisterTranslation("Hypno Zombies Health: NA", "Hypno Zombies Health: NA");
        RegisterRegexTranslation(@"^Hypno Zombies Health: (.*)$", "Hypno Zombies Health: {1}");
    }

    string displayText = "Hypno Zombies Health: NA";

    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    double GetZombieHealth()
    {
        double health = 0;
        foreach (Zombie zombie in ZombieList)
        {
            if (!zombie.isMindControlled) continue;
            health += zombie.theHealth + zombie.theFirstArmorHealth + zombie.theSecondArmorHealth;

        }
        return health > 0 ? health : 0;
    }

    public override void OnUpdateActive()
    {
        if (GameData.BoardInstanceIsNull) return;
        displayText = Translate($"Hypno Zombies Health: {FormatInternational(GetZombieHealth())}");
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
