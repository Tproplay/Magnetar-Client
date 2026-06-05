using Il2Cpp;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using UnityEngine;
using static Magnetar_Client.Game.GameData;
using static Magnetar_Client.Game.AppData;
using System.Linq;

namespace Magnetar_Client.Modules
{
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
        public override bool Active { get; set; } = false;
        public static float deltaTime = 0;

        public KillPlants()
        {
            instance = this;

            PlantsSelectedSetting = new MultiSelectSetting("Entities", typeof(PlantType))
            {
                MaxSelection = -1,
                Blacklist = new HashSet<int> {
                    (int)PlantType.Nothing,
                    257,258,259,260,261,262,263,264,265,266,267,268,
                    246,247,
                },
                CustomNames = TranslatedNames(typeof(PlantType))
            };

            PlantsSelectedSetting.Options.Keys.ToList().ForEach(PlantsSelectedSetting.Select);

            Settings.Add(PlantsSelectedSetting);

            AutoTurnOff = new BoolSetting("Auto Turn Off", TurnOffAfterUse);
            Settings.Add(AutoTurnOff);
        }

        public override void OnLanguageChanged()
        {
            PlantsSelectedSetting.CustomNames = TranslatedNames(typeof(PlantType));
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

            foreach (var plant in plantList)
            {
                if (PlantsSelectedSetting.IsSelected((int)plant.thePlantType))
                {
                    plant.Die(Plant.DieReason.BySelf);
                }
            }

        }

    }
}
