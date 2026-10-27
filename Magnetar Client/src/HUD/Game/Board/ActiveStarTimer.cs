using UnityEngine;

using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class ActiveStarTimer : HudElement
{
    public ActiveStarTimer() : base("Active Star drop Cooldown", HudElement.NewRect(180))
    {
        UpdateInterval = 0.1f;
        RegisterTranslation("Active Star drop: NA", "Active Star drop: NA");
        RegisterRegexTranslation(@"^Active Star drop: (.*)$", "Active Star drop: {1}");
    }

    string displayText = "Active Star drop: NA";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }
    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
            displayText = Translate($"Active Star drop: {FormatTime((int)GameData.BoardInstance.bigStarActiveCountDown)}");
        else
            displayText = Translate("Active Star drop: NA");
        AdjustWidthToText(displayText, HUDElementStyle, 10);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
