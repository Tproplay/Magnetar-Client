using UnityEngine;
using Magnetar_Client.UI.Themes;
using Magnetar_Client.Utils;

namespace Magnetar_Client.UI.Setting;

public class LabelSetting : Setting
{
    public string Text;
    public override bool CanReset => false;

    public LabelSetting(string text) { Name = text; Text = text; }
    public LabelSetting(string name, string text) { Name = name; Text = text; }

    public override void Draw(ref float y, float width)
    {
        string displayText = Translator.Translate(!string.IsNullOrEmpty(Text) ? Text : Name);
        Rect labelRect = new(Config.indent, y, width - (Config.indent * 2), Config.elementHeight);
        GUI.Label(labelRect, displayText, Magnetar_Default.SettingLabelStyle);
        y += Config.elementHeight + Config.spacing;
    }
}