using UnityEngine;
using static Magnetar_Client.GameData;

namespace Magnetar_Client.HUDElements;

public class NumberOfPlants : HudElement
{
    public NumberOfPlants() : base("Total Plants", HudElement.NewRect(150))
    {
        UpdateInterval = 1f;
        RegisterTranslation("Total Plants: NA", "Total Plants: NA");
        RegisterRegexTranslation(@"^Total Plants: (.*)$", "Total Plants: {1}");
    }

    private string _naText = "Total Plants: NA";
    private string _format = "Total Plants: {0}";
    private string displayText = "Total Plants: NA";

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
        _naText = Translate("Total Plants: NA");
        _format = GetFormat(@"^Total Plants: (.*)$", "Total Plants: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (PlantList != null)
            displayText = string.Format(_format, PlantList.Count);
        else
            displayText = _naText;

        AdjustWidthToText(displayText, 10f);
    }
}
