using static Magnetar_Client.Game.AppData;
using System.Linq;

using Magnetar_Client.UI.Setting;
#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class ChangeMap : Module
{
    public override string Name { get; set; } = "Change Map";
    public override string Description { get; set; } = "Allows you to change the background of the level.\n" +
        "This only changes the background map and won't change lanes.";
    public override string SearchHints { get; set; } = "changemap mapchanger levelbackground backgroundchanger" +
        " mapselector levelmap backgroundswitcher mapswap custommap backgroundmod mapmod stagechanger leveltheme" +
        " backgroundpicker mapoverride stagetheme levelchanger mapeditor environmentchanger backgroundmanager";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

    public static ChangeMap instance;

    public MultiSelectSetting MapSetting;
    public ButtonSetting ChangeMapButton;

    public ChangeMap()
    {
        instance = this;

        CreateCategory("General");

        MapSetting = new MultiSelectSetting("Map", typeof(SceneType))
        {
            MaxSelection = 1,
            CustomNames = TranslatedNames(typeof(SceneType)),
        };

        ChangeMapButton = new ButtonSetting("Change Map Now", ChangeMapNow);

        AddSettings(MapSetting, ChangeMapButton);
        EndCategory();
    }

    public override void OnLanguageChanged()
    {
        MapSetting.CustomNames = TranslatedNames(typeof(SceneType));
    }

    public void ChangeMapNow()
    {
        if (!Active) return;
        if (BoardInstanceIsNull) return;

        if (MapSetting.SelectedValues.Count != 1) return;

        int map = MapSetting.SelectedValues.First();

        BoardInstance.StartCoroutine(
            BoardInstance.SmoothlyChangeMap((SceneType)map)
            );

    }
}