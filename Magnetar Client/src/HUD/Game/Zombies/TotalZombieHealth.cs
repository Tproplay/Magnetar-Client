using UnityEngine;
using static Magnetar_Client.GameData;
using static Magnetar_Client.Utils.Maths;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.HUDElements;

public class TotalZombieHealth : HudElement
{
    public TotalZombieHealth() : base("Total Zombies Health", HudElement.NewRect(235))
    {
        UpdateInterval = 0.2f;
        RegisterTranslation("Total Zombies Health: NA", "Total Zombies Health: NA");
        RegisterRegexTranslation(@"^Total Zombies Health: (.*)$", "Total Zombies Health: {1}");
    }

    private string _naText = "Total Zombies Health: NA";
    private string _format = "Total Zombies Health: {0}";
    private string displayText = "Total Zombies Health: NA";

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
            if (zombie == null || zombie.isMindControlled) continue;
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
        _naText = Translate("Total Zombies Health: NA");
        _format = GetFormat(@"^Total Zombies Health: (.*)$", "Total Zombies Health: {0}");
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
