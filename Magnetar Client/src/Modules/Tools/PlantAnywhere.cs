using Il2Cpp;
using HarmonyLib;

namespace Magnetar_Client.Modules
{
    public class PlantAnywhere : Module
    {
        // Mod Info
        public override string Name { get; set; } = "Plant Anywhere";
        public override string Description { get; set; } = "Allows you to place plants in illegal Positions.";
        public override string SearchHints { get; set; } = "plantanywhere placeanywhere illegalplacement ignoreposition " +
            "anywheremode freeplant plantcheat placeeverywhere plantfreedom freeplacement placementfix ignoregrid " +
            "plantlimit overridegrid nodistriction placeanygrid anygrid plantgrid ignoreplacerules freebuild plantany " +
            "unrestrictedplant placeanywherecheat plantanywherehack placementcheat bypassgrid placeanywhereon gridfix";

        public override ModuleCategory Category { get; set; } = ModuleCategory.Tools;

        // Mod Data

        public static PlantAnywhere instance;

        public PlantAnywhere()
        {
            instance = this;
        }

        [HarmonyPatch(typeof(CreatePlant))]
        public static class CreatePlantPatch
        {
            [HarmonyPatch(nameof(CreatePlant.CheckBox))]
            [HarmonyPrefix]
            public static bool CheckBoxPrefix(ref bool __result)
            {
                if (instance != null && instance.Active)
                {
                    __result = true;
                    return false;
                }
                return true;
            }

        }

    }
}