using UnityEngine;
using Magnetar_Client.UI.Themes;

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
        return GUI.Button(rect, ResetSymbol, Magnetar_Default.ResetButtonStyle);
    }
}