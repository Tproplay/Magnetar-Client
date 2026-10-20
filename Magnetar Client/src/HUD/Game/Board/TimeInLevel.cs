using System;
using UnityEngine;

using static Magnetar_Client.UI.Themes.ThemeManager;

namespace Magnetar_Client.HUDElements;

public class TimeInLevel : HudElement
{
    public TimeInLevel() : base("Time In Current Level", HudElement.NewRect(180))
    { UpdateInterval = 0.1f; }

    DateTime StartTime = DateTime.Now;
    bool saved;
    public override void OnUpdate()
    {
        base.OnUpdate();
        if (GameData.BoardInstanceIsNull) { saved= false; return; }

        if (!saved) { StartTime = DateTime.Now; saved = true; }
    }

    string displayText = $"Playing For: {(DateTime.Now-DateTime.Now):hh\\:mm\\:ss}";
    protected override void DrawContent(float width, float height)
    {
        GUI.Label(new Rect(5, 4, width - 10, height - 10), displayText, HUDElementStyle);
    }

    public override void OnUpdateActive()
    {
        displayText = $"Playing For: {(DateTime.Now - StartTime):hh\\:mm\\:ss}";
        AdjustWidthToText(displayText, HUDElementStyle, 10);
    }
    public override void OnEnable()
    {
        AdjustWidthToText(displayText, HUDElementStyle, 10f);
    }
}
