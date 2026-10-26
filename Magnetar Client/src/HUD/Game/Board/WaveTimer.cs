using UnityEngine;

using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class WaveTimer : HudElement
{
    public WaveTimer() : base("Next wave arrival time", HudElement.NewRect(180))
    {
        UpdateInterval = 0.1f;
        RegisterTranslation("Next Wave arrival: Na", "Next Wave arrival: Na");
        RegisterRegexTranslation(@"^Next Wave arrival: (.*)$", "Next Wave arrival: {1}");
    }

    string displayText = "Next Wave arrival: Na";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }
    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
            displayText = Translate($"Next Wave arrival: {FormatTime((int)GameData.BoardInstance.timeUntilNextWave >= 0 ? (int)GameData.BoardInstance.timeUntilNextWave : 0)}");
        else
            displayText = Translate("Next Wave arrival: Na");
        AdjustWidthToText(displayText, HUDElementStyle, 10);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
