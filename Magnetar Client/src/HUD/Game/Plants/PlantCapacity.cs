using UnityEngine;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class PlantCapacity : HudElement
{
    public PlantCapacity() : base("Plant Capacity", HudElement.NewRect(150))
    {
        UpdateInterval = 0.5f;
        RegisterTranslation("Plant Cap: NA", "Plant Cap: NA");
        RegisterRegexTranslation(@"^Plant Cap: (.*)/(.*)$", "Plant Cap: {1}/{2}");
        RegisterRegexTranslation(@"^Plant Cap: (.*)/∞$", "Plant Cap: {1}/∞");
    }

    private string _naText = "Plant Cap: NA";
    private string _formatLimited = "Plant Cap: {0}/{1}";
    private string _formatUnlimited = "Plant Cap: {0}/∞";
    private string displayText = "Plant Cap: NA";

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
        _naText = Translate("Plant Cap: NA");
        _formatLimited = GetFormat(@"^Plant Cap: (.*)/(.*)$", "Plant Cap: {0}/{1}");
        _formatUnlimited = GetFormat(@"^Plant Cap: (.*)/∞$", "Plant Cap: {0}/∞");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            int current = GameData.BoardInstance.theCurrentPlantCount;
            int limit = GameData.BoardInstance.theLimPlantCount;

            if (limit > 0)
            {
                displayText = string.Format(_formatLimited, FormatInternational(current), FormatInternational(limit));
            }
            else
            {
                displayText = string.Format(_formatUnlimited, FormatInternational(current));
            }
        }
        else
        {
            displayText = _naText;
        }

        AdjustWidthToText(displayText, 10f);
    }
}
