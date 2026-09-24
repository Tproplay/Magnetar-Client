using Magnetar_Client.UI.WindowDrawing;
using Magnetar_Client.Utils;

namespace Magnetar_Client.UI.Setting;

public class CategorySetting : Setting
{
    public bool IsExpanded;
    public bool DefaultExpanded;

    public override bool CanReset => true;

    public CategorySetting(string name, bool defaultExpanded = true)
    {
        Name = name;
        DefaultExpanded = defaultExpanded;
        IsExpanded = defaultExpanded;
    }

    public override void Reset()
    {
        IsExpanded = DefaultExpanded;
    }

    public override void Draw(ref float y, float width)
    {
        IsExpanded = MiscDrawing.Seperator(
            ref y,
            width,
            Config.indent,
            Config.spacing,
            Translator.Translate(Name),
            true,
            IsExpanded
        );
    }
}

public class EndCategorySetting : Setting
{
    public override bool CanReset => false;
    public override void Draw(ref float y, float width) { }
}