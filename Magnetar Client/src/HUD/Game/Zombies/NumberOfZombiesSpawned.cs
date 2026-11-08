using UnityEngine;

namespace Magnetar_Client.HUDElements;

public class NumberOfZombiesSpawned : HudElement
{
    public NumberOfZombiesSpawned() : base("Zombies Spawned", HudElement.NewRect(100))
    {
        UpdateInterval = 0.2f;
        RegisterTranslation("Zombies Spawned: NA", "Zombies Spawned: NA");
        RegisterRegexTranslation(@"^Zombies Spawned: (.*)$", "Zombies Spawned: {1}");
    }

    private string _naText = "Zombies Spawned: NA";
    private string _format = "Zombies Spawned: {0}";
    private string displayText = "Zombies Spawned: NA";

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
        _naText = Translate("Zombies Spawned: NA");
        _format = GetFormat(@"^Zombies Spawned: (.*)$", "Zombies Spawned: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull && GameData.ZombieList != null)
        {
            int total = GameData.BoardInstance.boardStatistics.zombiesKilled + GameData.ZombieList.Count;
            displayText = string.Format(_format, total);
        }
        else
        {
            displayText = _naText;
        }

        AdjustWidthToText(displayText, 10f);
    }
}
