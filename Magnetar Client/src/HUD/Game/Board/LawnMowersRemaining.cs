using UnityEngine;
using static Magnetar_Client.Utils.Maths;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.HUDElements;

public class LawnMowersRemaining : HudElement
{
    public LawnMowersRemaining() : base("Lawn Mowers Remaining", HudElement.NewRect(140))
    {
        UpdateInterval = 0.5f;
        RegisterTranslation("Mowers: NA", "Mowers: NA");
        RegisterRegexTranslation(@"^Mowers: (.*)/(.*)$", "Mowers: {1}/{2}");
    }

    private string _naText = "Mowers: NA";
    private string _format = "Mowers: {0}/{1}";
    private string displayText = "Mowers: NA";

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
        _naText = Translate("Mowers: NA");
        _format = GetFormat(@"^Mowers: (.*)/(.*)$", "Mowers: {0}/{1}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!GameData.BoardInstanceIsNull)
        {
            int total = GameData.BoardInstance.rowNum;
            int active = 0;
            var mowers = GameData.BoardInstance.mowerArray;

            if (mowers != null)
            {
                if (mowers.Count > total) total = mowers.Count;
                for (int i = 0; i < mowers.Count; i++)
                {
                    var m = mowers[i];
                    if (m != null && !m.started && m.gameObject != null && m.gameObject.activeSelf)
                    {
                        active++;
                    }
                }
            }

            displayText = string.Format(_format, active, total);
        }
        else
        {
            displayText = _naText;
        }

        AdjustWidthToText(displayText, 10f);
    }
}
