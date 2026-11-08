using UnityEngine;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class PassiveStarTimer : HudElement
{
    public PassiveStarTimer() : base("Passive Star drop Cooldown", HudElement.NewRect(180))
    {
        UpdateInterval = 0.1f;
        RegisterTranslation("Passive Star drop: NA", "Passive Star drop: NA");
        RegisterRegexTranslation(@"^Passive Star drop: (.*)$", "Passive Star drop: {1}");
    }

    private string _naText = "Passive Star drop: NA";
    private string _format = "Passive Star drop: {0}";
    private string displayText = "Passive Star drop: NA";

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
        _naText = Translate("Passive Star drop: NA");
        _format = GetFormat(@"^Passive Star drop: (.*)$", "Passive Star drop: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull)
            displayText = string.Format(_format, FormatTime((int)GameData.BoardInstance.bigStarPassiveCountDown));
        else
            displayText = _naText;

        AdjustWidthToText(displayText, 10f);
    }
}
