using Magnetar_Client.UI.Themes;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

public abstract class Setting
{
    public string Name;
    public bool IsDisabled { get; set; } = false;
    public virtual bool CanReset => true;

    public const string ResetSymbol = "R";

    public virtual void Reset() { }

    public abstract void Draw(ref float y, float width);

    public static bool DrawResetButton(Rect rect)
    {
        Event e = Event.current;
        bool isHovered = rect.Contains(e.mousePosition);

        // Draw visuals without standard GUI.Button state capture
        GUI.Box(rect, ResetSymbol, ThemeManager.ResetButtonStyle);

        // Manual click detection
        if (isHovered && e.type == EventType.MouseDown && e.button == 0)
        {
            e.Use();
            return true;
        }

        return false;
    }
}