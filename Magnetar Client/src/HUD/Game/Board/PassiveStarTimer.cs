using UnityEngine;

using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class PassiveStarTimer : HudElement
{
    public PassiveStarTimer() : base("Passive Star drop Cooldown", HudElement.NewRect(180))
    { UpdateInterval = 0.1f; }

    string displayText = "Passive Star drop: NA";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }
    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
            displayText = $"Passive Star drop: {FormatTime((int)GameData.BoardInstance.bigStarPassiveCountDown)}";
        else
            displayText = "Passive Star drop: Na";
        AdjustWidthToText(displayText, HUDElementStyle, 10);
    }
    public override void OnEnable()
    {
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
