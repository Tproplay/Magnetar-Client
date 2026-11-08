using UnityEngine;
using static Magnetar_Client.Utils.Maths;

namespace Magnetar_Client.HUDElements;

public class WaveTimer : HudElement
{
    public WaveTimer() : base("Next wave arrival time", HudElement.NewRect(180))
    {
        UpdateInterval = 0.1f;
        RegisterTranslation("Next Wave arrival: NA", "Next Wave arrival: NA");
        RegisterRegexTranslation(@"^Next Wave arrival: (.*)$", "Next Wave arrival: {1}");
    }

    private string _naText = "Next Wave arrival: NA";
    private string _format = "Next Wave arrival: {0}";
    private string displayText = "Next Wave arrival: NA";

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
        _naText = Translate("Next Wave arrival: NA");
        _format = GetFormat(@"^Next Wave arrival: (.*)$", "Next Wave arrival: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            int time = (int)GameData.BoardInstance.timeUntilNextWave;
            displayText = string.Format(_format, FormatTime(time >= 0 ? time : 0));
        }
        else
        {
            displayText = _naText;
        }

        AdjustWidthToText(displayText, 10f);
    }
}
