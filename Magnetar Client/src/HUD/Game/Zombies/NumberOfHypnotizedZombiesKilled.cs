using UnityEngine;

namespace Magnetar_Client.HUDElements;

public class NumberOfHypnotizedZombiesKilled : HudElement
{
    public NumberOfHypnotizedZombiesKilled() : base("Hypnotized Zombies Killed", HudElement.NewRect(220))
    {
        UpdateInterval = 0.2f;
        GameData.OnZombieDie += (zombie) => Hypno_Zombies_Killed += zombie.isMindControlled ? 1 : 0;
        RegisterTranslation("Hypno Zombies Killed: NA", "Hypno Zombies Killed: NA");
        RegisterRegexTranslation(@"^Hypno Zombies Killed: (.*)$", "Hypno Zombies Killed: {1}");
    }

    private int Hypno_Zombies_Killed;
    private string _naText = "Hypno Zombies Killed: NA";
    private string _format = "Hypno Zombies Killed: {0}";
    private string displayText = "Hypno Zombies Killed: NA";

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
        _naText = Translate("Hypno Zombies Killed: NA");
        _format = GetFormat(@"^Hypno Zombies Killed: (.*)$", "Hypno Zombies Killed: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull)
            displayText = string.Format(_format, Hypno_Zombies_Killed);
        else
            displayText = _naText;

        AdjustWidthToText(displayText, 10f);
    }
}
