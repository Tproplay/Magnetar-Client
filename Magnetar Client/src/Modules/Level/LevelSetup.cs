

using Magnetar_Client.UI.Setting;


#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules;

public class LevelSetup : Module
{
    public override string Name { get; set; } = "Level Setup";
    public override string Description { get; set; } = "Allows you to manually trigger board's setup functions.";
    public override string SearchHints { get; set; } = "levelsetup boardsetup setupfunctions triggerboardsetup " +
        "setupmanager levelinit boardinit initializetrigger levelinitializer manualsetup setupcustomizer " +
        "boardconfigurator levelsync setupoverride boardhandler initializegame levelconfig startuplevel " +
        "setupdebug boardreset levelreset levelbuilder initboard boardloader customsetup runsetup";
    public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

    public static LevelSetup instance;
    public ButtonSetting SetGraves;
    public ButtonSetting SetFreezedPlants;
    public ButtonSetting SpawnGraveZombies;

    public LevelSetup()
    {
        instance = this;

        CreateCategory("General");

        SetGraves = new ButtonSetting("Set Graves", SetGravesNow);
        SetFreezedPlants = new ButtonSetting("Set Freezed Plants", SetFreezedPlantsNow);
        
        AddSettings(SetGraves, SetFreezedPlants);
        EndCategory();
        CreateCategory("Extra");

        SpawnGraveZombies = new ButtonSetting("Spawn Grave Zombies", SpawnGraveZombiesNow);

        AddSettings(SpawnGraveZombies);
        EndCategory();
    }

    public void SetGravesNow()
    {
        if (!Active) return;
        if (GameData.BoardInstanceIsNull) return;
        GameData.
                BoardInstance.SetGrave();
    }
    public void SpawnGraveZombiesNow()
    {
        if (!Active) return;
        if (GameData.BoardInstanceIsNull) return;

        foreach (var item in GameData.BoardInstance.griditemArray)
        {
            if (item is Grave grave)
            {
                grave.SetZombie();
            }
        }
    }

    public void SetFreezedPlantsNow()
    {
        if (!Active) return;
        if (GameData.BoardInstanceIsNull) return;
        GameData.
                BoardInstance.SetFreezedPlant();

    }
}