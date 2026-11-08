using UnityEngine;

#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.HUDElements;

public class ClosestZombieThreat : HudElement
{
    public ClosestZombieThreat() : base("Closest Zombie Threat", HudElement.NewRect(210))
    {
        UpdateInterval = 0.2f;
        RegisterTranslation("Threat: NA", "Threat: NA");
        RegisterTranslation("Threat: Safe", "Threat: Safe");
        RegisterRegexTranslation(@"^Threat: (.*) tiles \(Row (.*)\)$", "Threat: {1} tiles (Row {2})");
    }

    private string _naText = "Threat: NA";
    private string _safeText = "Threat: Safe";
    private string _format = "Threat: {0} tiles (Row {1})";
    private string displayText = "Threat: NA";

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
        _naText = Translate("Threat: NA");
        _safeText = Translate("Threat: Safe");
        _format = GetFormat(@"^Threat: (.*) tiles \(Row (.*)\)$", "Threat: {0} tiles (Row {1})");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (GameData.BoardInstanceIsNull)
        {
            displayText = _naText;
            AdjustWidthToText(displayText, 10f);
            return;
        }

        var zombies = GameData.ZombieList;
        if (zombies == null || zombies.Count == 0)
        {
            displayText = $"<color=#5CFF5C>{_safeText}</color>";
            AdjustWidthToText(_safeText, 10f);
            return;
        }

        float tileWidth = 1.37f;
        float mowerX = -5.5f;

        if (Mouse.Instance != null)
        {
            float c0 = Mouse.Instance.GetBoxXFromColumn(0);
            float c1 = Mouse.Instance.GetBoxXFromColumn(1);
            if (c1 > c0)
            {
                tileWidth = c1 - c0;
                mowerX = c0 - (tileWidth * 0.75f);
            }
        }

        float minDistance = float.MaxValue;
        int threatRow = 1;
        bool foundThreat = false;

        for (int i = 0; i < zombies.Count; i++)
        {
            var z = zombies[i];
            if (z == null || z.gameObject == null || !z.gameObject.activeSelf) continue;
            if (z.isMindControlled || z.beforeDying || z.theHealth <= 0) continue;

            float zX = z.transform.position.x;
            float dist = (zX - mowerX) / tileWidth;
            if (dist < 0f) dist = 0f;

            if (dist < minDistance)
            {
                minDistance = dist;
                threatRow = z.theZombieRow + 1;
                foundThreat = true;
            }
        }

        if (!foundThreat)
        {
            displayText = $"<color=#5CFF5C>{_safeText}</color>";
            AdjustWidthToText(_safeText, 10f);
            return;
        }

        string rawText = string.Format(_format, minDistance.ToString("0.0"), threatRow);

        string colorHex;
        if (minDistance < 2.0f)
            colorHex = "#FF4D4D"; // Danger Red
        else if (minDistance < 4.0f)
            colorHex = "#FFD13B"; // Caution Yellow
        else
            colorHex = "#5CFF5C"; // Safe Green

        displayText = $"<color={colorHex}>{rawText}</color>";
        AdjustWidthToText(rawText, 10f);
    }
}
