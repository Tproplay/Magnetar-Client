using static Magnetar_Client.UI.Themes.ThemeManager;
using UnityEngine;

namespace Magnetar_Client.HUDElements;

public class CurrentWave : HudElement
{
    public CurrentWave() : base("Current Wave", HudElement.NewRect(235))
    {
        UpdateInterval = 1f;
        RegisterTranslation("Wave: Na", "Wave: Na");
        RegisterRegexTranslation(@"^Wave: (\d+)/(\d+)$", "Wave: {1}/{2}");
    }

    string displayText = "Wave: Na";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            displayText = Translate($"Wave: {GameData.BoardInstance.theWave}/{GameData.BoardInstance.theMaxWave}");
        }
        else
        {
            displayText = Translate("Wave: Na");
        }

        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }

    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }

}
