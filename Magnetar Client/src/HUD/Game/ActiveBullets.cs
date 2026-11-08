using UnityEngine;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class ActiveBullets : HudElement
{
    public ActiveBullets() : base("Active Bullets", HudElement.NewRect(130))
    {
        UpdateInterval = 0.2f;
        RegisterTranslation("Active Bullets: NA", "Active Bullets: NA");
        RegisterRegexTranslation(@"^Active Bullets: (.*)$", "Active Bullets: {1}");
    }

    private string _naText = "Active Bullets: NA";
    private string _format = "Active Bullets: {0}";
    private string displayText = "Active Bullets: NA";

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
        _naText = Translate("Active Bullets: NA");
        _format = GetFormat(@"^Active Bullets: (.*)$", "Active Bullets: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            int activeBullets = GameData.BoardInstance.currentBulletNum;
            if (activeBullets < 0) activeBullets = 0;
            displayText = string.Format(_format, FormatInternational(activeBullets));
        }
        else
        {
            displayText = _naText;
        }

        AdjustWidthToText(displayText, 10f);
    }
}
