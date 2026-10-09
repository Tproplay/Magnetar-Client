#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#elif BEPINEX || RELEASE_BEPINEX
#endif
using Magnetar_Client.UI.Setting;
using UnityEngine;
using static Magnetar_Client.Game.AppData;
using static Magnetar_Client.Game.GameData;

namespace Magnetar_Client.Modules;

public class KillPlants : Module
{
    // Mod Info
    public override string Name { get; set; } = "Kill All Plants";
    public override string Description { get; set; } = "Kills the selected plant(s) while the module is active.";
    public override string SearchHints { get; set; } = "killallplants killplants plantkiller removeplants " +
        "deleteplants plantremoval plantslayer destroyplants exterminateplants plantexterminator plantclear " +
        "clearplants wipeplants plantwipe plantdeath deathplants killallplant killalplants kilallplants " +
        "killallplantes killallplantts plantdestructor plantdestroyer plantdeleter plantsmasher plantpurger " +
        "plantexecutioner plantelimination plantterminator plantender planteraser plantvanisher";

    public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

    // Mod Data

    public static KillPlants instance;

    public MultiSelectSetting PlantsSelectedSetting;

    public bool TurnOffAfterUse = true;
    public BoolSetting AutoTurnOff;
    public override bool Active { get; set; }
    public static float deltaTime;

    public KillPlants()
    {
        instance = this;

        CreateCategory("General");

        PlantsSelectedSetting = new MultiSelectSetting("Entities", typeof(PlantType))
        {
            Blacklist = Banned.PlantTypeBanned,
            CustomNames = TranslateEnum(typeof(PlantType))
        };

        PlantsSelectedSetting.SelectAll(setDefault: true);

        AddSettings(PlantsSelectedSetting);

        EndCategory();
        CreateCategory("Extra");

        AutoTurnOff = new BoolSetting("Auto Turn Off", TurnOffAfterUse);
        AddSettings(AutoTurnOff);

        EndCategory();

    }

    public override void OnLanguageChanged()
    {
        PlantsSelectedSetting.CustomNames = TranslateEnum(typeof(PlantType));
    }

    // Mod Logic
    public override void OnUpdateActive()
    {
        // Handle auto turn off
        if (AutoTurnOff.Value)
        {
            deltaTime += Time.deltaTime;
            if (deltaTime > 0.3f)
            {
                Active = false;
                deltaTime = 0;
            }
        }

        if (BoardInstanceIsNull) return;

        for (int i = PlantList.Count - 1; i >= 0; i--)
        {
            Plant plant = PlantList[i];
            if (plant != null)
            {
                plant.Die(Plant.DieReason.BySelf);
            }
        }
    }
}