using Il2Cpp;
using Magnetar_Client.Game;
using Magnetar_Client.Utils;
using System.Collections.Generic;


namespace Magnetar_Client.Modules
{
    public class KeepShooting : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Keep Shooting";
        public override string Description { get; set; } = "Makes Plants fire continuously.";
        public override string SearchHints { get; set; } = "keepshooting continuousfire autofire rapidfire" +
            " nonstopshooting endlessfire alwaysshooting holdfire automaticshooting shootingloop firingloop " +
            "fireloop keepfire shootingbot autoshot keepshotting keepshoting continousfire continuousshoot " +
            "nonstopfire rapidshooting firespam shootspam infinitieshooting infiniteshoot burstfire firingrate" +
            " firespeed shootalways perpetualfire constantfire";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Plant;

        // Mod Data

        public static KeepShooting instance;

        public MultiSelectSetting PlantsSelectedSetting;
        public override bool Active { get; set; } = false;
        private Dictionary<int, string> plantNameOverriden = new Dictionary<int, string>();

        public KeepShooting()
        {
            instance = this;

            plantNameOverriden = Translator.TranslateEnum(typeof(PlantType));

            foreach (var plant in plantNameOverriden)
            {
                plantNameOverriden[plant.Key] = $"{plant.Value} ({plant.Key})";
            }
            PlantsSelectedSetting = new MultiSelectSetting("Entities", typeof(PlantType))
            {
                MaxSelection = -1,
                Blacklist = new HashSet<int> {
                    (int)PlantType.Nothing,
                    257,258,259,260,261,262,263,264,265,266,267,268,
                    246,247,
                },
                CustomNames = plantNameOverriden
            };

            Settings.Add(PlantsSelectedSetting);
            PlantsSelectedSetting.SelectedValues.UnionWith(plantNameOverriden.Keys);
        }

        // Mod Logic

        private Dictionary<Plant, bool> originalValue = new Dictionary<Plant, bool>();
        public override void OnUpdateActive()
        {
            foreach(Plant plant in GameData.plantList)
            {
                if (!originalValue.ContainsKey(plant))
                {
                    originalValue[plant] = plant.keepShooting;
                    plant.keepShooting = true;
                }
            }
        }

        public override void OnDisable()
        {
            foreach (Plant plant in GameData.plantList)
            {
                if (originalValue.ContainsKey(plant))
                {
                    plant.keepShooting = originalValue[plant];
                }
            }

            originalValue.Clear();

        }
    }
}
