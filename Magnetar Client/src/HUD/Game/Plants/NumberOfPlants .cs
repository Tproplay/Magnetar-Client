using UnityEngine;

using static Magnetar_Client.GameData;
using static Magnetar_Client.UI.Themes.ThemeManager;

namespace Magnetar_Client.HUDElements;

public class NumberOfPlants : HudElement
{
    public NumberOfPlants() : base("Total Plants", HudElement.NewRect(150))
    {
        UpdateInterval = 1f;
        RegisterTranslation("Total Plants: Na", "Total Plants: Na");
        RegisterRegexTranslation(@"^Total Plants: (.*)$", "Total Plants: {1}");
    }

    string displayText = "Total Plants: Na";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        displayText = Translate($"Total Plants: {PlantList.Count}");
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
