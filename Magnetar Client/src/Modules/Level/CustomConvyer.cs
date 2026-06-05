using Il2Cpp;
using HarmonyLib;
using static Magnetar_Client.Utils.Translator;

namespace Magnetar_Client.Modules
{
    public class CustomConveyor : Module
    {
        public override string Name { get; set; } = "Custom Conveyor";
        public override string Description { get; set; } = "Forces specific plants into the conveyor belt. If none are selected, injects every plant.";
        public override string SearchHints { get; set; } = "customconveyor conveyor belt customplants forceplants conveyorforce" +
            " plantconveyor selectconveyor editconveyor conveyorinjector injectplants conveyoroverride customconveyer" +
            " conveyerbelt plantinject injectevery plantspawn custombelt forceconveyer converyor conveyorcustom conveyerbelt" +
            " forceplant allplants conveyoritem spawnplant conveyorpool custompool conveyorlist";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        public static CustomConveyor instance;

        public MultiSelectSetting ConveyorPlantsSetting;

        public CustomConveyor()
        {
            instance = this;

            ConveyorPlantsSetting = new MultiSelectSetting("Allowed Plants", typeof(PlantType))
            {
                CustomNames = TranslatedNames(typeof(PlantType)),
                Blacklist = new System.Collections.Generic.HashSet<int>
                {
                    (int)PlantType.Nothing,
                    257,258,259,260,261,262,263,264,265,266,267,268,
                    246,247,
                }
            };
            AddSettings(ConveyorPlantsSetting);
        }

        public override void OnLanguageChanged()
        {
            ConveyorPlantsSetting.CustomNames = TranslatedNames(typeof(PlantType));
        }

        [HarmonyPatch(typeof(ConveyManager))]
        public static class ConveyBeltMgrPatch
        {
            [HarmonyPatch(nameof(ConveyManager.GetCardPool))]
            [HarmonyPostfix]
            public static void PostGetCardPool(ref Il2CppSystem.Collections.Generic.List<PlantType> __result)
            {
                if (instance == null || !instance.Active) return;

                var customList = new Il2CppSystem.Collections.Generic.List<PlantType>();

                foreach (int plantId in instance.ConveyorPlantsSetting.SelectedValues)
                {
                    customList.Add((PlantType)plantId);
                }

                if (customList.Count > 0)
                {
                    __result = customList;
                }

                else if (GameAPP.resourcesManager != null && GameAPP.resourcesManager.allPlants != null)
                {
                    __result = GameAPP.resourcesManager.allPlants;
                }
            }

            [HarmonyPatch(nameof(ConveyManager.Awake))]
            [HarmonyPostfix]
            public static void PostAwake(ConveyManager __instance)
            {
                if (instance == null || !instance.Active) return;

                var customList = new Il2CppSystem.Collections.Generic.List<PlantType>();
                foreach (int plantId in instance.ConveyorPlantsSetting.SelectedValues)
                {
                    customList.Add((PlantType)plantId);
                }

                if (customList.Count > 0)
                {
                    __instance.plants = customList;
                }
                else if (GameAPP.resourcesManager != null && GameAPP.resourcesManager.allPlants != null)
                {
                    __instance.plants = GameAPP.resourcesManager.allPlants;
                }
            }
        }
    }
}