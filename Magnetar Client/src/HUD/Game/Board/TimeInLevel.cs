using System;
using UnityEngine;

namespace Magnetar_Client.HUDElements;

public class TimeInLevel : HudElement
{
    public TimeInLevel() : base("Time In Current Level", HudElement.NewRect(180))
    {
        UpdateInterval = 0.1f;
        RegisterRegexTranslation(@"^Playing For: (.*)$", "Playing For: {1}");
    }

    private DateTime StartTime = DateTime.Now;
    private bool saved;
    private string _format = "Playing For: {0}";
    private string displayText = "Playing For: 00:00:00";

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (GameData.BoardInstanceIsNull) { saved = false; return; }

        if (!saved) { StartTime = DateTime.Now; saved = true; }
    }

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
        _format = GetFormat(@"^Playing For: (.*)$", "Playing For: {0}");
    }

    public override void OnUpdateActive()
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        displayText = string.Format(_format, (DateTime.Now - StartTime).ToString(@"hh\:mm\:ss"));
        AdjustWidthToText(displayText, 10f);
    }
}
