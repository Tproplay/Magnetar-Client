using HarmonyLib;
using Il2Cpp;
using Magnetar_Client.Utils;
using System.Collections.Generic;
using System.Linq;
using static Magnetar_Client.Game.AppData;

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

        public KeepShooting()
        {
            instance = this;

            CreateCategory("General");

            PlantsSelectedSetting = new MultiSelectSetting("Entities", typeof(PlantType))
            {
                MaxSelection = -1,
                Blacklist = new HashSet<int> {
                    (int)PlantType.Nothing,
                    257,258,259,260,261,262,263,264,265,266,267,268,
                    246,247,3000,
                },
                CustomNames = TranslatedNames(typeof(PlantType))
            };

            PlantsSelectedSetting.Options.Keys.ToList().ForEach(PlantsSelectedSetting.Select);

            Settings.Add(PlantsSelectedSetting);

            EndCategory();
        }

        public override void OnLanguageChanged()
        {
            PlantsSelectedSetting.CustomNames = TranslatedNames(typeof(PlantType));
        }

        // Mod Logic


        [HarmonyPatch(typeof(Plant))]
        public static class PlantShootablePatch
        {
            [HarmonyPatch(nameof(Plant.Shootable))]
            [HarmonyPostfix]
            public static void ShootablePostfix(Plant __instance, ref bool __result)
            {
                if (instance == null || BoardInstanceIsNull) return;
                if (!instance.Active) return;

                if (instance.PlantsSelectedSetting.IsSelected((int)__instance.thePlantType))
                    __result = true;
            }
        }
    }
}
