using UnityEngine;
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

    private string _naText = "Active Star drop: NA";
    private string _format = "Active Star drop: {0}";
    private string displayText = "Active Star drop: NA";

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
        _naText = Translate("Active Star drop: NA");
        _format = GetFormat(@"^Active Star drop: (.*)$", "Active Star drop: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull)
            displayText = string.Format(_format, FormatTime((int)GameData.BoardInstance.bigStarActiveCountDown));
        else
            displayText = _naText;

        AdjustWidthToText(displayText, 10f);
    }
}
