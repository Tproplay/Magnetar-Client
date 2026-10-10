using Magnetar_Client.Core;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;
using UnityEngine;

namespace Magnetar_Client.UI.Setting;

public abstract class Setting
{
    public string Name;
    public bool IsDisabled { get; set; }
    public virtual bool CanReset => true;

    public const string ResetSymbol = "R";

    public virtual void Reset() { }
   
    public abstract void Draw(ref float y, float width);

    public static bool DrawResetButton(Rect rect)
    {
        Event e = Event.current;
        bool isHovered = rect.Contains(e.mousePosition);

        GUI.Box(rect, ResetSymbol, ThemeManager.ResetButtonStyle);

        if (isHovered && e.type == EventType.MouseDown && e.button == 0)
        {
            e.Use();
            return true;
        }

        return false;
    }
}