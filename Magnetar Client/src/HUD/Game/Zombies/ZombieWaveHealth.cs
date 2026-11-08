using UnityEngine;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class ZombieWaveHealth : HudElement
{
    public ZombieWaveHealth() : base("Current Zombie Wave Health", HudElement.NewRect(235))
    {
        UpdateInterval = 0.2f;
        RegisterTranslation("Wave Health: NA", "Wave Health: NA");
        RegisterRegexTranslation(@"^Wave Health: (.*)/(.*)$", "Wave Health: {1}/{2}");
    }

    private string _naText = "Wave Health: NA";
    private string _format = "Wave Health: {0}/{1}";
    private string displayText = "Wave Health: NA";

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
        _naText = Translate("Wave Health: NA");
        _format = GetFormat(@"^Wave Health: (.*)/(.*)$", "Wave Health: {0}/{1}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            displayText = string.Format(
                _format,
                FormatInternational(GameData.BoardInstance.zombieCurrentWaveHealth),
                FormatInternational(GameData.BoardInstance.zombieSpawnHealth));
        }
        else
        {
            displayText = _naText;
        }

        AdjustWidthToText(displayText, 10f);
    }
}
