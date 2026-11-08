using System.Linq;
using UnityEngine;
using static Magnetar_Client.GameData;

namespace Magnetar_Client.HUDElements;

public class NumberOfZombies : HudElement
{
    public NumberOfZombies() : base("Zombies On Lawn", HudElement.NewRect(100))
    {
        UpdateInterval = 0.2f;
        RegisterTranslation("Zombies On Lawn: NA", "Zombies On Lawn: NA");
        RegisterRegexTranslation(@"^Zombies On Lawn: (.*)$", "Zombies On Lawn: {1}");
    }

    private string _naText = "Zombies On Lawn: NA";
    private string _format = "Zombies On Lawn: {0}";
    private string displayText = "Zombies On Lawn: NA";

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
        _naText = Translate("Zombies On Lawn: NA");
        _format = GetFormat(@"^Zombies On Lawn: (.*)$", "Zombies On Lawn: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!BoardInstanceIsNull && ZombieList != null)
            displayText = string.Format(_format, ZombieList.Count(z => z != null && !z.isMindControlled));
        else
            displayText = _naText;

        AdjustWidthToText(displayText, 10f);
    }
}
