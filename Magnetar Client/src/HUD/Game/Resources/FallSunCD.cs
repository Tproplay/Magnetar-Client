using UnityEngine;

using static Magnetar_Client.UI.Themes.ThemeManager;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class FallSunCD : HudElement
{
    public FallSunCD() : base("Auto drop/fall sun timer", HudElement.NewRect(180))
    {
        UpdateInterval = 0.5f;
        RegisterTranslation("Auto Sun: NA", "Auto Sun: NA");
        RegisterTranslation("Auto Sun: 0s", "Auto Sun: 0s");
        RegisterRegexTranslation(@"^Auto Sun: (.*)$", "Auto Sun: {1}");
    }

    string displayText = "Auto Sun: NA";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        if (!GameData.BoardInstanceIsNull)
            displayText = Translate($"Auto Sun: {FormatTime((int)GameData.BoardInstance.theFallingSunCountDown)}");
        else
            displayText = Translate("Auto Sun: 0s");
        AdjustWidthToText(displayText, HUDElementStyle, 10);
    }
    public override void OnEnable()
    {
        displayText = Translate(displayText);
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}