using UnityEngine;
using static Magnetar_Client.GameData;
using static Magnetar_Client.Utils.Maths;

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

    private string _naText = "Hypno Zombies Health: NA";
    private string _format = "Hypno Zombies Health: {0}";
    private string displayText = "Hypno Zombies Health: NA";

    protected override void DrawContent(float width, float height)
    {
        Draw(displayText, width, height);
    }

    private double GetZombieHealth()
    {
        if (ZombieList == null) return 0;

        double health = 0;
        foreach (Zombie zombie in ZombieList)
        {
            if (zombie == null || !zombie.isMindControlled) continue;
            health += zombie.theHealth + zombie.theFirstArmorHealth + zombie.theSecondArmorHealth;
        }
        return health > 0 ? health : 0;
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
        _naText = Translate("Hypno Zombies Health: NA");
        _format = GetFormat(@"^Hypno Zombies Health: (.*)$", "Hypno Zombies Health: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull)
            displayText = string.Format(_format, FormatInternational(GetZombieHealth()));
        else
            displayText = _naText;

        AdjustWidthToText(displayText, 10f);
    }
}
