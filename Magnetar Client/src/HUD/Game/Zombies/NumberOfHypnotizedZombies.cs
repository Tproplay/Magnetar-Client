using System.Linq;
using UnityEngine;
using static Magnetar_Client.GameData;

namespace Magnetar_Client.HUDElements;

public class NumberOfHypnotizedZombies : HudElement
{
    public NumberOfHypnotizedZombies() : base("Hypnotized Zombies On Lawn", HudElement.NewRect(100))
    {
        UpdateInterval = 0.2f;
        RegisterTranslation("Hypno Zombies On Lawn: NA", "Hypno Zombies On Lawn: NA");
        RegisterRegexTranslation(@"^Hypno Zombies On Lawn: (.*)$", "Hypno Zombies On Lawn: {1}");
    }

    private string _naText = "Hypno Zombies On Lawn: NA";
    private string _format = "Hypno Zombies On Lawn: {0}";
    private string displayText = "Hypno Zombies On Lawn: NA";

    protected override void DrawContent(float width, float height)
    {
        Draw(displayText, width, height);
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
        _naText = Translate("Hypno Zombies On Lawn: NA");
        _format = GetFormat(@"^Hypno Zombies On Lawn: (.*)$", "Hypno Zombies On Lawn: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!BoardInstanceIsNull && ZombieList != null)
            displayText = string.Format(_format, ZombieList.Count(z => z != null && z.isMindControlled));
        else
            displayText = _naText;

        AdjustWidthToText(displayText, 10f);
    }
}
