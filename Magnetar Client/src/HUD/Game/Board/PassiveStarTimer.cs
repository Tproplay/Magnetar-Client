using UnityEngine;

using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class PassiveStarTimer : HudElement
{
    public PassiveStarTimer() : base("Passive Star drop Cooldown", HudElement.NewRect(180))
    {
        UpdateInterval = 0.1f;
        RegisterTranslation("Passive Star drop: NA", "Passive Star drop: NA");
        RegisterTranslation("Passive Star drop: Na", "Passive Star drop: Na");
        RegisterRegexTranslation(@"^Passive Star drop: (.*)$", "Passive Star drop: {1}");
    }

    string displayText = "Passive Star drop: NA";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }
    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
            displayText = Translate($"Passive Star drop: {FormatTime((int)GameData.BoardInstance.bigStarPassiveCountDown)}");
        else
            displayText = Translate("Passive Star drop: Na");
        AdjustWidthToText(displayText, HUDElementStyle, 10);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
